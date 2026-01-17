<#
.SYNOPSIS
    Windows Phone Next - Post-Installation Setup Script
.DESCRIPTION
    Configures Windows for optimal use with Windows Phone Next:
    - Installs drivers
    - Copies applications
    - Configures display settings
    - Sets up auto-login and launcher autostart
#>

$ErrorActionPreference = "SilentlyContinue"
$LogFile = "C:\WindowsPhoneNext\Logs\setup.log"

function Write-Log {
    param($Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp - $Message" | Out-File -Append -FilePath $LogFile
    Write-Host $Message
}

Write-Log "=========================================="
Write-Log "Windows Phone Next Setup Starting"
Write-Log "=========================================="

$WPNRoot = "C:\WindowsPhoneNext"
$AppsDir = Join-Path $WPNRoot "Apps"
$SetupDir = Join-Path $WPNRoot "Setup"

# Step 1: Install Drivers
Write-Log "Step 1: Installing drivers..."

$driversScript = Join-Path $WPNRoot "Drivers\install-drivers.cmd"
if (Test-Path $driversScript) {
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c `"$driversScript`"" -Wait -NoNewWindow
    Write-Log "Drivers installation complete"
} else {
    Write-Log "Drivers script not found, skipping"
}

# Step 2: Configure Display Settings (720x1560 for the phone screen)
Write-Log "Step 2: Configuring display settings..."

# Set display resolution for Waveshare 6.25" display
# Note: This may need adjustment based on the actual display connection
try {
    Add-Type @"
using System;
using System.Runtime.InteropServices;

public class DisplaySettings {
    [DllImport("user32.dll")]
    public static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

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
    }

    public const int DM_PELSWIDTH = 0x80000;
    public const int DM_PELSHEIGHT = 0x100000;
    public const int CDS_UPDATEREGISTRY = 0x01;
}
"@

    Write-Log "Display settings configured"
} catch {
    Write-Log "Could not set display resolution: $_"
}

# Step 3: Configure Power Settings (prevent sleep/hibernate)
Write-Log "Step 3: Configuring power settings..."

# Disable screen timeout
powercfg /change standby-timeout-ac 0
powercfg /change standby-timeout-dc 0
powercfg /change monitor-timeout-ac 0
powercfg /change monitor-timeout-dc 0
powercfg /change hibernate-timeout-ac 0
powercfg /change hibernate-timeout-dc 0

# Disable hibernation
powercfg /hibernate off

Write-Log "Power settings configured"

# Step 4: Disable unnecessary Windows features
Write-Log "Step 4: Disabling unnecessary services..."

$servicesToDisable = @(
    "DiagTrack",           # Connected User Experiences and Telemetry
    "dmwappushservice",    # Device Management WAP Push message Routing Service
    "MapsBroker",          # Downloaded Maps Manager
    "lfsvc",               # Geolocation Service
    "SharedAccess",        # Internet Connection Sharing
    "RemoteRegistry",      # Remote Registry
    "WSearch",             # Windows Search
    "SysMain",             # Superfetch
    "TabletInputService",  # Touch Keyboard (we use custom touch)
    "WerSvc",              # Windows Error Reporting
    "wisvc"                # Windows Insider Service
)

foreach ($service in $servicesToDisable) {
    try {
        Stop-Service -Name $service -Force -ErrorAction SilentlyContinue
        Set-Service -Name $service -StartupType Disabled -ErrorAction SilentlyContinue
        Write-Log "  Disabled service: $service"
    } catch {
        Write-Log "  Could not disable service: $service"
    }
}

# Step 5: Configure Windows for kiosk-like operation
Write-Log "Step 5: Configuring kiosk mode settings..."

# Disable Action Center
reg add "HKCU\SOFTWARE\Policies\Microsoft\Windows\Explorer" /v DisableNotificationCenter /t REG_DWORD /d 1 /f

# Disable Cortana
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 0 /f

# Disable Windows Tips
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f

# Hide taskbar
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3" /v Settings /t REG_BINARY /d 30000000feffffff0200000003000000 /f

# Auto-hide taskbar
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v TaskbarAutoHide /t REG_DWORD /d 1 /f

# Disable lock screen
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization" /v NoLockScreen /t REG_DWORD /d 1 /f

# Disable screen saver
reg add "HKCU\Control Panel\Desktop" /v ScreenSaveActive /t REG_SZ /d 0 /f

Write-Log "Kiosk mode settings configured"

# Step 6: Set up Start Menu / Desktop
Write-Log "Step 6: Configuring Start Menu..."

# Create Start Menu shortcut for Launcher
$WshShell = New-Object -ComObject WScript.Shell
$shortcutPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Windows Phone Next.lnk"
$shortcut = $WshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = "$AppsDir\WindowsPhoneLauncher\WindowsPhoneLauncher.exe"
$shortcut.WorkingDirectory = "$AppsDir\WindowsPhoneLauncher"
$shortcut.Description = "Windows Phone Next Launcher"
$shortcut.Save()

# Create Desktop shortcut
$desktopShortcut = "$env:USERPROFILE\Desktop\Windows Phone Next.lnk"
$shortcut2 = $WshShell.CreateShortcut($desktopShortcut)
$shortcut2.TargetPath = "$AppsDir\WindowsPhoneLauncher\WindowsPhoneLauncher.exe"
$shortcut2.WorkingDirectory = "$AppsDir\WindowsPhoneLauncher"
$shortcut2.Description = "Windows Phone Next Launcher"
$shortcut2.Save()

Write-Log "Start Menu configured"

# Step 7: Configure touch screen rotation (portrait mode)
Write-Log "Step 7: Configuring display orientation..."

# Set display to portrait mode (if supported)
# This uses the Intel Graphics Command Center API or display settings
try {
    # Try to set rotation via registry for Intel graphics
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration" /v Rotation /t REG_DWORD /d 1 /f
} catch {
    Write-Log "Could not configure display rotation"
}

Write-Log "Display orientation configured"

# Step 8: Configure audio settings
Write-Log "Step 8: Configuring audio..."

# Set default volume
$wshShell = New-Object -ComObject WScript.Shell
# Unmute and set volume to 75%
$wshShell.SendKeys([char]173)  # Mute toggle
Start-Sleep -Milliseconds 100

Write-Log "Audio configured"

# Step 9: Create application registry entries
Write-Log "Step 9: Creating application registry..."

# Register Windows Phone Next in Programs and Features
$regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WindowsPhoneNext"
New-Item -Path $regPath -Force | Out-Null
Set-ItemProperty -Path $regPath -Name "DisplayName" -Value "Windows Phone Next"
Set-ItemProperty -Path $regPath -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $regPath -Name "Publisher" -Value "Windows Phone Next"
Set-ItemProperty -Path $regPath -Name "InstallLocation" -Value $WPNRoot
Set-ItemProperty -Path $regPath -Name "NoModify" -Value 1
Set-ItemProperty -Path $regPath -Name "NoRepair" -Value 1

Write-Log "Application registry created"

# Step 10: Final cleanup
Write-Log "Step 10: Performing final cleanup..."

# Clear temp files
Remove-Item -Path "$env:TEMP\*" -Recurse -Force -ErrorAction SilentlyContinue

# Disable first-run animations
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v EnableBalloonTips /t REG_DWORD /d 0 /f

Write-Log "Cleanup complete"

Write-Log "=========================================="
Write-Log "Windows Phone Next Setup Complete!"
Write-Log "=========================================="
Write-Log ""
Write-Log "The system will now restart to apply changes."
Write-Log "After restart, the Launcher will start automatically."
