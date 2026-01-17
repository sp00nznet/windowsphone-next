# Gmail

Secure Gmail-only browser application with domain locking.

## Features

- **Gmail Access** - Full Gmail web interface
- **Domain Locked** - Only allows Google/Gmail domains
- **Mobile Optimized** - Renders Gmail mobile view
- **Security** - Blocks navigation to external sites
- **New Window Handling** - Links stay in-app

## Usage

### Accessing Gmail
1. Launch the app
2. Sign in with your Google account (if not already signed in)
3. Use Gmail as normal

### Security Features
- Only gmail.com and related Google domains allowed
- External links are blocked
- New windows open in same view (if allowed domain)

## Allowed Domains

| Domain | Purpose |
|--------|---------|
| `mail.google.com` | Gmail |
| `accounts.google.com` | Sign-in |
| `www.google.com` | OAuth |
| `google.com` | Authentication |
| `googleapis.com` | API calls |
| `gstatic.com` | Static assets |
| `googleusercontent.com` | User content |
| `gmail.com` | Alternative access |

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Close app |
| `F5` | Refresh |

## Why Domain Locking?

This app is designed as a dedicated Gmail client. Domain locking:
- Prevents accidental navigation away from email
- Improves security by limiting attack surface
- Creates a focused email experience

## Technical Details

- **Engine**: Microsoft WebView2 (Chromium)
- **User Agent**: Android/Mobile Chrome
- **Viewport**: 720px width forced
- **Navigation**: Blocked for non-Google domains

## Requirements

- WebView2 Runtime
- Google account
- Internet connection

## Building

```powershell
cd Apps/Gmail
dotnet build
```

## Project Structure

```
Gmail/
├── MainWindow.xaml        # Gmail WebView UI
├── MainWindow.xaml.cs     # Domain filtering logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── GmailTheme.xaml
└── WindowsPhoneGmail.csproj
```
