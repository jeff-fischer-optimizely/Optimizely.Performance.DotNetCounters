#if !NET472
using System;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Optimizely.Performance.DotNetCounters.Cache;
using Optimizely.Performance.DotNetCounters.Configuration;

// EPiServer.ServiceLocation has a ServiceDescriptor of its own, and this file needs both
// namespaces.
using ServiceDescriptor = Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Optimizely.Performance.DotNetCounters.Services
{
    /// <summary>
    /// Registers cache dependency instrumentation on Optimizely 12 and 13.
    /// </summary>
    public static class CacheInstrumentationServiceExtensions
    {
        // Resolved per call, not cached: this type is first touched during
        // ConfigureContainer, before the LogManager factory is wired up.
        private static ILogger Log => LogManager.GetLogger(typeof(CacheInstrumentationServiceExtensions));

        /// <summary>
        /// Wraps the caches so that dependency cascades, eviction reasons and entry lifetimes
        /// become measurable. Does nothing if instrumentation is disabled by configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The decoration is deferred to <see cref="ServiceConfigurationContext.ConfigurationComplete"/>
        /// because Optimizely 12 does not register <see cref="IObjectInstanceCache"/> in
        /// <c>ConfigureContainer</c> at all - <c>FrameworkInitialization</c> adds it from its own
        /// <c>ConfigurationComplete</c> handler, so the descriptor does not exist yet while
        /// modules are still configuring. Optimizely 13 registers it eagerly in
        /// <c>AddCmsCache</c>, but deferring works there too, and one code path that cannot
        /// depend on module ordering is worth more than an early registration.
        /// </para>
        /// </remarks>
        public static void AddOptimizelyCacheInstrumentation(
            this ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            var options = new CacheInstrumentationOptions();
            configuration.GetSection(CacheInstrumentationOptions.SectionName).Bind(options);

            if (!options.Enabled)
            {
                // Leave the caches entirely undecorated. Nothing this package does can then
                // reach a cache operation at all.
                return;
            }

            context.Services.Configure<CacheInstrumentationOptions>(
                configuration.GetSection(CacheInstrumentationOptions.SectionName));

            context.Services.AddSingleton(sp =>
                new CacheInstrumentationRecorder(sp.GetService<TelemetryClient>(), options));

            context.ConfigurationComplete += (_, e) =>
            {
                try
                {
                    // Order matters only in that all three must be wrapped. The memory cache
                    // counts entries; the object caches delimit the operation and name it.
                    Decorate<IMemoryCache>(e.Services, (sp, inner) =>
                        new InstrumentedMemoryCache(inner, Recorder(sp)));

                    Decorate<IObjectInstanceCache>(e.Services, (sp, inner) =>
                        new InstrumentedObjectInstanceCache(inner, Recorder(sp)));

                    Decorate<ISynchronizedObjectInstanceCache>(e.Services, (sp, inner) =>
                        new InstrumentedSynchronizedObjectInstanceCache(inner, Recorder(sp)));
                }
                catch (Exception ex)
                {
                    // A site with no cache metrics is a nuisance; a site that will not start
                    // is an outage. Leave whatever was already decorated in place and carry on.
                    Log.Error(
                        "Failed to install Optimizely cache instrumentation. Cache metrics will be " +
                        "incomplete or absent; cache behaviour is unaffected.",
                        ex);
                }
            };
        }

        private static CacheInstrumentationRecorder Recorder(IServiceProvider provider) =>
            provider.GetRequiredService<CacheInstrumentationRecorder>();

        /// <summary>
        /// Replaces the registration for <typeparamref name="TService"/> with one that builds
        /// the original and wraps it.
        /// </summary>
        /// <remarks>
        /// The last matching descriptor is the one replaced, because that is the one the
        /// container would have resolved. The original is rebuilt inside the new factory, so
        /// the decorated instance inherits the original lifetime rather than gaining one of
        /// its own.
        /// </remarks>
        private static void Decorate<TService>(
            IServiceCollection services,
            Func<IServiceProvider, TService, TService> decorate)
            where TService : class
        {
            for (var i = services.Count - 1; i >= 0; i--)
            {
                var descriptor = services[i];
                if (descriptor.ServiceType != typeof(TService))
                {
                    continue;
                }

                services[i] = new ServiceDescriptor(
                    typeof(TService),
                    sp => decorate(sp, CreateOriginal<TService>(sp, descriptor)),
                    descriptor.Lifetime);

                return;
            }

            // Nothing registered under this service type. Expected for
            // ISynchronizedObjectInstanceCache in hosts that never register one.
            Log.Information(
                $"No registration found for {typeof(TService).Name}; skipping cache instrumentation for it.");
        }

        private static TService CreateOriginal<TService>(IServiceProvider provider, ServiceDescriptor descriptor)
            where TService : class
        {
            if (descriptor.ImplementationInstance is TService instance)
            {
                return instance;
            }

            if (descriptor.ImplementationFactory != null)
            {
                return (TService)descriptor.ImplementationFactory(provider);
            }

            return (TService)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);
        }
    }
}
#endif
