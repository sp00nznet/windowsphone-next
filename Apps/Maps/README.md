# Maps

GPS navigation with OpenStreetMap and turn-by-turn routing.

## Features

- **Live Map** - OpenStreetMap tiles via Leaflet
- **GPS Support** - Real-time location tracking
- **Location Search** - Find places by name/address
- **Route Planning** - Calculate driving routes
- **Turn-by-Turn** - Route display with distance/time
- **Demo Mode** - Simulated GPS for testing
- **Zoom Controls** - Easy map navigation
- **My Location** - Center map on current position

## Usage

### Viewing the Map
- Pan by dragging the map
- Use **+/-** buttons to zoom
- Tap **My Location** to center on current position

### Searching for a Place
1. Enter location in the search box
2. Press **Enter**
3. Map centers on the location with a marker

### Getting Directions
1. Tap the **Route** button
2. "From" is set to current location
3. Enter destination in "To" field
4. Tap **Get Route**
5. Route displays on map with distance/time
6. Tap **Start** for navigation mode

### Demo Mode
When no GPS is connected:
- App automatically enters demo mode
- Simulated movement along a path in NYC
- Toggle in Settings overlay

## GPS Hardware

Supports USB GPS receivers:
- VK-172 USB GPS/GLONASS
- Any device outputting NMEA via serial port

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Enter` | Search location |
| `Escape` | Go back / Close settings |

## APIs Used

| Service | Purpose |
|---------|---------|
| **OpenStreetMap** | Map tiles |
| **Nominatim** | Geocoding (search) |
| **OSRM** | Route calculation |
| **Leaflet** | Map library |

## Data Storage

Settings stored in:
```
%LOCALAPPDATA%\WindowsPhoneNext\maps_settings.json
```

## Building

```powershell
cd Apps/Maps
dotnet build
```

## Project Structure

```
Maps/
├── MainWindow.xaml        # Map UI and controls
├── MainWindow.xaml.cs     # GPS and routing logic
├── GpsController.cs       # GPS serial communication
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── MapsTheme.xaml
└── WindowsPhoneMaps.csproj
```
