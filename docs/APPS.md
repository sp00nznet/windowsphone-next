# 📱 Windows Phone Next - Application Guide

Detailed documentation for each application in the Windows Phone Next platform.

---

## 🏠 Launcher

The main home screen and entry point for the phone.

### ✨ Features
- ⏰ Clock and date display with large readable time
- 📊 Status bar showing signal strength, network type, and battery
- 📲 Scrollable app grid with touch-friendly tiles (WrapPanel layout)
- 🔽 Bottom navigation bar (Phone, Home, Messages quick access)
- 📞 Incoming call overlay with accept/decline buttons
- ⌨️ Keyboard shortcuts for quick app launching

### ⌨️ Keyboard Shortcuts
| Key | Action | Key | Action |
|:---:|--------|:---:|--------|
| **P** | 📞 Phone | **M** | 💬 Messages |
| **O** | 👤 Contacts | **B** | 🌐 Browser |
| **E** | 📧 Gmail | **N** | 🗺️ Maps |
| **U** | 🎵 Music | **V** | 🎬 Video |
| **C** | 📅 Calendar | **G** | 🖼️ Gallery |
| **S** | ⚙️ Settings | **A** | 📦 Android |
| **T** | 🖥️ Terminal | **L** | 🤖 Claude |
| **1-9** | Launch app by position | **Esc** | Exit app |

---

## 📞 Dialer (Phone)

Full-featured phone application for voice calls.

### ✨ Features
- 🔢 T9-style dialpad with large touch targets
- 📋 Call history in Recents tab
- 👤 Contacts integration for quick calling
- 📱 Active call screen with:
  - 🔇 Mute toggle
  - 🔊 Speaker toggle
  - ⌨️ In-call keypad for DTMF tones
  - ⏱️ Call duration timer
- 🎭 Demo mode when hardware unavailable

### 📡 Hardware Required
Quectel EM06-A LTE modem (or compatible AT modem)

### 🎭 Demo Mode
When no modem is detected, the app simulates the complete call flow:
- Dialing animation
- Ringing state
- Connected call with timer
- Hang up

---

## 💬 Messaging

SMS messaging application with conversation view.

### ✨ Features
- 📋 Conversation list view with contact avatars
- 💭 Chat bubble interface:
  - 🔵 Blue bubbles for sent messages (right-aligned)
  - ⚪ Gray bubbles for received messages (left-aligned)
- ✏️ New message composition with contact picker
- 🔄 Auto-refresh for incoming messages (30-second interval)
- 🔴 Unread message badges
- 🎭 Demo mode with sample conversations

### 📡 Hardware Required
Quectel EM06-A LTE modem (or compatible AT modem)

---

## 👤 Contacts

Contact management application with full CRUD operations.

### ✨ Features
- 📋 Scrollable contact list with alphabetical sorting
- 🔍 Search bar for quick filtering by name or phone
- ➕ Add new contacts with:
  - 👤 First and Last name
  - 📞 Phone number
  - 📧 Email address
- ✏️ Edit existing contacts
- 🗑️ Delete contacts with confirmation
- 🎨 Auto-generated avatars with initials
- 📞 Quick-call button for each contact
- 💬 Quick-message button for each contact

### 💾 Data Storage
`%LocalAppData%\WindowsPhoneNext\contacts.json`

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Esc** | Close editor/Exit app |

---

## 🌐 Browser

Chromium-based web browser using Microsoft WebView2.

### ✨ Features
- 📑 Tabbed browsing interface
- 🔗 Address bar with URL input
- 🔍 Search integration (queries go to Google)
- ⬅️➡️🔄 Back, Forward, Refresh navigation
- 🖥️ Desktop-class web rendering
- ⚡ Full JavaScript and modern web support
- 📱 Mobile user agent option

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **F5** | Refresh page |
| **Esc** | Exit browser |

---

## 📧 Gmail

Secure Gmail-only browser with domain locking.

### ✨ Features
- 🔒 **Domain-locked** to Gmail/Google services only
- 🚫 Blocks navigation to non-Gmail websites
- 📧 Full Gmail web interface support
- 🔄 Refresh button for email updates
- ✅ Supports Google account sign-in flow
- 🛡️ Enhanced security - prevents phishing redirects

### 🔐 Allowed Domains
- `mail.google.com`
- `accounts.google.com`
- `google.com` (and subdomains)
- `googleapis.com`
- `gstatic.com`
- `googleusercontent.com`

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Esc** | Exit app |

---

## 🗺️ Maps

GPS navigation application with OpenStreetMap.

### ✨ Features
- 🗺️ OpenStreetMap tile rendering via Leaflet.js
- 🛣️ A-to-B route calculation via OSRM API
- 🔍 Location search via Nominatim geocoding
- 📍 Real-time GPS position tracking with blue marker
- 🧭 Turn-by-turn navigation mode with:
  - Voice-ready direction instructions
  - Distance to next turn
  - Estimated time remaining
- ➕➖ Zoom controls
- 📍 "My Location" button for centering
- 🎭 Demo mode with simulated movement

### 📡 Hardware Required
VK-172 USB GPS dongle (or compatible NMEA GPS device)

### 🎭 Demo Mode
When no GPS is detected, simulates movement through NYC with varying speeds for testing navigation features.

### 🌐 APIs Used
| Service | Purpose |
|---------|---------|
| [OpenStreetMap](https://www.openstreetmap.org/) | Map tiles |
| [OSRM](https://project-osrm.org/) | Routing engine |
| [Nominatim](https://nominatim.org/) | Geocoding/search |

---

## 🎵 Music

Audio player with real-time spectrum analyzer visualization.

### ✨ Features
- 🎵 Supports MP3, WAV, FLAC, OGG, WMA formats
- 📊 64-bar real-time spectrum analyzer:
  - 🌈 Gradient coloring (green to yellow to red)
  - 📈 Peak hold indicators with decay effect
- 📋 Playlist management with song queue
- 🔀 Shuffle mode for random playback
- 🔁 Repeat modes (off, all, one)
- 🔊 Volume control slider
- ⏱️ Progress bar with seek capability

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Space** | Play/Pause |
| **N** | Next track |
| **P** | Previous track |
| **M** | Mute/Unmute |
| **Esc** | Exit player |

---

## 🎬 Video

Video player application for media playback.

### ✨ Features
- 🎥 Supports MP4, AVI, MKV, WMV, MOV formats
- ▶️⏸️ Play/Pause toggle with large button
- ⏩⏪ 10-second skip forward/backward buttons
- 📊 Progress bar with time display
- 🖱️ Click-to-seek on progress bar
- 📺 Fullscreen toggle (double-click or button)
- 👻 Auto-hiding controls during playback
- 📂 Open file dialog for video selection

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Space** | Play/Pause |
| **Left Arrow** | Skip back 10s |
| **Right Arrow** | Skip forward 10s |
| **F** / **F11** | Toggle fullscreen |
| **Esc** | Exit fullscreen/app |

### 🎨 UI Behavior
- Controls auto-hide after 3 seconds during playback
- Mouse movement shows controls
- Semi-transparent control overlay

---

## 📅 Calendar

Calendar application with multiple view modes.

### ✨ Features
- 📆 **Month View** (default):
  - Full month grid display
  - Today highlighting with accent color
  - Marked dates shown with indicators
- 📋 **Day View**:
  - Hourly time slots (tap a day to enter)
  - Scroll through 24-hour schedule
- 📊 **Year View**:
  - 12-month overview
  - Quick month navigation
- 📌 Mark important dates (toggle with button)
- 💾 Persistent storage of marked dates

### 💾 Data Storage
`%LocalAppData%\WindowsPhoneNext\Calendar\marked_dates.json`

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Left/Right Arrow** | Previous/Next month |
| **Esc** | Back/Exit |

---

## 🖼️ Gallery

Image viewer with thumbnail navigation.

### ✨ Features
- 🖼️ Full-size image display with fit-to-window scaling
- 📜 Thumbnail strip at bottom (100px thumbnails)
- 📂 Open folder dialog to select image directory
- ℹ️ Image info overlay showing:
  - 📄 Filename
  - 📐 Dimensions (width × height)
  - 💾 File size
- ⚡ Asynchronous thumbnail loading for performance

### 🖼️ Supported Formats
JPG, JPEG, PNG, GIF, BMP, WebP, TIFF

### 📁 Default Folder
User's Pictures folder (`%USERPROFILE%\Pictures`)

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Left/Right Arrow** | Previous/Next image |
| **Home** | First image |
| **End** | Last image |
| **Esc** | Exit gallery |

---

## ⚙️ Settings

Comprehensive system configuration and connectivity management.

### ✨ Features

#### Bluetooth
- 🔵 Toggle Bluetooth on/off
- 🔍 Scan for nearby Bluetooth devices
- 📱 View paired devices list
- ➕ Pair with new devices
- 🗑️ Forget/unpair devices
- 💾 Persistent storage of paired devices

#### Wi-Fi
- 📶 Toggle Wi-Fi on/off
- 🔍 Scan for available networks
- 🔗 View currently connected network
- 📋 Saved networks list with quick connect
- 🔐 Password dialog for secured networks
- 🗑️ Forget saved networks
- 💾 Auto-save successful connections

#### About Phone
- 📱 Device name and version
- 📞 Phone number (from SIM)
- 🔢 IMEI number (from modem)
- 📡 Modem manufacturer and model
- 🏢 Network operator name
- 📊 Signal strength indicator

#### System
- 📊 Display information (720x1560)
- 💾 Storage usage statistics

### 💾 Data Storage
`%LocalAppData%\WindowsPhoneNext\connectivity_settings.json`

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Esc** | Exit app |

---

## 🖥️ Terminal

Tabbed terminal application supporting multiple shell environments.

### ✨ Features
- 📑 **Tabbed Interface** with three shells:
  - **CMD** - Windows Command Prompt (dark gray theme)
  - **PowerShell** - Windows PowerShell (navy blue theme)
  - **WSL** - Windows Subsystem for Linux (purple theme)
- ⌨️ Command input with history navigation
- 📜 Scrollable output with auto-scroll
- 🎨 Color-coded tabs and backgrounds for each shell
- 🔄 Real command execution with output capture
- ⏹️ Kill running processes (Ctrl+C)
- 🗑️ Clear screen command

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **1** | Switch to CMD tab |
| **2** | Switch to PowerShell tab |
| **3** | Switch to WSL tab |
| **Up/Down** | Navigate command history |
| **Ctrl+C** | Kill current process |
| **Enter** | Execute command |
| **Esc** | Exit terminal |

### 🎨 Shell Themes
| Shell | Background Color |
|-------|-----------------|
| CMD | `#1E1E1E` (Dark Gray) |
| PowerShell | `#012456` (Navy Blue) |
| WSL | `#300A24` (Ubuntu Purple) |

### 📝 Built-in Commands
- `cd` - Change directory
- `cls` / `clear` - Clear screen
- `exit` - Close terminal

---

## 🤖 Claude Code

AI-powered coding assistant with voice and text input for repository management.

### ✨ Features
- 💬 **Chat Interface** - Conversational interaction with Claude Code
- 🎤 **Voice Input** - Speak commands using Windows Speech Recognition
- ⌨️ **Text Input** - Type messages in chat field
- 📁 **Repository Management**:
  - GitHub repository support
  - GitLab repository support
  - Gitea repository support
  - Local repository paths
- 💾 **Saved Repositories** - Quick switch between projects
- 🔧 **Configurable CLI Path** - Custom Claude Code installation path

### 🎤 Voice Commands
Simply tap the microphone button and speak naturally:
- "Work on owner/repo"
- "Open repo myproject"
- "Switch to username/repository"
- Any coding question or task

### 🔗 Supported Git Providers
| Provider | Format |
|----------|--------|
| **GitHub** | `owner/repo` or full URL |
| **GitLab** | `owner/repo` or full URL |
| **Gitea** | Full URL required |
| **Local** | Full file path |

### ⚙️ Settings Panel
- Select git provider
- Enter repository URL or path
- Manage saved repositories
- Configure Claude Code CLI path

### 💾 Data Storage
`%LocalAppData%\WindowsPhoneNext\claudecode_settings.json`

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Enter** | Send message |
| **Esc** | Close settings panel / Exit app |

### 📋 Requirements
- Claude Code CLI installed and accessible
- Windows Speech Recognition (for voice input)
- Internet connection (for remote repositories)

---

## 📦 Android Apps (APK Sideloader)

Windows Subsystem for Android integration for running Android apps.

### ✨ Features
- 📲 Install APK files via file picker
- 📋 View list of installed Android apps
- 🚀 Launch installed Android apps directly
- 🔄 WSA connection status indicator:
  - 🟢 Green: WSA connected and ready
  - 🔵 Blue: WSA installed but not running
  - 🔴 Red: WSA not available
- 🗑️ Uninstall apps from list
- ⚙️ Quick access to WSA settings

### 📱 How It Works
1. Uses ADB (Android Debug Bridge) to communicate with WSA
2. Connects to WSA's local ADB server (`127.0.0.1:58526`)
3. Installs APKs using `adb install` command
4. Launches apps using Android intents

### 📋 Requirements
- Windows Subsystem for Android (WSA) installed
- ADB in one of these locations:
  - `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe`
  - `C:\platform-tools\adb.exe`
  - In system PATH

### 💾 Data Storage
`%LocalAppData%\WindowsPhoneNext\android_apps.json`

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **Esc** | Exit app |

---

## 🃏 Solitaire

Classic Klondike solitaire card game.

### ✨ Features
- 🃏 Standard Klondike rules:
  - 7 tableau piles with cascading cards
  - 4 foundation piles (build Ace to King by suit)
  - Stock pile with 3-card draw
- 👆 Touch/click card selection:
  - Tap card to select
  - Tap destination to move
  - Tap stock pile to draw cards
- ↩️ **Undo** button for move reversal
- 🏆 **Auto-complete** when all cards are face-up
- 🔢 Move counter to track efficiency
- 🎰 New game button to reshuffle

### 🎨 Visual Design
- Green felt background
- Classic card face designs
- Highlighted selection state
- Win celebration overlay

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **U** | Undo last move |
| **N** | New game |
| **Esc** | Exit game |

---

## 🀄 Mahjong

Tile matching puzzle game (Mahjong Solitaire style).

### ✨ Features
- 🀄 Classic tile matching rules:
  - Match identical free tiles
  - Free tiles have no tiles on top and at least one side open
- 🏗️ Layered pyramid layout with multiple levels
- 💡 **Hint** button highlights valid matches
- 🔀 **Shuffle** button redistributes remaining tiles
- 🔢 Remaining tiles counter
- 🏆 Win detection with celebration
- ⚠️ "No moves available" detection with auto-shuffle option

### 🎨 Tile Design
- Traditional Mahjong tile faces
- 3D layered appearance with shadows
- Highlighted state for selected tiles
- Matched tiles animate away

### ⌨️ Keyboard Shortcuts
| Key | Action |
|:---:|--------|
| **H** | Show hint |
| **S** | Shuffle tiles |
| **N** | New game |
| **Esc** | Exit game |

---

## 📂 Files

File browser application.

### 📝 Status
*Placeholder - Coming soon*

---

## 📷 Camera

Camera capture application.

### 📝 Status
*Placeholder - Coming soon*

---

## 📡 ModemLib (Shared Library)

Shared library for modem communication used by Dialer and Messaging.

### ✨ Features
- AT command interface for Quectel modems
- Voice call management (dial, answer, hang up)
- SMS send/receive functionality
- Signal strength monitoring
- Network registration status
- DTMF tone generation

### 🔌 Supported Modems
- Quectel EM06-A (LTE Cat 6)
- Other AT-compatible modems

---

## 🎨 UI Specifications

All apps follow consistent design guidelines:

| Property | Value |
|----------|-------|
| **Resolution** | 720 × 1560 pixels |
| **Aspect Ratio** | 9:19.5 (tall phone format) |
| **UI Framework** | WPF (.NET 8) |
| **Theme** | Dark mode with accent colors |
| **Touch Targets** | Minimum 48×48 pixels |

### 🎨 Color Palette
| Color | Hex | Usage |
|-------|-----|-------|
| Background | `#1A1A2E` | App backgrounds |
| Surface | `#16213E` | Cards, panels |
| Accent | `#0078D4` | Buttons, highlights |
| Text Primary | `#FFFFFF` | Main text |
| Text Secondary | `#9CA3AF` | Subtitles, hints |

---

Made with ❤️ for mobile enthusiasts
