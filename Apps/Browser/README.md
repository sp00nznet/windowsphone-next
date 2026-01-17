# Browser

Chromium-based web browser with mobile optimization.

## Features

- **WebView2 Engine** - Powered by Microsoft Edge's Chromium engine
- **Mobile User Agent** - Pages render optimized for phone display
- **Navigation** - Back, forward, refresh, URL bar
- **Smart URL** - Auto-adds https://, treats non-URLs as searches
- **Google Search** - Searches via Google when text doesn't look like a URL
- **Bookmarks** - Quick access to saved sites
- **Viewport Injection** - Forces mobile-friendly rendering

## Usage

### Navigating
1. Enter a URL or search term in the address bar
2. Press **Enter** or tap **Go**
3. Use **Back/Forward** buttons to navigate history

### Smart URL Handling
- `google.com` → navigates to `https://google.com`
- `weather forecast` → searches Google for "weather forecast"
- `https://example.com` → navigates directly

### Using Bookmarks
1. Tap the **Bookmarks** button
2. Select a saved bookmark
3. Page loads automatically

### Default Bookmarks
- Google
- Wikipedia
- Weather
- News

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Enter` | Navigate to URL |
| `Escape` | Close browser |

## Technical Details

- **Engine**: Microsoft WebView2 (Chromium)
- **User Agent**: Android/Mobile Chrome for optimal rendering
- **Viewport**: 720px width forced for phone display
- **Context Menus**: Enabled
- **Developer Tools**: Disabled
- **Zoom**: Disabled

## Requirements

- WebView2 Runtime (bundled with Windows 11, or install separately)
- Internet connection

## Building

```powershell
cd Apps/Browser
dotnet build
```

## Project Structure

```
Browser/
├── MainWindow.xaml        # Browser UI with WebView2
├── MainWindow.xaml.cs     # Navigation logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── BrowserTheme.xaml
└── WindowsPhoneBrowser.csproj
```
