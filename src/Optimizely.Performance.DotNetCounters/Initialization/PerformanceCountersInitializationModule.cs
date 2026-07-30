using System;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
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
        private bool _initialized;

        public void Initialize(InitializationEngine context)
        {
            if (_initialized)
            {
                return;
            }

#if NET472
            // .NET Framework initialization (Optimizely V11)
            InitializeFramework(context);
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

                System.Diagnostics.Debug.WriteLine(
                    "Optimizely Performance Counters initialized for .NET Framework 4.7.2 (V11)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to initialize Optimizely Performance Counters: {ex.Message}");
                // Don't throw - performance counters are optional
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

#if NET6_0
                System.Diagnostics.Debug.WriteLine(
                    "Optimizely Performance Counters initialized for .NET 6 (V12)");
#elif NET8_0
                System.Diagnostics.Debug.WriteLine(
                    "Optimizely Performance Counters initialized for .NET 8 (V13)");
#elif NET9_0
                System.Diagnostics.Debug.WriteLine(
                    "Optimizely Performance Counters initialized for .NET 9 (V13)");
#elif NET10_0
                System.Diagnostics.Debug.WriteLine(
                    "Optimizely Performance Counters initialized for .NET 10 (V13)");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to initialize Optimizely Performance Counters: {ex.Message}");
                // Don't throw - performance counters are optional
            }
        }
#endif

        public void Uninitialize(InitializationEngine context)
        {
            // No cleanup needed
        }
    }
}
