# Build System Documentation

This document describes the Windows Phone Next build system, including how to compile applications, download drivers, and create deployment images.

## Overview

The build system consists of four PowerShell scripts in the `Build/` directory:

| Script | Purpose |
|--------|---------|
| `build-all.ps1` | Compiles all 19 applications |
| `download-drivers.ps1` | Downloads LattePanda 3 Delta drivers |
| `create-image.ps1` | Creates custom Windows 11 installation image |
| `deploy.ps1` | Master script that orchestrates everything |

## Prerequisites

### Required
- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PowerShell 5.1 or later

### Optional (for image creation)
- Administrator privileges
- [Windows ADK](https://go.microsoft.com/fwlink/?linkid=2196127) (for oscdimg.exe)
- Windows 11 Enterprise LTSC ISO

---

## Build Script (build-all.ps1)

### Purpose

Compiles all Windows Phone Next applications in the correct dependency order and outputs them to a single directory.

### Usage

```powershell
# Basic build (Release mode)
.\Build\build-all.ps1

# Build with clean (removes previous builds)
.\Build\build-all.ps1 -Clean

# Build in Debug mode
.\Build\build-all.ps1 -Configuration Debug

# Custom output directory
.\Build\build-all.ps1 -OutputPath "C:\MyBuilds"
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Configuration` | String | `Release` | Build configuration (`Release` or `Debug`) |
| `-OutputPath` | String | `../Output` | Directory for built applications |
| `-Clean` | Switch | `false` | Remove previous builds before building |

### Build Order

The script builds projects in dependency order:

1. **Shared Libraries** (built first)
   - `ModemLib` - Modem AT command communication
   - `BlockingService` - Call/message blocking service
   - `SharedServices` - Theme manager and utilities

2. **Applications** (built after shared libraries)
   - Launcher, Dialer, Messaging, Contacts
   - Browser, Gmail, Maps
   - Music, Video, Calendar, Gallery
   - Settings, Terminal, ClaudeCode
   - AndroidApps, Camera, Files
   - Solitaire, Mahjong

### Output Structure

```
Output/
├── _Shared/
│   ├── ModemLib/
│   ├── BlockingService/
│   └── SharedServices/
├── WindowsPhoneLauncher/
│   ├── WindowsPhoneLauncher.exe
│   ├── WindowsPhoneLauncher.dll
│   └── ... (dependencies)
├── WindowsPhoneDialer/
├── WindowsPhoneMessaging/
├── ... (other apps)
└── apps-manifest.json
```

### Apps Manifest

The build script generates `apps-manifest.json` containing metadata about all built applications:

```json
{
  "BuildDate": "2024-01-15 14:30:00",
  "Configuration": "Release",
  "DotNetVersion": "8.0.100",
  "Applications": [
    {
      "Name": "WindowsPhoneLauncher",
      "Executable": "WindowsPhoneLauncher.exe",
      "Path": "WindowsPhoneLauncher"
    },
    ...
  ]
}
```

### Build Process Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    build-all.ps1                            │
├─────────────────────────────────────────────────────────────┤
│  1. Check Prerequisites                                     │
│     └── Verify .NET SDK installed                          │
│                                                             │
│  2. Clean (if -Clean specified)                            │
│     └── Remove bin/obj folders and Output directory        │
│                                                             │
│  3. Restore NuGet Packages                                 │
│     └── dotnet restore for all projects                    │
│                                                             │
│  4. Build Shared Libraries                                 │
│     ├── ModemLib                                           │
│     ├── BlockingService                                    │
│     └── SharedServices                                     │
│                                                             │
│  5. Build Applications                                     │
│     └── 19 apps built in parallel where possible           │
│                                                             │
│  6. Generate Manifest                                      │
│     └── apps-manifest.json                                 │
│                                                             │
│  7. Summary Report                                         │
│     └── Success/failure count and timing                   │
└─────────────────────────────────────────────────────────────┘
```

---

## Driver Download Script (download-drivers.ps1)

### Purpose

Downloads official LattePanda 3 Delta drivers from the LattePanda GitHub repository.

### Usage

```powershell
# Download to default location (../Drivers)
.\Build\download-drivers.ps1

# Custom output directory
.\Build\download-drivers.ps1 -OutputPath "C:\Drivers"
```

### Downloaded Drivers

| Driver | Description |
|--------|-------------|
| `Chipset_Driver` | Intel Chipset INF |
| `Graphics_Driver` | Intel UHD Graphics |
| `Audio_Driver` | Realtek Audio |
| `WiFi_Driver` | Intel WiFi 6 AX201 |
| `Bluetooth_Driver` | Intel Bluetooth |
| `Touch_Driver` | Touch Panel |
| `SerialPort_Driver` | USB Serial Port (CH340) |
| `Management_Engine_Driver` | Intel ME Interface |

### Output

```
Drivers/
├── Chipset_Driver/
│   └── (extracted driver files)
├── Graphics_Driver/
├── Audio_Driver/
├── WiFi_Driver/
├── Bluetooth_Driver/
├── Touch_Driver/
├── SerialPort_Driver/
├── Management_Engine_Driver/
├── Chipset_Driver.zip
├── Graphics_Driver.zip
├── ... (original zip files)
└── install-drivers.cmd
```

### Driver Installation Script

The script generates `install-drivers.cmd` which can be run on the target device:

```batch
@echo off
REM Run as Administrator
for /r "%~dp0Graphics_Driver" %%f in (*.inf) do (
    pnputil /add-driver "%%f" /install
)
REM ... (similar for other drivers)
```

---

## Image Creation Script (create-image.ps1)

### Purpose

Creates a custom Windows 11 installation image with:
- Pre-installed LattePanda 3 Delta drivers
- Windows Phone Next applications
- Unattended installation configuration
- Auto-boot to launcher

### Usage

```powershell
# Create image from Windows ISO
.\Build\create-image.ps1 -IsoPath "C:\Windows11LTSC.iso"

# Get download instructions
.\Build\create-image.ps1 -DownloadWindows
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-IsoPath` | String | Path to Windows 11 ISO file |
| `-DownloadWindows` | Switch | Show instructions to obtain Windows ISO |
| `-WorkingDir` | String | Temporary working directory |
| `-OutputIso` | String | Output ISO file path |

### Image Creation Process

```
┌─────────────────────────────────────────────────────────────┐
│                  create-image.ps1                           │
├─────────────────────────────────────────────────────────────┤
│  1. Prerequisites Check                                     │
│     ├── Administrator privileges                           │
│     ├── DISM available                                     │
│     └── oscdimg available (Windows ADK)                    │
│                                                             │
│  2. Build Applications                                     │
│     └── Run build-all.ps1 if Output/ missing               │
│                                                             │
│  3. Extract Windows ISO                                    │
│     ├── Mount ISO                                          │
│     ├── Copy contents to working directory                 │
│     └── Unmount ISO                                        │
│                                                             │
│  4. Mount Windows Image (install.wim)                      │
│     └── DISM /Mount-Wim                                    │
│                                                             │
│  5. Integrate Drivers                                      │
│     ├── Download drivers if missing                        │
│     └── DISM /Add-Driver for each driver                   │
│                                                             │
│  6. Copy Applications                                      │
│     └── Copy to C:\WindowsPhoneNext in image               │
│                                                             │
│  7. Configure First-Boot Setup                             │
│     ├── Add RunOnce registry entry                         │
│     └── Copy Autounattend.xml                              │
│                                                             │
│  8. Save and Unmount Image                                 │
│     └── DISM /Unmount-Wim /Commit                          │
│                                                             │
│  9. Create Bootable ISO                                    │
│     └── oscdimg with UEFI boot support                     │
└─────────────────────────────────────────────────────────────┘
```

### Windows Image Structure

After modification, the Windows image contains:

```
C:\
└── WindowsPhoneNext\
    ├── Apps\
    │   ├── WindowsPhoneLauncher\
    │   ├── WindowsPhoneDialer\
    │   └── ... (all apps)
    ├── Setup\
    │   ├── setup.ps1
    │   ├── configure-autostart.ps1
    │   └── first-boot.cmd
    ├── Drivers\
    │   └── install-drivers.cmd
    └── apps-manifest.json
```

---

## Master Deployment Script (deploy.ps1)

### Purpose

Orchestrates the entire build and deployment process with a single command.

### Usage

```powershell
# Full deployment (build + drivers + image)
.\Build\deploy.ps1 -IsoPath "C:\Windows11LTSC.iso"

# Build only (no image creation)
.\Build\deploy.ps1 -BuildOnly

# Skip driver download (use existing)
.\Build\deploy.ps1 -IsoPath "C:\Windows11LTSC.iso" -SkipDrivers

# Clean build
.\Build\deploy.ps1 -BuildOnly -Clean
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `-IsoPath` | String | Path to Windows 11 ISO file |
| `-BuildOnly` | Switch | Only build apps, skip image creation |
| `-SkipDrivers` | Switch | Use existing drivers, don't download |
| `-Clean` | Switch | Clean build (remove previous) |

### Manual Deployment Package

When no ISO is specified, the script creates a manual deployment package:

```
ManualDeploy/
├── Apps/
│   └── (all built applications)
├── Drivers/
│   └── (all drivers with install script)
├── Setup/
│   └── (setup scripts)
└── install.cmd
```

This can be copied to a device with Windows already installed and run manually.

---

## Obtaining Windows 11 LTSC

Windows 11 Enterprise LTSC (Long-Term Servicing Channel) is recommended for embedded devices because:
- No feature updates (stability)
- No Microsoft Store / consumer apps
- Extended support lifecycle
- Reduced resource usage

### Sources

1. **Microsoft Evaluation Center** (90-day trial)
   - https://www.microsoft.com/en-us/evalcenter/evaluate-windows-11-enterprise

2. **Visual Studio Subscriptions** (formerly MSDN)
   - Full license for subscribers
   - https://visualstudio.microsoft.com/subscriptions/

3. **Volume Licensing Service Center** (VLSC)
   - For organizations with volume licenses
   - https://www.microsoft.com/licensing/servicecenter/

---

## Troubleshooting

### Build Errors

**"MSBuild not found"**
```powershell
# Install .NET SDK
winget install Microsoft.DotNet.SDK.8
```

**"Package restore failed"**
```powershell
# Clear NuGet cache and retry
dotnet nuget locals all --clear
.\Build\build-all.ps1 -Clean
```

**"Project reference not found"**
```powershell
# Ensure shared projects are built first
.\Build\build-all.ps1 -Clean
```

### Driver Download Errors

**"Access denied" or "404 Not Found"**
- Check internet connection
- Drivers may have moved; check LattePanda GitHub manually

### Image Creation Errors

**"DISM failed"**
- Run PowerShell as Administrator
- Ensure ISO is not corrupted

**"oscdimg not found"**
- Install Windows ADK from Microsoft
- Or manually create bootable USB using Rufus

---

## Continuous Integration

Example GitHub Actions workflow:

```yaml
name: Build Windows Phone Next

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build
        run: .\Build\build-all.ps1

      - name: Upload Artifacts
        uses: actions/upload-artifact@v3
        with:
          name: windows-phone-next
          path: Output/
```

---

## Performance Tips

1. **Parallel Builds**: The script builds apps in parallel where dependencies allow
2. **Incremental Builds**: Don't use `-Clean` unless necessary
3. **SSD Storage**: Build on SSD for faster compilation
4. **Exclude from Antivirus**: Add build directories to AV exclusions
