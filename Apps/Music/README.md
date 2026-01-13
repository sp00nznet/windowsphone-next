# Windows Phone Next - Music Player

Custom Winamp build with integrated visualizer for the 720x1560 display.

## Layout (720x1560)

```
+----------------------------------+
|  Back Button (50px height)       |
+----------------------------------+
|                                  |
|                                  |
|     vis_wpnext Visualizer        |
|     (720x1390)                   |
|                                  |
|     - Spectrum Analyzer          |
|     - Oscilloscope overlay       |
|                                  |
+----------------------------------+
|     Winamp Player Controls       |
|     (720x120)                    |
|     - Transport buttons          |
|     - Progress/seek bar          |
|     - Volume                     |
+----------------------------------+
```

## Components

### 1. vis_wpnext Visualizer Plugin
Custom visualizer plugin located in `winamp-src/Src/Plugins/Visualization/vis_wpnext/`

Features:
- 64-bar spectrum analyzer with peak indicators
- Optional oscilloscope overlay
- 3 color schemes (Blue, Green, Purple)
- 3 bar styles (Solid, Gradient, Outline)
- Smooth animations with peak hold
- Double-buffered rendering (no flicker)

Keyboard controls:
- `O` - Toggle oscilloscope
- `1/2/3` - Change color scheme
- `B` - Change bar style
- `ESC` - Close visualizer

### 2. Custom Winamp Configuration
The `config/` directory contains:
- `winamp.ini` - Pre-configured settings for 720px width
- `studio.xnf` - Modern skin settings

## Building

### Prerequisites
- Visual Studio 2019 or later
- Windows SDK 10.0
- DirectX 9 SDK (for Winamp core, optional)

### Build vis_wpnext Plugin Only
```batch
cd winamp-src\Src\Plugins\Visualization\vis_wpnext
msbuild vis_wpnext.vcxproj /p:Configuration=Release /p:Platform=Win32
```

### Full Winamp Build
See `winamp-src/README.md` for full build requirements including:
- Intel IPP 6.1.1.035
- libvpx, libmpg123, OpenSSL-1.0.1u

## Installation

1. Build the vis_wpnext.dll plugin
2. Copy to Winamp's `Plugins` directory
3. Copy custom configuration from `config/` to Winamp directory
4. Run `WPNextMusicLauncher.exe` to start

## Integration with WindowsPhoneNext

The Music app is launched from the Launcher with:
- Fixed 720x1560 window
- Visualizer docked at top
- Player controls at bottom
- Back button handling via IPC

## Future Enhancements

The extra height space can be used for:
- Song info scroller
- Playlist preview
- Album art display
- Equalizer presets slider
