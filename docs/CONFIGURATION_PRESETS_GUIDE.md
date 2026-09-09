# Configuration Guide

This guide explains the performance counter configurations for Optimizely CMS V11, V12, and V13.

## Philosophy

The Optimizely.Performance.DotNetCounters package provides **comprehensive, production-ready configurations** that collect all critical metrics for website operations. These configurations are designed to:

- ✅ Provide complete visibility into application performance
- ✅ Capture all metrics critical to website health
- ✅ Enable proactive monitoring and alerting
- ✅ Support root cause analysis when issues occur
- ✅ Maintain low overhead (< 1% CPU impact)

## Available Configurations

### Optimizely V11 (.NET Framework 4.7.2)

Located in: `App_Data/Optimizely.PerformanceCounters/V11/`

| File | Purpose | Counters |
|------|---------|----------|
| `Optimizely.PerformanceCounters.config` | Complete configuration for all environments. Added to the **project root**, not to this folder, because `web.config` references it by `configSource` and that path is relative to the site root | 34 counters |
| `web.Development.config` | Transform to disable counters in development | Transform only |
| `web.config.snippet.xml` | The `<configSections>` and `<optimizely>` elements to paste into `web.config` | Reference only |

### Optimizely V12/V13 (.NET 6+)

Located in: `App_Data/Optimizely.PerformanceCounters/V12-V13/`

| File | Purpose | Counters |
|------|---------|----------|
| `appsettings.json` | Complete configuration for all environments | 39 counters |
| `appsettings.Development.json` | Override to disable counters in development | Override only |

## Counter Categories

### V11 (.NET Framework) Counters

#### ASP.NET Request Metrics (6 counters)
- **Requests Queued** - Number of requests waiting to be processed
- **Requests Rejected** - Requests rejected due to queue full
- **Requests/Sec** - Request throughput
- **Request Wait Time** - Time spent waiting in queue
- **Request Execution Time** - Time spent executing request
- **Requests Failed** - Failed request count

**Why These Matter**: Request queue buildup indicates thread pool starvation or slow processing. High execution times or failures point to application issues.

#### ASP.NET Cache Performance (9 counters)
- **Cache Total Entries** - Items in cache
- **Cache Total Hits** - Successful cache retrievals
- **Cache Total Misses** - Cache misses
- **Cache API Entries** - Items inserted through `HttpRuntime.Cache`
- **Cache API Trims** - API entries evicted under memory pressure
- **Cache Total Trims** - All entries evicted under memory pressure
- **Cache API Turnover Rate** - Additions and removals per second
- **Cache % Machine Memory Limit Used** - Pressure against the machine limit
- **Cache % Process Memory Limit Used** - Pressure against the process limit

**Why These Matter**: Cache hit ratio directly impacts performance. Low hit rates mean more database queries and slower responses.

The "API" counters are the interesting half on an Optimizely site: EPiServer's `HttpRuntimeCache`
inserts through `HttpRuntime.Cache`, so they track the content cache specifically, while the
"Total" counters add ASP.NET's own output and compiled-page caches. A rising trim count means
ASP.NET is evicting to relieve memory pressure, and because EPiServer hangs entries off master
keys, one trim can cascade far beyond the entries ASP.NET actually chose to drop. Read next to
**% Process Memory Limit Used**, that failure mode stops being invisible.

#### CLR Memory (9 counters)
- **# Bytes in all Heaps** - Total managed memory
- **% Time in GC** - Time spent garbage collecting
- **Gen 0/1/2 Collections** - Cumulative collection count per generation
- **Gen 0/1/2 heap size** - Size of each generation
- **Large Object Heap size** - LOH size

**Why These Matter**: High GC time impacts performance. Growing heaps or frequent Gen 2 collections indicate memory pressure or leaks.

#### CLR Threading & Locks (5 counters)
- **# of current logical Threads** - Total managed threads
- **# of current physical Threads** - OS-level threads
- **Contention Rate / sec** - Lock contention frequency
- **Current Queue Length** - Threads waiting for locks
- **Total # of Contentions** - Cumulative contention count

**Why These Matter**: High contention indicates locking issues. Thread count growth may indicate thread leaks.

#### Process Health (4 counters)
- **% Processor Time** - CPU usage for process
- **Private Bytes** - Process memory
- **Thread Count** - Total OS threads
- **Handle Count** - OS handle count

**Why These Matter**: Track overall process health. Handle leaks or excessive threads indicate resource issues.

#### Exception Tracking (1 counter)
- **# of Exceps Thrown / sec** - Exception rate

**Why These Matter**: High exception rates impact performance and indicate code issues.

### V12/V13 (.NET 6+) Counters

#### CPU & Process (2 counters)
- **cpu-usage** - CPU percentage
- **working-set** - Process working set size

#### Memory & Garbage Collection (12 counters)
- **gc-heap-size** - Total managed heap
- **gen-0/1/2-gc-count** - GC counts per generation
- **time-in-gc** - Percentage of time in GC
- **gen-0/1/2-size** - Generation sizes
- **loh-size** - Large Object Heap size
- **poh-size** - Pinned Object Heap size
- **alloc-rate** - Allocation rate
- **gc-fragmentation** - Heap fragmentation

**Why These Matter**: .NET 6+ provides more detailed GC metrics for memory optimization.

#### Threading & Concurrency (5 counters)
- **threadpool-thread-count** - Thread pool size
- **threadpool-queue-length** - Work items queued
- **threadpool-completed-items-count** - Completed work
- **monitor-lock-contention-count** - Lock contention
- **active-timer-count** - Active timers

**Why These Matter**: Thread pool queue growth indicates async bottlenecks or CPU-bound work on thread pool.

#### Application Health (2 counters)
- **exception-count** - Exceptions thrown
- **assembly-count** - Loaded assemblies

**Why These Matter**: Track application stability and assembly loading issues.

#### JIT Compilation (2 counters)
- **il-bytes-jitted** - IL compiled to native code
- **methods-jitted-count** - Methods JIT compiled

**Why These Matter**: Excessive JIT compilation on steady-state indicates tiering or startup issues.

#### ASP.NET Core Requests (4 counters)
- **requests-per-second** - Request throughput
- **total-requests** - Cumulative requests
- **current-requests** - Concurrent requests
- **failed-requests** - Failed request count

**Why These Matter**: Core metrics for request health and throughput.

#### Kestrel Web Server (5 counters)
- **connection-queue-length** - Connections queued
- **request-queue-length** - Requests queued
- **current-connections** - Active connections
- **total-connections** - Cumulative connections
- **current-tls-handshakes** - Active TLS handshakes

**Why These Matter**: Kestrel-specific metrics for connection management and queue depth.

#### HTTP Client (7 counters)
- **requests-started** - Outbound requests initiated
- **requests-started-rate** - Outbound request rate
- **requests-failed** - Failed outbound requests
- **current-requests** - Active outbound requests
- **http11/20/30-connections-current-total** - HTTP version-specific connections

**Why These Matter**: Track outbound API calls and dependencies.

## Setup Instructions

### V11 Setup

Installing the package already put `Optimizely.PerformanceCounters.config` — the counter list
itself — in your project root. The counters do not live in `web.config`; all `web.config` needs is
the three-element wiring that points at that file, which is a one-time edit that survives every
subsequent package upgrade.

1. Open `App_Data/Optimizely.PerformanceCounters/V11/web.config.snippet.xml`
2. Merge its three elements into your `web.config`:
   ```xml
   <configuration>
     <!-- Must be the first element inside <configuration> -->
     <configSections>
       <sectionGroup name="optimizely">
         <section name="performanceCounters"
                  type="Optimizely.Performance.DotNetCounters.Services.PerformanceCountersConfigSection, Optimizely.Performance.DotNetCounters" />
       </sectionGroup>
     </configSections>

     <optimizely>
       <!-- configSource is relative to the site root, which is where the file was added -->
       <performanceCounters configSource="Optimizely.PerformanceCounters.config" />
     </optimizely>

     <applicationInsights>
       <InstrumentationKey>YOUR-KEY-HERE</InstrumentationKey>
     </applicationInsights>
   </configuration>
   ```
3. Edit `Optimizely.PerformanceCounters.config` if you want a different counter list. Read the
   note under [Performance Impact](#performance-impact) first: naming counters replaces the
   built-in defaults rather than adding to them.

**To Disable in Development**:
- Copy `web.Development.config` to your project as `web.Debug.config`
- This transform sets `enabled="false"` when building in Debug mode

### V12/V13 Setup

1. Open `App_Data/Optimizely.PerformanceCounters/V12-V13/appsettings.json`
2. Copy the configuration
3. Merge into your `appsettings.json`:
   ```json
   {
     "ApplicationInsights": {
       "ConnectionString": "InstrumentationKey=YOUR-KEY-HERE"
     },
     "Optimizely": {
       "PerformanceCounters": {
         // Copy from template
       }
     }
   }
   ```

**To Disable in Development**:
- Copy `appsettings.Development.json` to your project root
- Set `ASPNETCORE_ENVIRONMENT=Development` when running locally

## Performance Impact

| Configuration | Counter Count | Overhead | Recommended For |
|--------------|---------------|----------|-----------------|
| V11 built-in defaults | 23 | ~0.5-1% CPU | Sites that configure nothing |
| V11 Complete (template) | 34 | ~0.5-1% CPU | All environments |
| V12/V13 built-in defaults | 23 | ~0.5-1% CPU | Sites that configure nothing |
| V12/V13 Complete (template) | 39 | ~0.5-1% CPU | All environments |

Two counts, because there are two things being counted. The **defaults** are compiled into
`DefaultCounters.cs` and are what a site collects when it names no counters of its own — 23 on
either side of the .NET Framework boundary, deliberately the same coverage. The **templates** are
the files this package copies into the project, and each is a superset of the defaults for its
version: V11 adds eleven (exception rate, cumulative contentions, per-generation heap sizes, CPU
and private bytes, rejected and failed requests, cache hits and misses), and V12/V13 add sixteen
(the whole Kestrel and `System.Net.Http` event sources — twelve between them — plus
`gc-fragmentation`, `poh-size`, `il-bytes-jitted` and `methods-jitted-count`, none of which have a
.NET Framework equivalent).

Superset is the important word. Naming any counter at all *replaces* the defaults rather than
adding to them, so a template that omitted one would silently cost you that counter — which is
exactly what the V11 template used to do with the six cache memory-pressure counters.

**Collection Frequency**: 60 seconds (controlled by Application Insights)

The overhead is minimal and provides comprehensive visibility that far outweighs the small performance cost.

## Customization

While the default configuration is comprehensive, you can customize by:

### Adding Counters

**V11**: Browse available counters using `perfmon.exe` on Windows

**V12/V13**: See [.NET Event Counters](https://docs.microsoft.com/dotnet/core/diagnostics/available-counters)

### Removing Counters

Simply delete counter entries you don't need from the configuration.

### Example: Adding SQL Server Counters (V11)

```xml
<add categoryName="\SQLServer:General Statistics\User Connections"
     reportedName="SQL Server Connections" />
```

## Viewing Metrics

### Application Insights Metrics Explorer

1. Azure Portal → Application Insights
2. Metrics → Custom namespace
3. Select your counter

### Kusto Queries

**All Performance Counters**:
```kusto
customMetrics
| where name startswith "CLR" 
    or name startswith "ASP.NET" 
    or name startswith "Process"
| order by timestamp desc
```

**Memory Usage Over Time**:
```kusto
customMetrics
| where name == "CLR Heap Size"
| summarize avg(value) by bin(timestamp, 5m)
| render timechart
```

**Request Throughput**:
```kusto
customMetrics
| where name == "ASP.NET Requests/Sec"
| summarize avg(value), max(value) by bin(timestamp, 1m)
| render timechart
```

**GC Pressure**:
```kusto
customMetrics
| where name == "CLR % Time in GC"
| summarize avg(value) by bin(timestamp, 5m)
| render timechart
```

**Thread Pool Health (V12/V13)**:
```kusto
customMetrics
| where name in ("threadpool-thread-count", "threadpool-queue-length")
| summarize avg(value) by name, bin(timestamp, 1m)
| render timechart
```

## Recommended Alerts

Set up Application Insights alerts for critical thresholds:

### V11 Alerts

| Metric | Warning | Critical |
|--------|---------|----------|
| ASP.NET Requests Queued | 25 | 50 |
| CLR % Time in GC | 15% | 20% |
| CLR Contention Rate | 50/sec | 100/sec |
| Process CPU % | 70% | 85% |
| CLR Heap Size | 1.5 GB | 2 GB |

### V12/V13 Alerts

| Metric | Warning | Critical |
|--------|---------|----------|
| cpu-usage | 60% | 80% |
| time-in-gc | 10% | 15% |
| threadpool-queue-length | 50 | 100 |
| failed-requests (rate) | 5/min | 20/min |
| monitor-lock-contention-count | 100 | 500 |

## Troubleshooting

### Counters Not Appearing

1. Wait 2-3 minutes for initial data
2. Verify Application Insights key is correct
3. Check `Enabled: true` in configuration
4. Review Debug output for errors

### High Overhead

If you experience performance issues:
1. Verify collection interval (default 60s)
2. Reduce counter count if needed (not typically necessary)
3. Contact support if overhead exceeds 1%

### Counter Values Seem Wrong

**V11**: Use `perfmon.exe` to verify counter values directly  
**V12/V13**: Use `dotnet-counters monitor --process-id <pid>` to verify

## Best Practices

1. ✅ **Use Complete Configuration**: All counters provide value
2. ✅ **Set Up Alerts**: Don't just collect - act on metrics
3. ✅ **Create Dashboards**: Build Application Insights workbooks
4. ✅ **Review Weekly**: Establish baselines and trends
5. ✅ **Disable in Dev**: Use transforms/overrides locally
6. ✅ **Commit Configuration**: Version control your setup

## Support

For questions or issues:
- GitHub: https://github.com/optimizely/performance-counters
- Application Insights docs: https://docs.microsoft.com/azure/azure-monitor/app/

---

**Last Updated**: 2026-07-30  
**Version**: 1.0.0
