# Changelog

All notable changes to the Optimizely.Performance.DotNetCounters project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-07-30

### Added

#### Core Features
- Multi-targeted assembly supporting .NET Framework 4.7.2, .NET 6, and .NET 8
- Support for Optimizely CMS V11, V12, and V13
- Automatic initialization via `IConfigurableModule`
- Application Insights integration for metric collection

#### .NET Framework 4.7.2 (Optimizely V11) Features
- Windows Performance Counter collection
- 16 default performance counters covering ASP.NET, CLR, and Process metrics
- Configuration via `web.config`
- Support for `IConfiguration` and legacy `ConfigurationSection`

#### .NET 6+ (Optimizely V12 & V13) Features
- Event Counter collection
- 22 default event counters covering System.Runtime and ASP.NET Core metrics
- Configuration via `appsettings.json`
- Dependency injection integration

#### Configuration
- Configuration-driven counter selection
- Sensible defaults for all platforms
- Ability to enable/disable counter collection
- Custom counter definitions
- Environment-specific configuration support

#### Default Counters - .NET Framework
- **ASP.NET**: Requests Queued, Requests/Sec, Request Wait Time, Request Execution Time
- **CLR Threads**: Logical Threads, Physical Threads, Contention Rate, Queue Length
- **CLR Memory**: Heap Size, % Time in GC, Gen 0/1/2 Collections, LOH Size
- **Process**: Thread Count, Handle Count

#### Default Counters - .NET Core+
- **System.Runtime**: CPU usage, working set, GC metrics, threading metrics, allocation rate
- **ASP.NET Core**: Requests/sec, total requests, current requests, failed requests

#### Documentation
- Comprehensive README with quick start guide
- BUILD_GUIDE.md with detailed build instructions and CI/CD examples
- USAGE_GUIDE.md with configuration examples and Application Insights queries
- PROJECT_STRUCTURE.md explaining architecture and design decisions
- IMPLEMENTATION_SUMMARY.md with complete implementation overview
- Example configurations for both `web.config` and `appsettings.json`

#### Developer Experience
- XML documentation for IntelliSense support
- Graceful error handling (no exceptions thrown during startup)
- Debug output for troubleshooting
- NuGet package configuration with Optimizely feed

### Technical Details
- Conditional compilation for platform-specific code
- Floating version references for Optimizely packages (11.*, 12.*, 13.*)
- Single codebase for all platforms using `#if` directives
- Comprehensive default counter sets
- Zero-configuration option (works out of the box)

### Package Information
- **Package ID**: Optimizely.Performance.DotNetCounters
- **License**: Apache-2.0
- **Target Frameworks**: net472, net6.0, net8.0
- **Dependencies**: Microsoft.ApplicationInsights, Optimizely packages

---

## Version History

### Version Numbering

This project follows semantic versioning:

- **Major** (X.0.0): Breaking changes, new Optimizely major version support
- **Minor** (1.X.0): New features, new counters, backward-compatible changes
- **Patch** (1.0.X): Bug fixes, performance improvements, documentation updates

### Planned Future Versions

#### [1.1.0] - Future
- Additional custom Optimizely-specific counters
- Health check endpoint
- Prometheus exporter support

#### [2.0.0] - Future
- .NET 9+ support (when Optimizely supports it)
- Breaking changes if needed for new architecture

---

## Migration Guide

### From No Performance Monitoring

Simply install the package and add Application Insights configuration. The library will automatically initialize with sensible defaults.

### From Custom Performance Counter Implementation

1. Remove your custom performance counter initialization code
2. Install Optimizely.Performance.DotNetCounters
3. Migrate your counter configuration to the new format
4. Test in staging to verify all desired counters are being collected

---

## Links

- [GitHub Repository](#)
- [NuGet Package](#)
- [Documentation](README.md)
- [Issue Tracker](#)
- [Application Insights Documentation](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

## Contributors

- Initial implementation: 2026-07-30

## License

Copyright © 2026 Optimizely

Licensed under the Apache License, Version 2.0. See LICENSE file for details.
