# Optimizely Performance Counters - Quick Start

Thank you for installing **Optimizely.Performance.DotNetCounters**!

## Files Added to Your Project

### For Optimizely V11 (.NET Framework 4.7.2)

**In your project root:**
- ✅ `Optimizely.PerformanceCounters.config` - **Main configuration file** (28 counters)

**In App_Data/Optimizely.PerformanceCounters/V11/:**
- 📄 `web.config.snippet.xml` - Lines to add to your web.config
- 📄 `web.Development.config` - Transform to disable in development

### For Optimizely V12/V13 (.NET 6+)

**In App_Data/Optimizely.PerformanceCounters/V12-V13/:**
- 📄 `appsettings.json` - Complete configuration (41 counters)
- 📄 `appsettings.Development.json` - Override to disable in development

**In App_Data/Optimizely.PerformanceCounters/:**
- 📄 `CONFIGURATION_PRESETS_GUIDE.md` - Complete reference guide

---

## Quick Setup (Choose Your Version)

### Optimizely V11 (.NET Framework) - 3 Steps

The main configuration file `Optimizely.PerformanceCounters.config` is already in your project!

**Step 1: Add configSection to web.config**

Find `<configSections>` in your web.config and add:

```xml
<configSections>
  <!-- Your existing sections... -->
  
  <sectionGroup name="optimizely">
    <section name="performanceCounters"
             type="Optimizely.Performance.DotNetCounters.Services.PerformanceCountersConfigSection, Optimizely.Performance.DotNetCounters" />
  </sectionGroup>
</configSections>
```

**Step 2: Reference the config file**

Add this anywhere inside `<configuration>`:

```xml
<optimizely>
  <performanceCounters configSource="Optimizely.PerformanceCounters.config" />
</optimizely>
```

**Step 3: Configure Application Insights**

```xml
<applicationInsights>
  <InstrumentationKey>YOUR-INSTRUMENTATION-KEY-HERE</InstrumentationKey>
</applicationInsights>
```

**That's it!** 

✅ The library auto-initializes on startup  
✅ Collects 28 performance counters every 60 seconds  
✅ Sends metrics to Application Insights

**💡 Optional: Disable in Development**

Copy `App_Data/Optimizely.PerformanceCounters/V11/web.Development.config` to your project root as `web.Debug.config`

**📄 Need help?** See `web.config.snippet.xml` for a complete example

---

### Optimizely V12/V13 (.NET 6+) - 2 Steps

**Step 1: Merge configuration**

Open `App_Data/Optimizely.PerformanceCounters/V12-V13/appsettings.json` and copy the content into your project's `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=YOUR-KEY-HERE"
  },
  
  "Optimizely": {
    "PerformanceCounters": {
      "Enabled": true,
      "EventCounters": [
        // ... paste all counters from template ...
      ]
    }
  }
}
```

**Step 2 (Optional): Disable in Development**

Copy `appsettings.Development.json` to your project root:

```json
{
  "Optimizely": {
    "PerformanceCounters": {
      "Enabled": false
    }
  }
}
```

**That's it!**

✅ Auto-initializes on startup  
✅ Collects 41 event counters every 60 seconds  
✅ Sends metrics to Application Insights

---

## What Gets Monitored

### V11 (.NET Framework) - 28 Counters

- **ASP.NET** (6): Request queue, throughput, timing, failures
- **Cache** (3): Cache entries, hits, misses
- **Memory** (9): Heap size, GC time, all generations + LOH
- **Threading** (5): Thread counts, lock contention
- **Process** (4): CPU, memory, threads, handles
- **Exceptions** (1): Exception rate

### V12/V13 (.NET 6+) - 41 Counters

- **Process** (2): CPU usage, working set
- **Memory & GC** (12): All generations, LOH, POH, fragmentation
- **Threading** (5): Thread pool, queue, contention
- **Application** (2): Exceptions, assemblies
- **JIT** (2): IL bytes, methods jitted
- **ASP.NET Core** (4): Request metrics
- **Kestrel** (5): Connection & request queues
- **HTTP Client** (7): Outbound request metrics
- **Runtime** (2): Additional metrics

---

## Using configSource Benefits (V11)

✅ **Clean separation** - Config separate from web.config  
✅ **Easy updates** - Update `Optimizely.PerformanceCounters.config` without touching web.config  
✅ **Version control friendly** - Easy to see what changed  
✅ **Reusable** - Share config across projects  

To customize counters, just edit `Optimizely.PerformanceCounters.config` - no web.config changes needed!

---

## Viewing Metrics

### Azure Portal
1. Application Insights → Metrics
2. Namespace: "Custom" or "Performance Counters"
3. Select your counter

### Kusto Query
```kusto
customMetrics
| where name startswith "CLR" or name startswith "ASP.NET" or name startswith "Process"
| order by timestamp desc
| take 100
```

### Memory Over Time
```kusto
customMetrics
| where name == "CLR Heap Size"
| summarize avg(value) by bin(timestamp, 5m)
| render timechart
```

---

## Troubleshooting

**Metrics not appearing?**
1. Wait 2-3 minutes for initial data
2. Check Application Insights key is correct
3. Verify `enabled="true"` in configuration
4. Check Debug output for errors

**V11: Can't find Optimizely.PerformanceCounters.config?**
- It should be in your project root (same directory as web.config)
- Check Solution Explorer → Show All Files
- Ensure it's included in the project

---

## Documentation

📖 **Configuration Guide**: `App_Data/Optimizely.PerformanceCounters/CONFIGURATION_PRESETS_GUIDE.md`  
📖 **Full Documentation**: See `docs/` folder in repository  
🔗 **GitHub**: https://github.com/optimizely/performance-counters  
🔗 **NuGet**: https://www.nuget.org/packages/Optimizely.Performance.DotNetCounters

---

**Version**: 1.0.0  
**License**: Apache-2.0
