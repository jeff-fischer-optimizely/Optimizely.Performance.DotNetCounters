# Usage Guide

This guide provides detailed instructions on how to integrate and use Optimizely Performance Counters in your Optimizely CMS projects.

## Quick Start

### 1. Install the Package

```bash
dotnet add package Optimizely.Performance.DotNetCounters
```

Or via Package Manager Console:

```powershell
Install-Package Optimizely.Performance.DotNetCounters
```

### 2. Configure Application Insights

Ensure your project has Application Insights configured:

**For .NET Framework 4.7.2 (V11):**

Add to `web.config`:

```xml
<configuration>
  <applicationInsights>
    <InstrumentationKey>YOUR-KEY-HERE</InstrumentationKey>
  </applicationInsights>
</configuration>
```

**For .NET 6+ (V12/V13):**

Add to `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=YOUR-KEY-HERE"
  }
}
```

### 3. Configure Performance Counters (Optional)

The library works with sensible defaults out of the box. Configuration is only needed if you want to customize which counters are collected.

## Configuration Examples

### .NET Framework 4.7.2 (Optimizely V11)

#### Using Default Counters

To use all default Windows Performance Counters, simply enable the feature in `web.config`:

```xml
<configuration>
  <configSections>
    <sectionGroup name="optimizely">
      <section name="performanceCounters"
               type="Optimizely.Performance.DotNetCounters.Services.PerformanceCountersConfigSection, Optimizely.Performance.DotNetCounters" />
    </sectionGroup>
  </configSections>

  <optimizely>
    <performanceCounters enabled="true">
      <!-- Leave counters empty to use defaults -->
    </performanceCounters>
  </optimizely>
</configuration>
```

#### Custom Counter Selection

To collect only specific counters:

```xml
<optimizely>
  <performanceCounters enabled="true">
    <counters>
      <!-- ASP.NET Performance -->
      <add categoryName="\ASP.NET\Requests Queued"
           reportedName="ASP.NET Requests Queued" />
      
      <!-- Memory -->
      <add categoryName="\.NET CLR Memory(_Global_)\# Bytes in all Heaps"
           reportedName="CLR Heap Size" />
      <add categoryName="\.NET CLR Memory(_Global_)\% Time in GC"
           reportedName="CLR % Time in GC" />
      
      <!-- Threading -->
      <add categoryName="\.NET CLR LocksAndThreads(_Global_)\# of current logical Threads"
           reportedName="CLR Logical Threads" />
    </counters>
  </performanceCounters>
</optimizely>
```

#### Disabling Counter Collection

```xml
<optimizely>
  <performanceCounters enabled="false" />
</optimizely>
```

### .NET 6+ (Optimizely V12 & V13)

#### Using Default Event Counters

To use all default .NET Event Counters, enable the feature in `appsettings.json`:

```json
{
  "Optimizely": {
    "PerformanceCounters": {
      "Enabled": true
    }
  }
}
```

#### Custom Event Counter Selection

To collect only specific event counters:

```json
{
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
          "EventSourceName": "System.Runtime",
          "CounterName": "threadpool-queue-length"
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

#### Environment-Specific Configuration

Use `appsettings.{Environment}.json` for environment-specific settings:

**appsettings.Development.json:**
```json
{
  "Optimizely": {
    "PerformanceCounters": {
      "Enabled": false
    }
  }
}
```

**appsettings.Production.json:**
```json
{
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

## Viewing Metrics in Application Insights

### Azure Portal

1. Navigate to your Application Insights resource
2. Select **Metrics** from the left menu
3. Click **+ New chart**
4. Configure the chart:
   - **Scope**: Your Application Insights resource
   - **Metric Namespace**: Custom or Performance Counters
   - **Metric**: Select the counter (e.g., "CLR Heap Size", "ASP.NET Requests Queued")
   - **Aggregation**: Avg, Min, Max, or Sum

### Kusto Query Language (KQL)

#### View All Performance Counters

```kusto
customMetrics
| where name startswith "ASP.NET" 
    or name startswith "CLR" 
    or name startswith "Process"
| project timestamp, name, value, cloud_RoleInstance
| order by timestamp desc
| take 100
```

#### Track Memory Over Time

```kusto
customMetrics
| where name == "CLR Heap Size"
| summarize avg(value), max(value) by bin(timestamp, 5m)
| render timechart
```

#### GC Performance Analysis

```kusto
customMetrics
| where name in ("CLR % Time in GC", "CLR Gen 0 Collections", "CLR Gen 2 Collections")
| summarize avg(value) by name, bin(timestamp, 1m)
| render timechart
```

#### Request Queue Monitoring

```kusto
customMetrics
| where name == "ASP.NET Requests Queued"
| summarize avg(value), max(value) by bin(timestamp, 1m)
| render timechart
```

#### Thread Contention Analysis

```kusto
customMetrics
| where name == "CLR Contention Rate"
| where value > 0
| summarize avg(value), count() by bin(timestamp, 5m)
| render timechart
```

### Creating Alerts

Set up alerts for critical metrics:

1. Go to **Alerts** in your Application Insights resource
2. Click **+ Create** → **Alert rule**
3. Configure the condition:
   - **Signal type**: Custom Metrics
   - **Signal name**: Select your counter
   - **Threshold**: Static or Dynamic
   - **Operator**: Greater than
   - **Threshold value**: Set your threshold
4. Add an action group (email, SMS, webhook, etc.)

**Example Alert Conditions:**
- ASP.NET Requests Queued > 50
- CLR % Time in GC > 20%
- CLR Contention Rate > 100/sec

## Performance Dashboard Example

Create a comprehensive performance dashboard using Application Insights Workbooks:

```json
{
  "version": "Notebook/1.0",
  "items": [
    {
      "type": 9,
      "content": {
        "version": "KqlParameterItem/1.0",
        "query": "customMetrics | where name == \"CLR Heap Size\" | summarize avg(value) by bin(timestamp, 5m)",
        "chartType": "line",
        "title": "Memory Usage (Heap Size)"
      }
    },
    {
      "type": 9,
      "content": {
        "version": "KqlParameterItem/1.0",
        "query": "customMetrics | where name == \"ASP.NET Requests/Sec\" | summarize avg(value) by bin(timestamp, 1m)",
        "chartType": "line",
        "title": "Request Throughput"
      }
    },
    {
      "type": 9,
      "content": {
        "version": "KqlParameterItem/1.0",
        "query": "customMetrics | where name in (\"CLR Gen 0 Collections\", \"CLR Gen 1 Collections\", \"CLR Gen 2 Collections\") | summarize avg(value) by name, bin(timestamp, 1m)",
        "chartType": "line",
        "title": "Garbage Collection Frequency"
      }
    }
  ]
}
```

## Advanced Scenarios

### Custom Initialization

If you need to customize the initialization process, you can create your own initialization module:

```csharp
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Optimizely.Performance.DotNetCounters.Configuration;

namespace MyProject.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(Optimizely.Performance.DotNetCounters.Initialization.PerformanceCountersInitializationModule))]
    public class CustomPerformanceMonitoringModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            // Add custom performance monitoring logic here
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}
```

### Programmatic Configuration (.NET 6+)

You can configure counters programmatically in `Startup.cs` or `Program.cs`:

```csharp
// Program.cs (.NET 6+)
var builder = WebApplication.CreateBuilder(args);

// Configure performance counter options programmatically
builder.Services.Configure<PerformanceCounterOptions>(options =>
{
    options.Enabled = true;
    options.EventCounters = new List<EventCounterDefinition>
    {
        new() { EventSourceName = "System.Runtime", CounterName = "cpu-usage" },
        new() { EventSourceName = "System.Runtime", CounterName = "gc-heap-size" },
        new() { EventSourceName = "Microsoft.AspNetCore.Hosting", CounterName = "requests-per-second" }
    };
});
```

### Multi-Instance Deployments

In scale-out scenarios (multiple web servers), each instance reports its own metrics. You can distinguish instances using the `cloud_RoleInstance` field:

```kusto
customMetrics
| where name == "CLR Heap Size"
| summarize avg(value) by cloud_RoleInstance, bin(timestamp, 5m)
| render timechart
```

### Exporting Metrics

Export metrics to other systems using Application Insights Continuous Export or Azure Monitor Metrics Export.

## Troubleshooting

### Metrics Not Appearing

1. **Check Application Insights Configuration**:
   - Verify the instrumentation key/connection string is correct
   - Ensure Application Insights SDK is installed

2. **Verify Counter Configuration**:
   - Check that `Enabled` is set to `true`
   - Review counter names for typos

3. **Check Initialization**:
   - Look for initialization messages in Debug output
   - Ensure the module is being loaded by Optimizely

4. **Permissions** (.NET Framework):
   - Ensure the Application Pool identity has permission to read performance counters
   - Run `perfmon` (Performance Monitor) to verify counters are available

### High Memory Usage from Counters

Performance counter collection has minimal overhead. If you suspect issues:

1. Reduce the number of collected counters
2. Increase the collection interval (Application Insights setting)
3. Use sampling in Application Insights

### Counter Values Seem Incorrect

1. **Windows Performance Counters** (.NET Framework):
   - Verify counter names using Performance Monitor (perfmon.exe)
   - Check instance names (especially for CLR counters)

2. **Event Counters** (.NET Core+):
   - Use `dotnet-counters` tool to verify counter values locally:
     ```bash
     dotnet-counters monitor --process-id <pid> System.Runtime
     ```

## Best Practices

1. **Start with Defaults**: Use the default counter set initially, then refine based on your needs

2. **Environment-Specific Configuration**: Disable counters in development, enable in staging/production

3. **Set Up Alerts**: Create alerts for critical thresholds (queue length, GC time, etc.)

4. **Regular Review**: Review collected metrics weekly to identify trends

5. **Correlate with Application Insights**: Use performance counters alongside Application Insights request telemetry for complete visibility

6. **Document Baselines**: Establish baseline values for your application under normal load

7. **Test in Staging**: Verify counter collection in a staging environment before production deployment

## References

- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [.NET Event Counters](https://docs.microsoft.com/dotnet/core/diagnostics/event-counters)
- [Windows Performance Counters](https://docs.microsoft.com/windows/win32/perfctrs/performance-counters-portal)
- [Optimizely CMS Documentation](https://docs.developers.optimizely.com/)
