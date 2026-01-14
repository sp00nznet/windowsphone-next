#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Windows Phone Next - System Optimization Script
.DESCRIPTION
    Removes unnecessary Windows apps and services to create a minimal phone-like environment.
    Installs Windows Subsystem for Android (WSA) for Android app sideloading.
    Installs Windows Subsystem for Linux (WSL) with Ubuntu for development/terminal access.
    Preserves touch input, on-screen keyboard, and essential system components.
.PARAMETER SkipWSA
    Skip Windows Subsystem for Android installation
.PARAMETER SkipWSL
    Skip Windows Subsystem for Linux and Ubuntu installation
.PARAMETER SkipADB
    Skip ADB (Android Debug Bridge) installation
.PARAMETER WhatIf
    Show what would be removed without actually removing
.PARAMETER Restore
    Attempt to restore previously removed apps
.EXAMPLE
    .\Remove-Bloatware.ps1
    Removes bloatware and installs WSA + WSL/Ubuntu + ADB
.EXAMPLE
    .\Remove-Bloatware.ps1 -WhatIf
    Shows what would be removed without making changes
.EXAMPLE
    .\Remove-Bloatware.ps1 -SkipWSA -SkipWSL
    Remove bloatware only, skip Android and Linux support
#>

param(
    [switch]$SkipWSA,
    [switch]$SkipWSL,
    [switch]$SkipADB,
    [switch]$WhatIf,
    [switch]$Restore
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
        "Header" { "Magenta" }
        "Remove" { "DarkRed" }
        "Keep" { "DarkGreen" }
    }
    $prefix = switch ($Type) {
        "Remove" { "[-]" }
        "Keep" { "[+]" }
        default { "[*]" }
    }
    Write-Host "$prefix " -NoNewline -ForegroundColor $color
    Write-Host $Message -ForegroundColor $color
}

# =============================================================================
# APPS TO REMOVE - Bloatware and unnecessary Windows apps
# =============================================================================
$AppsToRemove = @(
    # Xbox and Gaming
    "Microsoft.Xbox.TCUI"
    "Microsoft.XboxApp"
    "Microsoft.XboxGameOverlay"
    "Microsoft.XboxGamingOverlay"
    "Microsoft.XboxIdentityProvider"
    "Microsoft.XboxSpeechToTextOverlay"
    "Microsoft.GamingApp"
    "Microsoft.GamingServices"

    # Cortana and AI
    "Microsoft.549981C3F5F10"  # Cortana
    "Microsoft.Copilot"
    "Microsoft.Windows.Ai.Copilot.Provider"

    # Entertainment (we have our own apps)
    "Microsoft.ZuneMusic"           # Groove Music - we have Music app
    "Microsoft.ZuneVideo"           # Movies & TV - we have Video app
    "SpotifyAB.SpotifyMusic"
    "Disney.37853FC22B2CE"          # Disney+

    # Social and Communication (we have our own)
    "Microsoft.SkypeApp"
    "Microsoft.People"              # We have Contacts app
    "microsoft.windowscommunicationsapps"  # Mail/Calendar - we have our own
    "Microsoft.Teams"
    "MicrosoftTeams"

    # Office bloat (not needed for phone)
    "Microsoft.MicrosoftOfficeHub"
    "Microsoft.Office.OneNote"
    "Microsoft.MicrosoftSolitaireCollection"  # We have our own Solitaire
    "Microsoft.PowerAutomateDesktop"
    "Clipchamp.Clipchamp"
    "Microsoft.Todos"

    # 3D and Mixed Reality
    "Microsoft.Microsoft3DViewer"
    "Microsoft.3DBuilder"
    "Microsoft.MixedReality.Portal"
    "Microsoft.Print3D"

    # Misc bloatware
    "Microsoft.BingWeather"
    "Microsoft.BingNews"
    "Microsoft.BingFinance"
    "Microsoft.BingSports"
    "Microsoft.GetHelp"
    "Microsoft.Getstarted"          # Tips
    "Microsoft.WindowsFeedbackHub"
    "Microsoft.MicrosoftStickyNotes"
    "Microsoft.WindowsAlarms"       # We could make our own
    "Microsoft.WindowsMaps"         # We have Maps app
    "Microsoft.YourPhone"           # Ironic - we ARE the phone
    "Microsoft.WindowsSoundRecorder"
    "Microsoft.Wallet"
    "Microsoft.OneConnect"

    # Third-party bloat often pre-installed
    "king.com.CandyCrushSaga"
    "king.com.CandyCrushSodaSaga"
    "king.com.BubbleWitch3Saga"
    "king.com.CandyCrushFriends"
    "FACEBOOK.FACEBOOK"
    "Facebook.Instagram"
    "BytedancePte.Ltd.TikTok"
    "AmazonVideo.PrimeVideo"
    "Amazon.com.Amazon"
    "DolbyLaboratories.DolbyAccess"
    "Flipboard.Flipboard"
    "ShazamEntertainmentLtd.Shazam"
    "ClearChannelRadioDigital.iHeartRadio"
    "TheNewYorkTimes.NYTCrossword"
    "NORDCURRENT.COOKINGFEVER"
    "A278AB0D.MarchofEmpires"
    "A278AB0D.DragonManiaLegends"
    "828B5831.HiddenCityMysteryofShadows"
    "WinZipComputing.WinZipUniversal"

    # Dev tools not needed on device
    "Microsoft.WindowsTerminal"      # Not needed for phone UI
    "Microsoft.PowerShell"           # Keep system PowerShell, remove app
)

# =============================================================================
# APPS TO KEEP - Essential for Windows Phone functionality
# =============================================================================
$AppsToKeep = @(
    # Essential Windows components
    "Microsoft.WindowsStore"         # Needed for WSA installation
    "Microsoft.StorePurchaseApp"
    "Microsoft.DesktopAppInstaller"  # For MSIX/AppX installs
    "Microsoft.VCLibs*"              # Visual C++ runtime
    "Microsoft.NET*"                 # .NET runtime
    "Microsoft.UI.Xaml*"             # UI framework
    "Microsoft.HEIFImageExtension"   # Image format support
    "Microsoft.HEVCVideoExtension"   # Video codec
    "Microsoft.VP9VideoExtensions"   # Video codec
    "Microsoft.WebMediaExtensions"   # Media support
    "Microsoft.WebpImageExtension"   # WebP support
    "Microsoft.RawImageExtension"    # RAW image support

    # Windows Subsystem for Android (install if missing)
    "MicrosoftCorporationII.WindowsSubsystemForAndroid"

    # WebView2 - needed for Browser, Gmail, Maps
    "Microsoft.WebView2*"

    # Touch and input (CRITICAL)
    "Microsoft.InputApp"             # Touch keyboard
    "Microsoft.ScreenSketch"         # Screen capture (useful)

    # Photos - may be useful as fallback
    "Microsoft.Windows.Photos"

    # Calculator - handy utility
    "Microsoft.WindowsCalculator"

    # Notepad - basic text editing
    "Microsoft.WindowsNotepad"

    # Paint - basic image editing
    "Microsoft.Paint"
)

# =============================================================================
# SERVICES TO DISABLE - Not needed for phone
# =============================================================================
$ServicesToDisable = @(
    "XblAuthManager"                 # Xbox Live Auth
    "XblGameSave"                    # Xbox Live Game Save
    "XboxGipSvc"                     # Xbox Accessory Management
    "XboxNetApiSvc"                  # Xbox Live Networking
    "WSearch"                        # Windows Search (optional)
    "DiagTrack"                      # Telemetry
    "dmwappushservice"               # WAP Push Message
    "MapsBroker"                     # Downloaded Maps Manager
    "lfsvc"                          # Geolocation Service (we use GPS directly)
    "RetailDemo"                     # Retail Demo Service
    "WMPNetworkSvc"                  # Windows Media Player Network
    "wisvc"                          # Windows Insider Service
)

# =============================================================================
# SERVICES TO KEEP - Essential for phone functionality
# =============================================================================
$ServicesToKeep = @(
    "TabletInputService"             # Touch Keyboard and Handwriting
    "TouchInputService"              # Touch input
    "HidServ"                        # Human Interface Device Service
    "Wcmsvc"                         # Windows Connection Manager
    "WlanSvc"                        # WLAN AutoConfig
    "Netman"                         # Network Connections
    "Dhcp"                           # DHCP Client
    "Dnscache"                       # DNS Client
    "NlaSvc"                         # Network Location Awareness
    "AudioSrv"                       # Windows Audio
    "AudioEndpointBuilder"           # Audio Endpoint Builder
    "BthServ"                        # Bluetooth Support Service
    "bthavctpsvc"                    # Bluetooth Audio
    "BTAGService"                    # Bluetooth Audio Gateway
    "EventLog"                       # Event Log
    "PlugPlay"                       # Plug and Play
    "Power"                          # Power
    "SysMain"                        # Superfetch
    "Themes"                         # Themes
    "UsoSvc"                         # Update Orchestrator
    "WpnService"                     # Windows Push Notifications
)

# =============================================================================
# FUNCTIONS
# =============================================================================

function Test-AdminPrivileges {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Remove-BloatwareApps {
    Write-Host ""
    Write-Status "Removing bloatware apps..." -Type "Header"
    Write-Host ""

    $removed = 0
    $skipped = 0

    foreach ($appName in $AppsToRemove) {
        $apps = Get-AppxPackage -Name $appName -AllUsers -ErrorAction SilentlyContinue
        $provisionedApps = Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue |
                          Where-Object { $_.DisplayName -like $appName }

        if ($apps -or $provisionedApps) {
            if ($WhatIf) {
                Write-Status "Would remove: $appName" -Type "Remove"
                $removed++
            } else {
                try {
                    # Remove for all users
                    Get-AppxPackage -Name $appName -AllUsers | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue

                    # Remove provisioned package (prevents reinstall)
                    $provisionedApps | ForEach-Object {
                        Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName -ErrorAction SilentlyContinue
                    }

                    Write-Status "Removed: $appName" -Type "Remove"
                    $removed++
                }
                catch {
                    Write-Status "Failed to remove: $appName - $_" -Type "Warning"
                    $skipped++
                }
            }
        }
    }

    Write-Host ""
    if ($WhatIf) {
        Write-Status "Would remove $removed apps" -Type "Info"
    } else {
        Write-Status "Removed $removed apps, skipped $skipped" -Type "Success"
    }
}

function Disable-UnnecessaryServices {
    Write-Host ""
    Write-Status "Disabling unnecessary services..." -Type "Header"
    Write-Host ""

    $disabled = 0

    foreach ($serviceName in $ServicesToDisable) {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

        if ($service) {
            if ($WhatIf) {
                Write-Status "Would disable: $serviceName" -Type "Remove"
                $disabled++
            } else {
                try {
                    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
                    Set-Service -Name $serviceName -StartupType Disabled -ErrorAction SilentlyContinue
                    Write-Status "Disabled: $serviceName" -Type "Remove"
                    $disabled++
                }
                catch {
                    Write-Status "Failed to disable: $serviceName" -Type "Warning"
                }
            }
        }
    }

    Write-Host ""
    if ($WhatIf) {
        Write-Status "Would disable $disabled services" -Type "Info"
    } else {
        Write-Status "Disabled $disabled services" -Type "Success"
    }
}

function Enable-TouchSupport {
    Write-Host ""
    Write-Status "Ensuring touch input is enabled..." -Type "Header"
    Write-Host ""

    # Enable touch keyboard service
    $touchServices = @("TabletInputService", "TouchInputService")

    foreach ($serviceName in $touchServices) {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if ($service) {
            if (-not $WhatIf) {
                Set-Service -Name $serviceName -StartupType Automatic -ErrorAction SilentlyContinue
                Start-Service -Name $serviceName -ErrorAction SilentlyContinue
            }
            Write-Status "Enabled: $serviceName (Touch/Keyboard)" -Type "Keep"
        }
    }

    # Enable touch keyboard in registry
    if (-not $WhatIf) {
        $regPath = "HKCU:\Software\Microsoft\TabletTip\1.7"
        if (-not (Test-Path $regPath)) {
            New-Item -Path $regPath -Force | Out-Null
        }
        Set-ItemProperty -Path $regPath -Name "EnableDesktopModeAutoInvoke" -Value 1 -Type DWord
        Set-ItemProperty -Path $regPath -Name "TipbandDesiredVisibility" -Value 1 -Type DWord
    }

    Write-Status "Touch keyboard auto-invoke enabled" -Type "Keep"

    # Enable on-screen keyboard
    if (-not $WhatIf) {
        $oskPath = "HKCU:\Software\Microsoft\Accessibility"
        if (-not (Test-Path $oskPath)) {
            New-Item -Path $oskPath -Force | Out-Null
        }
        # Don't force OSK, but ensure it's available
    }

    Write-Status "On-screen keyboard available (osk.exe)" -Type "Keep"
}

function Install-WSA {
    if ($SkipWSA) {
        Write-Status "Skipping WSA installation (--SkipWSA)" -Type "Info"
        return
    }

    Write-Host ""
    Write-Status "Checking Windows Subsystem for Android..." -Type "Header"
    Write-Host ""

    # Check if WSA is already installed
    $wsa = Get-AppxPackage -Name "MicrosoftCorporationII.WindowsSubsystemForAndroid" -ErrorAction SilentlyContinue

    if ($wsa) {
        Write-Status "WSA is already installed: $($wsa.Version)" -Type "Success"
        return
    }

    if ($WhatIf) {
        Write-Status "Would install Windows Subsystem for Android" -Type "Info"
        return
    }

    Write-Status "WSA not found. Installing via Microsoft Store..." -Type "Info"

    # Check Windows version (WSA requires Windows 11)
    $osVersion = [System.Environment]::OSVersion.Version
    if ($osVersion.Build -lt 22000) {
        Write-Status "WSA requires Windows 11 (build 22000+). Current: $($osVersion.Build)" -Type "Error"
        Write-Status "Please upgrade to Windows 11 to use Android apps" -Type "Warning"
        return
    }

    # Check for required Windows features
    Write-Status "Checking required Windows features..." -Type "Info"

    $vmPlatform = Get-WindowsOptionalFeature -Online -FeatureName "VirtualMachinePlatform" -ErrorAction SilentlyContinue
    if ($vmPlatform.State -ne "Enabled") {
        Write-Status "Enabling Virtual Machine Platform..." -Type "Info"
        Enable-WindowsOptionalFeature -Online -FeatureName "VirtualMachinePlatform" -NoRestart -ErrorAction SilentlyContinue
    }

    $hyperV = Get-WindowsOptionalFeature -Online -FeatureName "Microsoft-Hyper-V" -ErrorAction SilentlyContinue
    # Hyper-V is optional but improves performance

    # Try to install WSA via winget (if available)
    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if ($winget) {
        Write-Status "Installing WSA via winget..." -Type "Info"
        try {
            winget install --id "9P3395VX91NR" --source msstore --accept-package-agreements --accept-source-agreements
            Write-Status "WSA installation initiated" -Type "Success"
        }
        catch {
            Write-Status "Winget installation failed. Please install manually from Microsoft Store." -Type "Warning"
        }
    } else {
        # Open Microsoft Store to WSA page
        Write-Status "Opening Microsoft Store to install WSA..." -Type "Info"
        Start-Process "ms-windows-store://pdp/?ProductId=9P3395VX91NR"
        Write-Status "Please complete WSA installation in the Microsoft Store" -Type "Warning"
    }

    Write-Host ""
    Write-Host "After WSA is installed:" -ForegroundColor Yellow
    Write-Host "1. Open 'Windows Subsystem for Android Settings' from Start menu" -ForegroundColor Gray
    Write-Host "2. Enable 'Developer mode' to allow ADB connections" -ForegroundColor Gray
    Write-Host "3. Use the Android app in Windows Phone to sideload APKs" -ForegroundColor Gray
}

function Install-ADB {
    if ($SkipADB) {
        Write-Status "Skipping ADB installation (--SkipADB)" -Type "Info"
        return
    }

    Write-Host ""
    Write-Status "Checking ADB (Android Debug Bridge)..." -Type "Header"
    Write-Host ""

    # Check common ADB locations
    $adbPaths = @(
        "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
        "C:\platform-tools\adb.exe"
        "C:\Android\platform-tools\adb.exe"
        "$env:ProgramFiles\Android\platform-tools\adb.exe"
    )

    $adbFound = $false
    foreach ($adbPath in $adbPaths) {
        if (Test-Path $adbPath) {
            Write-Status "ADB found at: $adbPath" -Type "Success"
            $adbFound = $true
            break
        }
    }

    # Also check PATH
    $adbInPath = Get-Command adb -ErrorAction SilentlyContinue
    if ($adbInPath) {
        Write-Status "ADB found in PATH: $($adbInPath.Source)" -Type "Success"
        $adbFound = $true
    }

    if ($adbFound) {
        return
    }

    if ($WhatIf) {
        Write-Status "Would download and install ADB platform-tools" -Type "Info"
        return
    }

    Write-Status "ADB not found. Downloading platform-tools..." -Type "Info"

    $downloadUrl = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip"
    $downloadPath = "$env:TEMP\platform-tools.zip"
    $installPath = "C:\platform-tools"

    try {
        # Download
        Write-Status "Downloading from Google..." -Type "Info"
        Invoke-WebRequest -Uri $downloadUrl -OutFile $downloadPath -UseBasicParsing

        # Extract
        Write-Status "Extracting to $installPath..." -Type "Info"
        if (Test-Path $installPath) {
            Remove-Item -Path $installPath -Recurse -Force
        }
        Expand-Archive -Path $downloadPath -DestinationPath "C:\" -Force

        # Cleanup
        Remove-Item -Path $downloadPath -Force

        # Add to PATH
        $currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        if ($currentPath -notlike "*$installPath*") {
            [Environment]::SetEnvironmentVariable("Path", "$currentPath;$installPath", "Machine")
            $env:Path = "$env:Path;$installPath"
            Write-Status "Added $installPath to system PATH" -Type "Success"
        }

        Write-Status "ADB installed successfully" -Type "Success"
    }
    catch {
        Write-Status "Failed to install ADB: $_" -Type "Error"
        Write-Host ""
        Write-Host "Manual installation:" -ForegroundColor Yellow
        Write-Host "1. Download: https://developer.android.com/studio/releases/platform-tools" -ForegroundColor Gray
        Write-Host "2. Extract to C:\platform-tools" -ForegroundColor Gray
        Write-Host "3. Add C:\platform-tools to system PATH" -ForegroundColor Gray
    }
}

function Install-WSL {
    if ($SkipWSL) {
        Write-Status "Skipping WSL installation (--SkipWSL)" -Type "Info"
        return
    }

    Write-Host ""
    Write-Status "Checking Windows Subsystem for Linux..." -Type "Header"
    Write-Host ""

    # Check if WSL is already installed
    $wslInstalled = $false
    try {
        $wslVersion = wsl --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            $wslInstalled = $true
            Write-Status "WSL is already installed" -Type "Success"
        }
    }
    catch {
        $wslInstalled = $false
    }

    # Check if Ubuntu is installed
    $ubuntuInstalled = $false
    try {
        $distros = wsl --list --quiet 2>$null
        if ($distros -match "Ubuntu") {
            $ubuntuInstalled = $true
            Write-Status "Ubuntu is already installed" -Type "Success"
        }
    }
    catch {
        $ubuntuInstalled = $false
    }

    if ($wslInstalled -and $ubuntuInstalled) {
        return
    }

    if ($WhatIf) {
        if (-not $wslInstalled) {
            Write-Status "Would install Windows Subsystem for Linux" -Type "Info"
        }
        if (-not $ubuntuInstalled) {
            Write-Status "Would install Ubuntu distribution" -Type "Info"
        }
        return
    }

    # Enable required Windows features for WSL
    Write-Status "Enabling WSL Windows features..." -Type "Info"

    # Enable WSL feature
    $wslFeature = Get-WindowsOptionalFeature -Online -FeatureName "Microsoft-Windows-Subsystem-Linux" -ErrorAction SilentlyContinue
    if ($wslFeature.State -ne "Enabled") {
        Write-Status "Enabling Windows Subsystem for Linux feature..." -Type "Info"
        Enable-WindowsOptionalFeature -Online -FeatureName "Microsoft-Windows-Subsystem-Linux" -NoRestart -ErrorAction SilentlyContinue
    }

    # Enable Virtual Machine Platform (required for WSL 2)
    $vmPlatform = Get-WindowsOptionalFeature -Online -FeatureName "VirtualMachinePlatform" -ErrorAction SilentlyContinue
    if ($vmPlatform.State -ne "Enabled") {
        Write-Status "Enabling Virtual Machine Platform..." -Type "Info"
        Enable-WindowsOptionalFeature -Online -FeatureName "VirtualMachinePlatform" -NoRestart -ErrorAction SilentlyContinue
    }

    # Install WSL if not present
    if (-not $wslInstalled) {
        Write-Status "Installing WSL..." -Type "Info"
        try {
            # Use wsl --install which handles everything
            wsl --install --no-distribution 2>$null
            Write-Status "WSL installed successfully" -Type "Success"
        }
        catch {
            Write-Status "WSL installation may require a restart" -Type "Warning"
        }
    }

    # Set WSL 2 as default
    Write-Status "Setting WSL 2 as default version..." -Type "Info"
    try {
        wsl --set-default-version 2 2>$null
    }
    catch {
        # May fail if restart is needed
    }

    # Install Ubuntu
    if (-not $ubuntuInstalled) {
        Write-Status "Installing Ubuntu..." -Type "Info"

        # Try using wsl --install with Ubuntu
        try {
            wsl --install -d Ubuntu 2>$null

            if ($LASTEXITCODE -eq 0) {
                Write-Status "Ubuntu installation initiated" -Type "Success"
            }
        }
        catch {
            # Fallback: try winget
            $winget = Get-Command winget -ErrorAction SilentlyContinue
            if ($winget) {
                Write-Status "Trying winget to install Ubuntu..." -Type "Info"
                try {
                    winget install --id "Canonical.Ubuntu.2204" --source winget --accept-package-agreements --accept-source-agreements 2>$null
                    Write-Status "Ubuntu installation initiated via winget" -Type "Success"
                }
                catch {
                    Write-Status "Please install Ubuntu manually from Microsoft Store" -Type "Warning"
                }
            }
            else {
                # Open Microsoft Store
                Write-Status "Opening Microsoft Store to install Ubuntu..." -Type "Info"
                Start-Process "ms-windows-store://pdp/?ProductId=9PDXGNCFSCZV"
                Write-Status "Please complete Ubuntu installation in the Microsoft Store" -Type "Warning"
            }
        }
    }

    Write-Host ""
    Write-Host "After installation:" -ForegroundColor Yellow
    Write-Host "1. Restart your computer if prompted" -ForegroundColor Gray
    Write-Host "2. Launch Ubuntu from Start menu to complete setup" -ForegroundColor Gray
    Write-Host "3. Create your Linux username and password" -ForegroundColor Gray
    Write-Host "4. Use the Terminal app in Windows Phone to access Ubuntu" -ForegroundColor Gray
}

function Disable-Telemetry {
    Write-Host ""
    Write-Status "Disabling telemetry and tracking..." -Type "Header"
    Write-Host ""

    if ($WhatIf) {
        Write-Status "Would disable Windows telemetry" -Type "Info"
        return
    }

    # Disable telemetry via registry
    $telemetryKeys = @(
        @{ Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"; Name = "AllowTelemetry"; Value = 0 }
        @{ Path = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"; Name = "AllowTelemetry"; Value = 0 }
        @{ Path = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Privacy"; Name = "TailoredExperiencesWithDiagnosticDataEnabled"; Value = 0 }
        @{ Path = "HKCU:\SOFTWARE\Microsoft\InputPersonalization"; Name = "RestrictImplicitTextCollection"; Value = 1 }
        @{ Path = "HKCU:\SOFTWARE\Microsoft\InputPersonalization"; Name = "RestrictImplicitInkCollection"; Value = 1 }
    )

    foreach ($key in $telemetryKeys) {
        try {
            if (-not (Test-Path $key.Path)) {
                New-Item -Path $key.Path -Force | Out-Null
            }
            Set-ItemProperty -Path $key.Path -Name $key.Name -Value $key.Value -Type DWord -ErrorAction SilentlyContinue
        }
        catch {
            # Silently continue
        }
    }

    Write-Status "Telemetry disabled" -Type "Remove"
}

function Optimize-ForPhone {
    Write-Host ""
    Write-Status "Optimizing system for phone use..." -Type "Header"
    Write-Host ""

    if ($WhatIf) {
        Write-Status "Would apply phone optimizations" -Type "Info"
        return
    }

    # Disable lock screen (optional - user may want it)
    # Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization" -Name "NoLockScreen" -Value 1

    # Disable Aero Shake (minimize gesture)
    Set-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "DisallowShaking" -Value 1 -ErrorAction SilentlyContinue

    # Hide desktop icons (we're using Launcher)
    $desktopPath = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
    Set-ItemProperty -Path $desktopPath -Name "HideIcons" -Value 1 -ErrorAction SilentlyContinue

    # Disable Action Center (we have our own notifications eventually)
    # This is optional - keeping it for now

    Write-Status "Phone optimizations applied" -Type "Success"
}

function Show-Summary {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host "  Windows Phone Next - System Ready    " -ForegroundColor Magenta
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host ""

    Write-Host "Essential components preserved:" -ForegroundColor Green
    Write-Host "  [+] Touch input and gestures" -ForegroundColor DarkGreen
    Write-Host "  [+] On-screen keyboard" -ForegroundColor DarkGreen
    Write-Host "  [+] Audio services" -ForegroundColor DarkGreen
    Write-Host "  [+] Network connectivity" -ForegroundColor DarkGreen
    Write-Host "  [+] Bluetooth" -ForegroundColor DarkGreen
    Write-Host "  [+] Microsoft Store (for updates)" -ForegroundColor DarkGreen
    Write-Host "  [+] WebView2 (for Browser/Gmail/Maps)" -ForegroundColor DarkGreen
    Write-Host ""

    if (-not $SkipWSA) {
        Write-Host "Android support:" -ForegroundColor Cyan
        Write-Host "  [+] Windows Subsystem for Android" -ForegroundColor DarkCyan
        Write-Host "  [+] ADB for APK sideloading" -ForegroundColor DarkCyan
        Write-Host ""
    }

    if (-not $SkipWSL) {
        Write-Host "Linux support:" -ForegroundColor Cyan
        Write-Host "  [+] Windows Subsystem for Linux (WSL 2)" -ForegroundColor DarkCyan
        Write-Host "  [+] Ubuntu distribution" -ForegroundColor DarkCyan
        Write-Host ""
    }

    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Restart your device to apply all changes" -ForegroundColor Gray
    Write-Host "  2. Run Build.ps1 to compile Windows Phone apps" -ForegroundColor Gray
    Write-Host "  3. Run Setup\Install-WindowsPhone.ps1 to configure startup" -ForegroundColor Gray
    $step = 4
    if (-not $SkipWSA) {
        Write-Host "  $step. Enable 'Developer mode' in WSA settings for APK installs" -ForegroundColor Gray
        $step++
    }
    if (-not $SkipWSL) {
        Write-Host "  $step. Launch Ubuntu from Start menu to complete initial setup" -ForegroundColor Gray
    }
    Write-Host ""
}

# =============================================================================
# MAIN EXECUTION
# =============================================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Windows Phone Next - System Cleanup  " -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

if (-not (Test-AdminPrivileges)) {
    Write-Status "This script requires administrator privileges" -Type "Error"
    Write-Host "Please run PowerShell as Administrator" -ForegroundColor Yellow
    exit 1
}

if ($WhatIf) {
    Write-Host "=== DRY RUN MODE - No changes will be made ===" -ForegroundColor Yellow
    Write-Host ""
}

if ($Restore) {
    Write-Status "Restore mode - attempting to reinstall removed apps..." -Type "Header"
    Write-Host ""
    Write-Host "To restore apps, run:" -ForegroundColor Yellow
    Write-Host "  Get-AppxPackage -AllUsers | ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register `$(`$_.InstallLocation)\AppXManifest.xml}" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or reinstall specific apps from Microsoft Store" -ForegroundColor Gray
    exit 0
}

# Run optimization steps
Remove-BloatwareApps
Disable-UnnecessaryServices
Enable-TouchSupport
Disable-Telemetry
Install-WSA
Install-WSL
Install-ADB
Optimize-ForPhone

if (-not $WhatIf) {
    Show-Summary
}

Write-Host ""
Write-Status "System optimization complete!" -Type "Success"
Write-Host ""
