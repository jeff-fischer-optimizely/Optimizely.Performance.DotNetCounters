using System.Collections.Generic;

namespace Optimizely.Performance.DotNetCounters.Configuration
{
    /// <summary>
    /// Configuration options for performance counters collection.
    /// </summary>
    public class PerformanceCounterOptions
    {
        /// <summary>
        /// Configuration section name for appsettings.json
        /// </summary>
        public const string SectionName = "Optimizely:PerformanceCounters";

        /// <summary>
        /// Gets or sets whether performance counter collection is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

#if NET472
        /// <summary>
        /// Gets or sets the Windows Performance Counters to collect (.NET Framework only).
        /// </summary>
        public List<WindowsPerformanceCounter> WindowsCounters { get; set; } = new List<WindowsPerformanceCounter>();

        /// <summary>
        /// Gets or sets whether to collect performance counters under IIS Express.
        /// Application Insights disables collection there by default, so a developer
        /// running the site locally would otherwise see no counters at all. Leave this
        /// off in production, where the site runs under full IIS.
        /// </summary>
        public bool EnableIISExpressPerformanceCounters { get; set; }
#endif

#if !NET472
        /// <summary>
        /// Gets or sets the Event Counters to collect (.NET Core+ only).
        /// </summary>
        public List<EventCounterDefinition> EventCounters { get; set; } = new List<EventCounterDefinition>();
#endif
    }

#if NET472
    /// <summary>
    /// Represents a Windows Performance Counter definition.
    /// </summary>
    public class WindowsPerformanceCounter
    {
        /// <summary>
        /// Gets or sets the performance counter category and name (e.g., "\ASP.NET\Requests Queued").
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reported metric name in Application Insights.
        /// </summary>
        public string ReportedName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the instance name (optional, use "_Global_" for CLR counters).
        /// </summary>
        public string? InstanceName { get; set; }
    }
#endif

#if !NET472
    /// <summary>
    /// Represents an Event Counter definition for .NET Core+.
    /// </summary>
    public class EventCounterDefinition
    {
        /// <summary>
        /// Gets or sets the event source name (e.g., "System.Runtime").
        /// </summary>
        public string EventSourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the counter name (e.g., "threadpool-queue-length").
        /// </summary>
        public string CounterName { get; set; } = string.Empty;
    }
#endif
}
