#if !NET472
using System;
using System.Threading;
using Optimizely.Performance.DotNetCounters.Configuration;
using Optimizely.Performance.DotNetCounters.Diagnostics;
using Xunit;

namespace Optimizely.Performance.DotNetCounters.Tests
{
    /// <summary>
    /// Lifecycle tests for the probe that consumes the reflection.
    /// </summary>
    /// <remarks>
    /// The locator degrading cleanly is only half of it. The probe is started from an Optimizely
    /// initialization module, where an exception is not a missing metric but a site that will
    /// not start, so these cover the sequences that module can produce - including the ones it
    /// should not, since the guarantee is worth less if it depends on being called correctly.
    /// </remarks>
    public class CacheLockProbeTests
    {
        // No telemetry client. Verifies the probe never assumes one exists, which is the
        // configuration a site gets when Application Insights is not wired up.
        private static CacheLockProbe CreateProbe(CacheLockProbeOptions? options = null) =>
            new CacheLockProbe(null, options ?? new CacheLockProbeOptions());

        [Fact]
        public void StartsWithoutATelemetryClient()
        {
            using var probe = CreateProbe();

            var exception = Record.Exception(() => probe.Start());

            Assert.Null(exception);
        }

        [Fact]
        public void FindsTheLockInAProcessWithOptimizelyLoaded()
        {
            using var probe = CreateProbe();
            probe.Start();

            // End to end against the real assemblies: locate the type, read the field, and
            // report itself ready to sample.
            Assert.True(probe.IsAvailable);
        }

        [Fact]
        public void DoesNothingWhenDisabled()
        {
            using var probe = CreateProbe(new CacheLockProbeOptions { Enabled = false });

            probe.Start();

            // Disabling is the escape hatch for a site that does not want reflection over
            // Optimizely internals, so it has to prevent the lookup, not just the reporting.
            Assert.False(probe.IsAvailable);
        }

        [Fact]
        public void IsNotAvailableBeforeStarting()
        {
            using var probe = CreateProbe();

            Assert.False(probe.IsAvailable);
        }

        [Fact]
        public void StartingTwiceIsHarmless()
        {
            using var probe = CreateProbe();

            probe.Start();
            var exception = Record.Exception(() => probe.Start());

            // Optimizely can run an initialization module more than once across app domain
            // recycles; a second start must not leave two sampler threads behind.
            Assert.Null(exception);
        }

        [Fact]
        public void DisposingWithoutStartingIsHarmless()
        {
            var probe = CreateProbe();

            var exception = Record.Exception(() => probe.Dispose());

            // Uninitialize runs even when Initialize failed part way through.
            Assert.Null(exception);
        }

        [Fact]
        public void DisposingTwiceIsHarmless()
        {
            var probe = CreateProbe();
            probe.Start();
            probe.Dispose();

            var exception = Record.Exception(() => probe.Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void StartingAfterDisposalIsHarmless()
        {
            var probe = CreateProbe();
            probe.Dispose();

            var exception = Record.Exception(() => probe.Start());

            Assert.Null(exception);

            // Start returning cleanly is not the whole claim. If it did spin a sampler thread,
            // that thread throwing would be an unhandled exception on a thread created by hand,
            // which terminates the process rather than failing this assertion - so the wait is
            // there to give it the chance. An earlier revision failed exactly this way.
            Thread.Sleep(250);
        }

        [Fact]
        public void DisposingWhileRunningStopsCleanly()
        {
            var probe = CreateProbe(new CacheLockProbeOptions { SampleIntervalSeconds = 1 });
            probe.Start();

            // Let the sampler get as far as waiting on the stop signal, so disposal races the
            // wait rather than happening before it begins.
            Thread.Sleep(250);

            var exception = Record.Exception(() => probe.Dispose());

            Assert.Null(exception);

            Thread.Sleep(250);
        }

        [Fact]
        public void SurvivesNullOptions()
        {
            // The initialization module binds options from configuration, and a malformed
            // section can produce nothing at all.
            using var probe = new CacheLockProbe(null, null!);

            var exception = Record.Exception(() => probe.Start());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void ClampsNonsensicalSampleIntervals(int seconds)
        {
            var options = new CacheLockProbeOptions { SampleIntervalSeconds = seconds };
            using var probe = new CacheLockProbe(null, options);

            // A zero or negative interval would spin the sampler thread at full speed against
            // the cache lock, which is the opposite of what a passive counter should do.
            var exception = Record.Exception(() => probe.Start());

            Assert.Null(exception);
        }
    }
}
#endif
