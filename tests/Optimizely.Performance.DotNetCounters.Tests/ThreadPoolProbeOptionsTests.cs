#if NET472
using Optimizely.Performance.DotNetCounters.Configuration;
using Xunit;

namespace Optimizely.Performance.DotNetCounters.Tests
{
    /// <summary>
    /// Tests the appSettings binding that makes the probe configurable on V11.
    /// </summary>
    /// <remarks>
    /// V11 sites rarely register an <c>IConfiguration</c>, and this binding is the only thing
    /// standing between that and a probe that runs a thread nobody can turn off. The values it
    /// reads come from the test project's App.config.
    /// </remarks>
    public class ThreadPoolProbeOptionsTests
    {
        [Fact]
        public void ReadsSettingsFromAppSettings()
        {
            var options = new ThreadPoolProbeOptions();

            ThreadPoolProbeOptions.BindAppSettings(options);

            Assert.Equal(37, options.SampleIntervalSeconds);
            Assert.Equal(250, options.SlowSampleThresholdMilliseconds);
        }

        [Fact]
        public void LeavesDefaultsForSettingsThatAreAbsent()
        {
            var options = new ThreadPoolProbeOptions();
            var expected = options.SampleTimeoutSeconds;

            ThreadPoolProbeOptions.BindAppSettings(options);

            // Partial configuration is the normal case; a site that sets one key must not lose
            // the defaults for every other.
            Assert.Equal(expected, options.SampleTimeoutSeconds);
        }

        [Fact]
        public void IgnoresValuesThatDoNotParse()
        {
            var options = new ThreadPoolProbeOptions();
            var expected = options.LogsPerMinute;

            ThreadPoolProbeOptions.BindAppSettings(options);

            // A typo falls back to the default rather than throwing out of an initialization
            // module, where the cost would be the site not starting.
            Assert.Equal(expected, options.LogsPerMinute);
        }

        [Fact]
        public void ReadsTheEnabledFlag()
        {
            var options = new ThreadPoolProbeOptions { Enabled = false };

            ThreadPoolProbeOptions.BindAppSettings(options);

            // The off switch is the whole reason this binding exists, so it has to be the one
            // setting proven to round-trip rather than merely not throw.
            Assert.True(options.Enabled);
        }

        [Fact]
        public void SurvivesNullOptions()
        {
            var exception = Record.Exception(() => ThreadPoolProbeOptions.BindAppSettings(null!));

            Assert.Null(exception);
        }
    }
}
#endif
