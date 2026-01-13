<#
.SYNOPSIS
    Build script for Windows Phone Next applications
.DESCRIPTION
    Builds all WPF applications and prepares them for deployment
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Clean,
    [switch]$Deploy,
    [string]$DeployPath = "C:\WindowsPhoneNext"
)

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppsPath = Join-Path $ScriptPath "Apps"
$OutputPath = Join-Path $ScriptPath "Output"

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    $color = switch ($Type) {
        "Info" { "Cyan" }
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
    }
    Write-Host "[BUILD] $Message" -ForegroundColor $color
}

# Check for .NET SDK
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Status ".NET SDK not found. Please install .NET 8 SDK." -Type "Error"
    exit 1
}

Write-Host ""
Write-Host "================================" -ForegroundColor Magenta
Write-Host "  Windows Phone Next - Build   " -ForegroundColor Magenta
Write-Host "================================" -ForegroundColor Magenta
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Status "Cleaning previous builds..."
    if (Test-Path $OutputPath) {
        Remove-Item -Path $OutputPath -Recurse -Force
    }
    Get-ChildItem -Path $AppsPath -Include "bin", "obj" -Recurse -Directory | Remove-Item -Recurse -Force
    Write-Status "Clean complete" -Type "Success"
}

# Create output directory
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Build solution
Write-Status "Building solution ($Configuration)..."

$solutionPath = Join-Path $AppsPath "WindowsPhoneNext.sln"

try {
    dotnet build $solutionPath -c $Configuration --no-incremental

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Build failed" -Type "Error"
        exit 1
    }

    Write-Status "Build successful" -Type "Success"
}
catch {
    Write-Status "Build failed: $_" -Type "Error"
    exit 1
}

# Publish applications
Write-Status "Publishing applications..."

$apps = @(
    @{ Name = "Launcher"; Project = "Launcher\WindowsPhoneLauncher.csproj" },
    @{ Name = "Dialer"; Project = "Dialer\WindowsPhoneDialer.csproj" },
    @{ Name = "Messaging"; Project = "Messaging\WindowsPhoneMessaging.csproj" }
)

foreach ($app in $apps) {
    Write-Status "  Publishing $($app.Name)..."

    $projectPath = Join-Path $AppsPath $app.Project
    $appOutput = Join-Path $OutputPath $app.Name

    dotnet publish $projectPath -c $Configuration -o $appOutput --self-contained false

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Failed to publish $($app.Name)" -Type "Error"
        exit 1
    }
}

Write-Status "All applications published" -Type "Success"

# Deploy if requested
if ($Deploy) {
    Write-Status "Deploying to $DeployPath..."

    # Create deployment directories
    $deployDirs = @("Launcher", "Dialer", "Messaging", "Config", "Scripts", "Logs")
    foreach ($dir in $deployDirs) {
        $fullPath = Join-Path $DeployPath $dir
        if (-not (Test-Path $fullPath)) {
            New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
        }
    }

    # Copy applications
    foreach ($app in $apps) {
        $source = Join-Path $OutputPath $app.Name
        $dest = Join-Path $DeployPath $app.Name
        Copy-Item -Path "$source\*" -Destination $dest -Recurse -Force
    }

    # Copy setup scripts
    $setupSource = Join-Path $ScriptPath "Setup"
    $scriptsPath = Join-Path $DeployPath "Scripts"
    Copy-Item -Path "$setupSource\*" -Destination $scriptsPath -Force

    Write-Status "Deployment complete" -Type "Success"
}

Write-Host ""
Write-Status "Build complete! Output at: $OutputPath" -Type "Success"
Write-Host ""

if (-not $Deploy) {
    Write-Host "To deploy, run: .\Build.ps1 -Deploy" -ForegroundColor Yellow
}
