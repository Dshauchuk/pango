#Requires -Version 5.0
<#
.SYNOPSIS
    Runs all tests with Coverlet (cobertura), then merges results into an HTML report via ReportGenerator.

.DESCRIPTION
    Restores local tools from dotnet-tools.json, executes `dotnet test` on the solution with
    XPlat Code Coverage, and generates `artifacts/coverage-report/index.html`.

    Prerequisites: `dotnet tool restore` must succeed (NuGet access to the public gallery for ReportGenerator).

.EXAMPLE
    ./scripts/Generate-CoverageReport.ps1
    ./scripts/Generate-CoverageReport.ps1 -Configuration Debug
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

$rawDir = Join-Path $repoRoot 'artifacts/coverage-raw'
$reportDir = Join-Path $repoRoot 'artifacts/coverage-report'
New-Item -ItemType Directory -Force -Path $rawDir | Out-Null
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

Write-Host "Restoring local dotnet tools..."
dotnet tool restore

if ($LASTEXITCODE -ne 0) {
    throw "dotnet tool restore failed. Fix NuGet authentication or run: dotnet tool install dotnet-reportgenerator-globaltool"
}

Write-Host "Running tests with coverage ($Configuration)..."
dotnet test (Join-Path $repoRoot 'Pango.sln') -c $Configuration --collect:"XPlat Code Coverage" --results-directory $rawDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed"
}

$files = @(Get-ChildItem -Path $rawDir -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
if ($files.Count -eq 0) {
    throw "No coverage.cobertura.xml files under $rawDir. Check that coverlet.collector is referenced by test projects."
}

$reportsArg = ($files -join ';')
Write-Host "Merging $($files.Count) coverage file(s) with ReportGenerator..."

dotnet tool run reportgenerator -- `
    "-reports:$reportsArg" `
    "-targetdir:$reportDir" `
    -reporttypes:Html `
    -verbosity:Warning

if ($LASTEXITCODE -ne 0) {
    throw "ReportGenerator failed"
}

$index = Join-Path $reportDir 'index.html'
Write-Host ""
Write-Host "Done. Open: $index"
