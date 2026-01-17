# Camera

Camera application for photo and video capture.

## Features

- **Photo Mode** - Capture still images
- **Video Mode** - Record video clips
- **Portrait Mode** - Coming soon
- **Flash Control** - Auto, On, Off
- **Camera Switch** - Front/rear toggle
- **Gallery Link** - View last photo
- **Recording Timer** - Video duration display

## Usage

### Taking a Photo
1. Select **Photo** mode
2. Aim camera at subject
3. Tap the **Capture** button
4. Photo saves automatically

### Recording Video
1. Select **Video** mode
2. Tap the **Record** button (turns red)
3. Recording timer starts
4. Tap again to stop
5. Video saves automatically

### Flash Settings
- Tap the **Flash** icon to cycle:
  - Auto (default)
  - On (always flash)
  - Off (no flash)

### Switching Cameras
- Tap the **Switch** icon
- Toggles between front and rear camera

### Viewing Photos
- Tap the **Gallery** thumbnail
- Opens last captured photo in Gallery

## File Locations

| Type | Path |
|------|------|
| Photos | `%USERPROFILE%\Pictures\Camera\` |
| Videos | `%USERPROFILE%\Videos\Camera\` |

## Camera Modes

| Mode | Icon | Description |
|------|------|-------------|
| Photo | Camera | Still image capture |
| Video | Film | Video recording |
| Portrait | Person | Depth effect (coming soon) |

## Simulated Mode

When no camera hardware is detected:
- Demo mode activates
- Tap to create simulated captures
- Shows "No camera detected" message

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Exit camera |

## Technical Details

- Uses Windows.Media.Capture when available
- Falls back to demo mode without camera
- JPEG output for photos
- MP4 output for videos

## Building

```powershell
cd Apps/Camera
dotnet build
```

## Project Structure

```
Camera/
├── MainWindow.xaml        # Camera viewfinder UI
├── MainWindow.xaml.cs     # Capture logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── CameraTheme.xaml
└── WindowsPhoneCamera.csproj
```
