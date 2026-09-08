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

#if NET472
        /// <summary>
        /// Applies any settings present in <c>appSettings</c>, keyed by
        /// <see cref="SectionName"/> plus the property name.
        /// </summary>
        /// <remarks>
        /// V11 sites rarely have an <c>IConfiguration</c> registered, and without one there is
        /// no way to turn the probe off - which is not an acceptable state for the only
        /// component here that runs a thread of its own. <c>appSettings</c> is the mechanism a
        /// .NET Framework site already has, and using the same key path as the
        /// <c>appsettings.json</c> section keeps one documented name across all three versions.
        /// A malformed value is ignored rather than thrown, on the same reasoning as everywhere
        /// else in this package: a typo in a monitoring setting must not stop a site starting.
        /// </remarks>
        /// <param name="options">The options to populate.</param>
        public static void BindAppSettings(ThreadPoolProbeOptions options)
        {
            if (options == null)
            {
                return;
            }

            options.Enabled = ReadBoolean(nameof(Enabled), options.Enabled);
            options.SampleIntervalSeconds =
                ReadInt32(nameof(SampleIntervalSeconds), options.SampleIntervalSeconds);
            options.SampleTimeoutSeconds =
                ReadInt32(nameof(SampleTimeoutSeconds), options.SampleTimeoutSeconds);
            options.SlowSampleThresholdMilliseconds =
                ReadInt32(nameof(SlowSampleThresholdMilliseconds), options.SlowSampleThresholdMilliseconds);
            options.LogsPerMinute = ReadInt32(nameof(LogsPerMinute), options.LogsPerMinute);
        }

        private static string? ReadSetting(string name)
        {
            try
            {
                return System.Configuration.ConfigurationManager.AppSettings[SectionName + ":" + name];
            }
            catch
            {
                // A configuration section this package does not own can be malformed, and
                // reading it throws rather than returning null.
                return null;
            }
        }

        private static bool ReadBoolean(string name, bool fallback) =>
            bool.TryParse(ReadSetting(name), out var value) ? value : fallback;

        private static int ReadInt32(string name, int fallback) =>
            int.TryParse(ReadSetting(name), out var value) ? value : fallback;
#endif
    }
}
