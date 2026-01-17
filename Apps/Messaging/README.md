# Messages

SMS messaging application with conversation view.

## Features

- **Conversation List** - All message threads with contact names
- **Chat View** - Bubble-style message display
- **Send/Receive SMS** - Full SMS support via modem
- **Contact Integration** - Shows names instead of numbers
- **Message Blocking** - Block unwanted senders (via Contacts)
- **Auto-Refresh** - New messages appear automatically
- **Demo Mode** - Sample conversations for testing

## Usage

### Viewing Conversations
- The main screen shows all message threads
- Each thread shows the contact/number and last message
- Unread messages are highlighted

### Reading Messages
1. Tap a conversation to open it
2. Messages are displayed in chat bubbles
3. Your messages appear on the right (blue)
4. Received messages appear on the left (gray)

### Sending a Message
1. Open a conversation or tap **New Message**
2. Type your message in the input field
3. Tap the send button or press Enter

### Starting a New Conversation
1. Tap the **+** button
2. Enter the phone number
3. Type and send your message

### Deleting Conversations
- Long-press a conversation to delete
- Or swipe left on the conversation

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Enter` | Send message |
| `Escape` | Go back |
| `Ctrl+N` | New message |

## Message Status

Messages show delivery status:
- **Sending** - Message is being sent
- **Sent** - Message delivered to network
- **Failed** - Send failed (tap to retry)

## Blocking

To block a number from sending messages:
1. Open the **Contacts** app
2. Find or add the contact
3. Enable **Block Messages**

Blocked messages are silently filtered.

## Technical Details

- Uses **ModemLib** for SMS communication
- Modem: Quectel EM06-A
- AT Commands: `AT+CMGS`, `AT+CMGL`, `AT+CMGR`, `AT+CMGD`
- SMS stored on SIM card

## Demo Mode

When no modem is connected, the app shows sample conversations for UI testing.

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Quectel EM06-A modem (or demo mode)

## Building

```powershell
cd Apps/Messaging
dotnet build
```

## Project Structure

```
Messaging/
├── MainWindow.xaml        # Conversation list and chat UI
├── MainWindow.xaml.cs     # SMS logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── MessagingTheme.xaml
└── WindowsPhoneMessaging.csproj
```
