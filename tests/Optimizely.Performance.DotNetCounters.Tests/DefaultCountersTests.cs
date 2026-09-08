using System;
using System.Linq;
using Optimizely.Performance.DotNetCounters.Configuration;
using Xunit;

namespace Optimizely.Performance.DotNetCounters.Tests
{
    /// <summary>
    /// Structural tests for the shipped counter definitions.
    /// </summary>
    /// <remarks>
    /// Counter definitions are data, and the failure mode for bad data here is quiet: a
    /// malformed path or a duplicated reported name produces a metric that is missing or
    /// merged with another rather than an error anybody sees.
    /// </remarks>
    public class DefaultCountersTests
    {
#if NET472
        [Fact]
        public void WindowsCounterPathsAreWellFormed()
        {
            foreach (var counter in DefaultCounters.GetDefaultWindowsCounters())
            {
                Assert.False(string.IsNullOrWhiteSpace(counter.CategoryName));
                Assert.False(string.IsNullOrWhiteSpace(counter.ReportedName));

                // "\Category\Counter" or "\Category(Instance)\Counter".
                Assert.StartsWith("\\", counter.CategoryName, StringComparison.Ordinal);
                Assert.True(
                    counter.CategoryName!.Count(c => c == '\\') >= 2,
                    $"Not a counter path: {counter.CategoryName}");
            }
        }

        [Fact]
        public void WindowsCounterReportedNamesAreUnique()
        {
            // Two counters reporting under one name silently interleave into a single series.
            var duplicates = DefaultCounters.GetDefaultWindowsCounters()
                .GroupBy(c => c.ReportedName, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void WindowsCounterInstancePlaceholdersAreBalanced()
        {
            foreach (var counter in DefaultCounters.GetDefaultWindowsCounters())
            {
                var open = counter.CategoryName!.Count(c => c == '(');
                var close = counter.CategoryName.Count(c => c == ')');

                Assert.True(open == close, $"Unbalanced instance parentheses: {counter.CategoryName}");
            }
        }
#else
        [Fact]
        public void EventCounterDefinitionsAreComplete()
        {
            foreach (var counter in DefaultCounters.GetDefaultEventCounters())
            {
                Assert.False(string.IsNullOrWhiteSpace(counter.EventSourceName));
                Assert.False(string.IsNullOrWhiteSpace(counter.CounterName));
            }
        }

        [Fact]
        public void EventCountersAreNotDuplicated()
        {
            // The collector adds one collection request per entry; a duplicate would enable
            // the same counter twice and double-report it.
            var duplicates = DefaultCounters.GetDefaultEventCounters()
                .GroupBy(c => $"{c.EventSourceName}/{c.CounterName}", StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void EventCounterNamesUseTheRuntimeConvention()
        {
            // EventCounter names are matched literally against the name the source registered.
            // They are lower kebab case throughout the runtime and SqlClient, and a stray
            // capital or underscore would match nothing without failing.
            foreach (var counter in DefaultCounters.GetDefaultEventCounters())
            {
                Assert.Equal(counter.CounterName, counter.CounterName!.ToLowerInvariant());
                Assert.DoesNotContain(' ', counter.CounterName);
                Assert.DoesNotContain('_', counter.CounterName);
            }
        }

        [Fact]
        public void IncludesTheSqlConnectionPoolCounters()
        {
            var counters = DefaultCounters.GetDefaultEventCounters();

            // Verified present under these exact names in both the SqlClient version Optimizely
            // 12 ships with and the one Optimizely 13 ships with.
            var expected = new[]
            {
                "number-of-pooled-connections",
                "number-of-active-connections",
                "number-of-free-connections",
                "hard-connects",
                "number-of-reclaimed-connections"
            };

            foreach (var name in expected)
            {
                Assert.Contains(
                    counters,
                    c => c.EventSourceName == "Microsoft.Data.SqlClient.EventSource"
                         && c.CounterName == name);
            }
        }
#endif
    }
}
