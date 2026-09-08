#if NET472
using System;
using System.Diagnostics;
using System.Linq;
using Optimizely.Performance.DotNetCounters.Configuration;
using Xunit;

namespace Optimizely.Performance.DotNetCounters.Tests
{
    /// <summary>
    /// Tests for the ADO.NET connection pool counter instance name.
    /// </summary>
    /// <remarks>
    /// The instance name is reconstructed rather than read, because the framework does not
    /// expose it anywhere. Any deviation from what ADO.NET publishes produces a counter path
    /// that matches nothing, and the counter is then reported as absent rather than as wrong -
    /// a failure mode with no symptom, which is why the rules are pinned down here.
    /// </remarks>
    public class SqlClientCountersTests
    {
        [Fact]
        public void InstanceNameEndsWithThisProcessId()
        {
            var instance = SqlClientCounters.ResolveInstanceName();

            // The process id suffix is the part that makes the instance unique across a web
            // garden, where several worker processes publish into the same category.
            Assert.EndsWith($"[{Process.GetCurrentProcess().Id}]", instance, StringComparison.Ordinal);
        }

        [Fact]
        public void InstanceNameUsesTheEntryAssemblyWhenThereIsOne()
        {
            // Under a test host or a console application there is an entry assembly, so the
            // name comes from it rather than from the app domain. Under IIS the reverse holds,
            // which is the case that makes the two hosts produce different names.
            var instance = SqlClientCounters.ResolveInstanceName();

            Assert.False(string.IsNullOrWhiteSpace(instance));
            Assert.NotEqual($"[{Process.GetCurrentProcess().Id}]", instance);
        }

        [Theory]
        [InlineData('(')]
        [InlineData(')')]
        [InlineData('/')]
        [InlineData('\\')]
        [InlineData('#')]
        public void InstanceNameContainsNoCharactersAdoNetSubstitutes(char forbidden)
        {
            // ADO.NET rewrites these before publishing. Parentheses matter most: a counter path
            // is "\Category(Instance)\Counter", so an unescaped one would split the path.
            var instance = SqlClientCounters.ResolveInstanceName();

            Assert.DoesNotContain(forbidden, instance);
        }

        [Fact]
        public void InstanceNameRespectsTheLengthLimit()
        {
            // 127 characters is the performance counter instance name limit. An app domain
            // friendly name under IIS can easily exceed it, at which point ADO.NET elides the
            // middle, and a name that is merely truncated would not match.
            var instance = SqlClientCounters.ResolveInstanceName();

            Assert.True(
                instance.Length <= 127,
                $"Instance name was {instance.Length} characters: {instance}");
        }

        [Fact]
        public void InstanceNameIsStableAcrossCalls()
        {
            // Resolved once at startup and reused for the life of the process, so it had
            // better not vary.
            Assert.Equal(SqlClientCounters.ResolveInstanceName(), SqlClientCounters.ResolveInstanceName());
        }

        [Fact]
        public void DetailCountersAreOffWithoutTheTraceSwitch()
        {
            // No switch is configured for this test host, matching an unmodified site. The four
            // detail counters read a constant zero in that state, so they must not be collected.
            Assert.False(SqlClientCounters.IsDetailEnabled());
        }

        [Fact]
        public void DetectingTheTraceSwitchNeverThrows()
        {
            var exception = Record.Exception(() => SqlClientCounters.IsDetailEnabled());

            Assert.Null(exception);
        }

        [Fact]
        public void DefaultCountersOmitTheDetailCountersWhenTheSwitchIsOff()
        {
            var counters = DefaultCounters.GetDefaultWindowsCounters();

            var detail = new[]
            {
                "NumberOfActiveConnections", "NumberOfFreeConnections",
                "SoftConnectsPerSecond", "SoftDisconnectsPerSecond"
            };

            foreach (var name in detail)
            {
                Assert.DoesNotContain(
                    counters,
                    c => c.CategoryName?.EndsWith(name, StringComparison.Ordinal) == true);
            }
        }

        [Fact]
        public void DefaultCountersIncludeTheAlwaysAvailablePoolCounters()
        {
            var counters = DefaultCounters.GetDefaultWindowsCounters();

            // NumberOfReclaimedConnections is the one that names a defect rather than
            // describing load: anything other than zero means connections are being left
            // undisposed and recovered by the garbage collector.
            Assert.Contains(
                counters,
                c => c.CategoryName?.EndsWith("NumberOfReclaimedConnections", StringComparison.Ordinal) == true);
            Assert.Contains(
                counters,
                c => c.CategoryName?.EndsWith("NumberOfPooledConnections", StringComparison.Ordinal) == true);
        }

        [Fact]
        public void SqlCounterPathsCarryTheInstanceToken()
        {
            // The token is substituted by the service at startup. If a path shipped without
            // it, the counter would silently never resolve.
            var sqlCounters = DefaultCounters.GetDefaultWindowsCounters()
                .Where(c => c.CategoryName?.Contains(".NET Data Provider for SqlServer") == true)
                .ToList();

            Assert.NotEmpty(sqlCounters);
            Assert.All(sqlCounters, c =>
                Assert.Contains(SqlClientCounters.InstanceNameToken, c.CategoryName, StringComparison.Ordinal));
        }
    }
}
#endif
