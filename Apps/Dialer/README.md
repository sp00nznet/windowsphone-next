# Phone (Dialer)

Voice calling application with full telephony support.

## Features

- **Dialpad** - Touch-friendly number entry with DTMF tones
- **Call History** - View recent incoming, outgoing, and missed calls
- **Active Call Screen** - Mute, speaker, keypad access during calls
- **Incoming Call UI** - Answer/decline with caller ID display
- **Contact Integration** - Shows contact names for known numbers
- **Call Blocking** - Block unwanted callers (via Contacts app)
- **Demo Mode** - Test UI without modem hardware

## Screenshots

The app has three main views:
1. **Dialpad** - Enter phone numbers
2. **Call History** - Recent calls list
3. **Active Call** - In-call controls

## Usage

### Making a Call
1. Enter the phone number using the dialpad
2. Tap the green call button
3. Wait for the call to connect

### Answering a Call
- When an incoming call arrives, tap **Answer** or **Decline**
- Caller ID is displayed if available

### During a Call
- **Mute** - Toggle microphone
- **Speaker** - Toggle speakerphone
- **Keypad** - Send DTMF tones
- **End** - Hang up the call

### Call History
- Tap the clock icon to view call history
- Tap any entry to call that number
- Swipe or long-press to delete entries

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `0-9` | Enter digits |
| `*` | Enter star |
| `#` | Enter hash |
| `Enter` | Make call |
| `Escape` | Go back / End call |
| `M` | Toggle mute (during call) |
| `S` | Toggle speaker (during call) |

## Demo Mode

The app includes a demo mode for testing without modem hardware:
- Simulates incoming calls
- Simulates call states (ringing, connected, ended)
- Toggle in Settings or when modem not connected

## Technical Details

- Uses **ModemLib** for AT command communication
- Modem: Quectel EM06-A (LTE Cat 6)
- AT Commands: `ATD`, `ATA`, `ATH`, `AT+CLCC`, `AT+VTS`

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Quectel EM06-A modem (or demo mode)

## Building

```powershell
cd Apps/Dialer
dotnet build
```

## Project Structure

```
Dialer/
├── MainWindow.xaml        # Main UI layout
├── MainWindow.xaml.cs     # Call logic and modem interaction
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── DialerTheme.xaml   # App-specific styles
└── WindowsPhoneDialer.csproj
```
