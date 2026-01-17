<#
.SYNOPSIS
    Configures Windows Phone Next Launcher to start automatically
.DESCRIPTION
    Sets up the Launcher to run at startup, optionally replacing Explorer shell
#>

param(
    [Parameter()]
    [switch]$ReplaceShell  # If set, replaces Explorer as the shell
)

$ErrorActionPreference = "SilentlyContinue"
$LogFile = "C:\WindowsPhoneNext\Logs\autostart.log"

function Write-Log {
    param($Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp - $Message" | Out-File -Append -FilePath $LogFile
    Write-Host $Message
}

Write-Log "Configuring Windows Phone Next autostart..."

$WPNRoot = "C:\WindowsPhoneNext"
$LauncherExe = "$WPNRoot\Apps\WindowsPhoneLauncher\WindowsPhoneLauncher.exe"

# Verify launcher exists
if (-not (Test-Path $LauncherExe)) {
    Write-Log "ERROR: Launcher not found at $LauncherExe"
    exit 1
}

# Method 1: Add to Startup folder (most compatible)
Write-Log "Adding to Startup folder..."

$startupFolder = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
$shortcutPath = Join-Path $startupFolder "WindowsPhoneLauncher.lnk"

$WshShell = New-Object -ComObject WScript.Shell
$shortcut = $WshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $LauncherExe
$shortcut.WorkingDirectory = Split-Path $LauncherExe
$shortcut.Description = "Windows Phone Next Launcher"
$shortcut.WindowStyle = 1  # Normal window
$shortcut.Save()

Write-Log "Added startup shortcut: $shortcutPath"

# Method 2: Registry Run key (backup method)
Write-Log "Adding to Registry Run key..."

$regPath = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
Set-ItemProperty -Path $regPath -Name "WindowsPhoneLauncher" -Value "`"$LauncherExe`""

Write-Log "Added registry entry"

# Method 3: Task Scheduler (most reliable for admin tasks)
Write-Log "Creating scheduled task..."

$taskName = "WindowsPhoneNextLauncher"

# Remove existing task if any
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

# Create new task
$action = New-ScheduledTaskAction -Execute $LauncherExe -WorkingDirectory (Split-Path $LauncherExe)
$trigger = New-ScheduledTaskTrigger -AtLogon
$principal = New-ScheduledTaskPrincipal -UserId "BUILTIN\Users" -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "Starts Windows Phone Next Launcher at login"

Write-Log "Created scheduled task: $taskName"

# Optional: Replace Explorer shell (kiosk mode)
if ($ReplaceShell) {
    Write-Log "Configuring shell replacement..."

    # Backup original shell
    $currentShell = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" -Name "Shell" -ErrorAction SilentlyContinue
    if ($currentShell) {
        Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" -Name "Shell_Backup" -Value $currentShell.Shell
    }

    # Set launcher as shell
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" -Name "Shell" -Value $LauncherExe

    Write-Log "Shell replaced with Launcher"
    Write-Log "Note: To restore Explorer, run: configure-autostart.ps1 -RestoreShell"
}

# Configure auto-login (ensure it's still set)
Write-Log "Verifying auto-login configuration..."

$autoLogonPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
Set-ItemProperty -Path $autoLogonPath -Name "AutoAdminLogon" -Value "1"
Set-ItemProperty -Path $autoLogonPath -Name "DefaultUserName" -Value "User"
Set-ItemProperty -Path $autoLogonPath -Name "DefaultPassword" -Value ""

Write-Log "Auto-login configured"

# Create a launcher wrapper script for additional functionality
Write-Log "Creating launcher wrapper..."

$wrapperScript = @'
@echo off
REM Windows Phone Next Launcher Wrapper
REM This script ensures the environment is ready before launching

REM Wait for desktop to be ready
timeout /t 2 /nobreak >nul

REM Set working directory
cd /d "C:\WindowsPhoneNext\Apps\WindowsPhoneLauncher"

REM Start the launcher
start "" "WindowsPhoneLauncher.exe"

REM Monitor and restart if crashed (optional)
REM :monitor
REM timeout /t 5 /nobreak >nul
REM tasklist /FI "IMAGENAME eq WindowsPhoneLauncher.exe" | find /i "WindowsPhoneLauncher.exe" >nul
REM if errorlevel 1 (
REM     echo Launcher crashed, restarting...
REM     start "" "WindowsPhoneLauncher.exe"
REM )
REM goto monitor
'@

$wrapperPath = "$WPNRoot\Setup\start-launcher.cmd"
$wrapperScript | Set-Content -Path $wrapperPath -Encoding ASCII

Write-Log "Created wrapper script: $wrapperPath"

# Create a restore script
$restoreScript = @'
<#
.SYNOPSIS
    Restores Windows Explorer as the default shell
#>

$ErrorActionPreference = "SilentlyContinue"

Write-Host "Restoring Windows Explorer shell..."

# Restore shell from backup
$backup = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" -Name "Shell_Backup" -ErrorAction SilentlyContinue
if ($backup) {
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" -Name "Shell" -Value $backup.Shell_Backup
} else {
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" -Name "Shell" -Value "explorer.exe"
}

# Remove launcher from startup
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\WindowsPhoneLauncher.lnk" -Force -ErrorAction SilentlyContinue

# Remove registry run key
Remove-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "WindowsPhoneLauncher" -ErrorAction SilentlyContinue

# Remove scheduled task
Unregister-ScheduledTask -TaskName "WindowsPhoneNextLauncher" -Confirm:$false -ErrorAction SilentlyContinue

Write-Host "Shell restored. Please restart to apply changes."
'@

$restoreScriptPath = "$WPNRoot\Setup\restore-shell.ps1"
$restoreScript | Set-Content -Path $restoreScriptPath -Encoding UTF8

Write-Log "Created restore script: $restoreScriptPath"

Write-Log "=========================================="
Write-Log "Autostart configuration complete!"
Write-Log "=========================================="
Write-Log ""
Write-Log "The Launcher will start automatically at login."
Write-Log "To disable autostart, run: restore-shell.ps1"
