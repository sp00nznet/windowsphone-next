<#
.SYNOPSIS
    Master deployment script for Windows Phone Next
.DESCRIPTION
    Builds all applications, downloads drivers, and creates a deployable image.
    Can automatically download Windows 11 IoT Enterprise LTSC from Microsoft.
.EXAMPLE
    .\deploy.ps1 -DownloadIso
    .\deploy.ps1 -IsoPath "C:\Windows11.iso"
    .\deploy.ps1 -BuildOnly
#>

param(
    [Parameter()]
    [string]$IsoPath,

    [Parameter()]
    [switch]$DownloadIso,

    [Parameter()]
    [switch]$BuildOnly,

    [Parameter()]
    [switch]$SkipDrivers,

    [Parameter()]
    [switch]$SkipBuild,

    [Parameter()]
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

function Write-Banner {
    param($Message)
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Magenta
    Write-Host "  $Message" -ForegroundColor Magenta
    Write-Host ("=" * 60) -ForegroundColor Magenta
    Write-Host ""
}

function Write-Step {
    param($Step, $Total, $Message)
    Write-Host ""
    Write-Host "[$Step/$Total] $Message" -ForegroundColor Yellow
    Write-Host ("-" * 50) -ForegroundColor DarkGray
}

Write-Banner "Windows Phone Next - Deployment System"

$BuildDir = $PSScriptRoot
$ProjectRoot = Resolve-Path "$BuildDir\.."
$IsoDir = Join-Path $ProjectRoot "ISO"

# Calculate total steps
$TotalSteps = 4
if ($BuildOnly) { $TotalSteps = 1 }
if ($DownloadIso) { $TotalSteps = 5 }
if ($SkipBuild -and -not $BuildOnly) { $TotalSteps-- }

$CurrentStep = 0

# Step: Download ISO (if requested)
if ($DownloadIso) {
    $CurrentStep++
    Write-Step $CurrentStep $TotalSteps "Downloading Windows 11 IoT Enterprise LTSC"

    # Create ISO directory
    if (-not (Test-Path $IsoDir)) {
        New-Item -ItemType Directory -Path $IsoDir -Force | Out-Null
    }

    # Run download script
    $downloadResult = & "$BuildDir\download-iso.ps1" -OutputPath $IsoDir

    if ($downloadResult -and (Test-Path $downloadResult)) {
        $IsoPath = $downloadResult
        Write-Host "ISO ready: $IsoPath" -ForegroundColor Green
    }
    else {
        Write-Host "ISO download was not completed." -ForegroundColor Red
        Write-Host "Please download manually and run:" -ForegroundColor Yellow
        Write-Host "  .\deploy.ps1 -IsoPath 'C:\path\to\windows.iso'" -ForegroundColor Yellow
        exit 1
    }
}

# Step: Build all applications
if (-not $SkipBuild) {
    $CurrentStep++
    Write-Step $CurrentStep $TotalSteps "Building all applications"

    $buildArgs = @()
    if ($Clean) { $buildArgs += "-Clean" }

    & "$BuildDir\build-all.ps1" @buildArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed! Please fix errors and try again." -ForegroundColor Red
        exit 1
    }

    Write-Host "Build completed successfully!" -ForegroundColor Green
}
else {
    Write-Host "Skipping build (using existing build output)" -ForegroundColor Yellow
}

if ($BuildOnly) {
    Write-Banner "Build Complete"
    Write-Host "Built applications are in: $ProjectRoot\Output"
    exit 0
}

# Step: Download drivers
if (-not $SkipDrivers) {
    $CurrentStep++
    Write-Step $CurrentStep $TotalSteps "Downloading LattePanda 3 Delta drivers"

    & "$BuildDir\download-drivers.ps1"

    Write-Host "Drivers downloaded successfully!" -ForegroundColor Green
} else {
    Write-Host "Skipping driver download (using existing drivers)" -ForegroundColor Yellow
}

# Step: Verify ISO
$CurrentStep++
Write-Step $CurrentStep $TotalSteps "Verifying Windows ISO"

if (-not $IsoPath) {
    Write-Host ""
    Write-Host "Windows ISO not specified." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To create a full deployment image, you need a Windows 11 IoT Enterprise LTSC ISO."
    Write-Host ""
    Write-Host "OPTION 1 - Automatic download (recommended):" -ForegroundColor Cyan
    Write-Host "  .\deploy.ps1 -DownloadIso"
    Write-Host ""
    Write-Host "OPTION 2 - Manual download:" -ForegroundColor Cyan
    Write-Host "  1. Visit: https://www.microsoft.com/en-us/evalcenter/evaluate-windows-11-iot-enterprise-ltsc"
    Write-Host "  2. Fill out the form and download the ISO"
    Write-Host "  3. Run: .\deploy.ps1 -IsoPath 'C:\path\to\windows.iso'"
    Write-Host ""
    Write-Host "Other ISO sources:"
    Write-Host "  - Microsoft Volume Licensing Service Center (VLSC)"
    Write-Host "  - Visual Studio Subscriptions"
    Write-Host ""

    # Create manual deployment package
    Write-Host "Creating manual deployment package instead..." -ForegroundColor Cyan

    $manualDeployDir = Join-Path $ProjectRoot "ManualDeploy"
    if (-not (Test-Path $manualDeployDir)) {
        New-Item -ItemType Directory -Path $manualDeployDir -Force | Out-Null
    }

    # Copy built apps
    Copy-Item -Path "$ProjectRoot\Output\*" -Destination "$manualDeployDir\Apps" -Recurse -Force

    # Copy drivers
    if (Test-Path "$ProjectRoot\Drivers") {
        Copy-Item -Path "$ProjectRoot\Drivers\*" -Destination "$manualDeployDir\Drivers" -Recurse -Force
    }

    # Copy setup scripts
    Copy-Item -Path "$ProjectRoot\Setup\*" -Destination "$manualDeployDir\Setup" -Recurse -Force

    # Create manual install script
    $manualInstall = @'
@echo off
REM Windows Phone Next - Manual Installation
REM Run this script as Administrator after installing Windows

echo ==========================================
echo  Windows Phone Next - Manual Install
echo ==========================================
echo.

set DEPLOY_DIR=%~dp0

echo Step 1: Installing drivers...
if exist "%DEPLOY_DIR%Drivers\install-drivers.cmd" (
    call "%DEPLOY_DIR%Drivers\install-drivers.cmd"
)

echo Step 2: Copying applications...
mkdir "C:\WindowsPhoneNext" 2>nul
xcopy /E /I /Y "%DEPLOY_DIR%Apps" "C:\WindowsPhoneNext\Apps"
xcopy /E /I /Y "%DEPLOY_DIR%Setup" "C:\WindowsPhoneNext\Setup"

echo Step 3: Running setup...
powershell -ExecutionPolicy Bypass -File "C:\WindowsPhoneNext\Setup\setup.ps1"

echo Step 4: Configuring autostart...
powershell -ExecutionPolicy Bypass -File "C:\WindowsPhoneNext\Setup\configure-autostart.ps1"

echo.
echo ==========================================
echo  Installation Complete!
echo ==========================================
echo.
echo The system needs to restart to complete setup.
echo Press any key to restart now, or close this window to restart later.
pause >nul
shutdown /r /t 0
'@

    $manualInstall | Set-Content -Path "$manualDeployDir\install.cmd" -Encoding ASCII

    Write-Host ""
    Write-Host "Manual deployment package created at: $manualDeployDir" -ForegroundColor Green
    Write-Host ""
    Write-Host "To install manually:"
    Write-Host "  1. Install Windows 11 on the LattePanda 3 Delta"
    Write-Host "  2. Copy the ManualDeploy folder to the device"
    Write-Host "  3. Run install.cmd as Administrator"
    Write-Host ""

    exit 0
}

if (-not (Test-Path $IsoPath)) {
    Write-Host "ISO file not found: $IsoPath" -ForegroundColor Red
    exit 1
}

Write-Host "Using ISO: $IsoPath" -ForegroundColor Green

# Step: Create deployment image
$CurrentStep++
Write-Step $CurrentStep $TotalSteps "Creating deployment image"

& "$BuildDir\create-image.ps1" -IsoPath $IsoPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "Image creation failed!" -ForegroundColor Red
    exit 1
}

Write-Banner "Deployment Complete"

Write-Host "Your Windows Phone Next deployment image is ready!"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Create a bootable USB drive using the generated files"
Write-Host "  2. Boot the LattePanda 3 Delta from the USB"
Write-Host "  3. Wait for automatic installation to complete"
Write-Host "  4. The system will restart and boot into the Launcher"
Write-Host ""
