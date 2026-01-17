# Contacts

Contact management application with search and call/message integration.

## Features

- **Contact List** - Alphabetically sorted contacts with initials
- **Search** - Find contacts by name or phone number
- **Add/Edit/Delete** - Full contact management
- **Blocking** - Block contacts from calls and messages
- **App Integration** - Call or message directly from contact view
- **Persistence** - Contacts saved locally as JSON

## Usage

### Viewing Contacts
- The main screen shows all contacts sorted alphabetically
- Each contact shows initials, name, and phone number
- Use the search box to filter by name or number

### Adding a Contact
1. Tap the **+** button
2. Enter first name, last name
3. Enter phone number (required)
4. Optionally add email
5. Tap **Save**

### Editing a Contact
1. Tap a contact to view details
2. Tap **Edit**
3. Modify the information
4. Tap **Save**

### Deleting a Contact
1. Tap a contact to view details
2. Tap **Delete**
3. Confirm deletion

### Blocking a Contact
1. Tap a contact to view details
2. Tap **Block Contact**
3. Confirm - blocked contacts cannot call or message you
4. Tap again to unblock

### Calling/Messaging
1. Tap a contact to view details
2. Tap the **Phone** icon to call
3. Tap the **Message** icon to send SMS

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Go back / Close |

## Data Storage

Contacts are stored in:
```
%LOCALAPPDATA%\WindowsPhoneNext\contacts.json
```

## Demo Mode

When first launched, sample contacts are created for testing:
- Alice Smith
- Bob Johnson
- Carol Williams
- David Brown
- Eve Davis

## Building

```powershell
cd Apps/Contacts
dotnet build
```

## Project Structure

```
Contacts/
├── MainWindow.xaml        # Contact list and detail UI
├── MainWindow.xaml.cs     # Contact logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── ContactsTheme.xaml
└── WindowsPhoneContacts.csproj
```
