#if NET6_0 || NET7_0
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Optimizely.Performance.DotNetCounters
{
    internal static class DotNetEventCounters
    {
        private static EventCounterListener? _listener;
        private static readonly ConcurrentDictionary<string, double> _values = new ConcurrentDictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            if (_listener != null) return;
            _listener = new EventCounterListener(_values);
        }

        public static IReadOnlyDictionary<string, double> ReadAll() => _values;

        public static void Shutdown()
        {
            try { _listener?.Dispose(); } catch { }
            _listener = null;
            _values.Clear();
        }

        private sealed class EventCounterListener : EventListener
        {
            private readonly ConcurrentDictionary<string, double> _store;

            public EventCounterListener(ConcurrentDictionary<string, double> store)
            {
                _store = store;
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                try
                {
                    if (string.Equals(eventSource.Name, "System.Runtime", StringComparison.Ordinal) ||
                        eventSource.Name?.StartsWith(".NETRuntime", StringComparison.Ordinal) == true)
                    {
                        EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All, new Dictionary<string, string>
                        {
                            { "EventCounterIntervalSec", "1" }
                        });
                    }
                }
                catch { }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData == null || eventData.Payload == null) return;

                foreach (var payload in eventData.Payload)
                {
                    if (payload is IDictionary<string, object> dict)
                    {
                        if (dict.TryGetValue("Name", out var nameObj) && dict.TryGetValue("Mean", out var meanObj))
                        {
                            var name = nameObj as string;
                            if (name == null) continue;
                            if (double.TryParse(Convert.ToString(meanObj), out var val))
                            {
                                _store[name] = val;
                            }
                        }
                        else if (dict.TryGetValue("Name", out nameObj) && dict.TryGetValue("Count", out var countObj))
                        {
                            var name = nameObj as string;
                            if (name == null) continue;
                            if (double.TryParse(Convert.ToString(countObj), out var val))
                            {
                                _store[name] = val;
                            }
                        }
                    }
                }
            }
        }
    }
}
#else
namespace Optimizely.Performance.DotNetCounters
{
    internal static class DotNetEventCounters
    {
        public static void Initialize() { }
        public static System.Collections.Generic.IReadOnlyDictionary<string, double> ReadAll() => new System.Collections.Generic.Dictionary<string, double>();
        public static void Shutdown() { }
    }
}
#endif
