# Streamlined Configuration with configSource

## Overview

The Optimizely.Performance.DotNetCounters package now uses **configSource** for .NET Framework (V11) configuration, providing a streamlined installation experience.

## Architecture

### Traditional Approach (Before)
```
User installs package
  ↓
User opens template file
  ↓
User copies 100+ lines of XML
  ↓
User pastes into web.config
  ↓
User replaces AI key
  ↓
Prone to errors, messy web.config
```

### New Approach (After - Using configSource)
```
User installs package
  ↓
Optimizely.PerformanceCounters.config auto-added to project
  ↓
User adds 2 small sections to web.config (one-time)
  ↓
User replaces AI key
  ↓
Clean, maintainable, updateable
```

## What Gets Installed

### V11 (.NET Framework 4.7.2)

**In Project Root** (automatically added by NuGet):
- `Optimizely.PerformanceCounters.config` - **Main configuration file** with all 34 counters

**In App_Data/Optimizely.PerformanceCounters/V11/** (reference/docs):
- `web.config.snippet.xml` - Copy-paste reference for web.config setup
- `web.Development.config` - Transform to disable counters in development

### V12/V13 (.NET 6+)

**In App_Data/Optimizely.PerformanceCounters/V12-V13/**:
- `appsettings.json` - Complete configuration with all 39 counters
- `appsettings.Development.json` - Override to disable in development

## User Experience

### V11 Setup (3 Steps)

**Step 1: Add configSection** (one line in `<configSections>`)
```xml
<sectionGroup name="optimizely">
  <section name="performanceCounters"
           type="Optimizely.Performance.DotNetCounters.Services.PerformanceCountersConfigSection, Optimizely.Performance.DotNetCounters" />
</sectionGroup>
```

**Step 2: Reference external config** (one line in `<configuration>`)
```xml
<optimizely>
  <performanceCounters configSource="Optimizely.PerformanceCounters.config" />
</optimizely>
```

**Step 3: Configure Application Insights**
```xml
<applicationInsights>
  <InstrumentationKey>YOUR-KEY-HERE</InstrumentationKey>
</applicationInsights>
```

**Total lines added to web.config: ~10 lines (vs ~100+ before)**

### V12/V13 Setup (2 Steps)

**Step 1: Merge appsettings.json** (copy from template)

**Step 2: Configure Application Insights**

## Benefits of configSource Approach

### 1. **Cleaner web.config**
- web.config stays minimal and readable
- Counter definitions don't clutter main config
- Easier to see what's configured at a glance

### 2. **Easier Updates**
When v2.0 of the package is released:
- User just updates `Optimizely.PerformanceCounters.config`
- No need to touch web.config
- Clear diff showing what changed

### 3. **Version Control Friendly**
```bash
# Clear diff when counters change
git diff Optimizely.PerformanceCounters.config

# vs having 100 lines changed in web.config
```

### 4. **Reusable Across Projects**
- Copy `Optimizely.PerformanceCounters.config` to other projects
- Standard configuration across all environments
- Easy to share within organization

### 5. **Environment-Specific Overrides**
```xml
<!-- In web.Debug.config -->
<performanceCounters enabled="false" xdt:Transform="Replace">
  <counters />
</performanceCounters>
```

### 6. **Easier Customization**
Users can:
- Edit `Optimizely.PerformanceCounters.config` directly
- Add/remove counters without touching web.config
- See all counter definitions in one dedicated file

## File Structure

### V11 Configuration Files

```
YourProject/
├── web.config                                      ← Minimal changes (3 sections)
├── Optimizely.PerformanceCounters.config          ← Auto-installed, contains all counters
├── web.Debug.config                               ← Optional: disable in dev
└── App_Data/
    └── Optimizely.PerformanceCounters/
        └── V11/
            ├── web.config.snippet.xml             ← Reference: what to add to web.config
            └── web.Development.config             ← Reference: transform example
```

### Complete web.config Example

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  
  <!-- STEP 1: Add section definition -->
  <configSections>
    <sectionGroup name="optimizely">
      <section name="performanceCounters"
               type="Optimizely.Performance.DotNetCounters.Services.PerformanceCountersConfigSection, Optimizely.Performance.DotNetCounters" />
    </sectionGroup>
  </configSections>

  <!-- STEP 2: Reference external config (1 line!) -->
  <optimizely>
    <performanceCounters configSource="Optimizely.PerformanceCounters.config" />
  </optimizely>

  <!-- STEP 3: Application Insights -->
  <applicationInsights>
    <InstrumentationKey>abc123-def456-...</InstrumentationKey>
  </applicationInsights>

  <!-- Rest of your existing web.config -->
  <appSettings>...</appSettings>
  <connectionStrings>...</connectionStrings>
  <system.web>...</system.web>
  
</configuration>
```

**That's it!** All 34 counter definitions live in the external file.

## NuGet Package Structure

```
Optimizely.Performance.DotNetCounters.1.0.0.nupkg
├── lib/
│   ├── net472/Optimizely.Performance.DotNetCounters.dll
│   ├── net6.0/Optimizely.Performance.DotNetCounters.dll
│   └── net8.0/Optimizely.Performance.DotNetCounters.dll
│
├── content/
│   ├── Optimizely.PerformanceCounters.config        ← Added to project root!
│   └── App_Data/Optimizely.PerformanceCounters/
│       ├── V11/
│       │   ├── web.config.snippet.xml
│       │   └── web.Development.config
│       └── V12-V13/
│           ├── appsettings.json
│           └── appsettings.Development.json
│
├── contentFiles/any/any/                            ← Same as content/
│
├── build/Optimizely.Performance.DotNetCounters.targets
├── tools/install.ps1
└── README.md
```

## Install.ps1 Enhancements

The PowerShell install script now:

1. **Detects project type** (.NET Framework vs .NET 6+)
2. **Shows what was added** to the project
3. **Displays setup instructions** with copy-paste examples
4. **Provides visual guidance** with color-coded output
5. **Lists all counters** that will be monitored

### Sample Output

```
========================================
 Optimizely Performance Counters
 Version: 1.0.0
========================================

✅ Installation complete!

📋 .NET Framework Project Detected (Optimizely V11)

Configuration files added to your project:
  ✅ Optimizely.PerformanceCounters.config (34 counters)
  📄 App_Data/Optimizely.PerformanceCounters/ (documentation)

⚙️  SETUP REQUIRED:

Add these lines to your web.config:

  1. Inside <configSections>:
     <sectionGroup name="optimizely">
       <section name="performanceCounters"
                type="Optimizely.Performance.DotNetCounters.Services..." />
     </sectionGroup>

  2. Inside <configuration>:
     <optimizely>
       <performanceCounters configSource="Optimizely.PerformanceCounters.config" />
     </optimizely>

  3. Configure Application Insights:
     <applicationInsights>
       <InstrumentationKey>YOUR-KEY-HERE</InstrumentationKey>
     </applicationInsights>

📄 Full example: web.config.snippet.xml in your project

What Gets Monitored:
  • ASP.NET request metrics (queue, throughput, timing)
  • CLR memory (heap, GC, all generations)
  • CLR threading (threads, contention, locks)
  • Process health (CPU, memory, handles)
  • Cache performance
  Total: 34 performance counters in the template (23 if you use the defaults)

Documentation:
  📄 App_Data/Optimizely.PerformanceCounters/README_AFTER_INSTALL.md
  📄 App_Data/Optimizely.PerformanceCounters/CONFIGURATION_PRESETS_GUIDE.md

Support: https://github.com/optimizely/performance-counters
========================================
```

## Comparison: Before vs After

| Aspect | Before (Manual Merge) | After (configSource) |
|--------|----------------------|---------------------|
| Lines added to web.config | ~100+ | ~10 |
| Setup complexity | Copy/paste 100 lines | Add 3 small sections |
| Error-prone | High (XML syntax) | Low (minimal changes) |
| Updateability | Hard (re-merge) | Easy (update one file) |
| Version control diffs | Large | Minimal |
| Customization | Edit web.config | Edit dedicated file |
| Sharing across projects | Copy XML blocks | Copy one file |
| Development override | Complex transform | Simple transform |

## Migration Path

**Existing users** who manually merged XML can migrate to configSource:

1. Create `Optimizely.PerformanceCounters.config` with their current counters
2. Replace the inline configuration in web.config with `configSource="..."`
3. Benefit from cleaner structure going forward

## Technical Implementation

### How configSource Works

```xml
<!-- In web.config -->
<performanceCounters configSource="Optimizely.PerformanceCounters.config" />
```

At runtime, the .NET configuration system:
1. Reads web.config
2. Sees `configSource` attribute
3. Loads `Optimizely.PerformanceCounters.config` from disk
4. Merges it as if it were inline

**Requirements**:
- External file must be in same directory as web.config (or relative path)
- External file contains only the inner XML (no `<performanceCounters>` wrapper)
- External file must be valid XML

### Content File Packaging

```xml
<!-- In .csproj -->
<Content Include="..\..\..\examples\V11\Optimizely.PerformanceCounters.config">
  <Pack>true</Pack>
  <PackagePath>content\;contentFiles\any\any\</PackagePath>
  <BuildAction>None</BuildAction>
  <CopyToOutputDirectory>Never</CopyToOutputDirectory>
</Content>
```

This ensures:
- File is added to project root on install
- File is visible in Solution Explorer
- File is NOT compiled or copied to output
- File IS deployed (it's part of the project)

## Future Enhancements

Potential improvements for future versions:

1. **Auto-configuration** (optional): PowerShell script could automatically modify web.config using XDT
2. **Multiple profiles**: Ship `Optimizely.PerformanceCounters.Production.config`, `.Development.config`, etc.
3. **Smart defaults**: Detect environment and use appropriate config automatically
4. **Configuration UI**: Admin interface to enable/disable counters without editing files

## Conclusion

The configSource approach provides:

✅ **Streamlined installation** - Minimal web.config changes  
✅ **Better maintainability** - Dedicated config file  
✅ **Easier updates** - Just replace one file  
✅ **Cleaner codebase** - Separation of concerns  
✅ **Professional experience** - Matches industry best practices  

This is the recommended approach for .NET Framework configuration going forward.

---

**Implemented**: 2026-07-30  
**Version**: 1.0.0  
**Status**: ✅ Complete and ready for packaging
