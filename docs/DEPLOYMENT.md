# Deployment Guide

This guide explains how to deploy Windows Phone Next to a LattePanda 3 Delta device, from building applications to creating a fully automated installation image.

## Deployment Options

| Option | Complexity | Requirements | Best For |
|--------|------------|--------------|----------|
| Manual Installation | Low | Windows already installed | Development/Testing |
| Manual Deployment Package | Medium | USB drive, Windows already installed | Single device deployment |
| Automated Image | High | Windows ADK, Windows ISO | Mass deployment |

---

## Option 1: Manual Installation (Development)

For development and testing, you can run Windows Phone Next directly on any Windows 10/11 system.

### Steps

1. **Build the applications**
   ```powershell
   cd windowsphone-next
   .\Build\build-all.ps1
   ```

2. **Run the launcher**
   ```powershell
   .\Output\WindowsPhoneLauncher\WindowsPhoneLauncher.exe
   ```

3. **Optional: Configure autostart**
   - Copy launcher shortcut to `shell:startup`
   - Or run `.\Setup\configure-autostart.ps1`

---

## Option 2: Manual Deployment Package

Create a deployment package that can be installed on a device with Windows already set up.

### Create the Package

```powershell
.\Build\deploy.ps1 -BuildOnly
```

This creates:
```
ManualDeploy/
├── Apps/                    # All built applications
├── Drivers/                 # LattePanda 3 Delta drivers
├── Setup/                   # Setup scripts
└── install.cmd              # One-click installer
```

### Deploy to Device

1. **Install Windows 11** on the LattePanda 3 Delta
2. **Copy** the `ManualDeploy/` folder to the device (USB drive or network)
3. **Run** `install.cmd` as Administrator
4. **Wait** for installation to complete
5. **Restart** when prompted

### What install.cmd Does

1. Installs LattePanda 3 Delta drivers
2. Creates `C:\WindowsPhoneNext\` directory
3. Copies all applications and setup scripts
4. Runs `setup.ps1` to configure Windows
5. Runs `configure-autostart.ps1` to enable launcher autostart
6. Prompts for restart

---

## Option 3: Automated Image (Full Deployment)

Create a custom Windows 11 installation image that automatically installs everything.

### Prerequisites

- Windows 10/11 (for building)
- Administrator privileges
- [Windows ADK](https://go.microsoft.com/fwlink/?linkid=2196127) installed
- Windows 11 Enterprise LTSC ISO

### Create the Image

```powershell
.\Build\deploy.ps1 -IsoPath "C:\path\to\Windows11LTSC.iso"
```

### Output

- `ImageWork/ISO/` - Modified Windows installation files
- `WindowsPhoneNext.iso` - Bootable ISO (if oscdimg available)
- `create-usb.ps1` - Script to create bootable USB

### Create Bootable USB

#### Method 1: Using the Generated Script
```powershell
cd ImageWork\ISO
.\create-usb.ps1 -DriveLetter E
```

#### Method 2: Using Rufus
1. Download [Rufus](https://rufus.ie/)
2. Select your USB drive
3. Select `WindowsPhoneNext.iso`
4. Click Start

#### Method 3: Manual Copy
1. Format USB as FAT32 (for UEFI boot)
2. Copy all files from `ImageWork\ISO\` to USB root
3. Ensure `Autounattend.xml` is in root

### Boot and Install

1. Insert USB into LattePanda 3 Delta
2. Power on and enter BIOS (Del or F2)
3. Set USB as first boot device
4. Save and exit BIOS
5. Installation is fully automatic:
   - Partitions disk
   - Installs Windows 11
   - Installs drivers
   - Installs applications
   - Configures autostart
   - Reboots into launcher

---

## Automated Installation Process

When booting from the custom image, the following happens automatically:

### Phase 1: Windows PE (Pre-Installation)

```
┌─────────────────────────────────────────────────────────────┐
│                     Windows PE Phase                        │
├─────────────────────────────────────────────────────────────┤
│  1. Read Autounattend.xml                                  │
│  2. Partition disk (GPT):                                  │
│     ├── 100MB EFI System Partition                         │
│     ├── 16MB Microsoft Reserved                            │
│     └── Remaining: Windows partition                       │
│  3. Apply Windows image to C:\                             │
│  4. Configure boot files                                   │
└─────────────────────────────────────────────────────────────┘
```

### Phase 2: Specialize (First Boot)

```
┌─────────────────────────────────────────────────────────────┐
│                    Specialize Phase                         │
├─────────────────────────────────────────────────────────────┤
│  1. Set computer name: WindowsPhoneNext                    │
│  2. Set timezone: Pacific Standard Time                    │
│  3. Disable Windows Update (registry)                      │
│  4. Disable Windows Defender (registry)                    │
│  5. Disable Windows Search                                 │
└─────────────────────────────────────────────────────────────┘
```

### Phase 3: OOBE (Out-of-Box Experience)

```
┌─────────────────────────────────────────────────────────────┐
│                      OOBE Phase                             │
├─────────────────────────────────────────────────────────────┤
│  1. Skip all OOBE screens (Autounattend.xml)               │
│  2. Create local user account "User" (no password)         │
│  3. Enable auto-login                                      │
│  4. First logon triggers setup scripts:                    │
│     ├── setup.ps1                                          │
│     ├── configure-autostart.ps1                            │
│     └── Scheduled restart                                  │
└─────────────────────────────────────────────────────────────┘
```

### Phase 4: First Logon Setup (setup.ps1)

```
┌─────────────────────────────────────────────────────────────┐
│                    First Logon Setup                        │
├─────────────────────────────────────────────────────────────┤
│  1. Install LattePanda 3 Delta drivers                     │
│     ├── Chipset                                            │
│     ├── Graphics                                           │
│     ├── Audio                                              │
│     ├── WiFi                                               │
│     ├── Bluetooth                                          │
│     ├── Touch                                              │
│     └── Serial Port                                        │
│                                                             │
│  2. Configure display settings                             │
│     └── Set resolution for 720x1560 display                │
│                                                             │
│  3. Configure power settings                               │
│     ├── Disable sleep                                      │
│     ├── Disable hibernate                                  │
│     └── Disable screen timeout                             │
│                                                             │
│  4. Disable unnecessary services                           │
│     ├── DiagTrack (Telemetry)                             │
│     ├── WSearch (Windows Search)                           │
│     ├── SysMain (Superfetch)                              │
│     └── Various others                                     │
│                                                             │
│  5. Configure kiosk mode                                   │
│     ├── Disable Action Center                              │
│     ├── Disable Cortana                                    │
│     ├── Auto-hide taskbar                                  │
│     ├── Disable lock screen                                │
│     └── Disable screen saver                               │
│                                                             │
│  6. Create Start Menu shortcuts                            │
│     └── Windows Phone Next launcher                        │
│                                                             │
│  7. Register in Programs and Features                      │
└─────────────────────────────────────────────────────────────┘
```

### Phase 5: Configure Autostart (configure-autostart.ps1)

```
┌─────────────────────────────────────────────────────────────┐
│                   Configure Autostart                       │
├─────────────────────────────────────────────────────────────┤
│  Method 1: Startup Folder                                  │
│  └── Create shortcut in shell:startup                      │
│                                                             │
│  Method 2: Registry Run Key                                │
│  └── HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run    │
│                                                             │
│  Method 3: Scheduled Task                                  │
│  └── Task: WindowsPhoneNextLauncher                        │
│      Trigger: At logon                                     │
│      Action: Start WindowsPhoneLauncher.exe                │
│                                                             │
│  Configure auto-login                                      │
│  └── AutoAdminLogon = 1                                    │
│      DefaultUserName = User                                │
└─────────────────────────────────────────────────────────────┘
```

### Phase 6: Final Restart

After setup completes, the system restarts and boots directly into the Windows Phone Next launcher.

---

## Autounattend.xml Configuration

The unattended installation file controls the automated setup:

### Key Settings

| Setting | Value | Purpose |
|---------|-------|---------|
| `UILanguage` | en-US | Installation language |
| `TimeZone` | Pacific Standard Time | Default timezone |
| `ComputerName` | WindowsPhoneNext | Device name |
| `AutoAdminLogon` | true | Auto-login enabled |
| `Username` | User | Local account name |
| `Password` | (empty) | No password |
| `SkipMachineOOBE` | true | Skip setup screens |
| `SkipUserOOBE` | true | Skip user setup |

### Customization

Edit `Setup/Autounattend.xml` to change:

**Language/Region:**
```xml
<UILanguage>en-US</UILanguage>
<InputLocale>en-US</InputLocale>
<SystemLocale>en-US</SystemLocale>
```

**Timezone:**
```xml
<TimeZone>Pacific Standard Time</TimeZone>
```

**Computer Name:**
```xml
<ComputerName>MyPhoneName</ComputerName>
```

**User Account:**
```xml
<LocalAccount>
    <Name>MyUser</Name>
    <Password>
        <Value>MyPassword</Value>
        <PlainText>true</PlainText>
    </Password>
</LocalAccount>
```

---

## Post-Installation

### Restore Windows Explorer

If you need to access the Windows desktop:

```powershell
# Run from launcher's Terminal app, or boot into Safe Mode
C:\WindowsPhoneNext\Setup\restore-shell.ps1
```

This will:
- Restore Explorer as the default shell
- Remove launcher from autostart
- Remove scheduled task

### Re-enable Launcher

To switch back to kiosk mode:

```powershell
C:\WindowsPhoneNext\Setup\configure-autostart.ps1
```

### Manual Driver Installation

If drivers weren't installed automatically:

```cmd
C:\WindowsPhoneNext\Drivers\install-drivers.cmd
```

---

## Troubleshooting

### Installation Hangs at "Getting Ready"

- Wait up to 30 minutes (driver installation takes time)
- If still stuck, boot into Safe Mode and check logs:
  ```
  C:\WindowsPhoneNext\Logs\setup.log
  ```

### Launcher Doesn't Start

1. Check if scheduled task exists:
   ```powershell
   Get-ScheduledTask -TaskName "WindowsPhoneNextLauncher"
   ```

2. Manually run the launcher:
   ```powershell
   C:\WindowsPhoneNext\Apps\WindowsPhoneLauncher\WindowsPhoneLauncher.exe
   ```

3. Check for .NET runtime:
   ```powershell
   dotnet --list-runtimes
   ```

### Display Issues

The default display settings assume a 720x1560 portrait display. For different displays:

1. Boot into Safe Mode
2. Run `restore-shell.ps1`
3. Adjust display settings in Windows
4. Run `configure-autostart.ps1`

### Touch Screen Not Working

1. Ensure Touch Driver is installed:
   ```cmd
   pnputil /enum-drivers | findstr Touch
   ```

2. Check Device Manager for unknown devices

3. Manually install touch driver:
   ```cmd
   pnputil /add-driver "C:\WindowsPhoneNext\Drivers\Touch_Driver\*.inf" /install
   ```

---

## Security Considerations

### Default Configuration

The automated installation creates a system optimized for embedded use, not security:

- **No password** on User account
- **Auto-login** enabled
- **Windows Defender** disabled
- **Windows Update** disabled

### Hardening for Production

For production deployments, consider:

1. **Set a password:**
   ```powershell
   net user User YourPassword
   ```

2. **Disable auto-login:**
   ```powershell
   reg delete "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v AutoAdminLogon /f
   ```

3. **Enable Windows Defender:**
   ```powershell
   reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender" /v DisableAntiSpyware /f
   ```

4. **Enable Windows Update** (for security patches):
   ```powershell
   reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU" /v NoAutoUpdate /f
   ```
