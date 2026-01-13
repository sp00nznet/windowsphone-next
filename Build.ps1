<#
.SYNOPSIS
    Build script for Windows Phone Next applications
.DESCRIPTION
    Builds WPF applications individually or all together
.PARAMETER App
    Build a specific app: Launcher, Dialer, Messaging, ModemLib, or All (default)
.PARAMETER Configuration
    Build configuration: Debug or Release (default)
.PARAMETER Clean
    Clean before building
.PARAMETER Deploy
    Deploy built applications to install directory
.PARAMETER DeployPath
    Deployment target path
.PARAMETER List
    List available build targets
.EXAMPLE
    .\Build.ps1
    Builds all applications in Release mode
.EXAMPLE
    .\Build.ps1 -App Launcher
    Builds only the Launcher application
.EXAMPLE
    .\Build.ps1 -App Dialer -Configuration Debug
    Builds the Dialer in Debug mode
.EXAMPLE
    .\Build.ps1 -Clean -Deploy
    Clean build all apps and deploy
#>

param(
    [ValidateSet("All", "Launcher", "Dialer", "Messaging", "Browser", "Music", "ModemLib")]
    [string]$App = "All",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Clean,
    [switch]$Deploy,
    [switch]$List,
    [string]$DeployPath = "C:\WindowsPhoneNext"
)

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppsPath = Join-Path $ScriptPath "Apps"
$OutputPath = Join-Path $ScriptPath "Output"

# App definitions
$Apps = @{
    ModemLib = @{
        Name = "ModemLib"
        Project = "Shared\ModemLib\ModemLib.csproj"
        Type = "Library"
        Description = "Shared AT command modem library"
    }
    Launcher = @{
        Name = "Launcher"
        Project = "Launcher\WindowsPhoneLauncher.csproj"
        Type = "Application"
        Description = "Home screen launcher (720x720)"
        DependsOn = @("ModemLib")
    }
    Dialer = @{
        Name = "Dialer"
        Project = "Dialer\WindowsPhoneDialer.csproj"
        Type = "Application"
        Description = "Phone/dialer application"
        DependsOn = @("ModemLib")
    }
    Messaging = @{
        Name = "Messaging"
        Project = "Messaging\WindowsPhoneMessaging.csproj"
        Type = "Application"
        Description = "SMS messaging application"
        DependsOn = @("ModemLib")
    }
    Browser = @{
        Name = "Browser"
        Project = "Browser\WindowsPhoneBrowser.csproj"
        Type = "Application"
        Description = "Chromium-based web browser (720x720)"
        DependsOn = @()
    }
    Music = @{
        Name = "Music"
        Project = "Music\Launcher\WindowsPhoneMusic.csproj"
        Type = "Application"
        Description = "Music player with spectrum visualizer"
        DependsOn = @()
    }
}

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    $color = switch ($Type) {
        "Info" { "Cyan" }
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        "Header" { "Magenta" }
    }
    Write-Host "[BUILD] " -NoNewline -ForegroundColor DarkGray
    Write-Host $Message -ForegroundColor $color
}

function Show-BuildTargets {
    Write-Host ""
    Write-Host "Available Build Targets:" -ForegroundColor Magenta
    Write-Host "========================" -ForegroundColor Magenta
    Write-Host ""

    foreach ($key in @("ModemLib", "Launcher", "Dialer", "Messaging", "Browser", "Music")) {
        $app = $Apps[$key]
        $type = if ($app.Type -eq "Library") { "[LIB]" } else { "[APP]" }
        Write-Host "  $($key.PadRight(12))" -NoNewline -ForegroundColor Cyan
        Write-Host "$type " -NoNewline -ForegroundColor Yellow
        Write-Host $app.Description -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "  All         " -NoNewline -ForegroundColor Cyan
    Write-Host "[ALL] " -NoNewline -ForegroundColor Yellow
    Write-Host "Build all applications" -ForegroundColor Gray
    Write-Host ""

    Write-Host "Usage Examples:" -ForegroundColor Magenta
    Write-Host "  .\Build.ps1                           # Build all (Release)" -ForegroundColor Gray
    Write-Host "  .\Build.ps1 -App Launcher             # Build Launcher only" -ForegroundColor Gray
    Write-Host "  .\Build.ps1 -App Dialer -Clean        # Clean build Dialer" -ForegroundColor Gray
    Write-Host "  .\Build.ps1 -Configuration Debug      # Build all in Debug" -ForegroundColor Gray
    Write-Host "  .\Build.ps1 -Deploy                   # Build and deploy" -ForegroundColor Gray
    Write-Host ""
}

function Build-App {
    param(
        [string]$AppName,
        [string]$Configuration
    )

    $app = $Apps[$AppName]
    if (-not $app) {
        Write-Status "Unknown app: $AppName" -Type "Error"
        return $false
    }

    $projectPath = Join-Path $AppsPath $app.Project

    if (-not (Test-Path $projectPath)) {
        Write-Status "Project not found: $projectPath" -Type "Error"
        return $false
    }

    Write-Status "Building $AppName ($Configuration)..."

    try {
        $output = dotnet build $projectPath -c $Configuration 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Host $output
            Write-Status "Failed to build $AppName" -Type "Error"
            return $false
        }

        # Check for warnings
        $warnings = $output | Select-String "warning"
        if ($warnings) {
            Write-Status "$AppName built with $($warnings.Count) warning(s)" -Type "Warning"
        } else {
            Write-Status "$AppName built successfully" -Type "Success"
        }

        return $true
    }
    catch {
        Write-Status "Build error: $_" -Type "Error"
        return $false
    }
}

function Publish-App {
    param(
        [string]$AppName,
        [string]$Configuration,
        [string]$OutputPath
    )

    $app = $Apps[$AppName]
    if (-not $app -or $app.Type -ne "Application") {
        return $true  # Skip libraries
    }

    $projectPath = Join-Path $AppsPath $app.Project
    $appOutput = Join-Path $OutputPath $AppName

    Write-Status "Publishing $AppName..."

    try {
        dotnet publish $projectPath -c $Configuration -o $appOutput --self-contained false 2>&1 | Out-Null

        if ($LASTEXITCODE -ne 0) {
            Write-Status "Failed to publish $AppName" -Type "Error"
            return $false
        }

        Write-Status "$AppName published to $appOutput" -Type "Success"
        return $true
    }
    catch {
        Write-Status "Publish error: $_" -Type "Error"
        return $false
    }
}

function Clean-App {
    param([string]$AppName)

    $app = $Apps[$AppName]
    if (-not $app) { return }

    $projectDir = Split-Path (Join-Path $AppsPath $app.Project)
    $binPath = Join-Path $projectDir "bin"
    $objPath = Join-Path $projectDir "obj"

    if (Test-Path $binPath) { Remove-Item -Path $binPath -Recurse -Force }
    if (Test-Path $objPath) { Remove-Item -Path $objPath -Recurse -Force }
}

# Show list and exit
if ($List) {
    Show-BuildTargets
    exit 0
}

# Check for .NET SDK
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Status ".NET SDK not found. Please install .NET 8 SDK." -Type "Error"
    exit 1
}

# Header
Write-Host ""
Write-Host "================================================" -ForegroundColor Magenta
Write-Host "       Windows Phone Next - Build System        " -ForegroundColor Magenta
Write-Host "================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "  Target:        " -NoNewline -ForegroundColor Gray
Write-Host $App -ForegroundColor Cyan
Write-Host "  Configuration: " -NoNewline -ForegroundColor Gray
Write-Host $Configuration -ForegroundColor Cyan
Write-Host "  .NET SDK:      " -NoNewline -ForegroundColor Gray
Write-Host $dotnetVersion -ForegroundColor Cyan
Write-Host ""

# Determine which apps to build
$appsToBuild = @()
if ($App -eq "All") {
    $appsToBuild = @("ModemLib", "Launcher", "Dialer", "Messaging", "Browser", "Music")
} else {
    # Add dependencies first
    $appDef = $Apps[$App]
    if ($appDef.DependsOn) {
        $appsToBuild += $appDef.DependsOn
    }
    $appsToBuild += $App
}

# Clean if requested
if ($Clean) {
    Write-Status "Cleaning previous builds..."

    foreach ($appName in $appsToBuild) {
        Clean-App -AppName $appName
    }

    if (Test-Path $OutputPath) {
        Remove-Item -Path $OutputPath -Recurse -Force
    }

    Write-Status "Clean complete" -Type "Success"
    Write-Host ""
}

# Create output directory
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Build apps
$buildSuccess = $true
$buildCount = 0

foreach ($appName in $appsToBuild) {
    if (-not (Build-App -AppName $appName -Configuration $Configuration)) {
        $buildSuccess = $false
        break
    }
    $buildCount++
}

if (-not $buildSuccess) {
    Write-Host ""
    Write-Status "Build failed after $buildCount step(s)" -Type "Error"
    exit 1
}

Write-Host ""
Write-Status "All $buildCount component(s) built successfully" -Type "Success"

# Publish if deploying
if ($Deploy) {
    Write-Host ""
    Write-Status "Publishing applications..." -Type "Header"

    foreach ($appName in $appsToBuild) {
        if (-not (Publish-App -AppName $appName -Configuration $Configuration -OutputPath $OutputPath)) {
            Write-Status "Publish failed" -Type "Error"
            exit 1
        }
    }

    # Deploy to target
    Write-Host ""
    Write-Status "Deploying to $DeployPath..." -Type "Header"

    # Create deployment directories
    $deployDirs = @("Launcher", "Dialer", "Messaging", "Browser", "Music", "Config", "Scripts", "Logs")
    foreach ($dir in $deployDirs) {
        $fullPath = Join-Path $DeployPath $dir
        if (-not (Test-Path $fullPath)) {
            New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
        }
    }

    # Copy applications
    foreach ($appName in $appsToBuild) {
        $app = $Apps[$appName]
        if ($app.Type -eq "Application") {
            $source = Join-Path $OutputPath $appName
            $dest = Join-Path $DeployPath $appName
            if (Test-Path $source) {
                Copy-Item -Path "$source\*" -Destination $dest -Recurse -Force
            }
        }
    }

    # Copy setup scripts
    $setupSource = Join-Path $ScriptPath "Setup"
    $scriptsPath = Join-Path $DeployPath "Scripts"
    if (Test-Path $setupSource) {
        Copy-Item -Path "$setupSource\*" -Destination $scriptsPath -Force
    }

    Write-Status "Deployment complete" -Type "Success"
}

# Summary
Write-Host ""
Write-Host "================================================" -ForegroundColor DarkGray
Write-Host ""

if ($App -eq "All") {
    Write-Status "Build complete! All applications ready." -Type "Success"
} else {
    Write-Status "Build complete! $App is ready." -Type "Success"
}

Write-Host ""
Write-Host "Output: $OutputPath" -ForegroundColor Gray

if (-not $Deploy) {
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  .\Build.ps1 -Deploy              # Deploy to $DeployPath" -ForegroundColor Gray
    Write-Host "  .\Build.ps1 -List                # Show all build targets" -ForegroundColor Gray
}

Write-Host ""
