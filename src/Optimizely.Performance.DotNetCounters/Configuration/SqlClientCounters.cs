#if NET472
using System;
using System.Diagnostics;
using System.Reflection;

namespace Optimizely.Performance.DotNetCounters.Configuration
{
    /// <summary>
    /// Helpers for the ADO.NET connection pool performance counters on .NET Framework.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These counters live in the <c>.NET Data Provider for SqlServer</c> category, whose
    /// instance name follows a scheme none of Application Insights' built-in placeholders
    /// produce, and four of whose counters are switched off unless the host asks for them.
    /// Both quirks are handled here rather than being left to whoever writes the config.
    /// </para>
    /// </remarks>
    public static class SqlClientCounters
    {
        /// <summary>
        /// Placeholder for the connection pool counter instance name.
        /// </summary>
        /// <remarks>
        /// Application Insights understands <c>??APP_WIN32_PROC??</c>, <c>??APP_CLR_PROC??</c>
        /// and <c>??APP_W3SVC_PROC??</c>, but ADO.NET names its instance differently from all
        /// three, so this one is substituted by <see cref="Services.PerformanceCounterService"/>
        /// before the path reaches the collector.
        /// </remarks>
        public const string InstanceNameToken = "??SQLCLIENT_INSTANCE??";

        /// <summary>
        /// Name of the trace switch that turns on the four detail-level pool counters.
        /// </summary>
        public const string DetailSwitchName = "ConnectionPoolPerformanceCounterDetail";

        private const int InstanceNameMaxLength = 127;
        private const string TruncationMarker = "[...]";

        /// <summary>
        /// Builds the counter instance name ADO.NET publishes under for this process.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This reproduces <c>DbConnectionPoolCounters.GetInstanceName</c> exactly, because
        /// there is no API that reports it. Under IIS the entry assembly is null, so the name
        /// comes from the app domain's friendly name - typically something along the lines of
        /// <c>/LM/W3SVC/2/ROOT-1-133...</c>, which the character substitutions below turn into
        /// <c>_LM_W3SVC_2_ROOT-1-133...</c>. A console or service host takes the entry assembly
        /// name instead, so the two hosts produce visibly different instance names for the
        /// same application.
        /// </para>
        /// <para>
        /// Being a faithful copy is the whole point: any deviation yields a path that matches
        /// no live instance, and the counter reads as absent rather than as an error.
        /// </para>
        /// </remarks>
        public static string ResolveInstanceName()
        {
            string? name = null;

            try
            {
                name = Assembly.GetEntryAssembly()?.GetName()?.Name;
            }
            catch
            {
                // Reflecting on the entry assembly can fail under partial trust. The app
                // domain fallback below is what ADO.NET itself would have used anyway.
            }

            if (string.IsNullOrEmpty(name))
            {
                name = AppDomain.CurrentDomain?.FriendlyName;
            }

            var instance = $"{name}[{GetCurrentProcessId()}]"
                .Replace('(', '[')
                .Replace(')', ']')
                .Replace('#', '_')
                .Replace('/', '_')
                .Replace('\\', '_');

            if (instance.Length <= InstanceNameMaxLength)
            {
                return instance;
            }

            // Same elision ADO.NET applies: keep both ends, drop the middle. The tail matters
            // because that is where the process id is.
            var head = (InstanceNameMaxLength - TruncationMarker.Length) / 2;
            var tail = InstanceNameMaxLength - head - TruncationMarker.Length;

            return instance.Substring(0, head)
                + TruncationMarker
                + instance.Substring(instance.Length - tail, tail);
        }

        /// <summary>
        /// Reports whether the four detail-level connection pool counters are being published.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>NumberOfActiveConnections</c>, <c>NumberOfFreeConnections</c>,
        /// <c>SoftConnectsPerSecond</c> and <c>SoftDisconnectsPerSecond</c> - the four that
        /// actually answer "how full is the pool" - are only created when the
        /// <see cref="DetailSwitchName"/> trace switch is set to <c>Verbose</c>. Without it
        /// they do not fail: the instance still exists, so the counters read a constant zero.
        /// A metric that is confidently and permanently wrong is worse than a missing one,
        /// which is why they are collected only when this returns <c>true</c>.
        /// </para>
        /// <para>
        /// Reading the same switch ADO.NET reads means the answer cannot drift from reality,
        /// and the site needs no setting beyond the one that turns the counters on:
        /// </para>
        /// <code>
        /// &lt;system.diagnostics&gt;
        ///   &lt;switches&gt;
        ///     &lt;add name="ConnectionPoolPerformanceCounterDetail" value="4" /&gt;
        ///   &lt;/switches&gt;
        /// &lt;/system.diagnostics&gt;
        /// </code>
        /// </remarks>
        public static bool IsDetailEnabled()
        {
            try
            {
                return new TraceSwitch(DetailSwitchName, "level of detail to track with connection pool performance counters")
                    .Level == TraceLevel.Verbose;
            }
            catch
            {
                // A malformed switch value throws. Treat that as "off", matching the
                // conservative reading of an unconfigured host.
                return false;
            }
        }

        private static int GetCurrentProcessId()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return process.Id;
            }
        }
    }
}
#endif
