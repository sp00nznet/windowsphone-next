# Files

File browser with Quick Access and navigation.

## Features

- **Quick Access** - Common folders at a glance
- **Folder Navigation** - Browse any directory
- **File Info** - Size, type, modification date
- **Clipboard** - Cut, copy, paste files
- **Context Menu** - Right-click actions
- **Storage Info** - Disk usage display
- **File Icons** - Type-based icons

## Quick Access Folders

| Folder | Description |
|--------|-------------|
| Desktop | User desktop |
| Documents | My Documents |
| Downloads | Downloaded files |
| Pictures | Image files |
| Music | Audio files |
| Videos | Video files |

## Usage

### Browsing
1. Tap a Quick Access folder, or
2. Navigate into any folder
3. Tap a folder to enter
4. Tap **Back** to go up

### File Operations

#### Copy
1. Select a file
2. Tap **Copy** in context menu
3. Navigate to destination
4. Tap **Paste**

#### Cut (Move)
1. Select a file
2. Tap **Cut** in context menu
3. Navigate to destination
4. Tap **Paste**

#### Delete
1. Select a file
2. Tap **Delete** in context menu
3. Confirm deletion

#### Rename
1. Select a file
2. Tap **Rename** in context menu
3. Enter new name
4. Confirm

#### Create Folder
1. Tap **New Folder** button
2. Enter folder name
3. Folder is created

### Opening Files
- Tap a file to open with default app
- Opens in associated Windows application

## File Type Icons

| Icon | Types |
|------|-------|
| 📁 | Folders |
| 📄 | Documents (.txt, .doc, .pdf) |
| 🖼️ | Images (.jpg, .png, .gif) |
| 🎵 | Audio (.mp3, .wav, .flac) |
| 🎬 | Video (.mp4, .avi, .mkv) |
| 📦 | Archives (.zip, .rar, .7z) |
| ⚙️ | Executables (.exe, .msi) |
| 📋 | Other files |

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Go back / Close |

## Storage Display

Shows at bottom:
- Used space
- Total space
- Free space

## Building

```powershell
cd Apps/Files
dotnet build
```

## Project Structure

```
Files/
├── MainWindow.xaml        # File browser UI
├── MainWindow.xaml.cs     # File operations
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── FilesTheme.xaml
└── WindowsPhoneFiles.csproj
```
