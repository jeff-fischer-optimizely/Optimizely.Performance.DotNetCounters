#if !NET472
using System;
using System.Linq;
using EPiServer.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            var section = configuration.GetSection(PerformanceCounterOptions.SectionName);

            // Registered so that the site can inject IOptions<PerformanceCounterOptions> and
            // read the same settings this method acts on.
            services.Configure<PerformanceCounterOptions>(section);

            // ...and bound a second time, here, for our own use. The obvious alternative -
            // resolving IOptions inside the callback below - needs a service provider, and the
            // only one available at that point is one built from this collection, which is a
            // whole parallel container: a second copy of every singleton the site resolves
            // through it, plus a disposable provider nobody disposes.
            //
            // The cost of reading it here instead is that a later services.Configure or
            // PostConfigure of the same options is not seen by the counter list. Nothing in
            // this package does that, and the list is only read once at startup anyway.
            var perfOptions = new PerformanceCounterOptions();
            section.Bind(perfOptions);

            // Added whether or not the counters are enabled. Turning this package off should
            // not also turn off the site's Application Insights, which other things use.
            services.AddApplicationInsightsTelemetry();

            if (!perfOptions.Enabled)
            {
                return services;
            }

            // Use configured counters or defaults
            var counters = perfOptions.EventCounters?.Any() == true
                ? perfOptions.EventCounters
                : DefaultCounters.GetDefaultEventCounters();

            // Configure Event Counter collection
            services.ConfigureTelemetryModule<EventCounterCollectionModule>(
                (module, options) =>
                {
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
