<#
.SYNOPSIS
    Creates a custom Windows 11 LTSC image with Windows Phone Next
.DESCRIPTION
    Downloads Windows 11 LTSC, integrates drivers and applications,
    and creates a bootable USB-ready image
.EXAMPLE
    .\create-image.ps1 -IsoPath "C:\Windows11.iso"
    .\create-image.ps1 -DownloadWindows
#>

param(
    [Parameter()]
    [string]$IsoPath,

    [Parameter()]
    [switch]$DownloadWindows,

    [Parameter()]
    [string]$WorkingDir = "$PSScriptRoot\..\ImageWork",

    [Parameter()]
    [string]$OutputIso = "$PSScriptRoot\..\WindowsPhoneNext.iso"
)

$ErrorActionPreference = "Stop"

function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Step { param($Step, $Message) Write-Host "[$Step] $Message" -ForegroundColor Yellow }

# Check for admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "This script requires Administrator privileges. Please run as Administrator."
    exit 1
}

Write-Host ""
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host "  Windows Phone Next - Custom Image Creator" -ForegroundColor Magenta
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host ""

# Paths
$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
$OutputDir = Join-Path $ProjectRoot "Output"
$DriversDir = Join-Path $ProjectRoot "Drivers"
$SetupDir = Join-Path $ProjectRoot "Setup"
$WorkingDir = [System.IO.Path]::GetFullPath($WorkingDir)
$MountDir = Join-Path $WorkingDir "Mount"
$IsoExtractDir = Join-Path $WorkingDir "ISO"
$WinPEMount = Join-Path $WorkingDir "WinPE"

# Check prerequisites
Write-Step "1/10" "Checking prerequisites..."

# Check for DISM
if (-not (Get-Command "dism.exe" -ErrorAction SilentlyContinue)) {
    Write-Error "DISM not found. Please run on Windows with DISM installed."
    exit 1
}

# Check for oscdimg (part of Windows ADK)
$oscdimg = "${env:ProgramFiles(x86)}\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe"
if (-not (Test-Path $oscdimg)) {
    Write-Warning "oscdimg.exe not found. Please install Windows ADK."
    Write-Warning "Download from: https://go.microsoft.com/fwlink/?linkid=2196127"
    Write-Info "Continuing without ISO creation capability..."
    $oscdimg = $null
}

# Check for built applications
if (-not (Test-Path $OutputDir)) {
    Write-Warning "Built applications not found. Running build script..."
    & "$PSScriptRoot\build-all.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed. Please fix build errors first."
        exit 1
    }
}

Write-Success "Prerequisites check passed"

# Create working directories
Write-Step "2/10" "Creating working directories..."
@($WorkingDir, $MountDir, $IsoExtractDir, $WinPEMount) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}
Write-Success "Working directories created"

# Handle Windows ISO
Write-Step "3/10" "Preparing Windows ISO..."

if ($DownloadWindows) {
    Write-Info "Note: Windows 11 LTSC requires a Volume License or MSDN subscription."
    Write-Info "You can obtain the ISO from:"
    Write-Info "  - Microsoft Volume Licensing Service Center (VLSC)"
    Write-Info "  - Visual Studio Subscriptions (formerly MSDN)"
    Write-Info "  - Microsoft Evaluation Center (90-day trial)"
    Write-Host ""
    Write-Info "Evaluation Center URL: https://www.microsoft.com/en-us/evalcenter/evaluate-windows-11-enterprise"
    Write-Host ""

    # For evaluation, we can use the Media Creation Tool approach
    $evalUrl = "https://www.microsoft.com/en-us/evalcenter/download-windows-11-enterprise"
    Write-Warning "Please download Windows 11 Enterprise LTSC ISO manually and specify with -IsoPath"
    Write-Info "Once downloaded, run: .\create-image.ps1 -IsoPath 'C:\path\to\windows.iso'"
    exit 0
}

if (-not $IsoPath -or -not (Test-Path $IsoPath)) {
    Write-Error "Windows ISO not found. Please specify a valid ISO path with -IsoPath"
    Write-Info "Or run with -DownloadWindows for download instructions"
    exit 1
}

Write-Success "Using ISO: $IsoPath"

# Mount and extract ISO
Write-Step "4/10" "Extracting Windows ISO..."

$mountResult = Mount-DiskImage -ImagePath $IsoPath -PassThru
$driveLetter = ($mountResult | Get-Volume).DriveLetter

if (-not $driveLetter) {
    Write-Error "Failed to mount ISO"
    exit 1
}

Write-Info "ISO mounted at ${driveLetter}:"

# Copy ISO contents
Write-Info "Copying ISO contents (this may take a while)..."
robocopy "${driveLetter}:\" $IsoExtractDir /E /NFL /NDL /NJH /NJS /nc /ns /np

# Unmount ISO
Dismount-DiskImage -ImagePath $IsoPath | Out-Null
Write-Success "ISO extracted"

# Find the install.wim
$installWim = Join-Path $IsoExtractDir "sources\install.wim"
if (-not (Test-Path $installWim)) {
    # Check for install.esd
    $installEsd = Join-Path $IsoExtractDir "sources\install.esd"
    if (Test-Path $installEsd) {
        Write-Info "Converting install.esd to install.wim..."
        dism /Export-Image /SourceImageFile:$installEsd /SourceIndex:1 /DestinationImageFile:$installWim /Compress:max
        Remove-Item $installEsd -Force
    } else {
        Write-Error "install.wim not found in ISO"
        exit 1
    }
}

# Get available Windows editions
Write-Step "5/10" "Checking Windows editions..."
$wimInfo = dism /Get-ImageInfo /ImageFile:$installWim
Write-Host $wimInfo

# Mount the Windows image (usually index 1 for single-edition ISOs)
Write-Step "6/10" "Mounting Windows image..."
dism /Mount-Wim /WimFile:$installWim /Index:1 /MountDir:$MountDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to mount Windows image"
    exit 1
}
Write-Success "Windows image mounted"

# Download and integrate drivers
Write-Step "7/10" "Integrating drivers..."

if (-not (Test-Path (Join-Path $DriversDir "Chipset_Driver"))) {
    Write-Info "Downloading drivers..."
    & "$PSScriptRoot\download-drivers.ps1" -OutputPath $DriversDir
}

# Add drivers to the image
$driverFolders = Get-ChildItem -Path $DriversDir -Directory
foreach ($folder in $driverFolders) {
    $infFiles = Get-ChildItem -Path $folder.FullName -Recurse -Filter "*.inf"
    if ($infFiles.Count -gt 0) {
        Write-Info "  Adding drivers from $($folder.Name)..."
        dism /Image:$MountDir /Add-Driver /Driver:$($folder.FullName) /Recurse /ForceUnsigned 2>$null
    }
}
Write-Success "Drivers integrated"

# Copy Windows Phone Next applications
Write-Step "8/10" "Integrating Windows Phone Next applications..."

$wpnDir = Join-Path $MountDir "WindowsPhoneNext"
$wpnAppsDir = Join-Path $wpnDir "Apps"
$wpnSetupDir = Join-Path $wpnDir "Setup"

New-Item -ItemType Directory -Path $wpnAppsDir -Force | Out-Null
New-Item -ItemType Directory -Path $wpnSetupDir -Force | Out-Null

# Copy all built applications
Write-Info "  Copying applications..."
Get-ChildItem -Path $OutputDir -Directory | ForEach-Object {
    if ($_.Name -ne "_Shared") {
        Copy-Item -Path $_.FullName -Destination $wpnAppsDir -Recurse -Force
    }
}

# Copy setup scripts
Write-Info "  Copying setup scripts..."
if (Test-Path $SetupDir) {
    Copy-Item -Path "$SetupDir\*" -Destination $wpnSetupDir -Recurse -Force
}

# Copy apps manifest
Copy-Item -Path (Join-Path $OutputDir "apps-manifest.json") -Destination $wpnDir -Force

Write-Success "Applications integrated"

# Create setup registry entries for first boot
Write-Step "9/10" "Configuring first-boot setup..."

# Create first-boot script
$firstBootScript = @'
@echo off
REM Windows Phone Next - First Boot Setup
REM This script runs automatically on first login

echo ==========================================
echo  Windows Phone Next - Setup
echo ==========================================

REM Run setup script
if exist "C:\WindowsPhoneNext\Setup\setup.ps1" (
    powershell -ExecutionPolicy Bypass -File "C:\WindowsPhoneNext\Setup\setup.ps1"
)

REM Remove this script from RunOnce
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce" /v "WPNSetup" /f

exit /b 0
'@

$firstBootPath = Join-Path $wpnSetupDir "first-boot.cmd"
$firstBootScript | Set-Content -Path $firstBootPath -Encoding ASCII

# Load the offline registry hive
$softwareHive = Join-Path $MountDir "Windows\System32\config\SOFTWARE"
reg load "HKLM\OFFLINE_SOFTWARE" $softwareHive

# Add RunOnce entry
reg add "HKLM\OFFLINE_SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce" /v "WPNSetup" /t REG_SZ /d "C:\WindowsPhoneNext\Setup\first-boot.cmd" /f

# Unload the hive
reg unload "HKLM\OFFLINE_SOFTWARE"

Write-Success "First-boot setup configured"

# Copy Autounattend.xml
Write-Info "Copying Autounattend.xml..."
$autounattendSrc = Join-Path $SetupDir "Autounattend.xml"
if (Test-Path $autounattendSrc) {
    Copy-Item -Path $autounattendSrc -Destination $IsoExtractDir -Force
}

# Unmount and save the image
Write-Step "10/10" "Saving Windows image..."
dism /Unmount-Wim /MountDir:$MountDir /Commit

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to save Windows image"
    dism /Unmount-Wim /MountDir:$MountDir /Discard
    exit 1
}
Write-Success "Windows image saved"

# Create bootable ISO
if ($oscdimg) {
    Write-Info "Creating bootable ISO..."

    $etfsboot = Join-Path $IsoExtractDir "boot\etfsboot.com"
    $efisys = Join-Path $IsoExtractDir "efi\microsoft\boot\efisys.bin"

    if ((Test-Path $etfsboot) -and (Test-Path $efisys)) {
        & $oscdimg -m -o -u2 -udfver102 `
            -bootdata:"2#p0,e,b$etfsboot#pEF,e,b$efisys" `
            $IsoExtractDir `
            $OutputIso

        if ($LASTEXITCODE -eq 0) {
            Write-Success "Bootable ISO created: $OutputIso"
        } else {
            Write-Error "Failed to create ISO"
        }
    } else {
        Write-Warning "Boot files not found. ISO creation skipped."
        Write-Info "You can manually create a bootable USB from: $IsoExtractDir"
    }
} else {
    Write-Warning "oscdimg.exe not available. ISO creation skipped."
    Write-Info "You can manually create a bootable USB from: $IsoExtractDir"
}

# Create USB creation script
$usbScript = @'
<#
.SYNOPSIS
    Creates a bootable USB drive from the Windows Phone Next image
.EXAMPLE
    .\create-usb.ps1 -DriveLetter E
#>
param(
    [Parameter(Mandatory)]
    [string]$DriveLetter
)

$ErrorActionPreference = "Stop"

# Confirm
Write-Host "WARNING: This will ERASE all data on drive ${DriveLetter}:" -ForegroundColor Red
$confirm = Read-Host "Type 'YES' to continue"
if ($confirm -ne "YES") {
    Write-Host "Cancelled."
    exit
}

# Format the drive
Write-Host "Formatting drive..."
$disk = Get-Disk | Where-Object { $_.PartitionStyle -eq 'MBR' -or $_.PartitionStyle -eq 'GPT' } |
    Get-Partition | Where-Object { $_.DriveLetter -eq $DriveLetter } |
    Get-Disk

Clear-Disk -Number $disk.Number -RemoveData -Confirm:$false
Initialize-Disk -Number $disk.Number -PartitionStyle GPT

# Create partitions
$systemPartition = New-Partition -DiskNumber $disk.Number -Size 100MB -GptType "{c12a7328-f81f-11d2-ba4b-00a0c93ec93b}"
Format-Volume -Partition $systemPartition -FileSystem FAT32 -NewFileSystemLabel "SYSTEM"

$dataPartition = New-Partition -DiskNumber $disk.Number -UseMaximumSize -AssignDriveLetter
Format-Volume -Partition $dataPartition -FileSystem NTFS -NewFileSystemLabel "WPN_INSTALL"

# Copy files
$usbLetter = $dataPartition.DriveLetter
Write-Host "Copying files to ${usbLetter}:..."
robocopy "$PSScriptRoot" "${usbLetter}:\" /E /NFL /NDL

Write-Host "Done! USB drive is ready for installation." -ForegroundColor Green
'@

$usbScriptPath = Join-Path $IsoExtractDir "create-usb.ps1"
$usbScript | Set-Content -Path $usbScriptPath -Encoding UTF8

# Summary
Write-Host ""
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host "  Image Creation Complete!" -ForegroundColor Magenta
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host ""
Write-Info "Image files location: $IsoExtractDir"
if ($oscdimg -and (Test-Path $OutputIso)) {
    Write-Info "Bootable ISO: $OutputIso"
}
Write-Host ""
Write-Info "To create a bootable USB drive:"
Write-Host "  1. Insert a USB drive (8GB+ recommended)"
Write-Host "  2. Run: .\create-usb.ps1 -DriveLetter X"
Write-Host "     (Replace X with your USB drive letter)"
Write-Host ""
Write-Info "Installation will automatically:"
Write-Host "  - Install Windows 11 LTSC"
Write-Host "  - Install LattePanda 3 Delta drivers"
Write-Host "  - Install Windows Phone Next applications"
Write-Host "  - Configure auto-boot to Launcher"
Write-Host ""
