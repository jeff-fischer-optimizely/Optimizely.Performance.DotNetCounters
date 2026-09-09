# Optimizely Performance Counters

Application Insights tells you a great deal about your Optimizely site's *requests* and almost nothing about the *process serving them*. When the site slows down, the question you actually need answered — is this garbage collection, thread pool starvation, lock contention, a large object heap that never shrinks, or plain CPU exhaustion? — is answered by .NET runtime counters, and the Application Insights SDK does not collect most of them by default.

This package turns them on. Install it and, on the next application start, an Optimizely initialization module registers a curated set of runtime counters with the Application Insights SDK your site already has. The metrics land in `customMetrics` next to your existing telemetry, queryable in Kusto and chartable against request duration. No code changes, no separate agent, no second monitoring bill.

## Why you'd want this

- **Diagnose causes, not just symptoms.** Request duration tells you the site is slow. `CLR % Time in GC`, `threadpool-queue-length`, `monitor-lock-contention-count` and `ASP.NET Requests Queued` tell you why.
- **Broader than the SDK default.** GC generation sizes, LOH size, allocation rate, loaded-assembly count and the ASP.NET request queue are not in Application Insights' default counter set. They are the ones that matter during a memory or throughput incident.
- **Counter names that are correct.** Windows performance counter paths fail silently when they are wrong — a missing leading `\` or a plausible-but-nonexistent name like `Gen 0 Collections/sec` simply collects nothing, forever, with no error. The defaults here are verified against running Foundation V11, V12 and V13 sites.
- **One configuration surface across V11, V12 and V13.** The same `Optimizely:PerformanceCounters` section works on all three, which matters if you are running mixed versions through a migration.
- **Nothing to write.** Auto-registers via `IConfigurableModule`; a single setting disables it again.

## What this gives you on your version

The value proposition is genuinely different on each Optimizely version, because the diagnostic tooling available to .NET Framework and to modern .NET is not remotely the same. Find your version below.

### Optimizely V11 — .NET Framework 4.7.2 (Windows)

**What you get:** 23 Windows Performance Counters by default — ASP.NET request queue depth, requests/sec, request wait and execution time; cache trims, cache entry counts, turnover rate and the machine and process memory-limit percentages that drive them; CLR logical and physical thread counts, contention rate and thread queue length; total managed heap bytes, % time in GC, Gen 0/1/2 collection counts and LOH size; process thread and handle counts. Collected by the Application Insights `PerformanceCollectorModule` already present in your site.

The cache counters are worth calling out. `Cache API Trims` rising means ASP.NET is evicting entries to relieve memory pressure — and because Optimizely's content cache hangs entries off master keys, a trim can cascade well beyond the entries ASP.NET actually chose to drop. Paired with `Cache % Process Memory Limit Used`, that turns a previously invisible failure mode into two lines on a chart.

**Alternatives that exist:**

| Option | Trade-off |
|---|---|
| Hand-edit `<Counters>` under `PerformanceCollectorModule` in `ApplicationInsights.config` | The closest equivalent, and the mechanism this package uses. But `ApplicationInsights.config` is a NuGet content file, so SDK upgrades can rewrite it, and there is no compile-time or startup check on the counter path strings |
| Application Insights Agent (codeless IIS attach) | No deployment change, but the counter set is fixed — you cannot ask it for LOH size or contention rate |
| PerfMon / Windows data collector sets | Full access to every counter on the box, but per-server, no retention in Application Insights, and no correlation with your request telemetry |
| Commercial APM (New Relic, Dynatrace, AppDynamics) | Far more capable, at the price of a second agent, a second data pipeline and a second bill |

**The gap this fills:** the modern .NET diagnostics stack does not reach .NET Framework at all. EventCounters and EventPipe are .NET Core constructs, so `dotnet-counters` cannot attach to a V11 site, and OpenTelemetry's runtime metrics instrumentation is built around the .NET Core runtime. That leaves `PerformanceCollectorModule` as the only in-process route from CLR internals into Application Insights on V11 — and its weak point is that a wrong counter path fails silently rather than throwing. A missing leading `\`, or a plausible-sounding name like `Gen 0 Collections/sec` that does not actually exist, collects nothing for the life of the site with no error anywhere. Both of those were real bugs found and fixed here by running against a live Foundation V11 site. On top of that, Application Insights disables performance counter collection under IIS Express by default, so a developer testing locally sees an empty dashboard and concludes the whole thing is broken; `EnableIISExpressPerformanceCounters` exists for exactly that. **This is where the package earns its keep most:** V11 is the version with the fewest alternatives, the most legacy load, and the sharpest silent-failure mode.

### Optimizely V12 — .NET 6 through .NET 9

**What you get:** 23 EventCounters by default — CPU usage, working set, GC heap size, Gen 0/1/2 collection counts *and* generation sizes, time in GC, LOH size, allocation rate, loaded assembly count, exception count, thread pool thread count, queue length and completed items, monitor lock contention count, active timer count, plus ASP.NET Core requests/sec, total, current and failed requests.

**Alternatives that exist:**

| Option | Trade-off |
|---|---|
| `services.ConfigureTelemetryModule<EventCounterCollectionModule>(...)` in `Startup.cs` | Perfectly reasonable — this is roughly what the package does internally, in about fifteen lines. You own the counter list, and changing it means a code change and a redeploy rather than an `appsettings.json` edit |
| `dotnet-counters` | Live values from an attached process. Excellent for reproducing something now; no retention, so it cannot tell you what happened at 03:00 last Tuesday |
| OpenTelemetry (`OpenTelemetry.Instrumentation.Runtime` + an Azure Monitor exporter) | Vendor-neutral and the strategic direction. It is a different telemetry pipeline, though — adopting it alongside a classic Application Insights SDK site is a migration, not a configuration change |
| Azure Monitor / DXP platform metrics | Host-level CPU, memory and disk. Outside the process, so blind to the CLR |

**The gap this fills:** smaller and more honest here — real alternatives exist on V12, and if you are already moving to OpenTelemetry you should keep going. What this package adds is a vetted default list (the SDK's own defaults omit GC generation sizes, LOH size and assembly count, which are precisely the three that identify a leak), configuration through `appsettings.json` rather than code, and the same configuration shape as your V11 and V13 sites.

### Optimizely V13 — .NET 10

**What you get:** the same 23 EventCounters as V12, through the same `EventCounterCollectionModule`, configured identically.

**Alternatives that exist:** the same set as V12. OpenTelemetry is more mature on .NET 10, and Microsoft's forward-looking recommendation is the Azure Monitor OpenTelemetry Distro; newer .NET releases also surface runtime metrics through `System.Diagnostics.Metrics`, which OTel consumes natively. If you are greenfielding a V13 site with no legacy Application Insights investment, that path is worth serious consideration.

**The gap this fills:** V13 is new, and the practical reality for most teams is an upgrade rather than a greenfield build. If your V11 or V12 site already reports these counters, this package keeps the same coverage and the same configuration section working after the upgrade, with no monitoring redesign in the middle of a CMS migration. One caveat worth setting expectations on: the *metric names* do change across the .NET Framework → modern .NET boundary, because the underlying counter APIs are different — V11 reports `CLR % Time in GC`, V12 and V13 report `time-in-gc`. Coverage and configuration carry over; V11-era Kusto queries and dashboards need their metric names updated once. V12 → V13 is a clean pass-through with no change at all.

### Regardless of version

Counter collection runs on a background thread on a 60-second interval, failed reads are swallowed rather than thrown, and the whole thing switches off with a single setting. Initialization success and failure are both logged, so a misconfigured site tells you so instead of quietly reporting nothing.

## Supported versions

| Optimizely CMS | .NET | Package asset | Counter API |
|---|---|---|---|
| V11 | .NET Framework 4.7.2+ (Windows) | `net472` | Windows Performance Counters |
| V12 | .NET 6 | `net6.0` | EventCounters |
| V12 | .NET 7 | `net7.0` | EventCounters |
| V12 | .NET 8 | `net8.0` | EventCounters |
| V12 | .NET 9 | `net9.0` | EventCounters |
| V13 | .NET 10 | `net10.0` | EventCounters |

There is an asset per runtime rather than one `net6.0` build rolled forward across the
range. Roll-forward works, but it hands a .NET 9 host the .NET 6 era
`Microsoft.Extensions` assemblies this package asked for, which the site then has to
unify back up. A matching asset avoids the question. The code is identical across all
five — only the resolved package versions differ.

### Older versions

- **CMS 10 and earlier are not supported.** The `net472` assembly will not load on .NET Framework below 4.7.2, and it compiles against `EPiServer.Framework` 11.1.0. A CMS 9/10 site would need a `net45`/`net461` target built against the matching CMS assemblies.
- **There is no `net5.0` asset**, so a CMS 12 site must be running on .NET 6 or later. Retarget the host first; no change to this package is required.
- **V11 support is Windows-only** by nature — Windows Performance Counters do not exist elsewhere. The EventCounter path used by V12/V13 works on Linux and in containers.

## Documentation

- 📖 **[Quick Reference](docs/QUICK_REFERENCE.md)** - Quick lookup guide
- 📖 **[Configuration Guide](docs/CONFIGURATION_PRESETS_GUIDE.md)** - Complete configuration reference
- 📖 **[Usage Guide](docs/USAGE_GUIDE.md)** - Detailed usage instructions and Kusto queries
- 📖 **[Build Guide](docs/BUILD_GUIDE.md)** - How to build and package
- 📖 **[Architecture](docs/ARCHITECTURE.md)** - Visual diagrams and architecture details
- 📖 **[Changelog](docs/CHANGELOG.md)** - Version history

---

## Features

- **Multi-Platform Support**: Ships `net472` plus a `net6.0` through `net10.0` asset per runtime — see [Supported versions](#supported-versions)
- **Multi-Version Support**: Compatible with Optimizely V11, V12, and V13
- **Smart Counter Selection**: Automatically uses Windows Performance Counters on .NET Framework and Event Counters on .NET Core+
- **Configuration-Driven**: Configure counters via `web.config` or `appsettings.json`
- **Auto-Initialization**: Integrates seamlessly using Optimizely's `IConfigurableModule`
- **Application Insights Integration**: Sends metrics directly to Application Insights

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

**ASP.NET Cache:**
- Cache API Trims
- Cache Total Trims
- Cache % Machine Memory Limit Used
- Cache % Process Memory Limit Used
- Cache API Entries
- Cache Total Entries
- Cache API Turnover Rate

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
3. Check your site's log for messages from `Optimizely.Performance.DotNetCounters` — the module logs one `Information` line on successful start and an `Error` with the exception if initialization fails (log4net on V11)

### .NET Core: Event Counters Not Collecting

1. Ensure `Microsoft.ApplicationInsights.AspNetCore` package is installed
2. Verify the Application Insights connection string is correct
3. Check that the event source names and counter names are spelled correctly
4. Review your site's log for messages from `Optimizely.Performance.DotNetCounters` — the module logs one `Information` line on successful start and an `Error` with the exception if initialization fails

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
