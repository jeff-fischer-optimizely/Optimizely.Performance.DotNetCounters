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
            var counters = new List<WindowsPerformanceCounter>
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

                // Cache memory pressure.
                //
                // "Cache API" counts entries inserted through HttpRuntime.Cache, which is
                // what EPiServer's HttpRuntimeCache uses - so these track the content cache
                // itself. "Cache Total" additionally includes ASP.NET's internal cache
                // (output cache, compiled pages), so it is the whole-process picture.
                //
                // A rising Trims count means ASP.NET is evicting cache entries to relieve
                // memory pressure. Because EPiServer entries carry dependencies, a trim can
                // cascade far beyond the entries ASP.NET chose to drop.
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Cache API Trims",
                    ReportedName = "ASP.NET Cache API Trims"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Cache Total Trims",
                    ReportedName = "ASP.NET Cache Total Trims"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Cache % Machine Memory Limit Used",
                    ReportedName = "ASP.NET Cache % Machine Memory Limit Used"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Cache % Process Memory Limit Used",
                    ReportedName = "ASP.NET Cache % Process Memory Limit Used"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Cache API Entries",
                    ReportedName = "ASP.NET Cache API Entries"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Cache Total Entries",
                    ReportedName = "ASP.NET Cache Total Entries"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = @"\ASP.NET Applications(__Total__)\Cache API Turnover Rate",
                    ReportedName = "ASP.NET Cache API Turnover Rate"
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
                },

                // SQL Server connection pool.
                //
                // Pool exhaustion is the usual end state of a cache problem: entries get
                // invalidated en masse, every request misses, and they all queue for a
                // connection. These counters are what distinguish "the database is slow"
                // from "we ran out of connections to the database", which look identical
                // from the outside and have nothing in common as fixes.
                //
                // The instance name is resolved at startup; see SqlClientCounters.
                new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("NumberOfPooledConnections"),
                    ReportedName = "SQL Pooled Connections"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("NumberOfNonPooledConnections"),
                    ReportedName = "SQL Non-Pooled Connections"
                },

                // A sustained hard connect rate means the pool is not holding connections:
                // each request pays the full TCP and authentication cost. Under steady load
                // this should settle near zero.
                new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("HardConnectsPerSecond"),
                    ReportedName = "SQL Hard Connects/Sec"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("HardDisconnectsPerSecond"),
                    ReportedName = "SQL Hard Disconnects/Sec"
                },

                // Pool count and pool group count should both be small and flat. Growth means
                // connection strings are varying at runtime - each variant gets its own pool
                // with its own Max Pool Size, so the site can exhaust a pool while the totals
                // still look healthy.
                new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("NumberOfActiveConnectionPools"),
                    ReportedName = "SQL Active Connection Pools"
                },
                new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("NumberOfActiveConnectionPoolGroups"),
                    ReportedName = "SQL Active Connection Pool Groups"
                },

                // Connections held open by an unfinished distributed transaction.
                new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("NumberOfStasisConnections"),
                    ReportedName = "SQL Stasis Connections"
                },

                // Connections the garbage collector had to recover because nothing disposed
                // them. Anything other than zero is a leak, and it is the one counter here
                // that names a defect rather than describing load.
                new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("NumberOfReclaimedConnections"),
                    ReportedName = "SQL Reclaimed Connections"
                }
            };

            // The four counters that answer "how full is the pool" are only published when
            // the host opts in; without the switch they read a constant zero rather than
            // failing, so they are collected only once they can be believed.
            if (SqlClientCounters.IsDetailEnabled())
            {
                counters.Add(new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("NumberOfActiveConnections"),
                    ReportedName = "SQL Active Connections"
                });
                counters.Add(new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("NumberOfFreeConnections"),
                    ReportedName = "SQL Free Connections"
                });
                counters.Add(new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("SoftConnectsPerSecond"),
                    ReportedName = "SQL Soft Connects/Sec"
                });
                counters.Add(new WindowsPerformanceCounter
                {
                    CategoryName = SqlPath("SoftDisconnectsPerSecond"),
                    ReportedName = "SQL Soft Disconnects/Sec"
                });
            }

            return counters;
        }

        private static string SqlPath(string counterName) =>
            $@"\.NET Data Provider for SqlServer({SqlClientCounters.InstanceNameToken})\{counterName}";
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
                },

                // SQL Server connection pool.
                //
                // Pool exhaustion is the usual end state of a cache problem: entries get
                // invalidated en masse, every request misses, and they all queue for a
                // connection. These counters are what distinguish "the database is slow"
                // from "we ran out of connections to the database", which look identical
                // from the outside and have nothing in common as fixes.
                //
                // Nothing needs enabling. The counters are created the first time anything
                // enables the event source, which is exactly what registering them here does.
                // Unlike the .NET Framework equivalents there is no detail-level switch, so
                // the full set is available on both Optimizely 12 and 13 - the counter names
                // and the source name are identical across the SqlClient 3.x and 6.x versions
                // those two ship with.
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "number-of-pooled-connections"
                },
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "number-of-non-pooled-connections"
                },

                // In-use versus available: the numerator and denominator of pool utilisation.
                // When free reaches zero and stays there, requests are blocking on the pool
                // and will fail with a connection timeout that names nothing useful.
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "number-of-active-connections"
                },
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "number-of-free-connections"
                },

                // A sustained hard connect rate means the pool is not holding connections:
                // each request pays the full TCP and authentication cost. Under steady load
                // this should settle near zero. Soft connects are pool hits, so the ratio
                // between the two is the pool's hit rate.
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "hard-connects"
                },
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "hard-disconnects"
                },
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "soft-connects"
                },
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "soft-disconnects"
                },

                // Pool count and pool group count should both be small and flat. Growth means
                // connection strings are varying at runtime - each variant gets its own pool
                // with its own Max Pool Size, so the site can exhaust a pool while the totals
                // still look healthy.
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "number-of-active-connection-pools"
                },
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "number-of-active-connection-pool-groups"
                },

                // Connections held open by an unfinished distributed transaction.
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "number-of-stasis-connections"
                },

                // Connections the garbage collector had to recover because nothing disposed
                // them. Anything other than zero is a leak, and it is the one counter here
                // that names a defect rather than describing load.
                new EventCounterDefinition
                {
                    EventSourceName = SqlClientEventSourceName,
                    CounterName = "number-of-reclaimed-connections"
                }
            };
        }

        private const string SqlClientEventSourceName = "Microsoft.Data.SqlClient.EventSource";
#endif
    }
}
