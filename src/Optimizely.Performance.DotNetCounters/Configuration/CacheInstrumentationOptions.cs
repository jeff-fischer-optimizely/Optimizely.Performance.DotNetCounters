namespace Optimizely.Performance.DotNetCounters.Configuration
{
    /// <summary>
    /// Configuration options for cache dependency instrumentation.
    /// </summary>
    /// <remarks>
    /// Optimizely's content cache hangs entries off master keys, so removing one key can
    /// cascade into a very large number of further removals. The cascade is invisible from
    /// outside the cache: callers see a single <c>Remove</c>, while the cache quietly
    /// discards everything that depended on it. These options control the instrumentation
    /// that makes that amplification measurable.
    /// </remarks>
    public class CacheInstrumentationOptions
    {
        /// <summary>
        /// Configuration section name for appsettings.json
        /// </summary>
        public const string SectionName = "Optimizely:PerformanceCounters:CacheInstrumentation";

        /// <summary>
        /// Gets or sets whether cache instrumentation is enabled. When this is off, the cache
        /// is left entirely undecorated and the package adds no work to any cache operation.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of removals in a single cascade at or above which the
        /// cascade is also written to the log, including the key that triggered it.
        /// </summary>
        /// <remarks>
        /// Telemetry gives you the shape of the problem; the log gives you the culprit.
        /// Metrics cannot carry a cache key as a dimension - keys are unbounded, and using
        /// one as a dimension would create a new time series per key - so the only way to
        /// find out <em>which</em> key is collapsing the cache is to log it. The threshold
        /// keeps that to the handful of events actually worth reading.
        /// </remarks>
        public int LargeRemovalThreshold { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether cascades at or above <see cref="LargeRemovalThreshold"/> are
        /// written to the log. Turn this off to keep the telemetry but silence the log.
        /// </summary>
        public bool LogLargeRemovals { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of large-removal log entries to write per minute.
        /// A cache that is collapsing repeatedly would otherwise flood the log at exactly
        /// the moment the site can least afford it.
        /// </summary>
        public int LargeRemovalLogsPerMinute { get; set; } = 10;

        /// <summary>
        /// Gets or sets the fraction of cache entries that carry an eviction callback, as a
        /// value between 0 and 1, where 1 means every entry.
        /// </summary>
        /// <remarks>
        /// Eviction reasons (expired, capacity, dependency) are only observable through a
        /// per-entry callback. On V12/V13 we do not add one: we substitute the callback
        /// Optimizely already registers, so full coverage costs no retained memory and the
        /// reason codes are exact rather than sampled. Lower this only if the per-insert
        /// allocation shows up in a profile.
        /// </remarks>
        public double EvictionCallbackSampleRate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the number of consecutive instrumentation failures after which
        /// instrumentation disables itself for the remaining lifetime of the process.
        /// </summary>
        /// <remarks>
        /// Instrumentation must never be the reason a site's cache misbehaves. Every
        /// instrumentation path is wrapped so that a failure cannot escape into the cache
        /// operation, and if failures repeat, the instrumentation stops rather than
        /// continuing to burn CPU on an exception path.
        /// </remarks>
        public int FailureThreshold { get; set; } = 20;
    }
}
