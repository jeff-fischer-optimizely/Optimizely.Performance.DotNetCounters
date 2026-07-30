#if NET472 && CMS11
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Optimizely.Performance.DotNetCounters
{
    internal static class WindowsPerfCounters
    {
        private static readonly Dictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var definitions = new (string key, string category, string counter, string instance)[]
            {
                ("ASP.NET.RequestsQueued", "ASP.NET", "Requests Queued", string.Empty),
                ("ASP.NET.RequestsPerSec", "ASP.NET", "Requests/Sec", string.Empty),
                ("ASP.NET.RequestWaitTime", "ASP.NET", "Request Wait Time", string.Empty),
                ("ASP.NET.RequestExecutionTime", "ASP.NET", "Request Execution Time", string.Empty),

                ("CLR.LocksAndThreads.LogicalThreads", ".NET CLR LocksAndThreads", "# of current logical Threads", string.Empty),
                ("CLR.LocksAndThreads.PhysicalThreads", ".NET CLR LocksAndThreads", "# of current physical Threads", string.Empty),
                ("CLR.LocksAndThreads.ContentionRatePerSec", ".NET CLR LocksAndThreads", "Contention Rate / sec", string.Empty),
                ("CLR.LocksAndThreads.CurrentQueueLength", ".NET CLR LocksAndThreads", "Current Queue Length", string.Empty),

                ("CLR.Memory.BytesInAllHeaps", ".NET CLR Memory", "# Bytes in all Heaps", string.Empty),
                ("CLR.Memory.PercentTimeInGC", ".NET CLR Memory", "% Time in GC", string.Empty),
                ("CLR.Memory.Gen0CollectionsPerSec", ".NET CLR Memory", "Gen 0 Collections/sec", string.Empty),
                ("CLR.Memory.Gen1CollectionsPerSec", ".NET CLR Memory", "Gen 1 Collections/sec", string.Empty),
                ("CLR.Memory.Gen2CollectionsPerSec", ".NET CLR Memory", "Gen 2 Collections/sec", string.Empty),
                ("CLR.Memory.LargeObjectHeapSize", ".NET CLR Memory", "Large Object Heap size", string.Empty),

                ("Process.ThreadCount", "Process", "Thread Count", string.Empty),
                ("Process.HandleCount", "Process", "Handle Count", string.Empty),
            };

            foreach (var def in definitions)
            {
                try
                {
                    PerformanceCounter pc = string.IsNullOrEmpty(def.instance)
                        ? new PerformanceCounter(def.category, def.counter, readOnly: true)
                        : new PerformanceCounter(def.category, def.counter, def.instance, readOnly: true);

                    try { var _ = pc.NextValue(); } catch { }
                    _counters[def.key] = pc;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"WindowsPerfCounters: failed to create counter {def.category}/{def.counter}: {ex.Message}");
                }
            }
        }

        public static IReadOnlyDictionary<string, float> ReadAll()
        {
            var result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _counters)
            {
                try { result[kv.Key] = kv.Value.NextValue(); }
                catch (Exception ex)
                {
                    Trace.WriteLine($"WindowsPerfCounters: error reading {kv.Key}: {ex.Message}");
                    result[kv.Key] = float.NaN;
                }
            }
            return result;
        }

        public static void Shutdown()
        {
            foreach (var kv in _counters)
            {
                try { kv.Value.Dispose(); } catch { }
            }
            _counters.Clear();
            _initialized = false;
        }
    }
}
#else
namespace Optimizely.Performance.DotNetCounters
{
    internal static class WindowsPerfCounters
    {
        public static void Initialize() { }
        public static System.Collections.Generic.IReadOnlyDictionary<string, float> ReadAll() => new System.Collections.Generic.Dictionary<string, float>();
        public static void Shutdown() { }
    }
}
#endif
