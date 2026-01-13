# Windows Phone Next - Application Guide

Detailed documentation for each application in the Windows Phone Next platform.

---

## Launcher

The main home screen and entry point for the phone.

**Features:**
- Clock and date display
- Status bar (signal strength, network, battery)
- 3x3 app grid with touch-friendly tiles
- Bottom navigation bar (Phone, Home, Messages)
- Incoming call overlay with accept/decline

**Keyboard Shortcuts:**
- `Escape` - Exit application

---

## Dialer

Full-featured phone application for voice calls.

**Features:**
- T9-style dialpad with large touch targets
- Call history (Recents tab)
- Contacts integration
- Active call screen with mute/speaker/keypad
- DTMF tone support during calls
- Demo mode when hardware unavailable

**Hardware Required:** Quectel EM06-A LTE modem (or compatible AT modem)

**Demo Mode:** When no modem is detected, the app simulates call flow (dialing, ringing, connected) for testing.

---

## Messaging

SMS messaging application with conversation view.

**Features:**
- Conversation list view with contact avatars
- Chat bubble interface (sent/received styling)
- New message composition
- Auto-refresh for incoming messages (30s interval)
- Unread message badges
- Demo mode with sample conversations

**Hardware Required:** Quectel EM06-A LTE modem (or compatible AT modem)

---

## Browser

Chromium-based web browser using WebView2.

**Features:**
- Tabbed browsing interface
- Address bar with search integration
- Back/forward/refresh navigation
- Desktop-class web rendering
- Full JavaScript support

**Keyboard Shortcuts:**
- `F5` - Refresh page
- `Escape` - Exit browser

---

## Music

Audio player with real-time spectrum analyzer visualization.

**Features:**
- Supports MP3, WAV, FLAC, OGG, WMA formats
- 64-bar real-time spectrum analyzer
- Peak hold visualization with decay
- Playlist management
- Shuffle and repeat modes
- Volume control

**Keyboard Shortcuts:**
- `Space` - Play/Pause
- `N` - Next track
- `P` - Previous track
- `M` - Mute/Unmute
- `Escape` - Exit player

---

## Maps

GPS navigation application with OpenStreetMap.

**Features:**
- OpenStreetMap tile rendering via Leaflet.js
- A-to-B route calculation via OSRM API
- Location search via Nominatim geocoding
- Real-time GPS position tracking
- Turn-by-turn navigation mode
- Zoom controls and "My Location" button
- Demo mode with simulated movement

**Hardware Required:** VK-172 USB GPS dongle (or compatible NMEA GPS device)

**Demo Mode:** When no GPS is detected, simulates movement through NYC with varying speeds.

**APIs Used:**
- [OpenStreetMap](https://www.openstreetmap.org/) - Map tiles
- [OSRM](https://project-osrm.org/) - Routing
- [Nominatim](https://nominatim.org/) - Geocoding

---

## Calendar

Calendar application with multiple view modes.

**Features:**
- Month view (default) - Full month grid
- Day view - Hourly time slots (tap a day to enter)
- Year view - 12-month overview (pinch out or button)
- Mark important dates (toggle with button)
- Persistent storage of marked dates
- Today highlighting

**Data Storage:** `%LocalAppData%\WindowsPhoneNext\Calendar\marked_dates.json`

**Keyboard Shortcuts:**
- `Left/Right Arrow` - Previous/Next month
- `Escape` - Back/Exit

---

## Gallery

Image viewer with thumbnail navigation.

**Features:**
- Thumbnail strip at bottom (100px thumbnails)
- Full-size image display with fit-to-window
- Open folder dialog to select image directory
- Image info overlay (filename, dimensions, size)
- Asynchronous thumbnail loading

**Supported Formats:** JPG, JPEG, PNG, GIF, BMP, WebP, TIFF

**Default Folder:** User's Pictures folder

**Keyboard Shortcuts:**
- `Left/Right Arrow` - Previous/Next image
- `Home` - First image
- `End` - Last image
- `Escape` - Exit gallery
