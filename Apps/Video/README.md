# Video

Video player with playback controls and fullscreen support.

## Features

- **Video Playback** - Plays common video formats
- **Progress Seeking** - Drag slider to seek
- **Skip Controls** - 10-second forward/backward skip
- **Fullscreen Mode** - Toggle fullscreen viewing
- **Auto-Hide Controls** - Controls fade during playback
- **Time Display** - Current position and duration
- **File Picker** - Open any video file

## Supported Formats

| Format | Extension |
|--------|-----------|
| MP4 | .mp4 |
| AVI | .avi |
| MKV | .mkv |
| MOV | .mov |
| WMV | .wmv |
| FLV | .flv |
| WebM | .webm |

## Usage

### Playing a Video
1. Tap **Open File** or the play area
2. Select a video file
3. Video starts automatically

### Playback Controls
- **Play/Pause** - Center button or `Space`
- **Seek** - Drag the progress slider
- **Skip Back** - Left button or `Left Arrow` (10 sec)
- **Skip Forward** - Right button or `Right Arrow` (10 sec)
- **Fullscreen** - Expand button or `F`

### Control Visibility
- Controls appear on mouse/touch movement
- Auto-hide after 3 seconds during playback
- Always visible when paused

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Space` | Play/Pause |
| `Left Arrow` | Skip back 10s |
| `Right Arrow` | Skip forward 10s |
| `F` / `F11` | Toggle fullscreen |
| `Ctrl+O` | Open file |
| `Escape` | Exit fullscreen / Close |

## Command Line

Open a video directly:
```powershell
WindowsPhoneVideo.exe "C:\Videos\movie.mp4"
```

## Building

```powershell
cd Apps/Video
dotnet build
```

## Project Structure

```
Video/
├── MainWindow.xaml        # Video player UI
├── MainWindow.xaml.cs     # Playback logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── VideoTheme.xaml
└── WindowsPhoneVideo.csproj
```
