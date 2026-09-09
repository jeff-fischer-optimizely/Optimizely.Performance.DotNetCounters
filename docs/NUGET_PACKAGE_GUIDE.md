# NuGet Package Structure Guide

This document explains how the Optimizely.Performance.DotNetCounters NuGet package is structured and how configuration templates are delivered to consuming projects.

## Package Structure

When built, the NuGet package contains the following structure:

```
Optimizely.Performance.DotNetCounters.1.0.0.nupkg
├── lib/
│   ├── net472/
│   │   └── Optimizely.Performance.DotNetCounters.dll     (.NET Framework build)
│   ├── net6.0/
│   │   └── Optimizely.Performance.DotNetCounters.dll     (.NET 6 build)
│   ├── net7.0/
│   │   └── Optimizely.Performance.DotNetCounters.dll     (.NET 7 build)
│   ├── net8.0/
│   │   └── Optimizely.Performance.DotNetCounters.dll     (.NET 8 build)
│   ├── net9.0/
│   │   └── Optimizely.Performance.DotNetCounters.dll     (.NET 9 build)
│   └── net10.0/
│       └── Optimizely.Performance.DotNetCounters.dll     (.NET 10 build)
│
├── content/
│   └── App_Data/
│       └── Optimizely.PerformanceCounters/
│           ├── V11/
│           │   ├── web.config.production
│           │   ├── web.config.development
│           │   └── web.config.troubleshooting
│           ├── V12-V13/
│           │   ├── appsettings.Production.json
│           │   ├── appsettings.Development.json
│           │   ├── appsettings.Troubleshooting.json
│           │   ├── appsettings.MemoryDiagnostics.json
│           │   └── appsettings.ThreadingDiagnostics.json
│           ├── CONFIGURATION_PRESETS_GUIDE.md
│           └── README_AFTER_INSTALL.md
│
├── contentFiles/
│   └── any/
│       └── any/
│           └── App_Data/
│               └── Optimizely.PerformanceCounters/
│                   └── (same as content/)
│
├── build/
│   └── Optimizely.Performance.DotNetCounters.targets   (MSBuild integration)
│
├── tools/
│   └── install.ps1                                      (PowerShell install script)
│
└── README.md                                             (Package documentation)
```

## How Content Files Work

### content/ vs contentFiles/

- **content/**: Used by packages.config (legacy .NET Framework projects)
- **contentFiles/**: Used by PackageReference (modern SDK-style projects)

Both are included to support all project types.

### File Placement on Install

When a project installs the package:

**Legacy projects (packages.config)**:
- Files from `content/` are copied directly into the project
- Path: `YourProject/App_Data/Optimizely.PerformanceCounters/`

**Modern projects (PackageReference)**:
- Files from `contentFiles/` are linked into the project (not copied by default)
- Path: `YourProject/App_Data/Optimizely.PerformanceCounters/`
- Can be accessed in Solution Explorer under the project

### Incremental Addition

The configuration templates are **additive** - they:
- ✅ Are added to `App_Data/` folder (excluded from compilation)
- ✅ Do NOT overwrite existing files
- ✅ Are NOT included in build output
- ✅ Are NOT deployed with the application
- ✅ Serve as **templates** to copy from

## Usage Flow

1. **Developer installs package**:
   ```bash
   dotnet add package Optimizely.Performance.DotNetCounters
   ```

2. **NuGet adds configuration templates** to:
   ```
   YourProject/App_Data/Optimizely.PerformanceCounters/
   ```

3. **Developer chooses a preset** based on:
   - Optimizely version (V11, V12, V13)
   - Environment (Production, Development, Troubleshooting)
   - Diagnostic needs (Memory, Threading)

4. **Developer copies configuration**:
   - **V11**: Copy section from template to `web.config`
   - **V12/V13**: Copy entire file as `appsettings.{Environment}.json`

5. **Developer configures**:
   - Replaces placeholder Application Insights key
   - Optionally customizes counters

6. **Application runs**:
   - Library auto-initializes via `IConfigurableModule`
   - Reads configuration from web.config or appsettings.json
   - Starts collecting metrics

## Build Integration

### MSBuild Targets

The `build/Optimizely.Performance.DotNetCounters.targets` file is automatically imported into consuming projects.

**What it does**:
- Displays informational message after build
- Ensures configuration templates are NOT deployed to `bin/` or `publish/`
- Reminds developers where templates are located

**Output during build**:
```
========================================
Optimizely Performance Counters
========================================
Configuration templates available at:
  App_Data\Optimizely.PerformanceCounters\

See README_AFTER_INSTALL.md for setup instructions.
========================================
```

### Install Script

The `tools/install.ps1` script runs automatically in Visual Studio when using packages.config (legacy).

**What it does**:
- Displays welcome message
- Lists available presets
- Provides setup instructions
- Shows documentation paths

**Note**: PackageReference projects don't run install scripts, so guidance is also in MSBuild targets and README files.

## Configuration Template Design

### Non-Intrusive Approach

Templates are designed to be **reference material**, not auto-configured:

**Why not auto-configure?**
- ❌ Would require overwriting user's existing config
- ❌ Placeholder keys would cause runtime errors
- ❌ Different projects have different needs
- ❌ Could conflict with existing configuration

**Instead, we provide**:
- ✅ Ready-to-use templates
- ✅ Clear documentation
- ✅ Multiple presets for different scenarios
- ✅ Copy-paste workflow (developer has full control)

### Side-by-Side Approach

All presets exist **side-by-side** in the same folder:

```
App_Data/Optimizely.PerformanceCounters/
  ├── V11/
  │   ├── web.config.production          ← Production preset
  │   ├── web.config.development         ← Development preset
  │   └── web.config.troubleshooting     ← Troubleshooting preset
  └── V12-V13/
      ├── appsettings.Production.json
      ├── appsettings.Development.json
      ├── appsettings.Troubleshooting.json
      ├── appsettings.MemoryDiagnostics.json
      └── appsettings.ThreadingDiagnostics.json
```

Developers can:
- Compare different presets
- Switch between presets easily
- Customize by mixing/matching counters
- Keep templates as reference

## Version Detection

The package **does not** auto-detect Optimizely version because:
- All three versions might exist in the same solution (different projects)
- Developers know which version they're targeting
- Templates are organized by version folder (V11/ vs V12-V13/)

Instead, developers self-select based on:
- Project target framework (net472 → V11, net6.0 through net9.0 → V12, net10.0 → V13)
- Folder structure makes it obvious which to use

## Environment-Specific Configuration

### Recommended Approach for V12/V13

Copy the template as an environment-specific file:

```bash
# Production
cp App_Data/Optimizely.PerformanceCounters/V12-V13/appsettings.Production.json \
   appsettings.Production.json

# Development
cp App_Data/Optimizely.PerformanceCounters/V12-V13/appsettings.Development.json \
   appsettings.Development.json
```

Then set environment variable on deployment:
- Development: `ASPNETCORE_ENVIRONMENT=Development`
- Staging: `ASPNETCORE_ENVIRONMENT=Staging` (use Production template)
- Production: `ASPNETCORE_ENVIRONMENT=Production`

### Transform Approach for V11

Use web.config transforms for environment-specific configuration:

```
web.config                          (base configuration)
web.Debug.config                    (development transform)
web.Release.config                  (production transform)
```

## Updating Configuration

When a new version of the package is released with updated templates:

1. NuGet **will not** overwrite existing template files
2. Developers can:
   - Delete old templates and reinstall package (to get new ones)
   - Manually compare and merge changes
   - Check release notes for configuration changes

## Creating a NuGet Package

To build the NuGet package with all content:

```bash
# Restore dependencies
dotnet restore

# Build for all target frameworks
dotnet build --configuration Release

# Create NuGet package
dotnet pack --configuration Release --output ./nupkg

# Result: 
# ./nupkg/Optimizely.Performance.DotNetCounters.1.0.0.nupkg
```

## Verifying Package Contents

To inspect the package contents:

```bash
# Extract .nupkg (it's a ZIP file)
unzip Optimizely.Performance.DotNetCounters.1.0.0.nupkg -d extracted/

# Or use NuGet Package Explorer (Windows)
# https://github.com/NuGetPackageExplorer/NuGetPackageExplorer
```

Check that:
- `lib/` contains DLLs for all target frameworks
- `content/App_Data/` contains all template files
- `contentFiles/any/any/App_Data/` contains same files
- `build/` contains .targets file
- `tools/` contains install.ps1
- `README.md` exists at root

## Publishing to NuGet.org

```bash
# Create API key at nuget.org

# Push package
dotnet nuget push ./nupkg/Optimizely.Performance.DotNetCounters.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

## Best Practices for Consuming Projects

1. **After installation**:
   - Read `App_Data/Optimizely.PerformanceCounters/README_AFTER_INSTALL.md`
   - Review available presets in the folder

2. **Choose preset**:
   - Start with Production preset for most environments
   - Use Development preset locally
   - Use Troubleshooting preset only when investigating issues

3. **Configure Application Insights**:
   - Add valid instrumentation key/connection string
   - Test in staging before production

4. **Commit templates to source control**:
   - Templates are useful for team members
   - Provides documentation of available presets
   - Can be referenced in deployment docs

5. **Don't deploy templates**:
   - Templates in `App_Data/` are NOT deployed
   - Only actual configuration (web.config/appsettings.json) is deployed

## Troubleshooting

### Templates not appearing after install

**Cause**: PackageReference doesn't always show contentFiles in Solution Explorer

**Solution**: Templates are still there, just check the file system:
```bash
dir App_Data\Optimizely.PerformanceCounters
```

Or add to your project explicitly:
```xml
<ItemGroup>
  <None Include="App_Data\Optimizely.PerformanceCounters\**\*.*" />
</ItemGroup>
```

### Templates included in published output

**Cause**: MSBuild targets not working correctly

**Solution**: Ensure files are in `App_Data/` folder (excluded from publish by default)

Or explicitly exclude:
```xml
<ItemGroup>
  <Content Remove="App_Data\Optimizely.PerformanceCounters\**\*.*" />
</ItemGroup>
```

---

**Last Updated**: 2026-07-30  
**Version**: 1.0.0
