using System;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

#if NET472
using Microsoft.Extensions.Configuration;
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
            // No cleanup needed
        }
    }
}
