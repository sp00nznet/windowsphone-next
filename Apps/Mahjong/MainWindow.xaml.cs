using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowsPhoneNext.Mahjong;

public partial class MainWindow : Window
{
    private readonly List<Tile> _tiles = new();
    private Tile? _selectedTile;
    private readonly DispatcherTimer _timer;
    private DateTime _startTime;
    private int _tilesRemaining;

    private const int TileWidth = 50;
    private const int TileHeight = 70;
    private const int LayerOffsetX = 4;
    private const int LayerOffsetY = 4;

    // Tile symbols - using simple characters for Mahjong tiles
    private static readonly string[] TileSymbols = new[]
    {
        // Dots (circles) 1-9
        "\u2776", "\u2777", "\u2778", "\u2779", "\u277A", "\u277B", "\u277C", "\u277D", "\u277E",
        // Bamboo 1-9
        "\u4E00", "\u4E8C", "\u4E09", "\u56DB", "\u4E94", "\u516D", "\u4E03", "\u516B", "\u4E5D",
        // Characters 1-9
        "1\u4E07", "2\u4E07", "3\u4E07", "4\u4E07", "5\u4E07", "6\u4E07", "7\u4E07", "8\u4E07", "9\u4E07",
        // Winds
        "\u6771", "\u5357", "\u897F", "\u5317",
        // Dragons
        "\u4E2D", "\u767C", "\u767D",
        // Flowers (bonus)
        "\u6625", "\u590F", "\u79CB", "\u51AC",
        // Seasons
        "\u6885", "\u862D", "\u7AF9", "\u83CA"
    };

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        NewGame();
    }

    private void NewGame()
    {
        _timer.Stop();
        _tiles.Clear();
        _selectedTile = null;
        WinOverlay.Visibility = Visibility.Collapsed;
        NoMovesOverlay.Visibility = Visibility.Collapsed;

        CreateLayout();
        ShuffleTiles();
        DrawGame();

        _startTime = DateTime.Now;
        _timer.Start();
    }

    private void CreateLayout()
    {
        // Create a classic turtle/pyramid layout
        // This is a simplified layout that fits the phone screen

        // Layer 0 (bottom) - 8x6 base
        int[,] layer0 = new int[8, 6];
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 6; y++)
                layer0[x, y] = 1;

        // Layer 1 - 6x4 middle
        int[,] layer1 = new int[6, 4];
        for (int x = 0; x < 6; x++)
            for (int y = 0; y < 4; y++)
                layer1[x, y] = 1;

        // Layer 2 - 4x2 top
        int[,] layer2 = new int[4, 2];
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 2; y++)
                layer2[x, y] = 1;

        // Layer 3 - 2x1 peak
        int[,] layer3 = new int[2, 1];
        layer3[0, 0] = 1;
        layer3[1, 0] = 1;

        double baseX = 60;
        double baseY = 200;

        // Create tiles for each layer
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                if (layer0[x, y] == 1)
                {
                    _tiles.Add(new Tile
                    {
                        GridX = x,
                        GridY = y,
                        Layer = 0,
                        X = baseX + x * TileWidth,
                        Y = baseY + y * TileHeight
                    });
                }
            }
        }

        double layer1BaseX = baseX + TileWidth;
        double layer1BaseY = baseY + TileHeight;
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                if (layer1[x, y] == 1)
                {
                    _tiles.Add(new Tile
                    {
                        GridX = x,
                        GridY = y,
                        Layer = 1,
                        X = layer1BaseX + x * TileWidth - LayerOffsetX,
                        Y = layer1BaseY + y * TileHeight - LayerOffsetY
                    });
                }
            }
        }

        double layer2BaseX = baseX + TileWidth * 2;
        double layer2BaseY = baseY + TileHeight * 2;
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (layer2[x, y] == 1)
                {
                    _tiles.Add(new Tile
                    {
                        GridX = x,
                        GridY = y,
                        Layer = 2,
                        X = layer2BaseX + x * TileWidth - LayerOffsetX * 2,
                        Y = layer2BaseY + y * TileHeight - LayerOffsetY * 2
                    });
                }
            }
        }

        double layer3BaseX = baseX + TileWidth * 3;
        double layer3BaseY = baseY + TileHeight * 2.5;
        for (int x = 0; x < 2; x++)
        {
            _tiles.Add(new Tile
            {
                GridX = x,
                GridY = 0,
                Layer = 3,
                X = layer3BaseX + x * TileWidth - LayerOffsetX * 3,
                Y = layer3BaseY - LayerOffsetY * 3
            });
        }

        _tilesRemaining = _tiles.Count;
    }

    private void ShuffleTiles()
    {
        var rng = new Random();

        // Create pairs of tile types (each symbol appears 4 times, or in pairs for flowers/seasons)
        var tileTypes = new List<int>();

        // Regular tiles (4 of each) - use first 34 symbols
        for (int i = 0; i < 34 && tileTypes.Count < _tiles.Count; i++)
        {
            for (int j = 0; j < 4 && tileTypes.Count < _tiles.Count; j++)
            {
                tileTypes.Add(i);
            }
        }

        // Shuffle tile types
        for (int i = tileTypes.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (tileTypes[i], tileTypes[j]) = (tileTypes[j], tileTypes[i]);
        }

        // Assign tile types to positions
        for (int i = 0; i < _tiles.Count; i++)
        {
            _tiles[i].TypeIndex = tileTypes[i];
            _tiles[i].Symbol = TileSymbols[tileTypes[i] % TileSymbols.Length];
            _tiles[i].IsRemoved = false;
        }
    }

    private void DrawGame()
    {
        GameCanvas.Children.Clear();

        // Sort tiles by layer (draw lower layers first)
        var sortedTiles = _tiles.Where(t => !t.IsRemoved)
            .OrderBy(t => t.Layer)
            .ThenBy(t => t.GridY)
            .ThenBy(t => t.GridX)
            .ToList();

        foreach (var tile in sortedTiles)
        {
            var tileElement = CreateTileElement(tile);
            Canvas.SetLeft(tileElement, tile.X);
            Canvas.SetTop(tileElement, tile.Y);
            GameCanvas.Children.Add(tileElement);
        }

        _tilesRemaining = _tiles.Count(t => !t.IsRemoved);
        TilesText.Text = _tilesRemaining.ToString();
    }

    private Border CreateTileElement(Tile tile)
    {
        bool isSelectable = IsTileSelectable(tile);
        bool isSelected = tile == _selectedTile;

        var shadowColor = isSelected
            ? Color.FromRgb(234, 179, 8)
            : Color.FromRgb(146, 64, 14);

        var faceColor = isSelected
            ? Color.FromRgb(253, 224, 71)
            : Color.FromRgb(254, 243, 199);

        // Shadow/side effect
        var container = new Border
        {
            Width = TileWidth + 4,
            Height = TileHeight + 4,
            Background = new SolidColorBrush(shadowColor),
            CornerRadius = new CornerRadius(4),
            Cursor = isSelectable ? Cursors.Hand : Cursors.Arrow,
            Opacity = isSelectable ? 1.0 : 0.7
        };

        // Tile face
        var face = new Border
        {
            Width = TileWidth,
            Height = TileHeight,
            Background = new SolidColorBrush(faceColor),
            CornerRadius = new CornerRadius(4),
            BorderBrush = new SolidColorBrush(Color.FromRgb(180, 83, 9)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 4, 4)
        };

        // Tile symbol
        var symbol = new TextBlock
        {
            Text = tile.Symbol,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        face.Child = symbol;
        container.Child = face;

        container.Tag = tile;
        container.MouseLeftButtonDown += Tile_Click;

        return container;
    }

    private bool IsTileSelectable(Tile tile)
    {
        if (tile.IsRemoved) return false;

        // Check if tile is blocked by tiles above it
        foreach (var other in _tiles.Where(t => !t.IsRemoved && t.Layer > tile.Layer))
        {
            // Check if the other tile overlaps this one (on a higher layer)
            if (TilesOverlap(tile, other))
                return false;
        }

        // Check if tile is blocked on both left and right sides
        bool blockedLeft = false;
        bool blockedRight = false;

        foreach (var other in _tiles.Where(t => !t.IsRemoved && t.Layer == tile.Layer && t != tile))
        {
            // Check left blocking (tile to the left that overlaps vertically)
            if (other.GridX == tile.GridX - 1 && Math.Abs(other.GridY - tile.GridY) < 1)
                blockedLeft = true;

            // Check right blocking
            if (other.GridX == tile.GridX + 1 && Math.Abs(other.GridY - tile.GridY) < 1)
                blockedRight = true;
        }

        // Tile is selectable if at least one side is free
        return !blockedLeft || !blockedRight;
    }

    private bool TilesOverlap(Tile lower, Tile upper)
    {
        // Simple overlap check based on grid positions
        double dx = Math.Abs(lower.X - upper.X);
        double dy = Math.Abs(lower.Y - upper.Y);
        return dx < TileWidth * 0.8 && dy < TileHeight * 0.8;
    }

    private void Tile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border element || element.Tag is not Tile tile)
            return;

        if (!IsTileSelectable(tile))
            return;

        if (_selectedTile == null)
        {
            _selectedTile = tile;
            DrawGame();
        }
        else if (_selectedTile == tile)
        {
            _selectedTile = null;
            DrawGame();
        }
        else
        {
            // Try to match
            if (_selectedTile.TypeIndex == tile.TypeIndex)
            {
                // Match! Remove both tiles
                _selectedTile.IsRemoved = true;
                tile.IsRemoved = true;
                _selectedTile = null;

                DrawGame();
                CheckWinOrNoMoves();
            }
            else
            {
                // No match, select new tile
                _selectedTile = tile;
                DrawGame();
            }
        }
    }

    private void CheckWinOrNoMoves()
    {
        var remaining = _tiles.Where(t => !t.IsRemoved).ToList();

        if (remaining.Count == 0)
        {
            _timer.Stop();
            var elapsed = DateTime.Now - _startTime;
            WinTimeText.Text = $"Time: {elapsed.Minutes}:{elapsed.Seconds:D2}";
            WinOverlay.Visibility = Visibility.Visible;
            return;
        }

        // Check if any moves are available
        if (!HasAvailableMoves())
        {
            NoMovesOverlay.Visibility = Visibility.Visible;
        }
    }

    private bool HasAvailableMoves()
    {
        var selectableTiles = _tiles.Where(t => !t.IsRemoved && IsTileSelectable(t)).ToList();

        // Check if any two selectable tiles match
        for (int i = 0; i < selectableTiles.Count; i++)
        {
            for (int j = i + 1; j < selectableTiles.Count; j++)
            {
                if (selectableTiles[i].TypeIndex == selectableTiles[j].TypeIndex)
                    return true;
            }
        }

        return false;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        // Timer display could be added if needed
    }

    private void Hint_Click(object sender, RoutedEventArgs e)
    {
        var selectableTiles = _tiles.Where(t => !t.IsRemoved && IsTileSelectable(t)).ToList();

        // Find a matching pair
        for (int i = 0; i < selectableTiles.Count; i++)
        {
            for (int j = i + 1; j < selectableTiles.Count; j++)
            {
                if (selectableTiles[i].TypeIndex == selectableTiles[j].TypeIndex)
                {
                    // Highlight the pair briefly
                    _selectedTile = selectableTiles[i];
                    DrawGame();

                    MessageBox.Show($"Hint: Look for matching tiles!", "Hint",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    _selectedTile = null;
                    DrawGame();
                    return;
                }
            }
        }

        MessageBox.Show("No matching pairs available. Try shuffling!", "No Matches",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Shuffle_Click(object sender, RoutedEventArgs e)
    {
        ShuffleRemainingTiles();
    }

    private void ShuffleFromOverlay_Click(object sender, RoutedEventArgs e)
    {
        NoMovesOverlay.Visibility = Visibility.Collapsed;
        ShuffleRemainingTiles();
    }

    private void ShuffleRemainingTiles()
    {
        var remaining = _tiles.Where(t => !t.IsRemoved).ToList();
        var types = remaining.Select(t => t.TypeIndex).ToList();

        var rng = new Random();
        for (int i = types.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (types[i], types[j]) = (types[j], types[i]);
        }

        for (int i = 0; i < remaining.Count; i++)
        {
            remaining[i].TypeIndex = types[i];
            remaining[i].Symbol = TileSymbols[types[i] % TileSymbols.Length];
        }

        _selectedTile = null;
        DrawGame();

        if (!HasAvailableMoves())
        {
            NoMovesOverlay.Visibility = Visibility.Visible;
        }
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        NewGame();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            NewGame();
            e.Handled = true;
        }
        else if (e.Key == Key.H)
        {
            Hint_Click(sender, e);
            e.Handled = true;
        }
    }
}

public class Tile
{
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int Layer { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int TypeIndex { get; set; }
    public string Symbol { get; set; } = "";
    public bool IsRemoved { get; set; }
}
