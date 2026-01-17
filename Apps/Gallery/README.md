# Gallery

Image viewer with thumbnail navigation and folder browsing.

## Features

- **Image Viewing** - Full-screen image display
- **Thumbnail Strip** - Quick navigation via thumbnails
- **Folder Browsing** - Open any image folder
- **Image Info** - File name, dimensions, size
- **Keyboard Navigation** - Arrow keys to browse
- **Default Folder** - Configure startup folder
- **Multiple Formats** - Supports common image types

## Supported Formats

| Format | Extensions |
|--------|------------|
| JPEG | .jpg, .jpeg |
| PNG | .png |
| GIF | .gif |
| BMP | .bmp |
| WebP | .webp |
| TIFF | .tiff, .tif |

## Usage

### Opening a Folder
1. Tap the **Folder** button
2. Select an image folder
3. Thumbnails appear at the bottom

### Browsing Images
- Tap a thumbnail to view
- Use **Left/Right** arrows or swipe
- Image info shows at bottom

### Setting Default Folder
1. Tap **Settings** (gear icon)
2. Tap **Browse** next to Default Path
3. Select your preferred folder
4. App opens this folder on launch

### Clearing Default
1. Open Settings
2. Tap **Clear** to remove default folder

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Left Arrow` | Previous image |
| `Right Arrow` | Next image |
| `Home` | First image |
| `End` | Last image |
| `Escape` | Close / Exit settings |

## Data Storage

Settings stored in:
```
%LOCALAPPDATA%\WindowsPhoneNext\gallery_settings.json
```

## Building

```powershell
cd Apps/Gallery
dotnet build
```

## Project Structure

```
Gallery/
├── MainWindow.xaml        # Gallery UI
├── MainWindow.xaml.cs     # Image loading logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── GalleryTheme.xaml
└── WindowsPhoneGallery.csproj
```
