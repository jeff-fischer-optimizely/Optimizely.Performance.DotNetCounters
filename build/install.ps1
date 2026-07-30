# Optimizely.Performance.DotNetCounters - Post-Install Script
param($installPath, $toolsPath, $package, $project)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Optimizely Performance Counters" -ForegroundColor Cyan
Write-Host " Version: 1.0.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Installation complete!" -ForegroundColor Green
Write-Host ""

# Detect project type
$isNetFramework = $false
$webConfigItem = $null

try {
    $webConfigItem = $project.ProjectItems | Where-Object { $_.Name -eq "web.config" }
    if ($webConfigItem) {
        $isNetFramework = $true
    }
}
catch {
    # Project may not support ProjectItems (SDK-style)
}

if ($isNetFramework) {
    Write-Host "📋 .NET Framework Project Detected (Optimizely V11)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Configuration files added to your project:" -ForegroundColor White
    Write-Host "  ✅ Optimizely.PerformanceCounters.config (28 counters)" -ForegroundColor Green
    Write-Host "  📄 App_Data/Optimizely.PerformanceCounters/ (documentation)" -ForegroundColor Gray
    Write-Host ""

    # Check if web.config already has the configuration
    $webConfigPath = $webConfigItem.Properties.Item("FullPath").Value
    $webConfigContent = Get-Content $webConfigPath -Raw -ErrorAction SilentlyContinue

    $hasConfigSection = $webConfigContent -match 'Optimizely\.Performance\.DotNetCounters'
    $hasOptimizelySection = $webConfigContent -match '<optimizely>'

    if ($hasConfigSection -and $hasOptimizelySection) {
        Write-Host "✅ Configuration already present in web.config" -ForegroundColor Green
        Write-Host ""
    }
    else {
        Write-Host "⚙️  SETUP REQUIRED:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Add these lines to your web.config:" -ForegroundColor White
        Write-Host ""
        Write-Host "  1. Inside <configSections>:" -ForegroundColor Gray
        Write-Host "     <sectionGroup name=`"optimizely`">" -ForegroundColor Cyan
        Write-Host "       <section name=`"performanceCounters`"" -ForegroundColor Cyan
        Write-Host "                type=`"Optimizely.Performance.DotNetCounters.Services.PerformanceCountersConfigSection, Optimizely.Performance.DotNetCounters`" />" -ForegroundColor Cyan
        Write-Host "     </sectionGroup>" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  2. Inside <configuration>:" -ForegroundColor Gray
        Write-Host "     <optimizely>" -ForegroundColor Cyan
        Write-Host "       <performanceCounters configSource=`"Optimizely.PerformanceCounters.config`" />" -ForegroundColor Cyan
        Write-Host "     </optimizely>" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  3. Configure Application Insights:" -ForegroundColor Gray
        Write-Host "     <applicationInsights>" -ForegroundColor Cyan
        Write-Host "       <InstrumentationKey>YOUR-KEY-HERE</InstrumentationKey>" -ForegroundColor Cyan
        Write-Host "     </applicationInsights>" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "📄 Full example: web.config.snippet.xml in your project" -ForegroundColor White
        Write-Host ""
    }

    Write-Host "💡 To disable in development:" -ForegroundColor Yellow
    Write-Host "   Copy web.Development.config as web.Debug.config" -ForegroundColor White
    Write-Host ""
}
else {
    Write-Host "📋 .NET 6+ Project Detected (Optimizely V12/V13)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Configuration files added:" -ForegroundColor White
    Write-Host "  📄 App_Data/Optimizely.PerformanceCounters/V12-V13/" -ForegroundColor Gray
    Write-Host "     - appsettings.json (41 counters)" -ForegroundColor Green
    Write-Host "     - appsettings.Development.json (disable override)" -ForegroundColor Green
    Write-Host ""
    Write-Host "⚙️  SETUP REQUIRED:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Copy App_Data/Optimizely.PerformanceCounters/V12-V13/appsettings.json" -ForegroundColor White
    Write-Host "   content into your project's appsettings.json" -ForegroundColor White
    Write-Host ""
    Write-Host "2. (Optional) Copy appsettings.Development.json to disable locally" -ForegroundColor White
    Write-Host ""
    Write-Host "3. Configure Application Insights connection string" -ForegroundColor White
    Write-Host ""
}

Write-Host "What Gets Monitored:" -ForegroundColor Yellow
if ($isNetFramework) {
    Write-Host "  • ASP.NET request metrics (queue, throughput, timing)" -ForegroundColor White
    Write-Host "  • CLR memory (heap, GC, all generations)" -ForegroundColor White
    Write-Host "  • CLR threading (threads, contention, locks)" -ForegroundColor White
    Write-Host "  • Process health (CPU, memory, handles)" -ForegroundColor White
    Write-Host "  • Cache performance" -ForegroundColor White
    Write-Host "  Total: 28 performance counters" -ForegroundColor Green
}
else {
    Write-Host "  • CPU, memory, GC metrics" -ForegroundColor White
    Write-Host "  • Thread pool & concurrency" -ForegroundColor White
    Write-Host "  • ASP.NET Core requests" -ForegroundColor White
    Write-Host "  • Kestrel server metrics" -ForegroundColor White
    Write-Host "  • HTTP client (outbound)" -ForegroundColor White
    Write-Host "  Total: 41 event counters" -ForegroundColor Green
}
Write-Host ""

Write-Host "Documentation:" -ForegroundColor Yellow
Write-Host "  📄 App_Data/Optimizely.PerformanceCounters/README_AFTER_INSTALL.md" -ForegroundColor White
Write-Host "  📄 App_Data/Optimizely.PerformanceCounters/CONFIGURATION_PRESETS_GUIDE.md" -ForegroundColor White
Write-Host ""
Write-Host "Support: https://github.com/optimizely/performance-counters" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
