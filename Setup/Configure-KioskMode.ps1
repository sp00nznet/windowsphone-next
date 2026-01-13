#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Configures Windows 11 Kiosk Mode for Windows Phone Next
.DESCRIPTION
    Sets up single-app kiosk mode with the launcher as the shell replacement
#>

param(
    [string]$LauncherPath = "C:\WindowsPhoneNext\Launcher\WindowsPhoneLauncher.exe",
    [switch]$Revert
)

$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    $color = switch ($Type) {
        "Info" { "Cyan" }
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
    }
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor $color
}

function Enable-KioskMode {
    Write-Status "Enabling Kiosk Mode..."

    # Backup current shell
    $shellKey = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
    $currentShell = Get-ItemPropertyValue -Path $shellKey -Name "Shell" -ErrorAction SilentlyContinue

    if ($currentShell) {
        Set-ItemProperty -Path $shellKey -Name "Shell_Backup" -Value $currentShell
    }

    # Set launcher as shell
    Set-ItemProperty -Path $shellKey -Name "Shell" -Value $LauncherPath

    # Disable Task Manager
    $polKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\System"
    if (-not (Test-Path $polKey)) {
        New-Item -Path $polKey -Force | Out-Null
    }
    Set-ItemProperty -Path $polKey -Name "DisableTaskMgr" -Value 1 -Type DWord

    # Disable Win key
    $explorerKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"
    if (-not (Test-Path $explorerKey)) {
        New-Item -Path $explorerKey -Force | Out-Null
    }
    Set-ItemProperty -Path $explorerKey -Name "NoWinKeys" -Value 1 -Type DWord

    # Hide taskbar
    $taskbarKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3"
    # Taskbar will be hidden by the launcher application itself

    Write-Status "Kiosk Mode enabled. Restart required." -Type "Success"
}

function Disable-KioskMode {
    Write-Status "Reverting Kiosk Mode..."

    $shellKey = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"

    # Restore original shell
    $backupShell = Get-ItemPropertyValue -Path $shellKey -Name "Shell_Backup" -ErrorAction SilentlyContinue
    if ($backupShell) {
        Set-ItemProperty -Path $shellKey -Name "Shell" -Value $backupShell
    } else {
        Set-ItemProperty -Path $shellKey -Name "Shell" -Value "explorer.exe"
    }

    # Re-enable Task Manager
    $polKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\System"
    Remove-ItemProperty -Path $polKey -Name "DisableTaskMgr" -ErrorAction SilentlyContinue

    # Re-enable Win key
    $explorerKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"
    Remove-ItemProperty -Path $explorerKey -Name "NoWinKeys" -ErrorAction SilentlyContinue

    Write-Status "Kiosk Mode disabled. Restart required." -Type "Success"
}

if ($Revert) {
    Disable-KioskMode
} else {
    Enable-KioskMode
}
