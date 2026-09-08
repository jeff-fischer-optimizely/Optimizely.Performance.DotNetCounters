using System;
using System.Threading;
using EPiServer.Logging;
using Microsoft.ApplicationInsights;
using Optimizely.Performance.DotNetCounters.Configuration;

namespace Optimizely.Performance.DotNetCounters.Cache
{
    /// <summary>
    /// Turns cache activity into Application Insights metrics and, for the rare cascades big
    /// enough to matter, log entries naming the key responsible.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here may ever surface a fault into the cache operation that called it. Every
    /// public method swallows its own exceptions, and if they keep happening the recorder
    /// switches itself off for the rest of the process rather than burning CPU on an
    /// exception path during what is probably already an incident.
    /// </para>
    /// <para>
    /// Cache keys are never used as metric dimensions. Keys are unbounded, so one dimension
    /// value per key would mean one time series per key - expensive, and throttled by
    /// Application Insights long before it became useful. Keys appear in logs only.
    /// </para>
    /// </remarks>
    public sealed class CacheInstrumentationRecorder
    {
        /// <summary>
        /// Entries removed by a single removal, including the entry that was asked for.
        /// </summary>
        /// <remarks>
        /// One metric answers the whole question, because Application Insights pre-aggregates
        /// count, sum, min, max and standard deviation per interval:
        /// <list type="bullet">
        /// <item><description><c>count</c> - removals requested by callers</description></item>
        /// <item><description><c>sum</c> - entries actually removed</description></item>
        /// <item><description><c>avg</c> - the amplification ratio</description></item>
        /// <item><description><c>max</c> - the worst single cascade in the interval</description></item>
        /// </list>
        /// </remarks>
        public const string RemovalFanOutMetric = "Optimizely Cache Removal Fan-Out";

        /// <summary>
        /// Wall-clock milliseconds a removal took.
        /// </summary>
        /// <remarks>
        /// Worth watching in its own right: removal runs under a process-wide write lock, so
        /// this is time during which no thread can read the cache at all.
        /// </remarks>
        public const string RemovalDurationMetric = "Optimizely Cache Removal Duration";

        /// <summary>
        /// Entries evicted, split by the reason the cache gave.
        /// </summary>
        public const string EvictionMetric = "Optimizely Cache Evictions";

        /// <summary>
        /// Time-to-live, in seconds, requested for an entry as it was inserted.
        /// </summary>
        /// <remarks>
        /// Recorded at insert rather than inferred from evictions, so it describes what the
        /// code asked for. A low average here is how you find the implementations that are
        /// churning the cache faster than it can pay for itself.
        /// </remarks>
        public const string InsertTtlMetric = "Optimizely Cache Insert TTL Seconds";

        /// <summary>
        /// Cache compactions triggered by memory pressure.
        /// </summary>
        public const string CompactionMetric = "Optimizely Cache Compactions";

        private const string OperationDimension = "Operation";
        private const string ReasonDimension = "Reason";

        private readonly TelemetryClient? _telemetryClient;
        private readonly CacheInstrumentationOptions _options;

        private int _consecutiveFailures;
        private int _disabled;

        private long _logWindowStartTicks;
        private int _logsInWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheInstrumentationRecorder"/> class.
        /// </summary>
        /// <param name="telemetryClient">
        /// The Application Insights client. May be null, in which case metrics are dropped and
        /// only threshold logging remains active.
        /// </param>
        /// <param name="options">Cache instrumentation options.</param>
        public CacheInstrumentationRecorder(TelemetryClient? telemetryClient, CacheInstrumentationOptions options)
        {
            _telemetryClient = telemetryClient;
            _options = options ?? new CacheInstrumentationOptions();
            _logWindowStartTicks = DateTime.UtcNow.Ticks;
        }

        // Resolved per call rather than cached: on V12/V13 this type is constructed during
        // container configuration, before the LogManager factory has been wired up.
        private static ILogger Log => LogManager.GetLogger(typeof(CacheInstrumentationRecorder));

        /// <summary>
        /// Gets whether the recorder has switched itself off after repeated failures.
        /// </summary>
        public bool IsDisabled => Volatile.Read(ref _disabled) != 0;

        /// <summary>
        /// Records a completed removal and its cascade.
        /// </summary>
        /// <param name="operation">The removal that was requested, for example <c>Remove</c>.</param>
        /// <param name="key">The key the caller asked to remove.</param>
        /// <param name="entriesRemoved">Entries removed in total, including <paramref name="key"/>.</param>
        /// <param name="elapsedMilliseconds">How long the removal took.</param>
        public void RecordRemoval(string operation, string key, int entriesRemoved, double elapsedMilliseconds)
        {
            if (IsDisabled)
            {
                return;
            }

            try
            {
                _telemetryClient?.GetMetric(RemovalFanOutMetric, OperationDimension)
                    .TrackValue(entriesRemoved, operation);
                _telemetryClient?.GetMetric(RemovalDurationMetric, OperationDimension)
                    .TrackValue(elapsedMilliseconds, operation);

                if (_options.LogLargeRemovals
                    && _options.LargeRemovalThreshold > 0
                    && entriesRemoved >= _options.LargeRemovalThreshold
                    && TryTakeLogSlot())
                {
                    Log.Warning(
                        $"Cache removal of '{key}' via {operation} cascaded to {entriesRemoved} entries " +
                        $"in {elapsedMilliseconds:F1} ms. The cache was held under its write lock for that " +
                        "time, so no thread could read from it. This key sits above a large dependency " +
                        "subtree; consider narrowing what depends on it.");
                }

                Succeeded();
            }
            catch (Exception ex)
            {
                Failed(ex);
            }
        }

        /// <summary>
        /// Records that an entry was evicted for the given reason.
        /// </summary>
        /// <param name="reason">
        /// A bounded, low-cardinality reason such as <c>Expired</c> or <c>Capacity</c>.
        /// </param>
        public void RecordEviction(string reason)
        {
            if (IsDisabled)
            {
                return;
            }

            try
            {
                _telemetryClient?.GetMetric(EvictionMetric, ReasonDimension).TrackValue(1, reason);
                Succeeded();
            }
            catch (Exception ex)
            {
                Failed(ex);
            }
        }

        /// <summary>
        /// Records the time-to-live requested for an entry being inserted.
        /// </summary>
        /// <param name="timeToLive">The requested lifetime. Ignored when not positive.</param>
        public void RecordInsertTimeToLive(TimeSpan timeToLive)
        {
            if (IsDisabled || timeToLive <= TimeSpan.Zero)
            {
                return;
            }

            try
            {
                _telemetryClient?.GetMetric(InsertTtlMetric).TrackValue(timeToLive.TotalSeconds);
                Succeeded();
            }
            catch (Exception ex)
            {
                Failed(ex);
            }
        }

        /// <summary>
        /// Records that the cache was compacted to relieve memory pressure.
        /// </summary>
        /// <param name="percentageRequested">
        /// The share of the cache the platform asked to drop, where 0.1 means ten percent.
        /// </param>
        public void RecordCompaction(double percentageRequested)
        {
            if (IsDisabled)
            {
                return;
            }

            try
            {
                _telemetryClient?.GetMetric(CompactionMetric).TrackValue(1);

                if (TryTakeLogSlot())
                {
                    Log.Warning(
                        $"Cache compaction triggered by memory pressure, dropping {percentageRequested:P0} " +
                        "of cached entries. Entries discarded this way take their dependents with them, so " +
                        "the miss storm that follows is larger than the share dropped. Repeated compactions " +
                        "mean the site is sized below its working set.");
                }

                Succeeded();
            }
            catch (Exception ex)
            {
                Failed(ex);
            }
        }

        private void Succeeded()
        {
            if (Volatile.Read(ref _consecutiveFailures) != 0)
            {
                Interlocked.Exchange(ref _consecutiveFailures, 0);
            }
        }

        private void Failed(Exception ex)
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            if (failures < _options.FailureThreshold)
            {
                return;
            }

            if (Interlocked.Exchange(ref _disabled, 1) == 0)
            {
                try
                {
                    Log.Error(
                        $"Cache instrumentation failed {failures} times in a row and has been disabled for " +
                        "the lifetime of this process. Cache behaviour is unaffected; only the cache metrics " +
                        "stop. Restart the application to re-enable it.",
                        ex);
                }
                catch
                {
                    // Logging failed while shutting down for repeated failures. There is
                    // nowhere left to report this, and it must not reach the cache.
                }
            }
        }

        /// <summary>
        /// Applies the per-minute cap on threshold logging, so that a cache collapsing over and
        /// over cannot flood the log at the moment the site can least afford it.
        /// </summary>
        private bool TryTakeLogSlot()
        {
            if (_options.LargeRemovalLogsPerMinute <= 0)
            {
                return false;
            }

            var now = DateTime.UtcNow.Ticks;
            var windowStart = Interlocked.Read(ref _logWindowStartTicks);

            if (now - windowStart >= TimeSpan.TicksPerMinute)
            {
                // Only the thread that wins the exchange opens the new window; the others fall
                // through and compete for a slot inside it.
                if (Interlocked.CompareExchange(ref _logWindowStartTicks, now, windowStart) == windowStart)
                {
                    Interlocked.Exchange(ref _logsInWindow, 0);
                }
            }

            return Interlocked.Increment(ref _logsInWindow) <= _options.LargeRemovalLogsPerMinute;
        }
    }
}
