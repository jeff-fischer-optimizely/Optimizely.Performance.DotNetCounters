# Quick Reference Guide

**TL;DR**: This library automatically collects performance metrics from your Optimizely CMS application and sends them to Application Insights.

## Installation

```bash
dotnet add package Optimizely.Performance.DotNetCounters
```

## Configuration (Optional - works with defaults)

### .NET Framework (V11)

Add to `web.config`:

```xml
<configSections>
  <sectionGroup name="optimizely">
    <section name="performanceCounters" 
             type="Optimizely.Performance.DotNetCounters.Services.PerformanceCountersConfigSection, Optimizely.Performance.DotNetCounters" />
  </sectionGroup>
</configSections>

<optimizely>
  <performanceCounters enabled="true" />
</optimizely>

<applicationInsights>
  <InstrumentationKey>YOUR-KEY-HERE</InstrumentationKey>
</applicationInsights>
```

### .NET 6+ (V12/V13)

Add to `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=YOUR-KEY-HERE"
  },
  "Optimizely": {
    "PerformanceCounters": {
      "Enabled": true
    }
  }
}
```

## What Gets Collected

These are the built-in defaults - what a site collects when it names no counters of its
own. The shipped configuration templates are supersets of them: 34 on V11 and 39 on
V12/V13. Naming any counter in configuration *replaces* the defaults rather than adding
to them.

### .NET Framework (23 counters)
- ASP.NET request queue, throughput, timing
- ASP.NET cache entries, hit rate and memory-pressure trims
- CLR memory (heap, GC, generations)
- CLR threading (threads, contention)
- Process metrics

### .NET 6+ (23 counters)
- CPU, memory, GC metrics
- Thread pool statistics
- Request throughput and failures
- Allocation rates

## Viewing Metrics

### Azure Portal
1. Go to Application Insights → Metrics
2. Select "Custom" or "Performance Counters" namespace
3. Choose your counter

### Kusto Query
```kusto
customMetrics
| where name startswith "CLR" or name startswith "ASP.NET"
| order by timestamp desc
```

## Common Queries

### Memory Over Time
```kusto
customMetrics
| where name == "CLR Heap Size"
| summarize avg(value) by bin(timestamp, 5m)
| render timechart
```

### Request Queue
```kusto
customMetrics
| where name == "ASP.NET Requests Queued"
| summarize max(value) by bin(timestamp, 1m)
| render timechart
```

### GC Pressure
```kusto
customMetrics
| where name == "CLR % Time in GC"
| summarize avg(value) by bin(timestamp, 1m)
| render timechart
```

## File Reference

| File | Purpose |
|------|---------|
| `README.md` | Overview & quick start |
| `BUILD_GUIDE.md` | How to build & package |
| `USAGE_GUIDE.md` | Detailed configuration & queries |
| `ARCHITECTURE.md` | Visual diagrams |
| `PROJECT_STRUCTURE.md` | Code organization |
| `IMPLEMENTATION_SUMMARY.md` | Complete overview |
| `CHANGELOG.md` | Version history |
| `examples/web.config.example` | Full .NET Framework config |
| `examples/appsettings.json.example` | Full .NET Core+ config |

## Platform Support

| .NET Version | Optimizely | Counter Type |
|--------------|-----------|--------------|
| 4.7.2 | V11 | Windows Performance Counters |
| 6 | V12 | Event Counters |
| 8 | V13 | Event Counters |

## Troubleshooting

**No metrics appearing?**
1. Check Application Insights key is correct
2. Wait 2-3 minutes for metrics to appear
3. Check Debug output for errors

**Wrong counter values?**
- .NET Framework: Use `perfmon.exe` to verify counter names
- .NET 6+: Use `dotnet-counters` tool to check values

**Disable in Development?**
```json
// appsettings.Development.json
{
  "Optimizely": {
    "PerformanceCounters": { "Enabled": false }
  }
}
```

## Support

- Issues: GitHub repository
- Docs: See file reference above
- Application Insights: https://docs.microsoft.com/azure/azure-monitor/app/

---

**License**: Apache-2.0  
**Version**: 1.0.0
