using System.Collections.Generic;

namespace Optimizely.Performance.DotNetCounters.Configuration
{
    /// <summary>
    /// Provides default performance counter configurations for different .NET versions.
    /// </summary>
    public static class DefaultCounters
    {
#if NET472
        /// <summary>
        /// Gets the default Windows Performance Counters for .NET Framework.
        /// </summary>
        public static List<WindowsPerformanceCounter> GetDefaultWindowsCounters()
        {
            return new List<WindowsPerformanceCounter>
            {
                // ASP.NET Counters
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET\Requests Queued",
                    ReportedName = "ASP.NET Requests Queued"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Requests/Sec",
                    ReportedName = "ASP.NET Requests/Sec"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Request Wait Time",
                    ReportedName = "ASP.NET Request Wait Time"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Request Execution Time",
                    ReportedName = "ASP.NET Request Execution Time"
                },

                // .NET CLR LocksAndThreads
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\# of current logical Threads",
                    ReportedName = "CLR Logical Threads"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\# of current physical Threads",
                    ReportedName = "CLR Physical Threads"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\Contention Rate / sec",
                    ReportedName = "CLR Contention Rate"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\Current Queue Length",
                    ReportedName = "CLR Thread Queue Length"
                },

                // .NET CLR Memory
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR Memory(_Global_)\# Bytes in all Heaps",
                    ReportedName = "CLR Heap Size"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR Memory(_Global_)\% Time in GC",
                    ReportedName = "CLR % Time in GC"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR Memory(_Global_)\# Gen 0 Collections",
                    ReportedName = "CLR Gen 0 Collections"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR Memory(_Global_)\# Gen 1 Collections",
                    ReportedName = "CLR Gen 1 Collections"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR Memory(_Global_)\# Gen 2 Collections",
                    ReportedName = "CLR Gen 2 Collections"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\.NET CLR Memory(_Global_)\Large Object Heap size",
                    ReportedName = "CLR LOH Size"
                },

                // Process Counters
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\Process(??APP_WIN32_PROC??)\Thread Count",
                    ReportedName = "Process Thread Count"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\Process(??APP_WIN32_PROC??)\Handle Count",
                    ReportedName = "Process Handle Count"
                }
            };
        }
#endif

#if !NET472
        /// <summary>
        /// Gets the default Event Counters for .NET Core and later.
        /// </summary>
        public static List<EventCounterDefinition> GetDefaultEventCounters()
        {
            return new List<EventCounterDefinition>
            {
                // System.Runtime counters
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "cpu-usage"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "working-set"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "gc-heap-size"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "gen-0-gc-count"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "gen-1-gc-count"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "gen-2-gc-count"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "time-in-gc"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "gen-0-size"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "gen-1-size"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "gen-2-size"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "loh-size"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "alloc-rate"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "assembly-count"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "exception-count"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "threadpool-thread-count"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "monitor-lock-contention-count"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "threadpool-queue-length"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "threadpool-completed-items-count"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "System.Runtime",
                    CounterName = "active-timer-count"
                },

                // Microsoft.AspNetCore.Hosting counters
                new EventCounterDefinition
                {
                    EventSourceName = "Microsoft.AspNetCore.Hosting",
                    CounterName = "requests-per-second"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "Microsoft.AspNetCore.Hosting",
                    CounterName = "total-requests"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "Microsoft.AspNetCore.Hosting",
                    CounterName = "current-requests"
                },
                new EventCounterDefinition
                {
                    EventSourceName = "Microsoft.AspNetCore.Hosting",
                    CounterName = "failed-requests"
                }
            };
        }
#endif
    }
}
