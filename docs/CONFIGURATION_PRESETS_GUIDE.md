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
| `web.config.xml` | Complete configuration for all environments | 28 counters |
| `web.Development.config` | Transform to disable counters in development | Transform only |

### Optimizely V12/V13 (.NET 6+)

Located in: `App_Data/Optimizely.PerformanceCounters/V12-V13/`

| File | Purpose | Counters |
|------|---------|----------|
| `appsettings.json` | Complete configuration for all environments | 41 counters |
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

#### ASP.NET Cache Performance (3 counters)
- **Cache Total Entries** - Items in cache
- **Cache Total Hits** - Successful cache retrievals
- **Cache Total Misses** - Cache misses

**Why These Matter**: Cache hit ratio directly impacts performance. Low hit rates mean more database queries and slower responses.

#### CLR Memory (9 counters)
- **# Bytes in all Heaps** - Total managed memory
- **% Time in GC** - Time spent garbage collecting
- **Gen 0/1/2 Collections/sec** - Collection frequency per generation
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

1. Open `App_Data/Optimizely.PerformanceCounters/V11/web.config.xml`
2. Copy the entire configuration
3. Merge into your `web.config`:
   ```xml
   <configuration>
     <configSections>
       <!-- Copy configSections entries -->
     </configSections>
     
     <optimizely>
       <!-- Copy optimizely section -->
     </optimizely>
     
     <applicationInsights>
       <!-- Update with your key -->
       <InstrumentationKey>YOUR-KEY-HERE</InstrumentationKey>
     </applicationInsights>
   </configuration>
   ```

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
| V11 Complete | 28 | ~0.5-1% CPU | All environments |
| V12/V13 Complete | 41 | ~0.5-1% CPU | All environments |

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
