# 📱 Windows Phone Next

A custom Windows 11 phone platform for embedded single-board computers. Experience a modern mobile interface with full telephony, messaging, and a rich app ecosystem!

![Platform](https://img.shields.io/badge/platform-Windows%2011-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Display](https://img.shields.io/badge/display-720x1560-green)
![License](https://img.shields.io/badge/license-Personal%20Use-orange)

---

## ✨ Features

- 📞 **Full Phone Functionality** - Voice calls, SMS messaging, contacts
- 🌐 **Web Browsing** - Chromium-based browser with mobile optimization
- 📧 **Gmail Integration** - Dedicated Gmail app (domain-locked for security)
- 🗺️ **GPS Navigation** - OpenStreetMap routing with turn-by-turn directions
- 🎵 **Media Playback** - Music player with 64-bar spectrum visualizer + video player
- 🤖 **Android App Support** - Sideload APKs via Windows Subsystem for Android
- 🎮 **Touch Games** - Solitaire and Mahjong for entertainment
- 📊 **Status Bar** - Signal strength, battery, network status
- 🖥️ **Terminal** - Tabbed terminal with CMD, PowerShell, and WSL support
- 🤖 **Claude Code Integration** - Voice/text chat with AI for repository management
- 📡 **Connectivity Settings** - Bluetooth and WiFi management with scan/pair/forget

---

## 🖥️ Hardware

| Component | Description |
|-----------|-------------|
| **SBC** | [LattePanda 3 Delta](https://www.lattepanda.com/lattepanda-3-delta) (x86-64 with Windows 11) |
| **Power** | [PiSugar2 Plus](https://www.pisugar.com/products/pisugar2-plus-5000-mah-raspberry-pi-ups) 5000mAh UPS |
| **Display** | Waveshare 6.25" LCD **720x1560** IPS touch |
| **Modem** | [Quectel EM06-A](https://www.quectel.com/product/lte-a-em06-series/) LTE Cat 6 M.2 |
| **GPS** | VK-172 USB GPS/GLONASS *(optional)* |

---

## 📲 Apps

### 📱 Communication
| App | Icon | Description |
|-----|:----:|-------------|
| **Phone** | 📞 | Voice calls with dialpad, call history, mute/speaker controls |
| **Messages** | 💬 | SMS messaging with conversation view and chat bubbles |
| **Contacts** | 👤 | Contact management with search, add/edit/delete |
| **Gmail** | 📧 | Gmail-only browser (secure, domain-locked) |

### 🌐 Internet & Navigation
| App | Icon | Description |
|-----|:----:|-------------|
| **Browser** | 🌐 | Chromium WebView2 with tabs, bookmarks, mobile UA |
| **Maps** | 🗺️ | GPS navigation with OpenStreetMap, turn-by-turn routing |

### 🎬 Media & Entertainment
| App | Icon | Description |
|-----|:----:|-------------|
| **Music** | 🎵 | Audio player with 64-bar spectrum visualizer, shuffle/repeat |
| **Video** | 🎬 | Video player with progress seeking, 10s skip, fullscreen |
| **Gallery** | 🖼️ | Image viewer with thumbnail strip navigation |
| **Solitaire** | 🃏 | Classic Klondike with undo and auto-complete |
| **Mahjong** | 🀄 | Tile matching game with hints and shuffle |

### 🛠️ Utilities
| App | Icon | Description |
|-----|:----:|-------------|
| **Calendar** | 📅 | Month/day/year views with date marking |
| **Settings** | ⚙️ | Bluetooth, WiFi, About Phone, system configuration |
| **Terminal** | 🖥️ | Tabbed terminal with CMD, PowerShell, WSL support |
| **Files** | 📂 | File browser *(placeholder)* |
| **Camera** | 📷 | Camera capture *(placeholder)* |

### 🤖 AI & Android Integration
| App | Icon | Description |
|-----|:----:|-------------|
| **Claude** | 🤖 | Voice/text chat with Claude Code for repo management |
| **Android** | 📦 | Sideload APK files via WSA, manage Android apps |

> 💡 **Tip:** Apps requiring hardware (Dialer, Maps) include **demo mode toggles** for testing without devices!

---

## ⌨️ Keyboard Shortcuts

| Key | App | Key | App |
|:---:|-----|:---:|-----|
| **P** | 📞 Phone | **M** | 💬 Messages |
| **O** | 👤 Contacts | **B** | 🌐 Browser |
| **E** | 📧 Gmail | **N** | 🗺️ Maps |
| **U** | 🎵 Music | **V** | 🎬 Video |
| **C** | 📅 Calendar | **G** | 🖼️ Gallery |
| **S** | ⚙️ Settings | **A** | 📦 Android |
| **T** | 🖥️ Terminal | **L** | 🤖 Claude |
| **1-9** | Quick launch by position | **Esc** | Exit app |

---

## 🚀 Quick Start

```powershell
# Build all apps
.\Build.ps1

# Output in Dist/ folder
```

### 📋 Requirements
- Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- *(Optional)* Windows Subsystem for Android (for APK sideloading)

---

## 📦 Installation

```powershell
# 1. Build the project
.\Build.ps1

# 2. Copy Dist/ folder to target device

# 3. Run the launcher
.\Dist\Start-WindowsPhone.bat

# 4. (Optional) Enable kiosk mode
.\Setup\Configure-KioskMode.ps1
```

---

## 🤖 Android App Sideloading

Windows Phone Next supports running Android apps through Windows Subsystem for Android (WSA):

1. **Install WSA** from the Microsoft Store
2. Open the **Android** app from the launcher
3. Tap **Install APK** to sideload any Android app
4. Installed apps appear in the list and can be launched directly

> ⚠️ **Note:** WSA must be running to launch Android apps. The Android app will show connection status.

---

## 📖 Documentation

- **[Application Guide](docs/APPS.md)** - Detailed app features and usage
- **[Development Guide](docs/DEVELOPMENT.md)** - API reference, AT commands, theming

---

## 🎨 UI Specifications

| Property | Value |
|----------|-------|
| **Resolution** | 720 × 1560 pixels |
| **Aspect Ratio** | 9:19.5 (tall phone format) |
| **UI Framework** | WPF (.NET 8) |
| **Theme** | Dark mode with Windows accent colors |

---

## 📁 Project Structure

```
Apps/
├── Launcher/          # 🏠 Home screen
├── Dialer/            # 📞 Phone calls
├── Messaging/         # 💬 SMS
├── Contacts/          # 👤 Contact management
├── Browser/           # 🌐 Web browser
├── Gmail/             # 📧 Gmail client
├── Maps/              # 🗺️ Navigation
├── Music/             # 🎵 Audio player
├── Video/             # 🎬 Video player
├── Calendar/          # 📅 Calendar
├── Gallery/           # 🖼️ Image viewer
├── Settings/          # ⚙️ System settings (Bluetooth, WiFi, About Phone)
├── Terminal/          # 🖥️ Tabbed terminal (CMD, PowerShell, WSL)
├── ClaudeCode/        # 🤖 AI assistant for code repositories
├── AndroidApps/       # 📦 APK sideloader
├── Solitaire/         # 🃏 Card game
├── Mahjong/           # 🀄 Tile game
└── Shared/ModemLib/   # 📡 Modem communication library
```

---

## 📜 License

This project is provided as-is for educational and personal use.

---

Made with ❤️ for mobile enthusiasts
