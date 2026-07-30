# Build Guide

This document explains how to build the Optimizely Performance Counters library and important considerations for package resolution.

## Prerequisites

### Required Software

- .NET SDK 6.0 or later (for building .NET 6 and .NET 8 targets)
- .NET Framework 4.7.2 Developer Pack (for building .NET Framework target)
- Visual Studio 2022 or later (recommended) OR JetBrains Rider
- Git (for version control)

### NuGet Feed Configuration

The project requires access to the Optimizely NuGet feed. A `nuget.config` file is included in the repository root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Optimizely" value="https://nuget.optimizely.com/feed/packages.svc/" />
  </packageSources>
</configuration>
```

**Note**: The Optimizely NuGet feed is public and does not require authentication for most packages.

## Building the Project

### Command Line Build

```bash
# Restore NuGet packages
dotnet restore

# Build all targets (net472, net6.0, net8.0)
dotnet build --configuration Release

# Build a specific target framework
dotnet build --configuration Release --framework net472
dotnet build --configuration Release --framework net6.0
dotnet build --configuration Release --framework net8.0
```

### Visual Studio Build

1. Open `Optimizely.Performance.DotNetCounters.sln` in Visual Studio
2. Select the desired configuration (Debug or Release)
3. Build → Build Solution (Ctrl+Shift+B)

The multi-targeting project will build all three target frameworks automatically.

### JetBrains Rider Build

1. Open `Optimizely.Performance.DotNetCounters.sln` in Rider
2. Select the desired configuration from the top toolbar
3. Build → Build Solution (Ctrl+F9)

## Package Versioning Strategy

The project uses **floating version references** for Optimizely packages:

```xml
<!-- .NET Framework 4.7.2 (V11) -->
<PackageReference Include="EPiServer.CMS.Core" Version="11.*" />

<!-- .NET 6 (V12) -->
<PackageReference Include="EPiServer.CMS.Core" Version="12.*" />

<!-- .NET 8+ (V13) -->
<PackageReference Include="EPiServer.Cms.Core" Version="13.*" />
```

This approach:
- ✅ Allows the library to work with any minor/patch version within a major version
- ✅ Avoids version conflicts when consumed by Optimizely projects
- ✅ Stays compatible with the latest security patches
- ⚠️ May require package restore if specific versions are unavailable

The Optimizely packages are marked with `PrivateAssets="compile"`, meaning they're only used during compilation and won't be deployed with your application.

## Troubleshooting Build Issues

### Issue: NU1101 - Unable to find package EPiServer.X

**Cause**: The Optimizely NuGet feed is not configured or accessible.

**Solution**:
1. Verify `nuget.config` exists in the repository root
2. Clear NuGet caches: `dotnet nuget locals all --clear`
3. Restore packages: `dotnet restore`

### Issue: NU1603 - Package version not found

**Cause**: The specific version requested is not available on the NuGet feed.

**Solution**: The project uses floating versions (`11.*`, `12.*`, `13.*`) which should resolve to the latest available version. If you see this warning, it's safe to ignore as long as the build succeeds.

### Issue: NU1605 - Package downgrade detected

**Cause**: Version conflict between Optimizely dependencies and our explicit package references.

**Solution**: This is suppressed in the project file via:
```xml
<NoWarn>$(NoWarn);NU1603;NU1605</NoWarn>
```

The conflicts are expected and safe because we're building against multiple framework versions.

### Issue: CS0246 - Type or namespace not found

**Cause**: Missing .NET Framework Developer Pack or SDK.

**Solution**:
1. Install .NET Framework 4.7.2 Developer Pack from https://dotnet.microsoft.com/download/dotnet-framework
2. Install .NET 6 SDK from https://dotnet.microsoft.com/download/dotnet/6.0
3. Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0

## Creating a NuGet Package

To create a distributable NuGet package:

```bash
# Pack all targets into a single NuGet package
dotnet pack --configuration Release --output ./nupkg

# The resulting package will contain all three target frameworks:
# - lib/net472/Optimizely.Performance.DotNetCounters.dll
# - lib/net6.0/Optimizely.Performance.DotNetCounters.dll
# - lib/net8.0/Optimizely.Performance.DotNetCounters.dll
```

The NuGet package will automatically select the correct DLL based on the consuming project's target framework.

## Supported Target Frameworks

| Target Framework | .NET Version | Optimizely Version | Status |
|-----------------|--------------|-------------------|--------|
| `net472` | .NET Framework 4.7.2 | V11 | Supported |
| `net6.0` | .NET 6 | V12 | Supported |
| `net8.0` | .NET 8 | V13 | Supported |

**Note**: .NET 9 and .NET 10 support can be added by extending the `<TargetFrameworks>` property, but as of this writing, Optimizely V13 officially supports .NET 8.

## Conditional Compilation

The project uses `#if` directives to compile different code for different target frameworks:

```csharp
#if NET472
    // .NET Framework-specific code (Windows Performance Counters)
#endif

#if !NET472
    // .NET Core and later code (Event Counters)
#endif

#if NET6_0
    // .NET 6 specific code
#endif

#if NET8_0
    // .NET 8 specific code
#endif
```

This allows a single codebase to support all platforms while only including relevant code in each build.

## Continuous Integration

### GitHub Actions Example

```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: |
          6.0.x
          8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Pack
      run: dotnet pack --configuration Release --no-build --output ./artifacts
    
    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: nuget-package
        path: ./artifacts/*.nupkg
```

### Azure DevOps Example

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  displayName: 'Install .NET 6'
  inputs:
    version: '6.0.x'

- task: UseDotNet@2
  displayName: 'Install .NET 8'
  inputs:
    version: '8.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '--configuration Release'

- task: DotNetCoreCLI@2
  displayName: 'Pack'
  inputs:
    command: 'pack'
    arguments: '--configuration Release --output $(Build.ArtifactStagingDirectory)'

- task: PublishBuildArtifacts@1
  displayName: 'Publish Artifacts'
  inputs:
    pathToPublish: '$(Build.ArtifactStagingDirectory)'
```

## Local Development Tips

1. **Use Directory.Build.props** (optional): Create a `Directory.Build.props` file in the repository root to centralize common MSBuild properties.

2. **Editor Config**: The project respects `.editorconfig` for code style consistency.

3. **XML Documentation**: Documentation comments are compiled into an XML file alongside the DLL for IntelliSense support.

4. **Debugging Multi-Targeted Projects**: When debugging in Visual Studio, you can select which target framework to debug from the dropdown next to the Start button.

## Version Management

We recommend using semantic versioning for releases:

- **Major** (1.0.0 → 2.0.0): Breaking changes, new Optimizely major version support
- **Minor** (1.0.0 → 1.1.0): New features, new counters, backward-compatible changes
- **Patch** (1.0.0 → 1.0.1): Bug fixes, performance improvements

Update the version in the `.csproj` file:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <PackageVersion>1.0.0</PackageVersion>
</PropertyGroup>
```
