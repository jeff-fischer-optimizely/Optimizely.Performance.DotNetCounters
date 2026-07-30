# Architecture Diagram

This document provides visual representations of the Optimizely.Performance.DotNetCounters architecture.

## Component Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Optimizely CMS Application                  │
│                                                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │         Optimizely Framework (IConfigurableModule)         │ │
│  │                                                              │ │
│  │    ┌──────────────────────────────────────────────────┐   │ │
│  │    │  PerformanceCountersInitializationModule         │   │ │
│  │    │                                                    │   │ │
│  │    │  ┌─────────────────┐   ┌────────────────────┐   │   │ │
│  │    │  │   NET472?       │   │   !NET472?         │   │   │ │
│  │    │  └────────┬────────┘   └─────────┬──────────┘   │   │ │
│  │    │           │                       │               │   │ │
│  │    └───────────┼───────────────────────┼──────────────┘   │ │
│  │                │                       │                   │ │
│  └────────────────┼───────────────────────┼──────────────────┘ │
│                   │                       │                     │
│                   ▼                       ▼                     │
│      ┌────────────────────────┐  ┌──────────────────────────┐ │
│      │ PerformanceCounter     │  │ EventCounterService      │ │
│      │ Service (.NET Fx)      │  │ Extensions (.NET Core+)  │ │
│      │                        │  │                          │ │
│      │ • web.config           │  │ • appsettings.json       │ │
│      │ • IConfiguration       │  │ • IServiceCollection     │ │
│      │ • DefaultCounters      │  │ • DefaultCounters        │ │
│      └───────────┬────────────┘  └──────────┬───────────────┘ │
│                  │                           │                  │
│                  ▼                           ▼                  │
│      ┌────────────────────────┐  ┌──────────────────────────┐ │
│      │ PerformanceCollector   │  │ EventCounterCollection   │ │
│      │ Module (AI)            │  │ Module (AI)              │ │
│      └───────────┬────────────┘  └──────────┬───────────────┘ │
│                  │                           │                  │
└──────────────────┼───────────────────────────┼──────────────────┘
                   │                           │
                   │        Application        │
                   │         Insights          │
                   │                           │
                   └───────────┬───────────────┘
                               │
                               ▼
                   ┌───────────────────────┐
                   │  Azure Application    │
                   │  Insights             │
                   │                       │
                   │  • Metrics Storage    │
                   │  • Dashboards         │
                   │  • Alerts             │
                   │  • Queries (KQL)      │
                   └───────────────────────┘
```

## Multi-Targeting Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                Single .csproj File                                │
│  <TargetFrameworks>net472;net6.0;net8.0</TargetFrameworks>      │
└─────────────┬───────────────┬──────────────┬─────────────────────┘
              │               │              │
              ▼               ▼              ▼
   ┌──────────────┐ ┌─────────────┐ ┌──────────────┐
   │  net472      │ │   net6.0    │ │   net8.0     │
   │              │ │             │ │              │
   │ EPiServer    │ │ EPiServer   │ │ EPiServer    │
   │ CMS Core 11  │ │ CMS Core 12 │ │ Cms Core 13  │
   │              │ │             │ │              │
   │ AI Perf      │ │ AI AspNet   │ │ AI AspNet    │
   │ Counter      │ │ Core        │ │ Core         │
   │ Collector    │ │             │ │              │
   └──────┬───────┘ └──────┬──────┘ └──────┬───────┘
          │                │               │
          ▼                ▼               ▼
   ┌──────────────┐ ┌─────────────┐ ┌──────────────┐
   │ Windows      │ │ Event       │ │ Event        │
   │ Performance  │ │ Counters    │ │ Counters     │
   │ Counters     │ │             │ │              │
   └──────────────┘ └─────────────┘ └──────────────┘
```

## Configuration Flow

### .NET Framework 4.7.2

```
┌──────────────┐
│  web.config  │
│              │
│ <optimizely> │
│   <perf...>  │
└──────┬───────┘
       │
       ▼
┌────────────────────────────────┐
│ PerformanceCountersConfigSection│
│ (ConfigurationSection)         │
└──────┬─────────────────────────┘
       │
       ▼
┌──────────────────────┐
│ PerformanceCounter   │         ┌─────────────────┐
│ Options              │◄────────┤ DefaultCounters │
│                      │         │ (if not config) │
└──────┬───────────────┘         └─────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ PerformanceCollectorModule.Counters  │
│ .Add(new PerformanceCounter...)      │
└──────────────────────────────────────┘
```

### .NET 6+

```
┌─────────────────────┐
│  appsettings.json   │
│                     │
│ "Optimizely": {     │
│   "Performance...   │
└──────┬──────────────┘
       │
       ▼
┌────────────────────────────────┐
│ IConfiguration.Bind()          │
│ (PerformanceCounterOptions)    │
└──────┬─────────────────────────┘
       │
       ▼
┌──────────────────────┐
│ PerformanceCounter   │         ┌─────────────────┐
│ Options              │◄────────┤ DefaultCounters │
│                      │         │ (if not config) │
└──────┬───────────────┘         └─────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│ EventCounterCollectionModule.Counters│
│ .Add(new EventCounterCollection...)  │
└──────────────────────────────────────┘
```

## Data Flow

```
┌──────────────────────────────────────────────────────────────┐
│                    Application Runtime                        │
│                                                                │
│  Windows Perf Counters (NET472)  │  Event Counters (NET6+)   │
│  ────────────────────────────────┼─────────────────────────  │
│  • ASP.NET\Requests Queued       │  • System.Runtime         │
│  • CLR Memory\Heap Size          │    - cpu-usage            │
│  • CLR Memory\% Time in GC       │    - gc-heap-size         │
│  • CLR Threads\Contention Rate   │    - threadpool-*         │
│  • Process\Thread Count          │  • AspNetCore.Hosting     │
│                                   │    - requests-per-second  │
└─────────────┬────────────────────┴────────────┬──────────────┘
              │                                 │
              │        Sampled every 60s        │
              │                                 │
              ▼                                 ▼
   ┌────────────────────────────────────────────────────┐
   │    Application Insights Telemetry Channel         │
   │                                                     │
   │  • Batching                                        │
   │  • Compression                                     │
   │  • Retry logic                                     │
   └─────────────────────┬──────────────────────────────┘
                         │
                         │ HTTPS
                         │
                         ▼
            ┌────────────────────────┐
            │  Azure Application     │
            │  Insights              │
            │                        │
            │  customMetrics table   │
            └────────┬───────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
         ▼           ▼           ▼
    ┌────────┐  ┌────────┐  ┌────────┐
    │Metrics │  │Alerts  │  │Dashbrd │
    │Explorer│  │        │  │        │
    └────────┘  └────────┘  └────────┘
```

## Deployment Scenarios

### Single Instance Deployment

```
┌──────────────────────────────────┐
│     Azure App Service            │
│  ┌────────────────────────────┐  │
│  │   Optimizely CMS           │  │
│  │   + Perf Counters          │  │
│  │                            │  │
│  │   Instance: myapp-web-01   │  │
│  └──────────────┬─────────────┘  │
└─────────────────┼────────────────┘
                  │
                  ├─ Metrics
                  ▼
         ┌────────────────┐
         │ App Insights   │
         │                │
         │ Role Instance: │
         │ myapp-web-01   │
         └────────────────┘
```

### Multi-Instance Scale-Out

```
┌───────────────────────────────────────────────────────┐
│              Azure App Service (Scale-out)            │
│                                                        │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐│
│  │ Instance 01  │  │ Instance 02  │  │ Instance 03 ││
│  │ Optimizely   │  │ Optimizely   │  │ Optimizely  ││
│  │ + Perf Ctrs  │  │ + Perf Ctrs  │  │ + Perf Ctrs ││
│  └──────┬───────┘  └──────┬───────┘  └──────┬──────┘│
└─────────┼──────────────────┼──────────────────┼───────┘
          │                  │                  │
          │ All send metrics │                  │
          ├──────────────────┼──────────────────┤
          ▼                  ▼                  ▼
       ┌──────────────────────────────────────────┐
       │     Application Insights (Shared)        │
       │                                          │
       │  Metrics grouped by cloud_RoleInstance  │
       │  • myapp-01                              │
       │  • myapp-02                              │
       │  • myapp-03                              │
       └──────────────────────────────────────────┘
```

## Conditional Compilation

```
Source Code (.cs files)
│
├─ #if NET472
│  │
│  ├─ Using System.Configuration
│  ├─ WindowsPerformanceCounter class
│  ├─ PerformanceCounterService
│  └─ web.config configuration
│
├─ #if !NET472 (NET6, NET8)
│  │
│  ├─ Using Microsoft.Extensions.*
│  ├─ EventCounterDefinition class
│  ├─ EventCounterService extensions
│  └─ appsettings.json configuration
│
└─ Shared code (no #if)
   ├─ PerformanceCounterOptions
   ├─ IConfigurableModule implementation
   └─ Common initialization logic

                    ▼ Compile Time

┌─────────────┬─────────────┬─────────────┐
│   net472    │   net6.0    │   net8.0    │
│    .dll     │    .dll     │    .dll     │
├─────────────┼─────────────┼─────────────┤
│ Windows     │ Event       │ Event       │
│ Perf Ctrs   │ Counters    │ Counters    │
│             │             │             │
│ 100 KB      │ 85 KB       │ 85 KB       │
└─────────────┴─────────────┴─────────────┘
```

## Class Diagram

```
┌───────────────────────────────────────────────────────┐
│         PerformanceCounterOptions                      │
├───────────────────────────────────────────────────────┤
│ + Enabled: bool                                        │
│ + WindowsCounters: List<WindowsPerformanceCounter>    │  (#if NET472)
│ + EventCounters: List<EventCounterDefinition>         │  (#if !NET472)
└───────────────────────────────────────────────────────┘
                        △
                        │
        ┌───────────────┴────────────────┐
        │                                │
┌───────────────────┐         ┌──────────────────────┐
│ DefaultCounters   │         │  Configuration       │
│ (static)          │         │  Binding             │
├───────────────────┤         └──────────────────────┘
│ + GetDefault...() │
└───────────────────┘

┌─────────────────────────────────────────────────────┐
│  PerformanceCountersInitializationModule            │
│  : IConfigurableModule                              │
├─────────────────────────────────────────────────────┤
│ + Initialize(InitializationEngine)                  │
│ + ConfigureContainer(ServiceConfigurationContext)   │
│ + Uninitialize(InitializationEngine)                │
└──────────────┬──────────────────────────────────────┘
               │
               │ uses
               │
   ┌───────────┴───────────┐
   │                       │
   ▼                       ▼
┌────────────────┐   ┌─────────────────────┐
│ PerformanceCounter  │   EventCounterService    │
│ Service         │   │ Extensions           │
│ (NET472)        │   │ (!NET472)            │
├────────────────┤   ├─────────────────────┤
│ + Initialize() │   │ + AddOptimizely...() │
└────────────────┘   └─────────────────────┘
```

## Technology Stack

```
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│                                                               │
│   Optimizely CMS V11 | V12 | V13                            │
└─────────────────────────────────────────────────────────────┘
                             │
┌─────────────────────────────────────────────────────────────┐
│              Performance Counters Library                    │
│                                                               │
│   • Multi-targeting                                          │
│   • Conditional compilation                                  │
│   • Configuration abstraction                                │
└─────────────────────────────────────────────────────────────┘
                             │
┌─────────────────────────────────────────────────────────────┐
│                 Application Insights SDK                     │
│                                                               │
│  NET472: AI.PerfCounterCollector                            │
│  NET6+:  AI.AspNetCore                                      │
└─────────────────────────────────────────────────────────────┘
                             │
┌─────────────────────────────────────────────────────────────┐
│                   Platform Layer                             │
│                                                               │
│  NET472: Windows Performance Counters API                   │
│  NET6+:  EventSource / EventCounter API                     │
└─────────────────────────────────────────────────────────────┘
```

---

This architecture supports the core principle of **"Write once, compile many"** through:

1. **Conditional compilation** - Platform-specific code isolated with `#if` directives
2. **Configuration abstraction** - Unified options model with platform-specific implementations
3. **Single module** - One initialization point supporting all platforms
4. **Default fallback** - Sensible defaults requiring zero configuration
5. **Graceful degradation** - Errors don't prevent startup

The result is a maintainable, testable, and extensible solution that works seamlessly across all supported Optimizely versions and .NET platforms.
