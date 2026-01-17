# Windows Phone Next - Build & Deployment System

This directory contains scripts to build, package, and deploy Windows Phone Next to a LattePanda 3 Delta device.

## Quick Start

### Build Only (Development)

```powershell
# Build all applications
.\build-all.ps1

# Build with clean
.\build-all.ps1 -Clean

# Build in Debug mode
.\build-all.ps1 -Configuration Debug
```

### Full Deployment

```powershell
# Run the master deployment script
.\deploy.ps1 -IsoPath "C:\path\to\Windows11.iso"

# Or just build (no ISO required)
.\deploy.ps1 -BuildOnly
```

## Scripts Overview

| Script | Description |
|--------|-------------|
| `build-all.ps1` | Compiles all 19 applications |
| `download-drivers.ps1` | Downloads LattePanda 3 Delta drivers |
| `create-image.ps1` | Creates custom Windows installation image |
| `deploy.ps1` | Master script that runs everything |

## Requirements

### For Building
- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (optional, for debugging)

### For Image Creation
- Administrator privileges
- Windows ADK (for oscdimg.exe)
- Windows 11 Enterprise LTSC ISO

## Obtaining Windows 11 LTSC

Windows 11 Enterprise LTSC is available from:

1. **Microsoft Volume Licensing Service Center (VLSC)** - For volume license customers
2. **Visual Studio Subscriptions** - For MSDN subscribers
3. **Microsoft Evaluation Center** - 90-day trial version
   - https://www.microsoft.com/en-us/evalcenter/evaluate-windows-11-enterprise

## Directory Structure

```
Build/
├── build-all.ps1          # Build script
├── download-drivers.ps1   # Driver download script
├── create-image.ps1       # Image creation script
├── deploy.ps1             # Master deployment script
└── README.md              # This file

Setup/
├── Autounattend.xml       # Unattended Windows installation config
├── setup.ps1              # Post-installation setup script
└── configure-autostart.ps1 # Launcher autostart configuration

Drivers/
├── Chipset_Driver/        # Intel chipset drivers
├── Graphics_Driver/       # Intel UHD graphics drivers
├── Audio_Driver/          # Realtek audio drivers
├── WiFi_Driver/           # Intel WiFi 6 drivers
├── Bluetooth_Driver/      # Intel Bluetooth drivers
├── Touch_Driver/          # Touch panel drivers
├── SerialPort_Driver/     # USB serial port drivers
└── install-drivers.cmd    # Driver installation script

Output/
├── WindowsPhoneLauncher/  # Built Launcher app
├── WindowsPhoneDialer/    # Built Dialer app
├── ...                    # Other built apps
└── apps-manifest.json     # Apps manifest file
```

## Manual Installation

If you prefer to install manually (without creating a custom ISO):

1. Install Windows 11 on the LattePanda 3 Delta
2. Run `.\deploy.ps1 -BuildOnly` to create a deployment package
3. Copy the `ManualDeploy` folder to the device
4. Run `install.cmd` as Administrator

## Customization

### Build Configuration

Edit `build-all.ps1` to modify:
- Project list
- Build configuration (Debug/Release)
- Output paths

### Windows Configuration

Edit `Setup/Autounattend.xml` to modify:
- Language settings
- Time zone
- User account settings
- Partition layout

### Post-Install Setup

Edit `Setup/setup.ps1` to modify:
- Services to disable
- Display settings
- Power settings
- Registry tweaks

### Autostart Behavior

Edit `Setup/configure-autostart.ps1` to modify:
- Startup methods (Registry, Scheduled Task, Shell replacement)
- Auto-login settings
- Recovery options

## Troubleshooting

### Build Errors

```powershell
# Clean and rebuild
.\build-all.ps1 -Clean

# Check for missing SDK
dotnet --list-sdks
```

### Driver Installation Issues

```powershell
# Manually install drivers
cd Drivers
.\install-drivers.cmd
```

### Launcher Not Starting

```powershell
# Check startup configuration
Get-ScheduledTask -TaskName "WindowsPhoneNextLauncher"

# Manually start launcher
& "C:\WindowsPhoneNext\Apps\WindowsPhoneLauncher\WindowsPhoneLauncher.exe"
```

### Restore Windows Explorer

```powershell
# Run the restore script
& "C:\WindowsPhoneNext\Setup\restore-shell.ps1"

# Then restart the computer
shutdown /r /t 0
```

## Hardware Requirements

- **Device**: LattePanda 3 Delta
- **Display**: Waveshare 6.25" LCD (720x1560)
- **Storage**: 32GB+ eMMC/SSD
- **RAM**: 8GB recommended

## License

This project is for educational and personal use. Windows and related components are property of Microsoft Corporation.
