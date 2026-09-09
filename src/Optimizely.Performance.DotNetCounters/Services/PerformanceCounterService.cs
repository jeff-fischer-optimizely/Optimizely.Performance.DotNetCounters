#if NET472
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using EPiServer.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.Extensions.Configuration;
using Optimizely.Performance.DotNetCounters.Configuration;

namespace Optimizely.Performance.DotNetCounters.Services
{
    /// <summary>
    /// Service for initializing Windows Performance Counters on .NET Framework.
    /// </summary>
    public class PerformanceCounterService
    {
        private static ILogger Log => LogManager.GetLogger(typeof(PerformanceCounterService));

        private readonly PerformanceCollectorModule _module;
        private bool _isInitialized;

        /// <summary>
        /// Creates the service. No counters are read until
        /// <see cref="Initialize"/> is called.
        /// </summary>
        public PerformanceCounterService()
        {
            _module = new PerformanceCollectorModule();
        }

        /// <summary>
        /// Initializes performance counters from configuration.
        /// </summary>
        public void Initialize(IConfiguration? configuration = null)
        {
            if (_isInitialized)
            {
                return;
            }

            var options = LoadConfiguration(configuration);

            if (!options.Enabled)
            {
                return;
            }

            _module.EnableIISExpressPerformanceCounters = options.EnableIISExpressPerformanceCounters;

            // Add configured counters or use defaults
            var counters = options.WindowsCounters?.Any() == true
                ? options.WindowsCounters
                : DefaultCounters.GetDefaultWindowsCounters();

            foreach (var counter in counters)
            {
                try
                {
                    _module.Counters.Add(new PerformanceCounterCollectionRequest(
                        counter.CategoryName,
                        counter.ReportedName));
                }
                catch (Exception ex)
                {
                    // Log but continue - some counters may not be available
                    Log.Warning($"Failed to add performance counter {counter.CategoryName}", ex);
                }
            }

            // Initialize with the active telemetry configuration
            _module.Initialize(TelemetryConfiguration.Active);
            _isInitialized = true;
        }

        private PerformanceCounterOptions LoadConfiguration(IConfiguration? configuration)
        {
            var options = new PerformanceCounterOptions();

            // Try to load from IConfiguration (appsettings.json) first
            if (configuration != null)
            {
                configuration.GetSection(PerformanceCounterOptions.SectionName).Bind(options);
            }

            // If no counters configured, try web.config
            if (options.WindowsCounters?.Any() != true)
            {
                LoadFromWebConfig(options);
            }

            return options;
        }

        private void LoadFromWebConfig(PerformanceCounterOptions options)
        {
            try
            {
                var section = ConfigurationManager.GetSection("optimizely/performanceCounters")
                    as PerformanceCountersConfigSection;

                if (section != null && section.Enabled)
                {
                    options.Enabled = section.Enabled;
                    options.EnableIISExpressPerformanceCounters = section.EnableIISExpressPerformanceCounters;
                    options.WindowsCounters = section.Counters
                        .Cast<PerformanceCounterElement>()
                        .Select(c => new WindowsPerformanceCounter
                        {
                            CategoryName = c.CategoryName,
                            ReportedName = c.ReportedName,
                            InstanceName = c.InstanceName
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to load performance counter configuration from web.config", ex);
            }
        }
    }

    #region Web.config Configuration Section

    /// <summary>
    /// Configuration section for performance counters in web.config.
    /// </summary>
    public class PerformanceCountersConfigSection : ConfigurationSection
    {
        /// <summary>
        /// Whether counters are collected at all. Defaults to <c>true</c>.
        /// </summary>
        [ConfigurationProperty("enabled", DefaultValue = true)]
        public bool Enabled
        {
            get => (bool)(this["enabled"] ?? true);
            set => this["enabled"] = value;
        }

        /// <summary>
        /// Opt in to collection under IIS Express, which Application Insights otherwise
        /// refuses. Useful for local development; leave <c>false</c> in production, where
        /// the site runs under full IIS.
        /// </summary>
        [ConfigurationProperty("enableIISExpressPerformanceCounters", DefaultValue = false)]
        public bool EnableIISExpressPerformanceCounters
        {
            get => (bool)(this["enableIISExpressPerformanceCounters"] ?? false);
            set => this["enableIISExpressPerformanceCounters"] = value;
        }

        /// <summary>
        /// The counters to collect. Naming any counter here <em>replaces</em> the built-in
        /// defaults rather than adding to them, so this list has to carry every default
        /// that should be kept.
        /// </summary>
        [ConfigurationProperty("counters")]
        [ConfigurationCollection(typeof(PerformanceCounterElementCollection))]
        public PerformanceCounterElementCollection Counters
        {
            get => (PerformanceCounterElementCollection)(this["counters"]
                ?? new PerformanceCounterElementCollection());
            set => this["counters"] = value;
        }
    }

    /// <summary>
    /// The <c>&lt;counters&gt;</c> collection of a
    /// <see cref="PerformanceCountersConfigSection"/>.
    /// </summary>
    public class PerformanceCounterElementCollection : ConfigurationElementCollection
    {
        /// <inheritdoc />
        protected override ConfigurationElement CreateNewElement()
        {
            return new PerformanceCounterElement();
        }

        /// <inheritdoc />
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PerformanceCounterElement)element).CategoryName;
        }
    }

    /// <summary>
    /// A single <c>&lt;add&gt;</c> element naming one Windows performance counter.
    /// </summary>
    public class PerformanceCounterElement : ConfigurationElement
    {
        /// <summary>
        /// The full perfmon path, for example
        /// <c>\ASP.NET Applications(__Total__)\Requests/Sec</c>.
        /// </summary>
        [ConfigurationProperty("categoryName", IsRequired = true)]
        public string CategoryName
        {
            get => (string)(this["categoryName"] ?? string.Empty);
            set => this["categoryName"] = value;
        }

        /// <summary>
        /// The name the counter is reported under in Application Insights.
        /// </summary>
        [ConfigurationProperty("reportedName", IsRequired = true)]
        public string ReportedName
        {
            get => (string)(this["reportedName"] ?? string.Empty);
            set => this["reportedName"] = value;
        }

        /// <summary>
        /// Optional instance to read, for counters whose category is instanced. Left unset,
        /// the instance embedded in <see cref="CategoryName"/> applies.
        /// </summary>
        [ConfigurationProperty("instanceName", IsRequired = false)]
        public string? InstanceName
        {
            get => (string?)this["instanceName"];
            set => this["instanceName"] = value;
        }
    }

    #endregion
}
#endif
