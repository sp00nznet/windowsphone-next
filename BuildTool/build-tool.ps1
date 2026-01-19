<#
.SYNOPSIS
    Builds the Windows Phone Next Build Tool
.DESCRIPTION
    Compiles the build tool Win32 application
.EXAMPLE
    .\build-tool.ps1
    .\build-tool.ps1 -Configuration Debug
    .\build-tool.ps1 -Clean
#>

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }

Write-Host ""
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host "  Windows Phone Next - Build Tool Compiler" -ForegroundColor Magenta
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host ""

$ProjectRoot = $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "WindowsPhoneNextBuildTool.csproj"
$OutputDir = Join-Path $ProjectRoot "bin" $Configuration "net8.0-windows"

# Check for .NET SDK
Write-Info "Checking for .NET 8.0 SDK..."
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Error ".NET SDK not found. Please install .NET 8.0 SDK."
    Write-Info "Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

Write-Success ".NET SDK version: $dotnetVersion"

# Clean if requested
if ($Clean) {
    Write-Info "Cleaning build artifacts..."
    if (Test-Path (Join-Path $ProjectRoot "bin")) {
        Remove-Item (Join-Path $ProjectRoot "bin") -Recurse -Force
    }
    if (Test-Path (Join-Path $ProjectRoot "obj")) {
        Remove-Item (Join-Path $ProjectRoot "obj") -Recurse -Force
    }
    Write-Success "Clean completed"
}

# Restore NuGet packages
Write-Info "Restoring NuGet packages..."
dotnet restore $ProjectFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "NuGet restore failed"
    exit 1
}
Write-Success "NuGet restore completed"

# Build the project
Write-Info "Building Windows Phone Next Build Tool ($Configuration)..."
dotnet build $ProjectFile --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

Write-Success "Build completed successfully!"
Write-Info "Output: $OutputDir\WindowsPhoneNextBuildTool.exe"

# Check if output exists
if (Test-Path (Join-Path $OutputDir "WindowsPhoneNextBuildTool.exe")) {
    Write-Host ""
    Write-Success "Build Tool is ready to use!"
    Write-Info "To run: "
    Write-Host "  cd BuildTool" -ForegroundColor Yellow
    Write-Host "  .\bin\$Configuration\net8.0-windows\WindowsPhoneNextBuildTool.exe" -ForegroundColor Yellow
    Write-Host ""
    Write-Info "Remember: Must be run as Administrator"
} else {
    Write-Error "Build completed but executable not found"
    exit 1
}
