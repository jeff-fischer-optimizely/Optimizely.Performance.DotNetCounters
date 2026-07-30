Compilation symbols used across this repository

Purpose
This document records the canonical compilation symbols (preprocessor defines) used by the Optimizely.Performance.DotNetCounters repository. These are intended as the shared "edicts" to keep naming uniform across related solutions and CI.

Symbols (canonical)
- CMS11 — Use when building for Optimizely / Episerver CMS 11 (typically .NET Framework 4.7.2)
- CMS12 — Use when building for Optimizely / Episerver CMS 12 (typically .NET 6)
- CMS13 — Use when building for Optimizely / Episerver CMS 13 (future / .NET 10)

Notes and mapping
- Previously used symbols in other repos: OPTI_V11, OPTI_V12, OPTI_V13 or OPTIMIZELY_V11, etc. Replace those with CMS11/CMS12/CMS13.
- Example usage in MSBuild / dotnet:
  - msbuild ... /p:DefineConstants="CMS11"
  - dotnet build -c Release /p:DefineConstants="CMS12"

Repository-level policy
- All projects that require a product-specific compilation path should use CMS11/CMS12/CMS13 as the symbol names.
- CI pipelines and build scripts should set these symbols explicitly per job so builds are deterministic.

Where to find definitions
- The primary csproj defines the symbols by default (search for "CMS11" in project files).

If you need me to rename symbols across other repositories, provide the repo list and I can apply consistent edits.
