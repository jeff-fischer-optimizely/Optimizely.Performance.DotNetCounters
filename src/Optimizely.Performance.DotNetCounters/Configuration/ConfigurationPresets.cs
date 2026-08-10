using System.Collections.Generic;

namespace Optimizely.Performance.DotNetCounters.Configuration
{
    /// <summary>
    /// Provides recommended configuration presets for different scenarios and Optimizely CMS versions.
    /// </summary>
    public static class ConfigurationPresets
    {
#if NET472
        /// <summary>
        /// Gets the recommended preset for Optimizely V11 Production environments.
        /// Balanced set of counters for monitoring production performance.
        /// </summary>
        public static List<WindowsPerformanceCounter> GetOptimizelyV11ProductionPreset()
        {
            return new List<WindowsPerformanceCounter>
            {
                // Critical ASP.NET Metrics
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET\Requests Queued", ReportedName = "ASP.NET Requests Queued" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Requests/Sec", ReportedName = "ASP.NET Requests/Sec" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Request Execution Time", ReportedName = "ASP.NET Request Execution Time" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Requests Failed", ReportedName = "ASP.NET Requests Failed" },

                // Memory Health
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\# Bytes in all Heaps", ReportedName = "CLR Heap Size" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\% Time in GC", ReportedName = "CLR % Time in GC" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\# Gen 2 Collections", ReportedName = "CLR Gen 2 Collections" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\Large Object Heap size", ReportedName = "CLR LOH Size" },

                // Threading & Contention
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\Contention Rate / sec", ReportedName = "CLR Contention Rate" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\# of current logical Threads", ReportedName = "CLR Logical Threads" },

                // Process Health
                new WindowsPerformanceCounter { CategoryName = @"\Process(??APP_WIN32_PROC??)\% Processor Time", ReportedName = "Process CPU %" },
                new WindowsPerformanceCounter { CategoryName = @"\Process(??APP_WIN32_PROC??)\Private Bytes", ReportedName = "Process Private Bytes" }
            };
        }

        /// <summary>
        /// Gets the minimal preset for Optimizely V11 Development environments.
        /// Lightweight set for local development.
        /// </summary>
        public static List<WindowsPerformanceCounter> GetOptimizelyV11DevelopmentPreset()
        {
            return new List<WindowsPerformanceCounter>
            {
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET\Requests Queued", ReportedName = "ASP.NET Requests Queued" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\# Bytes in all Heaps", ReportedName = "CLR Heap Size" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\% Time in GC", ReportedName = "CLR % Time in GC" }
            };
        }

        /// <summary>
        /// Gets the comprehensive preset for Optimizely V11 troubleshooting.
        /// Extended set for diagnosing performance issues.
        /// </summary>
        public static List<WindowsPerformanceCounter> GetOptimizelyV11TroubleshootingPreset()
        {
            return new List<WindowsPerformanceCounter>
            {
                // All ASP.NET Counters
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET\Requests Queued", ReportedName = "ASP.NET Requests Queued" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET\Requests Rejected", ReportedName = "ASP.NET Requests Rejected" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Requests/Sec", ReportedName = "ASP.NET Requests/Sec" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Request Wait Time", ReportedName = "ASP.NET Request Wait Time" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Request Execution Time", ReportedName = "ASP.NET Request Execution Time" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Requests Failed", ReportedName = "ASP.NET Requests Failed" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Cache Total Entries", ReportedName = "ASP.NET Cache Entries" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Cache Total Hits", ReportedName = "ASP.NET Cache Hits" },
                new WindowsPerformanceCounter { CategoryName = @"\ASP.NET Applications(__Total__)\Cache Total Misses", ReportedName = "ASP.NET Cache Misses" },

                // Complete CLR Memory
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\# Bytes in all Heaps", ReportedName = "CLR Heap Size" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\% Time in GC", ReportedName = "CLR % Time in GC" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\# Gen 0 Collections", ReportedName = "CLR Gen 0 Collections" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\# Gen 1 Collections", ReportedName = "CLR Gen 1 Collections" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\# Gen 2 Collections", ReportedName = "CLR Gen 2 Collections" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\Large Object Heap size", ReportedName = "CLR LOH Size" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\Gen 0 heap size", ReportedName = "CLR Gen 0 Size" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\Gen 1 heap size", ReportedName = "CLR Gen 1 Size" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Memory(_Global_)\Gen 2 heap size", ReportedName = "CLR Gen 2 Size" },

                // Complete Threading
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\# of current logical Threads", ReportedName = "CLR Logical Threads" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\# of current physical Threads", ReportedName = "CLR Physical Threads" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\Contention Rate / sec", ReportedName = "CLR Contention Rate" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\Current Queue Length", ReportedName = "CLR Thread Queue Length" },
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR LocksAndThreads(_Global_)\Total # of Contentions", ReportedName = "CLR Total Contentions" },

                // Process Metrics
                new WindowsPerformanceCounter { CategoryName = @"\Process(??APP_WIN32_PROC??)\% Processor Time", ReportedName = "Process CPU %" },
                new WindowsPerformanceCounter { CategoryName = @"\Process(??APP_WIN32_PROC??)\Private Bytes", ReportedName = "Process Private Bytes" },
                new WindowsPerformanceCounter { CategoryName = @"\Process(??APP_WIN32_PROC??)\Thread Count", ReportedName = "Process Thread Count" },
                new WindowsPerformanceCounter { CategoryName = @"\Process(??APP_WIN32_PROC??)\Handle Count", ReportedName = "Process Handle Count" },

                // CLR Exceptions
                new WindowsPerformanceCounter { CategoryName = @"\.NET CLR Exceptions(_Global_)\# of Exceps Thrown / sec", ReportedName = "CLR Exceptions/sec" }
            };
        }
#endif

#if !NET472
        /// <summary>
        /// Gets the recommended preset for Optimizely V12/V13 Production environments.
        /// Balanced set of event counters for monitoring production performance.
        /// </summary>
        public static List<EventCounterDefinition> GetOptimizelyV12V13ProductionPreset()
        {
            return new List<EventCounterDefinition>
            {
                // Critical Runtime Metrics
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "cpu-usage" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "working-set" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gc-heap-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "time-in-gc" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-2-gc-count" },

                // Thread Pool Health
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "threadpool-thread-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "threadpool-queue-length" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "monitor-lock-contention-count" },

                // ASP.NET Core Metrics
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "requests-per-second" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "current-requests" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "failed-requests" },

                // Exception Tracking
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "exception-count" }
            };
        }

        /// <summary>
        /// Gets the minimal preset for Optimizely V12/V13 Development environments.
        /// Lightweight set for local development.
        /// </summary>
        public static List<EventCounterDefinition> GetOptimizelyV12V13DevelopmentPreset()
        {
            return new List<EventCounterDefinition>
            {
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "cpu-usage" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gc-heap-size" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "requests-per-second" }
            };
        }

        /// <summary>
        /// Gets the comprehensive preset for Optimizely V12/V13 troubleshooting.
        /// Extended set for diagnosing performance issues.
        /// </summary>
        public static List<EventCounterDefinition> GetOptimizelyV12V13TroubleshootingPreset()
        {
            return new List<EventCounterDefinition>
            {
                // Complete System.Runtime
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "cpu-usage" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "working-set" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gc-heap-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-0-gc-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-1-gc-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-2-gc-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "time-in-gc" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-0-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-1-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-2-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "loh-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "poh-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "alloc-rate" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gc-fragmentation" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "assembly-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "exception-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "threadpool-thread-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "monitor-lock-contention-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "threadpool-queue-length" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "threadpool-completed-items-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "active-timer-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "il-bytes-jitted" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "methods-jitted-count" },

                // Complete ASP.NET Core
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "requests-per-second" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "total-requests" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "current-requests" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "failed-requests" },

                // HTTP Connections (Kestrel)
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Server.Kestrel", CounterName = "connection-queue-length" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Server.Kestrel", CounterName = "request-queue-length" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Server.Kestrel", CounterName = "current-connections" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Server.Kestrel", CounterName = "total-connections" },
                new EventCounterDefinition { EventSourceName = "Microsoft.AspNetCore.Server.Kestrel", CounterName = "current-tls-handshakes" },

                // HTTP Client
                new EventCounterDefinition { EventSourceName = "System.Net.Http", CounterName = "requests-started" },
                new EventCounterDefinition { EventSourceName = "System.Net.Http", CounterName = "requests-started-rate" },
                new EventCounterDefinition { EventSourceName = "System.Net.Http", CounterName = "requests-failed" },
                new EventCounterDefinition { EventSourceName = "System.Net.Http", CounterName = "current-requests" },
                new EventCounterDefinition { EventSourceName = "System.Net.Http", CounterName = "http11-connections-current-total" },
                new EventCounterDefinition { EventSourceName = "System.Net.Http", CounterName = "http20-connections-current-total" },
                new EventCounterDefinition { EventSourceName = "System.Net.Http", CounterName = "http30-connections-current-total" }
            };
        }

        /// <summary>
        /// Gets a memory-focused preset for diagnosing memory issues.
        /// </summary>
        public static List<EventCounterDefinition> GetMemoryDiagnosticsPreset()
        {
            return new List<EventCounterDefinition>
            {
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "working-set" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gc-heap-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-0-gc-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-1-gc-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-2-gc-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "time-in-gc" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-0-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-1-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gen-2-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "loh-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "poh-size" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "alloc-rate" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "gc-fragmentation" }
            };
        }

        /// <summary>
        /// Gets a threading-focused preset for diagnosing concurrency issues.
        /// </summary>
        public static List<EventCounterDefinition> GetThreadingDiagnosticsPreset()
        {
            return new List<EventCounterDefinition>
            {
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "threadpool-thread-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "threadpool-queue-length" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "threadpool-completed-items-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "monitor-lock-contention-count" },
                new EventCounterDefinition { EventSourceName = "System.Runtime", CounterName = "active-timer-count" }
            };
        }
#endif
    }
}
