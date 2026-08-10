#if !NET472
using System;
using System.Linq;
using EPiServer.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Optimizely.Performance.DotNetCounters.Configuration;

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
