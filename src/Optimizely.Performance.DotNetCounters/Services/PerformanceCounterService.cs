#if NET472
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
                    System.Diagnostics.Debug.WriteLine(
                        $"Failed to add performance counter {counter.CategoryName}: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load performance counter configuration from web.config: {ex.Message}");
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
