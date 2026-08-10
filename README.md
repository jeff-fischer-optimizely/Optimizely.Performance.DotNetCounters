# Optimizely Performance Counters

A multi-targeted .NET library that provides seamless performance counter integration for Optimizely CMS across all major versions and .NET platforms.

## Documentation

- 📖 **[Quick Reference](docs/QUICK_REFERENCE.md)** - Quick lookup guide
- 📖 **[Configuration Guide](docs/CONFIGURATION_PRESETS_GUIDE.md)** - Complete configuration reference
- 📖 **[Usage Guide](docs/USAGE_GUIDE.md)** - Detailed usage instructions and Kusto queries
- 📖 **[Build Guide](docs/BUILD_GUIDE.md)** - How to build and package
- 📖 **[Architecture](docs/ARCHITECTURE.md)** - Visual diagrams and architecture details
- 📖 **[Changelog](docs/CHANGELOG.md)** - Version history

---

## Features

- **Multi-Platform Support**: Works with .NET Framework 4.7.2, .NET 6, 8, 9, and 10
- **Multi-Version Support**: Compatible with Optimizely V11, V12, and V13
- **Smart Counter Selection**: Automatically uses Windows Performance Counters on .NET Framework and Event Counters on .NET Core+
- **Configuration-Driven**: Configure counters via `web.config` or `appsettings.json`
- **Auto-Initialization**: Integrates seamlessly using Optimizely's `IConfigurableModule`
- **Application Insights Integration**: Sends metrics directly to Application Insights

## Supported Platforms

| .NET Version | Optimizely Version | Counter Type |
|--------------|-------------------|--------------|
| .NET Framework 4.7.2 | V11 | Windows Performance Counters |
| .NET 6 | V12 | Event Counters |
| .NET 8 | V13 | Event Counters |
| .NET 9 | V13 | Event Counters |
| .NET 10 | V13 | Event Counters |

## Installation

1. Add the NuGet package to your Optimizely project:

```bash
dotnet add package Optimizely.Performance.DotNetCounters
```

2. The initialization module will automatically register on application startup.

## Configuration

### For .NET Framework 4.7.2 (Optimizely V11)

Add the following to your `web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <sectionGroup name="optimizely">
      <section name="performanceCounters"
               type="Optimizely.Performance.DotNetCounters.Services.PerformanceCountersConfigSection, Optimizely.Performance.DotNetCounters" />
    </sectionGroup>
  </configSections>

  <optimizely>
    <performanceCounters enabled="true">
      <!-- Leave empty to use defaults, or add custom counters -->
      <counters>
        <add categoryName="\ASP.NET\Requests Queued"
             reportedName="ASP.NET Requests Queued" />
        <add categoryName=".NET CLR Memory(_Global_)\# Bytes in all Heaps"
             reportedName="CLR Heap Size" />
        <!-- Add more counters as needed -->
      </counters>
    </performanceCounters>
  </optimizely>

  <!-- Application Insights Configuration -->
  <applicationInsights>
    <InstrumentationKey>YOUR-INSTRUMENTATION-KEY-HERE</InstrumentationKey>
  </applicationInsights>
</configuration>
```

### For .NET 6+ (Optimizely V12 & V13)

Add the following to your `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=YOUR-INSTRUMENTATION-KEY-HERE"
  },

  "Optimizely": {
    "PerformanceCounters": {
      "Enabled": true,
      "EventCounters": [
        {
          "EventSourceName": "System.Runtime",
          "CounterName": "cpu-usage"
        },
        {
          "EventSourceName": "System.Runtime",
          "CounterName": "gc-heap-size"
        },
        {
          "EventSourceName": "Microsoft.AspNetCore.Hosting",
          "CounterName": "requests-per-second"
        }
      ]
    }
  }
}
```

**Note**: If you leave the `EventCounters` array empty or omit it, the library will use a comprehensive set of default counters.

## Default Counters

### .NET Framework 4.7.2 (Windows Performance Counters)

The following counters are collected by default:

**ASP.NET Counters:**
- ASP.NET Requests Queued
- Requests/Sec
- Request Wait Time
- Request Execution Time

**.NET CLR LocksAndThreads:**
- \# of current logical Threads
- \# of current physical Threads
- Contention Rate / sec
- Current Queue Length

**.NET CLR Memory:**
- \# Bytes in all Heaps
- % Time in GC
- Gen 0 Collections
- Gen 1 Collections
- Gen 2 Collections
- Large Object Heap size

**Process:**
- Thread Count
- Handle Count

### .NET 6+ (Event Counters)

The following event counters are collected by default:

**System.Runtime:**
- cpu-usage
- working-set
- gc-heap-size
- gen-0-gc-count, gen-1-gc-count, gen-2-gc-count
- time-in-gc
- gen-0-size, gen-1-size, gen-2-size
- loh-size
- alloc-rate
- assembly-count
- exception-count
- threadpool-thread-count
- monitor-lock-contention-count
- threadpool-queue-length
- threadpool-completed-items-count
- active-timer-count

**Microsoft.AspNetCore.Hosting:**
- requests-per-second
- total-requests
- current-requests
- failed-requests

## Usage

### Automatic Initialization

The library automatically initializes when your Optimizely application starts. No code changes are required - just install the package and configure it.

### Viewing Metrics in Application Insights

1. Navigate to your Application Insights resource in the Azure Portal
2. Go to **Metrics**
3. Select the metric namespace **"Custom"** or **"Performance Counters"**
4. Select the specific counter you want to view
5. Add filters and aggregations as needed

### Kusto Queries

Query custom metrics in Application Insights using Kusto:

```kusto
// View all performance counter metrics
customMetrics
| where name startswith "ASP.NET" or name startswith "CLR" or name startswith "Process"
| project timestamp, name, value
| order by timestamp desc

// Track GC performance over time
customMetrics
| where name == "CLR % Time in GC"
| summarize avg(value), max(value) by bin(timestamp, 5m)
| render timechart
```

## Troubleshooting

### .NET Framework: Counters Not Appearing

1. Ensure the Application Pool identity has permissions to read performance counters
2. Verify the performance counter names are correct (use Performance Monitor to browse available counters)
3. Check the Debug output window for error messages during initialization

### .NET Core: Event Counters Not Collecting

1. Ensure `Microsoft.ApplicationInsights.AspNetCore` package is installed
2. Verify the Application Insights connection string is correct
3. Check that the event source names and counter names are spelled correctly
4. Review the Debug output for initialization errors

### Disabling Counter Collection

Set `enabled="false"` in `web.config` or `"Enabled": false` in `appsettings.json`.

## Advanced Scenarios

### Custom Counter Selection

You can mix and match counters by explicitly specifying only the ones you need in configuration. The library will only collect the counters you specify.

### Multi-Instance Deployments

The library works seamlessly in multi-instance deployments (scale-out scenarios). Each instance reports its own metrics to Application Insights with automatic instance identification.

### Performance Impact

The performance impact of this library is minimal:
- Counter collection happens on a background thread
- Default collection interval is 60 seconds
- Failed counter reads are gracefully handled without exceptions

## License

Apache-2.0

## Support

For issues and feature requests, please use the GitHub issue tracker.

## Examples

See the `examples/` directory for complete configuration examples:
- `web.config.example` - Full .NET Framework configuration
- `appsettings.json.example` - Full .NET Core+ configuration
