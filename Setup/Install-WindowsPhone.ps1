#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Windows Phone Next - Main Installation Script
.DESCRIPTION
    Configures Windows 11 for UP Core board with LattePanda 3 Delta 864,
    PiSugar2 Plus power, 720x720 display, and EM06-A LTE modem
.NOTES
    Target Hardware:
    - UP Core Board (https://up-board.org/upcore/specifications/)
    - LattePanda 3 Delta 864
    - PiSugar2 Plus (Power Management)
    - 720x1560 9:19.5 Display
    - EM06-A LTE Card (Phone/SMS functionality)
#>

param(
    [switch]$SkipDrivers,
    [switch]$SkipDisplay,
    [switch]$SkipAutoStart,
    [switch]$SkipAppRemoval,
    [string]$InstallPath = "C:\WindowsPhoneNext"
)

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    $color = switch ($Type) {
        "Info" { "Cyan" }
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
    }
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline
    Write-Host $Message -ForegroundColor $color
}

function Test-AdminPrivileges {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Remove-UnnecessaryApps {
    Write-Status "Removing unnecessary Windows apps..."

    # List of bloatware apps to remove for a clean phone experience
    $appsToRemove = @(
        "Microsoft.3DBuilder"
        "Microsoft.BingFinance"
        "Microsoft.BingNews"
        "Microsoft.BingSports"
        "Microsoft.BingWeather"
        "Microsoft.GetHelp"
        "Microsoft.Getstarted"
        "Microsoft.Microsoft3DViewer"
        "Microsoft.MicrosoftOfficeHub"
        "Microsoft.MicrosoftSolitaireCollection"
        "Microsoft.MixedReality.Portal"
        "Microsoft.Office.OneNote"
        "Microsoft.OneConnect"
        "Microsoft.People"
        "Microsoft.Print3D"
        "Microsoft.SkypeApp"
        "Microsoft.Wallet"
        "Microsoft.WindowsAlarms"
        "Microsoft.WindowsFeedbackHub"
        "Microsoft.WindowsMaps"
        "Microsoft.Xbox.TCUI"
        "Microsoft.XboxApp"
        "Microsoft.XboxGameOverlay"
        "Microsoft.XboxGamingOverlay"
        "Microsoft.XboxIdentityProvider"
        "Microsoft.XboxSpeechToTextOverlay"
        "Microsoft.YourPhone"
        "Microsoft.ZuneMusic"
        "Microsoft.ZuneVideo"
        "Clipchamp.Clipchamp"
        "Microsoft.Todos"
        "Microsoft.PowerAutomateDesktop"
        "MicrosoftTeams"
        "Microsoft.549981C3F5F10"  # Cortana
    )

    $removedCount = 0
    foreach ($app in $appsToRemove) {
        $package = Get-AppxPackage -Name $app -AllUsers -ErrorAction SilentlyContinue
        if ($package) {
            try {
                Get-AppxPackage -Name $app -AllUsers | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue
                Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -eq $app } | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue
                $removedCount++
                Write-Status "  Removed: $app" -Type "Info"
            }
            catch {
                Write-Status "  Could not remove: $app" -Type "Warning"
            }
        }
    }

    # Disable Cortana
    $cortanaKey = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"
    if (-not (Test-Path $cortanaKey)) {
        New-Item -Path $cortanaKey -Force | Out-Null
    }
    Set-ItemProperty -Path $cortanaKey -Name "AllowCortana" -Value 0 -Type DWord -ErrorAction SilentlyContinue

    # Disable Windows Consumer Features (prevents automatic app reinstallation)
    $cloudKey = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent"
    if (-not (Test-Path $cloudKey)) {
        New-Item -Path $cloudKey -Force | Out-Null
    }
    Set-ItemProperty -Path $cloudKey -Name "DisableWindowsConsumerFeatures" -Value 1 -Type DWord -ErrorAction SilentlyContinue

    # Disable suggested apps
    Set-ItemProperty -Path $cloudKey -Name "DisableSoftLanding" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $cloudKey -Name "DisableCloudOptimizedContent" -Value 1 -Type DWord -ErrorAction SilentlyContinue

    Write-Status "Removed $removedCount unnecessary apps" -Type "Success"
}

function Install-RequiredFeatures {
    Write-Status "Installing required Windows features..."

    # Enable .NET Framework
    Enable-WindowsOptionalFeature -Online -FeatureName "NetFx3" -NoRestart -ErrorAction SilentlyContinue

    # Enable Windows Subsystem components if needed
    Write-Status "Windows features configured" -Type "Success"
}

function Set-DisplayConfiguration {
    Write-Status "Configuring 720x1560 display..."

    # Create display configuration script
    $displayScript = @"
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public class DisplaySettings {
    [DllImport("user32.dll")]
    public static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

    [DllImport("user32.dll")]
    public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public const int ENUM_CURRENT_SETTINGS = -1;
    public const int CDS_UPDATEREGISTRY = 0x01;
    public const int CDS_TEST = 0x02;
    public const int DISP_CHANGE_SUCCESSFUL = 0;
}
'@
"@

    Set-Content -Path "$InstallPath\Scripts\Set-Display.ps1" -Value $displayScript
    Write-Status "Display configuration script created" -Type "Success"
}

function Install-EM06ADrivers {
    Write-Status "Configuring EM06-A LTE modem..."

    # Create modem configuration
    $modemConfig = @{
        PortName = "COM3"  # Default, will be detected
        BaudRate = 115200
        DataBits = 8
        Parity = "None"
        StopBits = 1
        ATTimeout = 5000
    }

    $modemConfig | ConvertTo-Json | Set-Content -Path "$InstallPath\Config\modem.json"

    # Create COM port detection script
    $detectScript = @'
# Detect EM06-A COM port
$ports = Get-WmiObject Win32_PnPEntity | Where-Object { $_.Name -match "COM\d+" -and $_.Name -match "Quectel|EM06" }
if ($ports) {
    $portMatch = $ports.Name | Select-String -Pattern "COM(\d+)" | ForEach-Object { $_.Matches.Value }
    Write-Output $portMatch
} else {
    # Fallback to checking all COM ports
    $comPorts = [System.IO.Ports.SerialPort]::GetPortNames()
    Write-Output $comPorts
}
'@

    Set-Content -Path "$InstallPath\Scripts\Detect-ModemPort.ps1" -Value $detectScript
    Write-Status "EM06-A modem configured" -Type "Success"
}

function Set-AutoStartLauncher {
    Write-Status "Configuring auto-start launcher..."

    # Create startup task
    $taskAction = New-ScheduledTaskAction -Execute "$InstallPath\Launcher\WindowsPhoneLauncher.exe"
    $taskTrigger = New-ScheduledTaskTrigger -AtLogOn
    $taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    $taskPrincipal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -RunLevel Highest

    Register-ScheduledTask -TaskName "WindowsPhoneLauncher" -Action $taskAction -Trigger $taskTrigger -Settings $taskSettings -Principal $taskPrincipal -Force

    # Also add to shell startup
    $shellKey = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
    # Note: For full kiosk mode, uncomment the following:
    # Set-ItemProperty -Path $shellKey -Name "Shell" -Value "$InstallPath\Launcher\WindowsPhoneLauncher.exe"

    Write-Status "Auto-start configured" -Type "Success"
}

function Set-PowerManagement {
    Write-Status "Configuring power management for PiSugar2 Plus..."

    # Create power scheme optimized for mobile device
    $powerConfig = @'
# Disable sleep when on battery for phone functionality
powercfg /change standby-timeout-ac 0
powercfg /change standby-timeout-dc 0
powercfg /change hibernate-timeout-ac 0
powercfg /change hibernate-timeout-dc 0

# Optimize for battery
powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 80
powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 60

# Keep WiFi/LTE active
powercfg /setacvalueindex SCHEME_CURRENT SUB_WIRELESS 12BBEBE6-58D6-4636-95BB-3217EF867C1A 0
powercfg /setdcvalueindex SCHEME_CURRENT SUB_WIRELESS 12BBEBE6-58D6-4636-95BB-3217EF867C1A 0

powercfg /setactive SCHEME_CURRENT
'@

    Set-Content -Path "$InstallPath\Scripts\Set-PowerManagement.ps1" -Value $powerConfig
    & "$InstallPath\Scripts\Set-PowerManagement.ps1"

    Write-Status "Power management configured" -Type "Success"
}

function Install-Applications {
    Write-Status "Installing Windows Phone applications..."

    # Create application directories
    $apps = @("Launcher", "Dialer", "Messaging")
    foreach ($app in $apps) {
        $appPath = Join-Path $InstallPath $app
        if (-not (Test-Path $appPath)) {
            New-Item -ItemType Directory -Path $appPath -Force | Out-Null
        }
    }

    # Copy applications if they exist in setup directory
    $sourceApps = Join-Path $ScriptPath "..\Apps"
    if (Test-Path $sourceApps) {
        Copy-Item -Path "$sourceApps\*" -Destination $InstallPath -Recurse -Force
    }

    Write-Status "Applications installed" -Type "Success"
}

function Set-TouchConfiguration {
    Write-Status "Configuring touch input for 720x1560 display..."

    # Touch calibration script
    $touchConfig = @'
# Configure touch input mapping for 720x1560 display
# This may need adjustment based on specific touch controller

Add-Type -AssemblyName System.Windows.Forms

# Get screen dimensions
$screen = [System.Windows.Forms.Screen]::PrimaryScreen
Write-Host "Primary Screen: $($screen.Bounds.Width)x$($screen.Bounds.Height)"

# For capacitive touch panels, configure proper scaling
$touchKey = "HKLM:\SOFTWARE\Microsoft\Wisp\Touch"
if (-not (Test-Path $touchKey)) {
    New-Item -Path $touchKey -Force | Out-Null
}

# Enable touch
Set-ItemProperty -Path $touchKey -Name "TouchGate" -Value 1 -Type DWord -ErrorAction SilentlyContinue
'@

    Set-Content -Path "$InstallPath\Scripts\Set-TouchConfig.ps1" -Value $touchConfig
    Write-Status "Touch configuration created" -Type "Success"
}

# Main installation flow
function Start-Installation {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host "  Windows Phone Next - Installation    " -ForegroundColor Magenta
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host ""

    if (-not (Test-AdminPrivileges)) {
        Write-Status "This script requires administrator privileges" -Type "Error"
        exit 1
    }

    # Create installation directory structure
    Write-Status "Creating installation directories..."
    $directories = @(
        $InstallPath,
        "$InstallPath\Config",
        "$InstallPath\Scripts",
        "$InstallPath\Launcher",
        "$InstallPath\Dialer",
        "$InstallPath\Messaging",
        "$InstallPath\Logs",
        "$InstallPath\Data"
    )

    foreach ($dir in $directories) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
    }
    Write-Status "Directories created" -Type "Success"

    # Run installation steps
    if (-not $SkipAppRemoval) {
        Remove-UnnecessaryApps
    }
    Install-RequiredFeatures

    if (-not $SkipDisplay) {
        Set-DisplayConfiguration
        Set-TouchConfiguration
    }

    if (-not $SkipDrivers) {
        Install-EM06ADrivers
    }

    Set-PowerManagement
    Install-Applications

    if (-not $SkipAutoStart) {
        Set-AutoStartLauncher
    }

    Write-Host ""
    Write-Status "Installation complete!" -Type "Success"
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Build and deploy the applications from the Apps directory"
    Write-Host "2. Configure the EM06-A modem COM port in $InstallPath\Config\modem.json"
    Write-Host "3. Restart the system to apply all changes"
    Write-Host ""
}

# Run installation
Start-Installation
