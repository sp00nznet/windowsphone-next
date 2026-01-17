# Calendar

Date navigation with month, year, and day views.

## Features

- **Three Views** - Year, Month, and Day views
- **Date Marking** - Mark important dates
- **Today Highlight** - Current date always visible
- **24-Hour Day View** - Hour-by-hour timeline
- **Current Hour** - Highlighted in day view
- **Persistence** - Marked dates saved locally
- **Quick Navigation** - Jump between views easily

## Usage

### Month View (Default)
- Shows full month calendar
- Today is highlighted
- Marked dates show a dot indicator
- Tap any day to see day view
- Use arrows to navigate months

### Year View
1. Tap the year in header
2. Shows all 12 months
3. Each month shows marked date count
4. Tap a month to enter month view

### Day View
1. Tap any day from month view
2. Shows 24-hour timeline
3. Current hour highlighted (if today)
4. Mark/unmark the day with button

### Marking a Day
1. Navigate to day view
2. Tap **Mark as Important**
3. Star indicator appears
4. Tap again to unmark

### Quick Navigation
- **Today** button returns to current date
- **Back** navigates up (Day → Month → Year)

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Go back / Close |

## Data Storage

Marked dates stored in:
```
%LOCALAPPDATA%\WindowsPhoneNext\Calendar\marked_dates.json
```

## Building

```powershell
cd Apps/Calendar
dotnet build
```

## Project Structure

```
Calendar/
├── MainWindow.xaml        # Calendar views UI
├── MainWindow.xaml.cs     # Date logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── CalendarTheme.xaml
└── WindowsPhoneCalendar.csproj
```
