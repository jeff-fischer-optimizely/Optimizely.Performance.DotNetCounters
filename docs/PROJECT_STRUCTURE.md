# Project Structure

This document explains the architecture and organization of the Optimizely Performance Counters library.

## Directory Structure

```
OptiDotNetCounters/
├── src/
│   └── Optimizely.Performance.DotNetCounters/
│       ├── Configuration/
│       │   ├── PerformanceCounterOptions.cs    # Configuration models
│       │   └── DefaultCounters.cs              # Default counter definitions
│       ├── Services/
│       │   ├── PerformanceCounterService.cs    # .NET Framework service
│       │   └── EventCounterService.cs          # .NET Core+ service
│       ├── Initialization/
│       │   └── PerformanceCountersInitializationModule.cs  # Optimizely module
│       └── Optimizely.Performance.DotNetCounters.csproj
├── examples/
│   ├── web.config.example          # .NET Framework configuration example
│   └── appsettings.json.example    # .NET Core+ configuration example
├── Optimizely.Performance.DotNetCounters.sln
├── README.md
└── PROJECT_STRUCTURE.md
```

## Multi-Targeting Strategy

The project uses a single `.csproj` file with multi-targeting to support all platforms:

```xml
<TargetFrameworks>net472;net6.0;net8.0;net9.0;net10.0</TargetFrameworks>
```

### Conditional Compilation

The codebase uses `#if` directives to compile platform-specific code:

- `#if NET472` - .NET Framework 4.7.2 specific code (Windows Performance Counters)
- `#if !NET472` - .NET Core and later code (Event Counters)

This approach ensures:
1. Single codebase for all platforms
2. No runtime overhead from unused code
3. Type-safe access to platform-specific APIs

## Component Architecture

### Configuration Layer

**PerformanceCounterOptions.cs**
- Defines the configuration model
- Uses conditional compilation to expose different properties per platform
- .NET Framework: `WindowsPerformanceCounter` collection
- .NET Core+: `EventCounterDefinition` collection

**DefaultCounters.cs**
- Provides sensible defaults for each platform
- .NET Framework: Windows Performance Counters (ASP.NET, CLR, Process)
- .NET Core+: Event Counters (System.Runtime, ASP.NET Core)

### Service Layer

**PerformanceCounterService.cs** (.NET Framework only)
- Initializes `PerformanceCollectorModule` from Application Insights
- Reads configuration from web.config or IConfiguration
- Includes web.config ConfigurationSection handlers

**EventCounterService.cs** (.NET Core+ only)
- Extension method for `IServiceCollection`
- Configures `EventCounterCollectionModule` from Application Insights
- Reads configuration from appsettings.json via IConfiguration

### Initialization Layer

**PerformanceCountersInitializationModule.cs**
- Implements `IConfigurableModule` for Optimizely auto-registration
- Uses conditional compilation to initialize the correct service
- .NET Framework: Calls `Initialize()` in `Initialize()` method
- .NET Core+: Calls `ConfigureContainer()` to register services

## Version Matrix

| Component | .NET Framework 4.7.2 | .NET 6 | .NET 8/9/10 |
|-----------|---------------------|---------|-------------|
| **Optimizely Version** | V11 | V12 | V13 |
| **Counter Type** | Windows Performance Counters | Event Counters | Event Counters |
| **Configuration** | web.config | appsettings.json | appsettings.json |
| **AI Package** | Microsoft.ApplicationInsights.PerfCounterCollector | Microsoft.ApplicationInsights.AspNetCore | Microsoft.ApplicationInsights.AspNetCore |
| **Initialization** | IInitializableModule | IConfigurableModule | IConfigurableModule |

## Build Process

The project uses the modern SDK-style project format which automatically handles:
- Multi-targeting compilation
- NuGet package resolution per framework
- Reference assemblies for each target

To build all targets:

```bash
dotnet build
```

To build a specific target:

```bash
dotnet build -f net472
dotnet build -f net6.0
dotnet build -f net8.0
```

## Package References

### .NET Framework 4.7.2
- EPiServer.CMS.Core 11.20.10 (Optimizely V11)
- EPiServer.Framework 11.20.10
- Microsoft.ApplicationInsights 2.22.0
- Microsoft.ApplicationInsights.PerfCounterCollector 2.22.0

### .NET 6
- EPiServer.CMS.Core 12.31.0 (Optimizely V12)
- EPiServer.Framework 12.31.0
- Microsoft.ApplicationInsights.AspNetCore 2.22.0

### .NET 8/9/10
- EPiServer.CMS.Core 13.0.0 (Optimizely V13)
- EPiServer.Framework 13.0.0
- Microsoft.ApplicationInsights.AspNetCore 2.22.0

## Configuration Flow

### .NET Framework 4.7.2

1. Application starts
2. Optimizely calls `ConfigureContainer()` on `PerformanceCountersInitializationModule`
3. Module creates `PerformanceCounterService`
4. Service reads configuration from web.config or IConfiguration
5. Service adds counters to `PerformanceCollectorModule`
6. Service calls `module.Initialize(TelemetryConfiguration.Active)`
7. Counters begin reporting to Application Insights

### .NET 6+

1. Application starts
2. Optimizely calls `ConfigureContainer()` on `PerformanceCountersInitializationModule`
3. Module calls `services.AddOptimizelyPerformanceCounters(configuration)`
4. Extension method configures `EventCounterCollectionModule`
5. Extension method reads configuration and adds event counters
6. Application Insights begins collecting metrics

## Extensibility

### Adding Custom Counters

Users can extend the default counters by:

1. **Via Configuration** - Add counters in web.config or appsettings.json
2. **Programmatically** - Create a custom initialization module that runs after this one

### Supporting New .NET Versions

To add support for a new .NET version:

1. Add the target framework to `<TargetFrameworks>` in the .csproj
2. Add appropriate Optimizely package references for that version
3. Update conditional compilation if platform APIs change

## Error Handling

The library is designed to be resilient:
- Individual counter failures don't prevent initialization
- Configuration errors fall back to defaults
- All exceptions are caught and logged to Debug output
- The library never throws exceptions that would break application startup

## Testing Strategy

To test this library:

1. **Unit Tests** - Mock IConfiguration and verify counter registration
2. **Integration Tests** - Deploy to actual Optimizely instances and verify metrics appear in Application Insights
3. **Manual Testing** - Use Performance Monitor (Windows) or dotnet-counters (.NET Core+) to verify counter collection

## Performance Characteristics

- **Startup Impact**: Minimal (<100ms typically)
- **Runtime Impact**: Negligible (counters collected on background thread)
- **Collection Frequency**: 60 seconds (default Application Insights interval)
- **Memory Overhead**: ~1-2 MB per process

## Future Enhancements

Potential areas for expansion:

1. Support for custom Application Insights TelemetryConfiguration
2. Dynamic counter management (add/remove at runtime)
3. Counter filtering based on environment (dev/staging/production)
4. Integration with other monitoring systems (Prometheus, DataDog, etc.)
5. Health check endpoint to verify counter collection
