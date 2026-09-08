#if !NET472
using System;
using System.Linq;
using EPiServer.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Optimizely.Performance.DotNetCounters.Configuration;
using Optimizely.Performance.DotNetCounters.Diagnostics;

namespace Optimizely.Performance.DotNetCounters.Services
{
    /// <summary>
    /// Service for initializing Event Counters on .NET Core and later.
    /// </summary>
    public static class EventCounterServiceExtensions
    {
        // Resolved per call, not cached: this type is first touched during
        // ConfigureContainer, before the LogManager factory is wired up.
        private static ILogger Log => LogManager.GetLogger(typeof(EventCounterServiceExtensions));

        /// <summary>
        /// Configures Event Counter collection for Application Insights.
        /// </summary>
        public static IServiceCollection AddOptimizelyPerformanceCounters(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind configuration
            services.Configure<PerformanceCounterOptions>(
                configuration.GetSection(PerformanceCounterOptions.SectionName));

            // Add Application Insights
            services.AddApplicationInsightsTelemetry();

            // Bound now rather than resolved through IOptions later, because the probe outlives
            // any scope and has no use for reloaded configuration: changing its interval at
            // runtime would only make the series it produces inconsistent with itself.
            var probeOptions = new ThreadPoolProbeOptions();
            configuration.GetSection(ThreadPoolProbeOptions.SectionName).Bind(probeOptions);

            // Registered here but deliberately not started: sampling before the container is
            // built would measure a thread pool that is not yet serving requests. The
            // initialization module starts it once the host is up.
            services.AddSingleton(sp =>
                new ThreadPoolQueueDelayProbe(sp.GetService<TelemetryClient>(), probeOptions));

            var cacheLockOptions = new CacheLockProbeOptions();
            configuration.GetSection(CacheLockProbeOptions.SectionName).Bind(cacheLockOptions);

            services.AddSingleton(sp =>
                new CacheLockProbe(sp.GetService<TelemetryClient>(), cacheLockOptions));

            // Configure Event Counter collection
            services.ConfigureTelemetryModule<EventCounterCollectionModule>(
                (module, options) =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    var perfOptions = serviceProvider.GetService<IOptions<PerformanceCounterOptions>>()?.Value
                        ?? new PerformanceCounterOptions();

                    if (!perfOptions.Enabled)
                    {
                        return;
                    }

                    // Use configured counters or defaults
                    var counters = perfOptions.EventCounters?.Any() == true
                        ? perfOptions.EventCounters
                        : DefaultCounters.GetDefaultEventCounters();

                    foreach (var counter in counters)
                    {
                        try
                        {
                            module.Counters.Add(new EventCounterCollectionRequest(
                                counter.EventSourceName,
                                counter.CounterName));
                        }
                        catch (Exception ex)
                        {
                            // Log but continue - some counters may not be available
                            Log.Warning(
                                $"Failed to add event counter {counter.EventSourceName}/{counter.CounterName}", ex);
                        }
                    }
                });

            return services;
        }
    }
}
#endif
