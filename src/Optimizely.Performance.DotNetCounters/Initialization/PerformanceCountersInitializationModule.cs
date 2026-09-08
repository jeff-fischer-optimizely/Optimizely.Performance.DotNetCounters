using System;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

using Optimizely.Performance.DotNetCounters.Diagnostics;

#if NET472
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Optimizely.Performance.DotNetCounters.Configuration;
using Optimizely.Performance.DotNetCounters.Services;
#else
using Microsoft.Extensions.DependencyInjection;
using Optimizely.Performance.DotNetCounters.Services;
#endif

namespace Optimizely.Performance.DotNetCounters.Initialization
{
    /// <summary>
    /// Initialization module for Optimizely Performance Counters.
    /// Supports Optimizely V11 (.NET Framework 4.7.2), V12 (.NET 6), and future versions.
    /// </summary>
    [InitializableModule]
    public class PerformanceCountersInitializationModule : IConfigurableModule
    {
        // EPiServer.Logging is used rather than Microsoft.Extensions.Logging so the same
        // call works on all three targets: on V11 there is no MEL pipeline to attach to,
        // and on V12/V13 ConfigureContainer runs before anything can be resolved from the
        // container. EPiServer.Logging routes to log4net on V11 and to the host's
        // ILoggerFactory on V12/V13.
        //
        // Deliberately resolved per call rather than cached in a static field: on V12/V13
        // the LogManager factory is not wired up yet when ConfigureContainer runs, so a
        // logger captured at that point stays a no-op logger for the life of the process.
        private static ILogger Log =>
            LogManager.GetLogger(typeof(PerformanceCountersInitializationModule));

        private bool _initialized;

        // Held so Uninitialize can stop it. On V12/V13 the container owns the instance and
        // this is only a reference to it, not a second one.
        private ThreadPoolQueueDelayProbe? _threadPoolProbe;

#if !NET472
        // V12/V13 only: the lock this samples does not exist on V11, whose cache is
        // HttpRuntime.Cache rather than MemoryObjectInstanceCache.
        private CacheLockProbe? _cacheLockProbe;
#endif

#if !NET472
        // ConfigureContainer runs before logging is available, so its outcome is stashed
        // here and reported from Initialize, which runs once the host is up.
        private Exception? _configureError;
#endif

        public void Initialize(InitializationEngine context)
        {
            if (_initialized)
            {
                return;
            }

#if NET472
            // .NET Framework initialization (Optimizely V11)
            InitializeFramework(context);
#else
            ReportCoreResult();

            if (_configureError == null)
            {
                StartThreadPoolProbe(context);
                StartCacheLockProbe(context);
            }
#endif

            _initialized = true;
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
#if !NET472
            // .NET Core+ initialization (Optimizely V12 and V13)
            InitializeCore(context);
#endif
        }

#if NET472
        private void InitializeFramework(InitializationEngine context)
        {
            try
            {
                // Try to get IConfiguration from service locator (if available)
                IConfiguration? configuration = null;
                try
                {
                    configuration = context.Locate.Advanced.GetInstance<IConfiguration>();
                }
                catch
                {
                    // IConfiguration may not be available in pure .NET Framework setup
                }

                var service = new PerformanceCounterService();
                service.Initialize(configuration);

                StartThreadPoolProbe(configuration);

                Log.Information(
                    "Optimizely Performance Counters initialized for .NET Framework 4.7.2 (V11)");
            }
            catch (Exception ex)
            {
                // Don't throw - performance counters are optional - but this must be
                // loud, or the library silently does nothing for the life of the site.
                Log.Error("Failed to initialize Optimizely Performance Counters", ex);
            }
        }

        /// <remarks>
        /// There is no container to resolve from on V11, so the probe is built here against the
        /// active telemetry configuration - the same one the counter collector above reports to.
        /// A failure to start the probe must not cost the site its counters, so it is caught
        /// separately from the block that set those up.
        /// </remarks>
        private void StartThreadPoolProbe(IConfiguration? configuration)
        {
            try
            {
                var options = new ThreadPoolProbeOptions();

                // appSettings first, so the probe is configurable on a site that has no
                // IConfiguration at all - which is most V11 sites. Anything the site does
                // register wins, since that is the more deliberate of the two.
                ThreadPoolProbeOptions.BindAppSettings(options);
                configuration?.GetSection(ThreadPoolProbeOptions.SectionName).Bind(options);

                if (!options.Enabled)
                {
                    return;
                }

                _threadPoolProbe = new ThreadPoolQueueDelayProbe(
                    new TelemetryClient(TelemetryConfiguration.Active), options);
                _threadPoolProbe.Start();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start the thread pool queue delay probe.", ex);
            }
        }
#endif

#if !NET472
        private void InitializeCore(ServiceConfigurationContext context)
        {
            try
            {
                // Get IConfiguration from the service collection
                var serviceProvider = context.Services.BuildServiceProvider();
                var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

                // Register performance counter services
                context.Services.AddOptimizelyPerformanceCounters(configuration);

                // Cache dependency instrumentation. Takes the context rather than the service
                // collection because it has to defer its work to ConfigurationComplete, which
                // is the only point at which Optimizely 12's cache registrations exist.
                context.AddOptimizelyCacheInstrumentation(configuration);
            }
            catch (Exception ex)
            {
                // Don't throw - performance counters are optional. Reported from
                // Initialize, once the host's logging pipeline exists.
                _configureError = ex;
            }
        }

        /// <remarks>
        /// Started from <c>Initialize</c> rather than from container configuration so the first
        /// sample lands after the host is serving. Resolved rather than constructed, so the
        /// container disposes the singleton at shutdown even if <c>Uninitialize</c> never runs.
        /// </remarks>
        private void StartThreadPoolProbe(InitializationEngine context)
        {
            try
            {
                _threadPoolProbe = context.Locate.Advanced.GetInstance<ThreadPoolQueueDelayProbe>();
                _threadPoolProbe.Start();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start the thread pool queue delay probe.", ex);
            }
        }

        /// <remarks>
        /// Started after the cache exists, because the probe resolves the lock once at start
        /// and a null field would look to it like an incompatible Optimizely version.
        /// </remarks>
        private void StartCacheLockProbe(InitializationEngine context)
        {
            try
            {
                _cacheLockProbe = context.Locate.Advanced.GetInstance<CacheLockProbe>();
                _cacheLockProbe.Start();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start the cache lock probe.", ex);
            }
        }

        private void ReportCoreResult()
        {
            if (_configureError != null)
            {
                // This must be loud, or the library silently does nothing for the
                // life of the site.
                Log.Error("Failed to initialize Optimizely Performance Counters", _configureError);
                return;
            }

#if NET6_0
            Log.Information("Optimizely Performance Counters initialized for .NET 6 (V12)");
#elif NET10_0
            Log.Information("Optimizely Performance Counters initialized for .NET 10 (V13)");
#endif
        }
#endif

        public void Uninitialize(InitializationEngine context)
        {
            // The probe's thread is a background thread and cannot hold up shutdown on its own,
            // but stopping it here keeps it from sampling a pool that is being torn down.
            _threadPoolProbe?.Dispose();
            _threadPoolProbe = null;

#if !NET472
            _cacheLockProbe?.Dispose();
            _cacheLockProbe = null;
#endif
        }
    }
}
