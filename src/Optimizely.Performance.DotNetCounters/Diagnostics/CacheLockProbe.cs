#if !NET472
using System;
using System.Threading;
using EPiServer.Logging;
using Microsoft.ApplicationInsights;
using Optimizely.Performance.DotNetCounters.Configuration;

namespace Optimizely.Performance.DotNetCounters.Diagnostics
{
    /// <summary>
    /// Samples how many threads are queued on Optimizely's cache lock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optimizely's memory cache serialises every write behind one process-wide
    /// <see cref="ReaderWriterLockSlim"/>. Cache removal duration already says how long a write
    /// holds it; this says how many threads were waiting behind it while it did. The two
    /// together separate the case that looks alarming but is not - a slow removal nobody was
    /// waiting on - from the case that is - a fast removal with fifty threads queued behind it,
    /// which is a stalled site.
    /// </para>
    /// <para>
    /// The general contention counters cannot report this. They count monitor contention across
    /// the process, and this is a reader/writer lock, which they do not see at all.
    /// </para>
    /// <para>
    /// This is the one part of the package that depends on Optimizely internals, so it is built
    /// to lose that dependency without consequence: if the lock cannot be found the probe logs
    /// once, reports itself unavailable and stops. Everything else keeps working, and the cache
    /// is never touched - the probe only ever reads counters off the lock, and never acquires
    /// it.
    /// </para>
    /// </remarks>
    public sealed class CacheLockProbe : IDisposable
    {
        /// <summary>
        /// Threads waiting to take the cache lock for writing.
        /// </summary>
        /// <remarks>
        /// The number that matters. Writers block every reader, so a queue here is the whole
        /// site waiting on cache invalidation.
        /// </remarks>
        public const string WaitingWritersMetric = "Optimizely Cache Lock Waiting Writers";

        /// <summary>
        /// Threads waiting to take the cache lock for reading.
        /// </summary>
        /// <remarks>
        /// Readers only wait when a writer holds the lock or is queued ahead of them, so this
        /// counts threads stalled by an in-progress invalidation.
        /// </remarks>
        public const string WaitingReadersMetric = "Optimizely Cache Lock Waiting Readers";

        /// <summary>
        /// Threads currently holding the cache lock for reading.
        /// </summary>
        public const string CurrentReadersMetric = "Optimizely Cache Lock Current Readers";

        /// <summary>
        /// One when the write lock was held at the moment of sampling, zero otherwise.
        /// </summary>
        /// <remarks>
        /// Averaged over an interval this approximates the share of time the cache was closed
        /// to readers - a duty cycle rather than an event count, which is what makes it
        /// comparable across sites of different sizes.
        /// </remarks>
        public const string WriteLockHeldMetric = "Optimizely Cache Lock Write Held";

        private readonly TelemetryClient? _telemetryClient;
        private readonly CacheLockProbeOptions _options;

        // Deliberately never disposed. The sampler waits on this from its own thread, so any
        // disposal would race that wait, and touching a disposed wait handle throws - on a
        // thread created by hand, where an unhandled exception terminates the process. One
        // event held for the life of a single process-wide probe is the cheaper trade.
        private readonly ManualResetEventSlim _stop = new ManualResetEventSlim(false);

        private ReaderWriterLockSlim? _cacheLock;
        private Thread? _thread;
        private int _started;
        private int _disposed;

        private long _logWindowStartTicks;
        private int _logsInWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheLockProbe"/> class.
        /// </summary>
        /// <param name="telemetryClient">
        /// The Application Insights client. May be null, in which case only threshold logging
        /// remains active.
        /// </param>
        /// <param name="options">Probe options.</param>
        public CacheLockProbe(TelemetryClient? telemetryClient, CacheLockProbeOptions options)
        {
            _telemetryClient = telemetryClient;
            _options = options ?? new CacheLockProbeOptions();
            _logWindowStartTicks = DateTime.UtcNow.Ticks;
        }

        // Resolved per call rather than cached: on V12/V13 this type may be constructed during
        // container configuration, before the LogManager factory has been wired up.
        private static ILogger Log => LogManager.GetLogger(typeof(CacheLockProbe));

        /// <summary>
        /// Gets whether the cache lock was found and is being sampled.
        /// </summary>
        public bool IsAvailable => Volatile.Read(ref _cacheLock) != null;

        /// <summary>
        /// Resolves the cache lock and starts sampling. Does nothing if disabled, already
        /// started, or disposed, and nothing beyond a single log entry if the lock cannot be
        /// found.
        /// </summary>
        public void Start()
        {
            if (!_options.Enabled
                || Volatile.Read(ref _disposed) != 0
                || Interlocked.Exchange(ref _started, 1) != 0)
            {
                return;
            }

            string diagnostic;
            bool located;

            try
            {
                located = CacheLockLocator.TryGetLock(
                    CacheLockLocator.FindCacheType(), out var cacheLock, out diagnostic);

                _cacheLock = cacheLock;
            }
            catch (Exception ex)
            {
                // TryGetLock is written not to throw, but it is reflection over a type this
                // package does not own, and being wrong about that must not cost the site its
                // startup.
                located = false;
                diagnostic = $"Locating the Optimizely cache lock threw: {ex.Message}";
            }

            if (!located)
            {
                Log.Information(
                    "Cache lock contention will not be reported. " + diagnostic +
                    " This metric reads an Optimizely internal that is not part of any public API, " +
                    "so it is expected to lapse across upgrades. No other counter is affected and " +
                    "cache behaviour is unchanged.");
                return;
            }

            Log.Information(diagnostic);

            _thread = new Thread(Loop)
            {
                // Background so it can never hold up process shutdown, and below normal so that
                // on a saturated machine the probe yields to the work it is measuring.
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "Optimizely Cache Lock Probe"
            };

            _thread.Start();
        }

        /// <summary>
        /// Stops sampling.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _stop.Set();
        }

        /// <remarks>
        /// The whole body is guarded. This runs on a thread created here rather than a pool
        /// thread, so anything escaping it is an unhandled exception, which on .NET Core takes
        /// the process with it. A performance counter has no business being able to do that.
        /// </remarks>
        private void Loop()
        {
            try
            {
                while (!_stop.IsSet)
                {
                    Sample();

                    if (_stop.Wait(_options.SampleInterval))
                    {
                        return;
                    }
                }
            }
            catch
            {
                // Nothing left to do but stop quietly.
            }
        }

        private void Sample()
        {
            var cacheLock = Volatile.Read(ref _cacheLock);
            if (cacheLock == null)
            {
                return;
            }

            try
            {
                // Plain property reads on a framework type. No reflection runs here, and the
                // lock is never acquired, so sampling cannot itself add contention.
                var waitingWriters = cacheLock.WaitingWriteCount;
                var waitingReaders = cacheLock.WaitingReadCount;
                var currentReaders = cacheLock.CurrentReadCount;
                var writeHeld = cacheLock.IsWriteLockHeld;

                _telemetryClient?.GetMetric(WaitingWritersMetric).TrackValue(waitingWriters);
                _telemetryClient?.GetMetric(WaitingReadersMetric).TrackValue(waitingReaders);
                _telemetryClient?.GetMetric(CurrentReadersMetric).TrackValue(currentReaders);
                _telemetryClient?.GetMetric(WriteLockHeldMetric).TrackValue(writeHeld ? 1 : 0);

                if (_options.QueueDepthThreshold > 0
                    && waitingReaders + waitingWriters >= _options.QueueDepthThreshold)
                {
                    TryLog(() => Log.Warning(
                        $"{waitingReaders + waitingWriters} threads are queued on the Optimizely cache " +
                        $"lock ({waitingWriters} waiting to write, {waitingReaders} waiting to read). " +
                        "Writes to the cache are exclusive, so while this queue exists no thread can " +
                        "read from the cache at all. Look for a dependency cascade in the cache removal " +
                        "metrics over the same interval."));
                }
            }
            catch (Exception ex)
            {
                // Stop rather than keep failing: the lock reference is resolved once, so a fault
                // reading it will recur on every sample until the process restarts.
                Volatile.Write(ref _cacheLock, null);

                TryLog(() => Log.Warning(
                    "Sampling the Optimizely cache lock failed; the metric has been stopped for the " +
                    "lifetime of this process. Cache behaviour is unaffected.",
                    ex));
            }
        }

        /// <summary>
        /// Applies the per-minute log cap, so sustained contention cannot flood the log at the
        /// moment the site can least afford it.
        /// </summary>
        private void TryLog(Action write)
        {
            if (_options.LogsPerMinute <= 0)
            {
                return;
            }

            var now = DateTime.UtcNow.Ticks;
            var windowStart = Interlocked.Read(ref _logWindowStartTicks);

            if (now - windowStart >= TimeSpan.TicksPerMinute
                && Interlocked.CompareExchange(ref _logWindowStartTicks, now, windowStart) == windowStart)
            {
                Interlocked.Exchange(ref _logsInWindow, 0);
            }

            if (Interlocked.Increment(ref _logsInWindow) > _options.LogsPerMinute)
            {
                return;
            }

            try
            {
                write();
            }
            catch
            {
                // Logging is the last thing standing; if it fails there is nowhere to say so.
            }
        }
    }
}
#endif
