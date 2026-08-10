# Configuration Templates Summary

## Overview

The Optimizely.Performance.DotNetCounters NuGet package now includes comprehensive configuration templates that are **automatically added** to consuming projects upon installation.

## What's Included

### Configuration Templates

#### Optimizely V11 (.NET Framework 4.7.2)

Located in: `App_Data/Optimizely.PerformanceCounters/V11/`

| File | Counters | Purpose |
|------|----------|---------|
| `web.config.production` | 12 | **Recommended** for production - balanced monitoring |
| `web.config.development` | 3 | Minimal overhead for local development |
| `web.config.troubleshooting` | 26 | Comprehensive diagnostics (temporary use) |

**Metrics Collected**:
- ASP.NET request metrics (queue, throughput, timing, failures)
- CLR memory (heap size, GC pressure, generations, LOH)
- CLR threading (thread counts, contention rates, queue length)
- Process metrics (CPU, memory, threads, handles)
- Cache performance (troubleshooting only)
- Exception rates (troubleshooting only)

#### Optimizely V12/V13 (.NET 6+)

Located in: `App_Data/Optimizely.PerformanceCounters/V12-V13/`

| File | Counters | Purpose |
|------|----------|---------|
| `appsettings.Production.json` | 12 | **Recommended** for production - balanced monitoring |
| `appsettings.Development.json` | 3 | Minimal overhead for local development |
| `appsettings.Troubleshooting.json` | 39 | Complete diagnostics including Kestrel & HTTP client |
| `appsettings.MemoryDiagnostics.json` | 13 | Focus on memory leaks and GC issues |
| `appsettings.ThreadingDiagnostics.json` | 5 | Focus on thread pool starvation and contention |

**Metrics Collected**:
- System.Runtime (CPU, memory, GC, threading, allocations, JIT)
- ASP.NET Core Hosting (requests, failures)
- Kestrel Server (connections, queues, TLS - troubleshooting only)
- HTTP Client (outbound requests, connections - troubleshooting only)

### Documentation

| File | Purpose |
|------|---------|
| `CONFIGURATION_PRESETS_GUIDE.md` | Complete guide to all presets and usage |
| `README_AFTER_INSTALL.md` | Quick start guide after installation |

### Build Integration

| File | Purpose |
|------|---------|
| `build/Optimizely.Performance.DotNetCounters.targets` | MSBuild integration, post-build messages |
| `tools/install.ps1` | PowerShell script for packages.config installations |

## How It Works

### Installation Flow

```
1. Developer installs NuGet package
   ↓
2. NuGet adds configuration templates to:
   App_Data/Optimizely.PerformanceCounters/
   ↓
3. MSBuild displays guidance message
   ↓
4. Developer selects preset based on:
   - Optimizely version (V11 / V12 / V13)
   - Environment (Production / Development)
   - Diagnostic needs (Memory / Threading)
   ↓
5. Developer copies configuration:
   - V11: Copy section to web.config
   - V12/V13: Copy file as appsettings.{Environment}.json
   ↓
6. Developer configures Application Insights key
   ↓
7. Library auto-initializes on app startup
   ↓
8. Metrics flow to Application Insights
```

### Key Design Principles

1. **Non-Intrusive**: Templates don't auto-configure (avoids breaking existing setups)
2. **Side-by-Side**: All presets available simultaneously for easy comparison
3. **Additive**: Templates added to App_Data/ (won't be deployed)
4. **Self-Service**: Developers choose and copy what they need
5. **Version-Specific**: Clear separation between V11 and V12/V13
6. **Scenario-Specific**: Presets for common scenarios (prod, dev, diagnostics)

## Usage Examples

### Example 1: New V13 Production Deployment

```bash
# 1. Install package
dotnet add package Optimizely.Performance.DotNetCounters

# 2. Copy production preset
cp App_Data/Optimizely.PerformanceCounters/V12-V13/appsettings.Production.json \
   appsettings.Production.json

# 3. Edit and add your Application Insights connection string
code appsettings.Production.json

# 4. Deploy with ASPNETCORE_ENVIRONMENT=Production
```

### Example 2: V11 Development Setup

```xml
<!-- 1. Install package via NuGet Package Manager -->

<!-- 2. Open App_Data/Optimizely.PerformanceCounters/V11/web.config.development -->

<!-- 3. Copy the <optimizely> section -->

<!-- 4. Paste into your web.config: -->
<configuration>
  <optimizely>
    <performanceCounters enabled="true">
      <counters>
        <add categoryName="\ASP.NET\Requests Queued" reportedName="ASP.NET Requests Queued" />
        <add categoryName="\.NET CLR Memory(_Global_)\# Bytes in all Heaps" reportedName="CLR Heap Size" />
        <add categoryName="\.NET CLR Memory(_Global_)\% Time in GC" reportedName="CLR % Time in GC" />
      </counters>
    </performanceCounters>
  </optimizely>
</configuration>

<!-- 5. Add Application Insights configuration -->
```

### Example 3: Investigating Memory Leak (V12/V13)

```bash
# 1. Already have package installed

# 2. Copy memory diagnostics preset
cp App_Data/Optimizely.PerformanceCounters/V12-V13/appsettings.MemoryDiagnostics.json \
   appsettings.Staging.json

# 3. Deploy to staging
# 4. Monitor memory metrics in Application Insights
# 5. Once diagnosed, switch back to Production preset
```

## File Inventory

### V11 Configuration Templates (3 files)

```
examples/V11/
├── web.config.production         (12 counters)
├── web.config.development        (3 counters)
└── web.config.troubleshooting    (26 counters)
```

### V12/V13 Configuration Templates (5 files)

```
examples/V12-V13/
├── appsettings.Production.json          (12 counters)
├── appsettings.Development.json         (3 counters)
├── appsettings.Troubleshooting.json     (39 counters)
├── appsettings.MemoryDiagnostics.json   (13 counters)
└── appsettings.ThreadingDiagnostics.json (5 counters)
```

### Legacy Examples (2 files - for documentation only)

```
examples/
├── web.config.example            (Full V11 example with comments)
└── appsettings.json.example      (Full V12/V13 example with comments)
```

### Documentation (2 files)

```
examples/
├── README_AFTER_INSTALL.md       (Quick start guide)
└── (reference to) CONFIGURATION_PRESETS_GUIDE.md
```

### Build Files (2 files)

```
build/
├── Optimizely.Performance.DotNetCounters.targets (MSBuild integration)
└── install.ps1                                    (PowerShell script)
```

## NuGet Package Structure

When packed, the package includes:

```
Optimizely.Performance.DotNetCounters.1.0.0.nupkg
├── lib/
│   ├── net472/*.dll
│   ├── net6.0/*.dll
│   └── net8.0/*.dll
├── content/App_Data/Optimizely.PerformanceCounters/
│   ├── V11/ (3 templates)
│   ├── V12-V13/ (5 templates)
│   ├── CONFIGURATION_PRESETS_GUIDE.md
│   └── README_AFTER_INSTALL.md
├── contentFiles/any/any/App_Data/... (same as content/)
├── build/Optimizely.Performance.DotNetCounters.targets
└── tools/install.ps1
```

## Benefits for End Users

1. **Zero Research Required**: All common scenarios pre-configured
2. **Best Practices Built-In**: Recommended counters based on Microsoft guidance
3. **Environment-Specific**: Easy to use different configs per environment
4. **Copy-Paste Ready**: No need to look up counter names
5. **Documented**: Each preset explains what it monitors and when to use it
6. **Troubleshooting Paths**: Specialized presets for common issues
7. **Version-Aware**: Separate configs for V11 vs V12/V13
8. **No Deployment Bloat**: Templates stay in App_Data/, never deployed

## Preset Selection Guide

| Scenario | Optimizely Version | Preset to Use |
|----------|-------------------|---------------|
| Production monitoring | V11 | `web.config.production` |
| Production monitoring | V12/V13 | `appsettings.Production.json` |
| Local development | V11 | `web.config.development` |
| Local development | V12/V13 | `appsettings.Development.json` |
| Performance issue | V11 | `web.config.troubleshooting` |
| Performance issue | V12/V13 | `appsettings.Troubleshooting.json` |
| Memory leak | V12/V13 | `appsettings.MemoryDiagnostics.json` |
| Thread starvation | V12/V13 | `appsettings.ThreadingDiagnostics.json` |

## Maintenance and Updates

### Updating Templates in Future Versions

When releasing new versions of the package with updated templates:

1. **Version the templates**: Include version in filename or folder
2. **Provide migration guide**: Document changes between versions
3. **Don't break existing configs**: Keep backward compatibility
4. **Announce in release notes**: Highlight template changes

### User Customization

Users can customize templates by:
- Adding new counters from Microsoft documentation
- Removing counters they don't need
- Mixing counters from multiple presets
- Creating their own custom presets based on provided examples

### Source Control

**Recommendations**:
- ✅ Commit templates to source control (useful reference for team)
- ✅ Commit actual configuration (web.config, appsettings.json)
- ✅ Use .gitignore for sensitive keys (use environment variables instead)

## Statistics

| Metric | Count |
|--------|-------|
| **Total Configuration Templates** | 10 |
| V11 Templates | 3 |
| V12/V13 Templates | 5 |
| Legacy Examples | 2 |
| **Total Presets Covered** | 5 |
| Production | 1 |
| Development | 1 |
| Troubleshooting | 1 |
| Memory Diagnostics | 1 |
| Threading Diagnostics | 1 |
| **Total Counters Defined** | 100+ |
| V11 Unique Counters | 26 |
| V12/V13 Unique Counters | 39 |
| **Documentation Files** | 2 |
| **Build Integration Files** | 2 |

## Next Steps

After implementing these configuration templates:

1. ✅ Build NuGet package: `dotnet pack`
2. ✅ Test installation in sample project
3. ✅ Verify templates appear in App_Data/
4. ✅ Test each preset
5. ✅ Publish to NuGet.org
6. ✅ Update main README with template information
7. ✅ Create release notes highlighting templates

---

**Implementation Date**: 2026-07-30  
**Version**: 1.0.0  
**Status**: ✅ Complete - Ready for packaging and distribution
