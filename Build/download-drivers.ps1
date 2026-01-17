<#
.SYNOPSIS
    Downloads LattePanda 3 Delta drivers for Windows 10/11
.DESCRIPTION
    Downloads all required drivers from LattePanda's official sources
.EXAMPLE
    .\download-drivers.ps1
#>

param(
    [Parameter()]
    [string]$OutputPath = "$PSScriptRoot\..\Drivers"
)

$ErrorActionPreference = "Stop"

function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  LattePanda 3 Delta Driver Download" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# Create output directory
$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# LattePanda 3 Delta drivers from official documentation
# https://docs.lattepanda.com/content/3rd_delta_edition/drivers_and_software/

$Drivers = @(
    @{
        Name = "Chipset_Driver"
        Description = "Intel Chipset Driver"
        Url = "https://github.com/LattePandaTeam/LattePanda-Win10-Software/raw/master/Drivers/3Delta/Chipset_Driver.zip"
        FileName = "Chipset_Driver.zip"
    },
    @{
        Name = "Graphics_Driver"
        Description = "Intel UHD Graphics Driver"
        Url = "https://github.com/LattePandaTeam/LattePanda-Win10-Software/raw/master/Drivers/3Delta/Graphics_Driver.zip"
        FileName = "Graphics_Driver.zip"
    },
    @{
        Name = "Audio_Driver"
        Description = "Realtek Audio Driver"
        Url = "https://github.com/LattePandaTeam/LattePanda-Win10-Software/raw/master/Drivers/3Delta/Audio_Driver.zip"
        FileName = "Audio_Driver.zip"
    },
    @{
        Name = "WiFi_Driver"
        Description = "Intel WiFi 6 AX201 Driver"
        Url = "https://github.com/LattePandaTeam/LattePanda-Win10-Software/raw/master/Drivers/3Delta/WiFi_Driver.zip"
        FileName = "WiFi_Driver.zip"
    },
    @{
        Name = "Bluetooth_Driver"
        Description = "Intel Bluetooth Driver"
        Url = "https://github.com/LattePandaTeam/LattePanda-Win10-Software/raw/master/Drivers/3Delta/Bluetooth_Driver.zip"
        FileName = "Bluetooth_Driver.zip"
    },
    @{
        Name = "Touch_Driver"
        Description = "Touch Panel Driver"
        Url = "https://github.com/LattePandaTeam/LattePanda-Win10-Software/raw/master/Drivers/3Delta/Touch_Driver.zip"
        FileName = "Touch_Driver.zip"
    },
    @{
        Name = "SerialPort_Driver"
        Description = "USB Serial Port Driver"
        Url = "https://github.com/LattePandaTeam/LattePanda-Win10-Software/raw/master/Drivers/3Delta/SerialPort_Driver.zip"
        FileName = "SerialPort_Driver.zip"
    },
    @{
        Name = "Management_Engine_Driver"
        Description = "Intel Management Engine Interface"
        Url = "https://github.com/LattePandaTeam/LattePanda-Win10-Software/raw/master/Drivers/3Delta/Management_Engine_Driver.zip"
        FileName = "Management_Engine_Driver.zip"
    }
)

# Progress tracking
$totalDrivers = $Drivers.Count
$currentDriver = 0

Write-Info "Downloading $totalDrivers drivers to: $OutputPath"
Write-Host ""

foreach ($driver in $Drivers) {
    $currentDriver++
    $destPath = Join-Path $OutputPath $driver.FileName
    $extractPath = Join-Path $OutputPath $driver.Name

    Write-Host "[$currentDriver/$totalDrivers] $($driver.Description)... " -NoNewline

    try {
        # Download driver using curl (more reliable with GitHub)
        if (-not (Test-Path $destPath)) {
            $curlResult = & curl.exe -L -s -o $destPath $driver.Url 2>&1
            if ($LASTEXITCODE -ne 0) {
                throw "curl failed: $curlResult"
            }
        }

        # Extract driver
        if (Test-Path $destPath) {
            if (-not (Test-Path $extractPath)) {
                New-Item -ItemType Directory -Path $extractPath -Force | Out-Null
            }
            Expand-Archive -Path $destPath -DestinationPath $extractPath -Force
            Write-Success "OK"
        } else {
            Write-Warning "SKIPPED (download failed)"
        }
    }
    catch {
        Write-Error "FAILED"
        Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
}

# Create driver installation script
Write-Host ""
Write-Info "Creating driver installation script..."

$installScript = @'
@echo off
REM LattePanda 3 Delta Driver Installation Script
REM Run this script as Administrator after Windows installation

echo ==========================================
echo  LattePanda 3 Delta Driver Installation
echo ==========================================
echo.

set DRIVER_PATH=%~dp0

echo Installing Chipset Driver...
if exist "%DRIVER_PATH%Chipset_Driver\SetupChipset.exe" (
    start /wait "%DRIVER_PATH%Chipset_Driver\SetupChipset.exe" -s
)

echo Installing Graphics Driver...
if exist "%DRIVER_PATH%Graphics_Driver" (
    for /r "%DRIVER_PATH%Graphics_Driver" %%f in (*.inf) do (
        pnputil /add-driver "%%f" /install
    )
)

echo Installing Audio Driver...
if exist "%DRIVER_PATH%Audio_Driver" (
    for /r "%DRIVER_PATH%Audio_Driver" %%f in (*.inf) do (
        pnputil /add-driver "%%f" /install
    )
)

echo Installing WiFi Driver...
if exist "%DRIVER_PATH%WiFi_Driver" (
    for /r "%DRIVER_PATH%WiFi_Driver" %%f in (*.inf) do (
        pnputil /add-driver "%%f" /install
    )
)

echo Installing Bluetooth Driver...
if exist "%DRIVER_PATH%Bluetooth_Driver" (
    for /r "%DRIVER_PATH%Bluetooth_Driver" %%f in (*.inf) do (
        pnputil /add-driver "%%f" /install
    )
)

echo Installing Touch Driver...
if exist "%DRIVER_PATH%Touch_Driver" (
    for /r "%DRIVER_PATH%Touch_Driver" %%f in (*.inf) do (
        pnputil /add-driver "%%f" /install
    )
)

echo Installing Serial Port Driver...
if exist "%DRIVER_PATH%SerialPort_Driver" (
    for /r "%DRIVER_PATH%SerialPort_Driver" %%f in (*.inf) do (
        pnputil /add-driver "%%f" /install
    )
)

echo Installing Management Engine Driver...
if exist "%DRIVER_PATH%Management_Engine_Driver" (
    for /r "%DRIVER_PATH%Management_Engine_Driver" %%f in (*.inf) do (
        pnputil /add-driver "%%f" /install
    )
)

echo.
echo ==========================================
echo  Driver installation complete!
echo ==========================================
echo.

exit /b 0
'@

$installScriptPath = Join-Path $OutputPath "install-drivers.cmd"
$installScript | Set-Content -Path $installScriptPath -Encoding ASCII
Write-Success "Created: $installScriptPath"

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Download Summary" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""
Write-Info "Drivers saved to: $OutputPath"
Write-Info "Run 'install-drivers.cmd' as Administrator to install all drivers"
Write-Host ""
