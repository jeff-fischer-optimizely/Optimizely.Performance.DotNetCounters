#if !NET472
using System;

namespace Optimizely.Performance.DotNetCounters.Configuration
{
    /// <summary>
    /// Options for the cache lock contention probe.
    /// </summary>
    public class CacheLockProbeOptions
    {
        /// <summary>
        /// Configuration section this binds to.
        /// </summary>
        public const string SectionName = "Optimizely:PerformanceCounters:CacheLockProbe";

        /// <summary>
        /// Gets or sets whether the probe runs at all.
        /// </summary>
        /// <remarks>
        /// This is the switch that turns off the only reflection over Optimizely internals in
        /// the package. A site that would rather not have it can set this to false and lose
        /// nothing else.
        /// </remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the seconds between samples.
        /// </summary>
        /// <remarks>
        /// A sample is four property reads, so this can be frequent. Lock queues form and clear
        /// in well under a second, and a sparse sample will miss them entirely - the point of
        /// the metric is the distribution of a spiky quantity, which needs enough samples per
        /// aggregation interval to have a meaningful maximum.
        /// </remarks>
        public int SampleIntervalSeconds { get; set; } = 2;

        /// <summary>
        /// Gets or sets the total queue depth above which a sample is logged.
        /// </summary>
        /// <remarks>
        /// Set to zero to disable logging and keep only the metrics.
        /// </remarks>
        public int QueueDepthThreshold { get; set; } = 10;

        /// <summary>
        /// Gets or sets the cap on probe log entries per minute.
        /// </summary>
        public int LogsPerMinute { get; set; } = 4;

        internal TimeSpan SampleInterval =>
            TimeSpan.FromSeconds(Math.Max(1, SampleIntervalSeconds));
    }
}
#endif
