using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace WindowsPhoneNext.Maps;

public partial class MainWindow : Window
{
    private readonly GpsController _gps;
    private readonly HttpClient _httpClient;
    private bool _isRouteMode;
    private bool _isNavigating;
    private double _currentLat = 40.7128;  // Default: NYC
    private double _currentLon = -74.0060;
    private int _currentZoom = 13;

    // Demo mode
    private bool _isDemoMode;
    private System.Windows.Threading.DispatcherTimer? _demoTimer;
    private int _demoStep;
    private readonly List<(double Lat, double Lon, double Speed, double Heading)> _demoPath = new()
    {
        (40.7128, -74.0060, 0, 0),        // Start: NYC
        (40.7135, -74.0055, 15, 45),
        (40.7142, -74.0048, 25, 45),
        (40.7150, -74.0040, 30, 40),
        (40.7160, -74.0030, 35, 35),
        (40.7168, -74.0020, 30, 30),
        (40.7175, -74.0010, 25, 25),
        (40.7180, -74.0005, 20, 20),
        (40.7185, -74.0000, 15, 15),
        (40.7190, -73.9995, 10, 10),
    };

    public MainWindow()
    {
        InitializeComponent();

        _gps = new GpsController();
        _gps.PositionChanged += Gps_PositionChanged;
        _gps.StatusChanged += Gps_StatusChanged;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "WindowsPhoneNext-Maps/1.0");
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeMapAsync();
        await InitializeGpsAsync();
    }

    private async Task InitializeMapAsync()
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            await MapView.EnsureCoreWebView2Async();

            // Configure WebView2
            MapView.CoreWebView2.Settings.IsScriptEnabled = true;
            MapView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            MapView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            // Load the map HTML
            MapView.CoreWebView2.NavigateToString(GetMapHtml());

            // Handle messages from JavaScript
            MapView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading map: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task InitializeGpsAsync()
    {
        UpdateGpsStatus("Searching...", false);

        if (await _gps.AutoConnectAsync())
        {
            UpdateGpsStatus($"Connected ({_gps.Satellites} sats)", _gps.HasFix);
        }
        else
        {
            // Start demo mode
            StartDemoMode();
        }
    }

    private void StartDemoMode()
    {
        _isDemoMode = true;
        _demoStep = 0;

        UpdateGpsStatus("Demo Mode", true);

        // Set initial position
        var start = _demoPath[0];
        _currentLat = start.Lat;
        _currentLon = start.Lon;
        ExecuteMapScript($"updateCurrentLocation({start.Lat}, {start.Lon}, {start.Heading});");
        ExecuteMapScript($"setView({start.Lat}, {start.Lon}, 15);");

        // Start demo timer
        _demoTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _demoTimer.Tick += DemoTimer_Tick;
        _demoTimer.Start();
    }

    private void DemoTimer_Tick(object? sender, EventArgs e)
    {
        _demoStep = (_demoStep + 1) % _demoPath.Count;
        var pos = _demoPath[_demoStep];

        _currentLat = pos.Lat;
        _currentLon = pos.Lon;

        UpdateGpsStatus($"Demo: {pos.Speed:F0} km/h", true);
        ExecuteMapScript($"updateCurrentLocation({pos.Lat}, {pos.Lon}, {pos.Heading});");

        // Follow the simulated position
        if (_isNavigating)
        {
            ExecuteMapScript($"setView({pos.Lat}, {pos.Lon}, 16);");
        }
    }

    private void StopDemoMode()
    {
        _isDemoMode = false;
        _demoTimer?.Stop();
        _demoTimer = null;
    }

    private void UpdateGpsStatus(string status, bool hasFix)
    {
        Dispatcher.Invoke(() =>
        {
            GpsStatusText.Text = $"GPS: {status}";
            GpsIndicator.Fill = new SolidColorBrush(hasFix ?
                Color.FromRgb(76, 175, 80) :  // Green
                Color.FromRgb(244, 67, 54));   // Red
        });
    }

    private void Gps_PositionChanged(object? sender, GpsPositionEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _currentLat = e.Latitude;
            _currentLon = e.Longitude;

            UpdateGpsStatus($"{e.Satellites} sats, {e.Speed:F0} km/h", e.HasFix);

            // Update map marker
            ExecuteMapScript($"updateCurrentLocation({e.Latitude}, {e.Longitude}, {e.Heading});");
        });
    }

    private void Gps_StatusChanged(object? sender, GpsStatusEventArgs e)
    {
        UpdateGpsStatus(e.Message, _gps.HasFix);
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = JsonSerializer.Deserialize<MapMessage>(e.WebMessageAsJson);
            if (message == null) return;

            switch (message.Type)
            {
                case "locationSelected":
                    HandleLocationSelected(message.Lat, message.Lon, message.Name);
                    break;
                case "mapClick":
                    HandleMapClick(message.Lat, message.Lon);
                    break;
            }
        }
        catch { }
    }

    private void HandleLocationSelected(double lat, double lon, string? name)
    {
        if (_isRouteMode)
        {
            // Set as destination
            ToBox.Text = name ?? $"{lat:F5}, {lon:F5}";
            ToBox.Tag = new LatLon(lat, lon);
        }
    }

    private void HandleMapClick(double lat, double lon)
    {
        if (_isRouteMode)
        {
            ToBox.Text = $"{lat:F5}, {lon:F5}";
            ToBox.Tag = new LatLon(lat, lon);
            ExecuteMapScript($"setDestinationMarker({lat}, {lon});");
        }
    }

    private string GetMapHtml()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=720, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{ height: 100%; width: 100%; overflow: hidden; }}
        #map {{ height: 100%; width: 100%; }}
        .leaflet-control-zoom {{ display: none; }}
        .current-location-marker {{
            background: #0078D4;
            border: 3px solid white;
            border-radius: 50%;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
        }}
        .destination-marker {{
            background: #F44336;
            border: 3px solid white;
            border-radius: 50%;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var map = L.map('map', {{
            zoomControl: false,
            attributionControl: false
        }}).setView([{_currentLat}, {_currentLon}], {_currentZoom});

        L.tileLayer('https://tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            maxZoom: 19
        }}).addTo(map);

        var currentLocationMarker = null;
        var destinationMarker = null;
        var routeLine = null;
        var headingArrow = null;

        // Current location marker (blue dot)
        var currentLocationIcon = L.divIcon({{
            className: 'current-location-marker',
            iconSize: [20, 20],
            iconAnchor: [10, 10]
        }});

        // Destination marker (red dot)
        var destinationIcon = L.divIcon({{
            className: 'destination-marker',
            iconSize: [16, 16],
            iconAnchor: [8, 8]
        }});

        function updateCurrentLocation(lat, lon, heading) {{
            if (currentLocationMarker) {{
                currentLocationMarker.setLatLng([lat, lon]);
            }} else {{
                currentLocationMarker = L.marker([lat, lon], {{icon: currentLocationIcon}}).addTo(map);
            }}
        }}

        function setDestinationMarker(lat, lon) {{
            if (destinationMarker) {{
                destinationMarker.setLatLng([lat, lon]);
            }} else {{
                destinationMarker = L.marker([lat, lon], {{icon: destinationIcon}}).addTo(map);
            }}
        }}

        function clearDestination() {{
            if (destinationMarker) {{
                map.removeLayer(destinationMarker);
                destinationMarker = null;
            }}
            if (routeLine) {{
                map.removeLayer(routeLine);
                routeLine = null;
            }}
        }}

        function drawRoute(coordinates) {{
            if (routeLine) {{
                map.removeLayer(routeLine);
            }}
            routeLine = L.polyline(coordinates, {{
                color: '#0078D4',
                weight: 5,
                opacity: 0.8
            }}).addTo(map);

            map.fitBounds(routeLine.getBounds(), {{padding: [50, 50]}});
        }}

        function setView(lat, lon, zoom) {{
            map.setView([lat, lon], zoom);
        }}

        function zoomIn() {{
            map.zoomIn();
        }}

        function zoomOut() {{
            map.zoomOut();
        }}

        // Handle map clicks
        map.on('click', function(e) {{
            window.chrome.webview.postMessage(JSON.stringify({{
                type: 'mapClick',
                lat: e.latlng.lat,
                lon: e.latlng.lng
            }}));
        }});
    </script>
</body>
</html>";
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            await SearchLocationAsync(SearchBox.Text);
        }
    }

    private async Task SearchLocationAsync(string query)
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            // Use Nominatim API for geocoding
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
            var response = await _httpClient.GetStringAsync(url);
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(response);

            if (results?.Count > 0)
            {
                var result = results[0];
                var lat = double.Parse(result.lat);
                var lon = double.Parse(result.lon);

                ExecuteMapScript($"setView({lat}, {lon}, 15);");
                ExecuteMapScript($"setDestinationMarker({lat}, {lon});");

                if (_isRouteMode)
                {
                    ToBox.Text = result.display_name;
                    ToBox.Tag = new LatLon(lat, lon);
                }
            }
            else
            {
                MessageBox.Show("Location not found", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void RouteToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isRouteMode = !_isRouteMode;

        if (_isRouteMode)
        {
            SearchBox.Visibility = Visibility.Collapsed;
            RoutePanel.Visibility = Visibility.Visible;
            GetRouteButton.Visibility = Visibility.Visible;

            // Set current location as start
            if (_gps.HasFix || _isDemoMode)
            {
                FromBox.Text = _isDemoMode ? "Current Location (Demo)" : "Current Location";
                FromBox.Tag = new LatLon(_currentLat, _currentLon);
            }
            else
            {
                FromBox.Text = "";
            }
            ToBox.Text = "";
            ToBox.Tag = null;
        }
        else
        {
            SearchBox.Visibility = Visibility.Visible;
            RoutePanel.Visibility = Visibility.Collapsed;
            GetRouteButton.Visibility = Visibility.Collapsed;
            RouteInfoPanel.Visibility = Visibility.Collapsed;

            ExecuteMapScript("clearDestination();");
        }
    }

    private async void GetRoute_Click(object sender, RoutedEventArgs e)
    {
        var fromCoord = FromBox.Tag as LatLon;
        var toCoord = ToBox.Tag as LatLon;

        if (fromCoord == null)
        {
            if (_gps.HasFix || _isDemoMode)
            {
                fromCoord = new LatLon(_currentLat, _currentLon);
            }
            else
            {
                MessageBox.Show("Please set a starting location or enable GPS", "Route",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        if (toCoord == null)
        {
            if (!string.IsNullOrWhiteSpace(ToBox.Text))
            {
                await SearchAndSetDestination(ToBox.Text);
                toCoord = ToBox.Tag as LatLon;
            }

            if (toCoord == null)
            {
                MessageBox.Show("Please set a destination", "Route",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        await CalculateRouteAsync(fromCoord, toCoord);
    }

    private async Task SearchAndSetDestination(string query)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
            var response = await _httpClient.GetStringAsync(url);
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(response);

            if (results?.Count > 0)
            {
                var result = results[0];
                ToBox.Text = result.display_name;
                ToBox.Tag = new LatLon(double.Parse(result.lat), double.Parse(result.lon));
            }
        }
        catch { }
    }

    private async Task CalculateRouteAsync(LatLon from, LatLon to)
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            // Use OSRM public API for routing
            var url = $"https://router.project-osrm.org/route/v1/driving/{from.Lon},{from.Lat};{to.Lon},{to.Lat}?overview=full&geometries=geojson";
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<OsrmResponse>(response);

            if (result?.routes?.Count > 0)
            {
                var route = result.routes[0];

                // Draw route on map
                var coordsJson = JsonSerializer.Serialize(
                    route.geometry.coordinates.Select(c => new[] { c[1], c[0] }).ToList());
                ExecuteMapScript($"drawRoute({coordsJson});");

                // Show route info
                var distanceKm = route.distance / 1000.0;
                var durationMin = route.duration / 60.0;

                RouteDistanceText.Text = distanceKm >= 1
                    ? $"{distanceKm:F1} km"
                    : $"{route.distance:F0} m";
                RouteDurationText.Text = durationMin >= 60
                    ? $"{durationMin / 60:F0} hr {durationMin % 60:F0} min"
                    : $"{durationMin:F0} min";

                RouteInfoPanel.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Could not calculate route", "Route", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Routing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void StartNavigation_Click(object sender, RoutedEventArgs e)
    {
        _isNavigating = true;
        MessageBox.Show("Turn-by-turn navigation started!\n\nFollow the blue route on the map.",
            "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ExecuteMapScript("zoomIn();");
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ExecuteMapScript("zoomOut();");
    }

    private void MyLocation_Click(object sender, RoutedEventArgs e)
    {
        if (_gps.HasFix || _isDemoMode)
        {
            ExecuteMapScript($"setView({_currentLat}, {_currentLon}, 16);");
        }
        else
        {
            MessageBox.Show("GPS position not available", "Location",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _gps.Dispose();
        Close();
    }

    private void ExecuteMapScript(string script)
    {
        try
        {
            if (MapView.CoreWebView2 != null)
            {
                MapView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }
        catch { }
    }

    protected override void OnClosed(EventArgs e)
    {
        StopDemoMode();
        _gps.Dispose();
        _httpClient.Dispose();
        base.OnClosed(e);
    }
}

// Helper classes
public record LatLon(double Lat, double Lon);

public class MapMessage
{
    public string Type { get; set; } = "";
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string? Name { get; set; }
}

public class NominatimResult
{
    public string lat { get; set; } = "";
    public string lon { get; set; } = "";
    public string display_name { get; set; } = "";
}

public class OsrmResponse
{
    public List<OsrmRoute>? routes { get; set; }
}

public class OsrmRoute
{
    public double distance { get; set; }
    public double duration { get; set; }
    public OsrmGeometry geometry { get; set; } = new();
}

public class OsrmGeometry
{
    public List<double[]> coordinates { get; set; } = new();
}
