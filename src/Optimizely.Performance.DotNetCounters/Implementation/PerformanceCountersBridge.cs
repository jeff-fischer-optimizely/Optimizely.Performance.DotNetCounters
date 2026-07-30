using System;
#if NET472
using System.Diagnostics;
#else
using System.Diagnostics;
#endif

namespace Optimizely.Performance.DotNetCounters
{
    /// <summary>
    /// Public entry point for registering and managing performance counters / event counters.
    /// </summary>
    public static class PerformanceCountersBridge
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

#if NET472
            try
            {
                WindowsPerfCounters.Initialize();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"PerformanceCountersBridge: WindowsPerfCounters.Initialize failed: {ex}");
            }
#else
            try
            {
                DotNetEventCounters.Initialize();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"PerformanceCountersBridge: DotNetEventCounters.Initialize failed: {ex}");
            }
#endif
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;

#if NET472
            try { WindowsPerfCounters.Shutdown(); } catch { }
#else
            try { DotNetEventCounters.Shutdown(); } catch { }
#endif
        }
    }
}
