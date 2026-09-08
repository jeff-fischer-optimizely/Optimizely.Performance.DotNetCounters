using System;
using System.Diagnostics;
using System.Threading;
using EPiServer.Logging;
using Microsoft.ApplicationInsights;
using Optimizely.Performance.DotNetCounters.Configuration;

namespace Optimizely.Performance.DotNetCounters.Diagnostics
{
    /// <summary>
    /// Measures how long the thread pool takes to start a queued work item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Queue length counters say how many items are waiting; they do not say how long anything
    /// waits, and the two are not interchangeable. A queue of ten items is harmless if the pool
    /// drains it instantly and fatal if the pool is injecting one thread per second. This probe
    /// measures the quantity that actually matters to a request: the delay between handing work
    /// to the pool and the pool starting it. Every asynchronous continuation in the site pays
    /// that delay, which is why thread pool starvation presents as uniform slowness across
    /// endpoints that have nothing else in common.
    /// </para>
    /// <para>
    /// The sampler runs on a dedicated thread rather than a timer. Timer callbacks are
    /// themselves dispatched on the thread pool, so a timer-driven probe is delayed by the very
    /// condition it exists to detect and reports a number biased towards health exactly when
    /// the site is least healthy. One thread is a small price for a measurement that stays
    /// truthful under starvation.
    /// </para>
    /// </remarks>
    public sealed class ThreadPoolQueueDelayProbe : IDisposable
    {
        /// <summary>
        /// Milliseconds between queueing a work item and the pool starting it.
        /// </summary>
        public const string QueueDelayMetric = "Optimizely ThreadPool Queue Delay Ms";

        /// <summary>
        /// Worker threads currently in use, out of the configured maximum.
        /// </summary>
        /// <remarks>
        /// Read on the same tick as the delay so the two can be compared directly. Rising delay
        /// with busy workers well below the maximum means the pool is throttling thread
        /// injection rather than running out of headroom - usually blocking calls occupying
        /// threads faster than the injection rate can replace them.
        /// </remarks>
        public const string BusyWorkerThreadsMetric = "Optimizely ThreadPool Busy Worker Threads";

        /// <summary>
        /// Completion port threads currently in use.
        /// </summary>
        public const string BusyCompletionPortThreadsMetric = "Optimizely ThreadPool Busy IO Threads";

        /// <summary>
        /// Samples that did not complete within the timeout.
        /// </summary>
        public const string StarvationSampleMetric = "Optimizely ThreadPool Starvation Samples";

        private readonly TelemetryClient? _telemetryClient;
        private readonly ThreadPoolProbeOptions _options;

        // Deliberately never disposed. The sampler waits on this from its own thread, so any
        // disposal would race that wait, and touching a disposed wait handle throws - on a
        // thread created by hand, where an unhandled exception terminates the process. One
        // event held for the life of a single process-wide probe is the cheaper trade.
        private readonly ManualResetEventSlim _stop = new ManualResetEventSlim(false);

        private Thread? _thread;
        private int _started;
        private int _disposed;

        private long _logWindowStartTicks;
        private int _logsInWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadPoolQueueDelayProbe"/> class.
        /// </summary>
        /// <param name="telemetryClient">
        /// The Application Insights client. May be null, in which case only threshold logging
        /// remains active.
        /// </param>
        /// <param name="options">Probe options.</param>
        public ThreadPoolQueueDelayProbe(TelemetryClient? telemetryClient, ThreadPoolProbeOptions options)
        {
            _telemetryClient = telemetryClient;
            _options = options ?? new ThreadPoolProbeOptions();
            _logWindowStartTicks = DateTime.UtcNow.Ticks;
        }

        // Resolved per call rather than cached: on V12/V13 this type may be constructed during
        // container configuration, before the LogManager factory has been wired up.
        private static ILogger Log => LogManager.GetLogger(typeof(ThreadPoolQueueDelayProbe));

        /// <summary>
        /// Starts sampling. Does nothing if disabled, already started, or disposed.
        /// </summary>
        public void Start()
        {
            if (!_options.Enabled
                || Volatile.Read(ref _disposed) != 0
                || Interlocked.Exchange(ref _started, 1) != 0)
            {
                return;
            }

            _thread = new Thread(Loop)
            {
                // Background so it can never hold up process shutdown, and below normal so
                // that on a saturated machine the probe yields to the work it is measuring.
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "Optimizely ThreadPool Probe"
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
                    try
                    {
                        Sample();
                    }
                    catch (Exception ex)
                    {
                        // Never let a sampling fault take the thread down: a probe that dies at
                        // the first hiccup leaves a silent gap that looks identical to a
                        // healthy pool.
                        TryLog(() => Log.Warning("Thread pool probe sample failed.", ex));
                    }

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
            var signal = new ManualResetEventSlim(false);
            var enqueued = Stopwatch.GetTimestamp();

            // Unsafe because the probe has no interest in the ambient execution context, and
            // capturing it would add allocation and flow cost to the thing being measured.
            ThreadPool.UnsafeQueueUserWorkItem(SignalCallback, signal);

            var completed = signal.Wait(_options.SampleTimeout);
            var delayMilliseconds = (Stopwatch.GetTimestamp() - enqueued) * 1000.0 / Stopwatch.Frequency;

            if (completed)
            {
                // Only safe to dispose once the callback has certainly run. On timeout the
                // work item is still pending and disposal would race it, so those events are
                // left for the garbage collector instead.
                signal.Dispose();
            }

            Record(delayMilliseconds, completed);
        }

        // Static so no closure is allocated per sample. The catch matters: on timeout the
        // probe abandons the event, and an ObjectDisposedException escaping here would be
        // unhandled on a pool thread, which terminates the process on .NET Core.
        private static readonly WaitCallback SignalCallback = state =>
        {
            try
            {
                ((ManualResetEventSlim)state!).Set();
            }
            catch
            {
                // The sampler already gave up on this one.
            }
        };

        private void Record(double delayMilliseconds, bool completed)
        {
            try
            {
                _telemetryClient?.GetMetric(QueueDelayMetric).TrackValue(delayMilliseconds);

                ThreadPool.GetMaxThreads(out var maxWorkers, out var maxCompletionPort);
                ThreadPool.GetAvailableThreads(out var freeWorkers, out var freeCompletionPort);

                _telemetryClient?.GetMetric(BusyWorkerThreadsMetric).TrackValue(maxWorkers - freeWorkers);
                _telemetryClient?.GetMetric(BusyCompletionPortThreadsMetric)
                    .TrackValue(maxCompletionPort - freeCompletionPort);

                if (!completed)
                {
                    _telemetryClient?.GetMetric(StarvationSampleMetric).TrackValue(1);

                    TryLog(() => Log.Error(
                        $"The thread pool did not start a queued work item within " +
                        $"{_options.SampleTimeout.TotalSeconds:F0} s. {maxWorkers - freeWorkers} of " +
                        $"{maxWorkers} worker threads are in use. Every asynchronous continuation in " +
                        "the site is waiting at least this long, so requests will be timing out for " +
                        "reasons that have nothing to do with the work they are doing. The usual cause " +
                        "is blocking on asynchronous calls, which occupies threads faster than the pool " +
                        "injects replacements."));

                    return;
                }

                if (delayMilliseconds >= _options.SlowSampleThresholdMilliseconds)
                {
                    TryLog(() => Log.Warning(
                        $"Thread pool queue delay was {delayMilliseconds:F0} ms, with " +
                        $"{maxWorkers - freeWorkers} of {maxWorkers} worker threads in use. Work queued " +
                        "to the pool is waiting this long before it starts, which is added to every " +
                        "request regardless of what the request itself does."));
                }
            }
            catch (Exception ex)
            {
                TryLog(() => Log.Warning("Thread pool probe failed to record a sample.", ex));
            }
        }

        /// <summary>
        /// Applies the per-minute log cap, so sustained starvation cannot flood the log at the
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
