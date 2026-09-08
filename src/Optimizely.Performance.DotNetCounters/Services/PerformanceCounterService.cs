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

            // Resolved once. The instance name cannot change while the process lives, and
            // building it involves reflection over the entry assembly.
            var sqlInstance = SqlClientCounters.ResolveInstanceName();

            foreach (var counter in counters)
            {
                // Applied to configured counters as well as defaults, so a site that lists
                // its own connection pool counters in web.config can use the same token.
                var path = counter.CategoryName?.Replace(
                    SqlClientCounters.InstanceNameToken, sqlInstance);

                try
                {
                    _module.Counters.Add(new PerformanceCounterCollectionRequest(
                        path,
                        counter.ReportedName));
                }
                catch (Exception ex)
                {
                    // Log but continue - some counters may not be available
                    Log.Warning($"Failed to add performance counter {path}", ex);
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
        [ConfigurationProperty("enabled", DefaultValue = true)]
        public bool Enabled
        {
            get => (bool)(this["enabled"] ?? true);
            set => this["enabled"] = value;
        }

        [ConfigurationProperty("enableIISExpressPerformanceCounters", DefaultValue = false)]
        public bool EnableIISExpressPerformanceCounters
        {
            get => (bool)(this["enableIISExpressPerformanceCounters"] ?? false);
            set => this["enableIISExpressPerformanceCounters"] = value;
        }

        [ConfigurationProperty("counters")]
        [ConfigurationCollection(typeof(PerformanceCounterElementCollection))]
        public PerformanceCounterElementCollection Counters
        {
            get => (PerformanceCounterElementCollection)(this["counters"]
                ?? new PerformanceCounterElementCollection());
            set => this["counters"] = value;
        }
    }

    public class PerformanceCounterElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new PerformanceCounterElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PerformanceCounterElement)element).CategoryName;
        }
    }

    public class PerformanceCounterElement : ConfigurationElement
    {
        [ConfigurationProperty("categoryName", IsRequired = true)]
        public string CategoryName
        {
            get => (string)(this["categoryName"] ?? string.Empty);
            set => this["categoryName"] = value;
        }

        [ConfigurationProperty("reportedName", IsRequired = true)]
        public string ReportedName
        {
            get => (string)(this["reportedName"] ?? string.Empty);
            set => this["reportedName"] = value;
        }

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
