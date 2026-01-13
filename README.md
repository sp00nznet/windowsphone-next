# Windows Phone Next

A custom Windows 11 phone platform for embedded single-board computers.

![Platform](https://img.shields.io/badge/platform-Windows%2011-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-Personal%20Use-green)

---

## Hardware

| Component | Description |
|-----------|-------------|
| **SBC** | [UP Core](https://up-board.org/upcore/specifications/) or [LattePanda 3 Delta](https://www.lattepanda.com/lattepanda-3-delta) |
| **Power** | [PiSugar2 Plus](https://www.pisugar.com/products/pisugar2-plus-5000-mah-raspberry-pi-ups) 5000mAh UPS |
| **Display** | [Waveshare 4" LCD](https://www.waveshare.com/4inch-dpi-lcd-c.htm) 720x720 IPS touch |
| **Modem** | [Quectel EM06-A](https://www.quectel.com/product/lte-a-em06-series/) LTE Cat 6 M.2 |
| **GPS** | VK-172 USB GPS/GLONASS *(optional)* |

---

## Apps

| App | Description |
|-----|-------------|
| **Launcher** | Home screen with status bar, app grid, and quick navigation |
| **Dialer** | Voice calls with dialpad, call history, and in-call controls |
| **Messaging** | SMS with conversation view and chat bubbles |
| **Browser** | Chromium-based web browser with tabs |
| **Music** | Audio player with 64-bar spectrum visualizer |
| **Maps** | GPS navigation with OpenStreetMap routing |
| **Calendar** | Month/day/year views with date marking |
| **Gallery** | Image viewer with thumbnail strip |

> Apps requiring hardware (Dialer, Maps) include **demo modes** for testing without devices.

---

## Quick Start

```powershell
# Build all apps
.\Build.ps1

# Output in Dist/ folder
```

### Requirements
- Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Installation

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

## Documentation

- **[Application Guide](docs/APPS.md)** - Detailed app features and usage
- **[Development Guide](docs/DEVELOPMENT.md)** - API reference, AT commands, theming

---

## License

This project is provided as-is for educational and personal use.
