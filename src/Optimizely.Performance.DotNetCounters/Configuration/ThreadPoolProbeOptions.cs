using System;

namespace Optimizely.Performance.DotNetCounters.Configuration
{
    /// <summary>
    /// Options for the thread pool queue delay probe.
    /// </summary>
    public class ThreadPoolProbeOptions
    {
        /// <summary>
        /// Configuration section this binds to.
        /// </summary>
        public const string SectionName = "Optimizely:PerformanceCounters:ThreadPoolProbe";

        /// <summary>
        /// Gets or sets whether the probe runs at all.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the seconds between samples.
        /// </summary>
        /// <remarks>
        /// Twelve samples a minute is enough for Application Insights to produce a meaningful
        /// max and standard deviation over its aggregation interval without the probe itself
        /// becoming load. Each sample is one queued work item.
        /// </remarks>
        public int SampleIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets how long to wait for a sample before giving up on it.
        /// </summary>
        /// <remarks>
        /// A sample that does not complete within this window is recorded at the timeout value
        /// and counted as a starvation sample. The recorded number is therefore a floor, not a
        /// measurement: the true delay was at least this long and may have been far longer.
        /// </remarks>
        public int SampleTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Gets or sets the delay, in milliseconds, above which a sample is logged.
        /// </summary>
        /// <remarks>
        /// The default is deliberately well clear of normal scheduling jitter. A healthy pool
        /// services a queued item in well under a millisecond; tens of milliseconds means
        /// requests are already waiting on threads before any of their own work begins.
        /// </remarks>
        public int SlowSampleThresholdMilliseconds { get; set; } = 100;

        /// <summary>
        /// Gets or sets the cap on probe log entries per minute.
        /// </summary>
        public int LogsPerMinute { get; set; } = 4;

        internal TimeSpan SampleInterval =>
            TimeSpan.FromSeconds(Math.Max(1, SampleIntervalSeconds));

        internal TimeSpan SampleTimeout =>
            TimeSpan.FromSeconds(Math.Max(1, SampleTimeoutSeconds));
    }
}
