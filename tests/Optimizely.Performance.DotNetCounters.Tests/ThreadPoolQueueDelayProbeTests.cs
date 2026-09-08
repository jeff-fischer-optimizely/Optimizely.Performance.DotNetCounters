using System;
using System.Threading;
using Optimizely.Performance.DotNetCounters.Configuration;
using Optimizely.Performance.DotNetCounters.Diagnostics;
using Xunit;

namespace Optimizely.Performance.DotNetCounters.Tests
{
    /// <summary>
    /// Lifecycle tests for the thread pool queue delay probe.
    /// </summary>
    /// <remarks>
    /// The probe runs a thread for the life of the process and queues work to the pool it is
    /// measuring, so the sequences that matter are the ones where it is stopped, restarted or
    /// misconfigured - a probe that leaks a thread per app domain recycle would eventually
    /// become the problem it exists to detect.
    /// </remarks>
    public class ThreadPoolQueueDelayProbeTests
    {
        private static ThreadPoolQueueDelayProbe CreateProbe(ThreadPoolProbeOptions? options = null) =>
            new ThreadPoolQueueDelayProbe(null, options ?? new ThreadPoolProbeOptions());

        [Fact]
        public void StartsWithoutATelemetryClient()
        {
            using var probe = CreateProbe();

            var exception = Record.Exception(() => probe.Start());

            Assert.Null(exception);
        }

        [Fact]
        public void DoesNothingWhenDisabled()
        {
            using var probe = CreateProbe(new ThreadPoolProbeOptions { Enabled = false });

            var exception = Record.Exception(() => probe.Start());

            Assert.Null(exception);
        }

        [Fact]
        public void StartingTwiceIsHarmless()
        {
            using var probe = CreateProbe();

            probe.Start();
            var exception = Record.Exception(() => probe.Start());

            Assert.Null(exception);
        }

        [Fact]
        public void DisposingWithoutStartingIsHarmless()
        {
            var probe = CreateProbe();

            var exception = Record.Exception(() => probe.Dispose());

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
            var probe = CreateProbe(new ThreadPoolProbeOptions { SampleIntervalSeconds = 1 });
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
            using var probe = new ThreadPoolQueueDelayProbe(null, null!);

            var exception = Record.Exception(() => probe.Start());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ClampsNonsensicalSampleIntervals(int seconds)
        {
            // Without clamping, a misconfigured interval would queue work items to the pool in
            // a tight loop - the probe would become a source of thread pool pressure rather
            // than a measure of it.
            var options = new ThreadPoolProbeOptions { SampleIntervalSeconds = seconds };

            Assert.True(options.SampleInterval >= TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-30)]
        public void ClampsNonsensicalSampleTimeouts(int seconds)
        {
            var options = new ThreadPoolProbeOptions { SampleTimeoutSeconds = seconds };

            // A zero timeout would report every sample as starvation.
            Assert.True(options.SampleTimeout >= TimeSpan.FromSeconds(1));
        }
    }
}
