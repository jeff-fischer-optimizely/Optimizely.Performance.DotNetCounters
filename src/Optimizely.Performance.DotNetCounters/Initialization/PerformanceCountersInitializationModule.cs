using System;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Configuration;
using Optimizely.Performance.DotNetCounters.Services;

#if !NET472
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
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
        private bool _configurationMissing;
#endif

        /// <summary>
        /// On .NET Framework, starts collection. Elsewhere the work happened in
        /// <see cref="ConfigureContainer"/> and this reports its outcome, now that the
        /// host's logging pipeline exists.
        /// </summary>
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

        /// <summary>
        /// Registers the counter services on .NET 6 and later. A no-op on .NET Framework,
        /// which has no service collection to register into at this point.
        /// </summary>
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
                var configuration = FindConfiguration(context.Services);

                if (configuration == null)
                {
                    _configurationMissing = true;
                    return;
                }

                // Register performance counter services
                context.Services.AddOptimizelyPerformanceCounters(configuration);
            }
            catch (Exception ex)
            {
                // Don't throw - performance counters are optional. Reported from
                // Initialize, once the host's logging pipeline exists.
                _configureError = ex;
            }
        }

        // Reads the descriptor rather than calling BuildServiceProvider. Building a provider
        // here would construct a second, parallel container from the same descriptors: every
        // singleton resolved through it becomes a duplicate of the one the site will use, and
        // the provider itself is disposable and would never be disposed. It is also the reason
        // Log above is a property - nothing can be resolved this early.
        //
        // The generic host registers its configuration with AddSingleton(IConfiguration), an
        // instance registration, which is what makes reading it back possible at all. A host
        // that registered it by factory cannot be read here, and the caller says so rather than
        // guessing.
        private static IConfiguration? FindConfiguration(IServiceCollection services) =>
            services
                .LastOrDefault(descriptor => descriptor.ServiceType == typeof(IConfiguration))?
                .ImplementationInstance as IConfiguration;

        private void ReportCoreResult()
        {
            if (_configureError != null)
            {
                // This must be loud, or the library silently does nothing for the
                // life of the site.
                Log.Error("Failed to initialize Optimizely Performance Counters", _configureError);
                return;
            }

            if (_configurationMissing)
            {
                // Same reasoning as the error above: without configuration nothing was
                // registered, and a silent no-op is the outcome this package most needs to
                // avoid. Named precisely, because the fix is a host wiring change and not a
                // settings change.
                Log.Error(
                    "Optimizely Performance Counters could not start: no IConfiguration was " +
                    "registered as an instance in the service collection when ConfigureContainer " +
                    "ran, so appsettings.json could not be read. No counters are being collected.");
                return;
            }

            // Read at run time rather than from a NET6_0/NET10_0 compile symbol. Those symbols
            // named two of the six target frameworks, so the line disappeared entirely on the
            // rest, and they describe what this assembly was compiled for rather than what the
            // site is running on - which are different things the moment a build rolls forward.
            Log.Information(
                $"Optimizely Performance Counters initialized on {RuntimeInformation.FrameworkDescription}");
        }
#endif

        /// <summary>
        /// Nothing to tear down. Collection is owned by the Application Insights telemetry
        /// modules, which the host disposes with its own service provider.
        /// </summary>
        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}
