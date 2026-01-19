# Windows Phone Next

A custom Windows 11 phone platform for embedded single-board computers. Experience a modern mobile interface with full telephony, messaging, and a rich app ecosystem.

![Platform](https://img.shields.io/badge/platform-Windows%2011%20LTSC-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Display](https://img.shields.io/badge/display-720x1560-green)
![License](https://img.shields.io/badge/license-Personal%20Use-orange)

---

## Features

### Core Functionality
- **Full Phone Functionality** - Voice calls, SMS messaging, contacts with call/message blocking
- **Web Browsing** - Chromium-based browser with tabs, bookmarks, mobile optimization
- **Gmail Integration** - Dedicated Gmail app (domain-locked for security)
- **GPS Navigation** - OpenStreetMap routing with turn-by-turn directions

### Media & Entertainment
- **Music Player** - 64-bar spectrum visualizer, shuffle/repeat, playlist support
- **Video Player** - Progress seeking, 10s skip, fullscreen mode
- **Gallery** - Image viewer with thumbnail navigation
- **Games** - Solitaire and Mahjong with touch controls

### System Features
- **9 Built-in Themes** - Dark, Light, Midnight Blue, Forest Green, Purple Night, Sunset Orange, Rose Pink, Ocean Teal, High Contrast
- **Curved Bezel Support** - UI adapts to curved screen edges
- **Kiosk Mode** - Auto-boot directly into the launcher
- **Settings App** - Bluetooth, WiFi, themes, system info

### Developer Tools
- **Terminal** - Tabbed terminal with CMD, PowerShell, and WSL support
- **Claude Code** - Voice/text AI chat for repository management
- **Android Support** - Sideload APKs via Windows Subsystem for Android

### Deployment
- **Windows Phone Next Build Tool** - GUI application for building, compiling, and image creation
- **Automated Build System** - Single command builds all 19 applications
- **Custom Image Creator** - Create bootable Windows 11 installation with apps pre-installed
- **Driver Integration** - LattePanda 3 Delta drivers included
- **ISO Management** - Browse or auto-download Windows 11 IoT Enterprise LTSC

---

## Hardware

| Component | Specification |
|-----------|---------------|
| **SBC** | [LattePanda 3 Delta](https://www.lattepanda.com/lattepanda-3-delta) (Intel N5105, 8GB RAM) |
| **Display** | Waveshare 6.25" IPS LCD (720x1560, touch) |
| **Power** | [PiSugar2 Plus](https://www.pisugar.com/) 5000mAh UPS |
| **Modem** | [Quectel EM06-A](https://www.quectel.com/) LTE Cat 6 M.2 |
| **GPS** | VK-172 USB GPS/GLONASS *(optional)* |

---

## Applications

### Communication
| App | Description |
|-----|-------------|
| **Phone** | Voice calls with dialpad, call history, mute/speaker controls |
| **Messages** | SMS messaging with conversation view, contact blocking |
| **Contacts** | Contact management with search, call/message blocking |
| **Gmail** | Gmail-only browser (secure, domain-locked) |

### Internet & Navigation
| App | Description |
|-----|-------------|
| **Browser** | Chromium WebView2 with tabs, bookmarks, mobile UA |
| **Maps** | GPS navigation with OpenStreetMap, turn-by-turn routing |

### Media & Entertainment
| App | Description |
|-----|-------------|
| **Music** | Audio player with 64-bar spectrum visualizer |
| **Video** | Video player with progress seeking, fullscreen |
| **Gallery** | Image viewer with thumbnail navigation |
| **Solitaire** | Classic Klondike with undo and auto-complete |
| **Mahjong** | Tile matching with hints and shuffle |

### Utilities
| App | Description |
|-----|-------------|
| **Calendar** | Month/day/year views with date marking |
| **Settings** | Bluetooth, WiFi, themes, system configuration |
| **Terminal** | Tabbed terminal (CMD, PowerShell, WSL) |
| **Files** | File browser |
| **Camera** | Camera capture |

### AI & Integration
| App | Description |
|-----|-------------|
| **Claude Code** | Voice/text chat with AI for repository management |
| **Android Apps** | Sideload APK files via WSA |

---

## Quick Start

### Prerequisites
- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PowerShell 5.1+
- Administrator privileges (for image creation)

### Option 1: Using the Build Tool (Recommended)

The easiest way to build Windows Phone Next is with the graphical Build Tool:

```powershell
# Clone the repository
git clone https://github.com/sp00nznet/windowsphone-next.git
cd windowsphone-next

# Build the Build Tool
.\BuildTool\build-tool.ps1

# Run the Build Tool (as Administrator)
.\BuildTool\bin\Release\net8.0-windows\WindowsPhoneNextBuildTool.exe
```

The Build Tool provides:
- Windows Phone Next-themed GUI
- ISO management (browse or auto-download)
- One-click build for all 19 applications
- Driver integration for LattePanda 3 Delta
- Bootable ISO image creation
- Real-time progress tracking and logs

See [BuildTool/README.md](BuildTool/README.md) for detailed documentation.

### Option 2: Command Line Build

```powershell
# Clone the repository
git clone https://github.com/sp00nznet/windowsphone-next.git
cd windowsphone-next

# Build all applications
.\Build\build-all.ps1

# Output is in the Output/ folder
```

### Run the Launcher

```powershell
# Start the launcher
.\Output\WindowsPhoneLauncher\WindowsPhoneLauncher.exe
```

---

## Deployment

### Option 1: Build Tool GUI (Easiest)

Use the Windows Phone Next Build Tool for a streamlined experience:

1. **Build the Build Tool**:
   ```powershell
   .\BuildTool\build-tool.ps1
   ```

2. **Launch as Administrator**:
   ```powershell
   .\BuildTool\bin\Release\net8.0-windows\WindowsPhoneNextBuildTool.exe
   ```

3. **Configure Options**:
   - Browse for Windows 11 ISO (or enable auto-download)
   - Select build options (apps, drivers, image creation)
   - Click "Start Build"

4. **Deploy**:
   - Output: `ImageWork/WindowsPhoneNext.iso`
   - Create bootable USB with included script

### Option 2: Manual Installation

1. Build the applications: `.\Build\build-all.ps1`
2. Copy the `Output/` folder to the target device
3. Run `WindowsPhoneLauncher.exe`

### Option 3: Command Line Deployment

Create a bootable Windows 11 installation with everything pre-configured:

```powershell
# Build apps and create deployment package
.\Build\deploy.ps1 -IsoPath "C:\Windows11LTSC.iso"

# Or auto-download Windows 11 IoT Enterprise LTSC
.\Build\deploy.ps1 -DownloadIso

# Or just create a manual deployment package (no ISO required)
.\Build\deploy.ps1 -BuildOnly
```

See [Build System Documentation](docs/BUILD.md) for detailed instructions.

---

## Theming

Windows Phone Next includes 9 built-in themes configurable through the Settings app:

| Theme | Description |
|-------|-------------|
| **Dark** | Default navy/blue dark theme |
| **Light** | Clean white/gray theme |
| **Midnight Blue** | Deep blue night theme |
| **Forest Green** | Natural green theme |
| **Purple Night** | Rich purple theme |
| **Sunset Orange** | Warm orange theme |
| **Rose Pink** | Elegant pink theme |
| **Ocean Teal** | Calm teal theme |
| **High Contrast** | Accessibility theme |

Themes are applied system-wide and persist across app restarts.

---

## Keyboard Shortcuts

| Key | App | Key | App |
|:---:|-----|:---:|-----|
| **P** | Phone | **M** | Messages |
| **O** | Contacts | **B** | Browser |
| **E** | Gmail | **N** | Maps |
| **U** | Music | **V** | Video |
| **C** | Calendar | **G** | Gallery |
| **S** | Settings | **A** | Android |
| **T** | Terminal | **L** | Claude |
| **1-9** | Quick launch | **Esc** | Exit app |

---

## Project Structure

```
windowsphone-next/
├── Apps/
│   ├── Launcher/           # Home screen
│   ├── Dialer/             # Phone calls
│   ├── Messaging/          # SMS messaging
│   ├── Contacts/           # Contact management
│   ├── Browser/            # Web browser
│   ├── Gmail/              # Gmail client
│   ├── Maps/               # GPS navigation
│   ├── Music/              # Audio player
│   ├── Video/              # Video player
│   ├── Calendar/           # Calendar
│   ├── Gallery/            # Image viewer
│   ├── Settings/           # System settings
│   ├── Terminal/           # Tabbed terminal
│   ├── ClaudeCode/         # AI assistant
│   ├── AndroidApps/        # APK sideloader
│   ├── Camera/             # Camera
│   ├── Files/              # File browser
│   ├── Solitaire/          # Card game
│   ├── Mahjong/            # Tile game
│   └── Shared/
│       ├── ModemLib/       # Modem AT command library
│       ├── BlockingService/ # Call/message blocking
│       ├── Services/       # Theme manager
│       └── Themes/         # Shared theme resources
├── BuildTool/              # Win32 Build Tool (GUI)
│   ├── MainWindow.xaml     # Windows Phone Next-themed UI
│   ├── build-tool.ps1      # Build Tool compiler
│   ├── Themes/             # Build Tool theme resources
│   └── README.md           # Build Tool documentation
├── Build/
│   ├── build-all.ps1       # Build all applications
│   ├── download-drivers.ps1 # Download LattePanda drivers
│   ├── create-image.ps1    # Create Windows image
│   └── deploy.ps1          # Master deployment script
├── Setup/
│   ├── Autounattend.xml    # Unattended Windows install
│   ├── setup.ps1           # Post-install setup
│   └── configure-autostart.ps1 # Launcher autostart
├── Drivers/                # LattePanda 3 Delta drivers
├── Output/                 # Built applications
└── docs/
    ├── APPS.md             # Application guide
    ├── BUILD.md            # Build system documentation
    ├── DEPLOYMENT.md       # Deployment guide
    └── DEVELOPMENT.md      # Development guide
```

---

## Documentation

- [Application Guide](docs/APPS.md) - Detailed app features and usage
- [Build System](docs/BUILD.md) - Build scripts and process
- [Deployment Guide](docs/DEPLOYMENT.md) - Image creation and installation
- [Development Guide](docs/DEVELOPMENT.md) - API reference, theming, architecture

---

## UI Specifications

| Property | Value |
|----------|-------|
| **Resolution** | 720 x 1560 pixels |
| **Aspect Ratio** | 9:19.5 (tall phone format) |
| **UI Framework** | WPF (.NET 8.0) |
| **Bezel Radius** | 32px corner radius |
| **Default Theme** | Dark mode |

---

## License

This project is provided as-is for educational and personal use. Windows and related components are property of Microsoft Corporation.

---

Made with care for mobile enthusiasts
