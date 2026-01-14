using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WindowsPhoneNext.Solitaire;

public partial class MainWindow : Window
{
    private readonly List<Card> _deck = new();
    private readonly List<Card>[] _tableau = new List<Card>[7];
    private readonly List<Card>[] _foundations = new List<Card>[4];
    private readonly List<Card> _stock = new();
    private readonly List<Card> _waste = new();
    private readonly Stack<GameMove> _undoStack = new();

    private Card? _selectedCard;
    private int _selectedPileIndex = -1;
    private string _selectedPileType = "";
    private int _moves;

    private const int CardWidth = 85;
    private const int CardHeight = 120;
    private const int CardOverlap = 25;
    private const int FaceDownOverlap = 15;

    public MainWindow()
    {
        InitializeComponent();

        for (int i = 0; i < 7; i++)
            _tableau[i] = new List<Card>();
        for (int i = 0; i < 4; i++)
            _foundations[i] = new List<Card>();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        NewGame();
    }

    private void NewGame()
    {
        _moves = 0;
        MovesText.Text = "0";
        _undoStack.Clear();
        _selectedCard = null;
        WinOverlay.Visibility = Visibility.Collapsed;

        // Clear all piles
        _stock.Clear();
        _waste.Clear();
        for (int i = 0; i < 7; i++)
            _tableau[i].Clear();
        for (int i = 0; i < 4; i++)
            _foundations[i].Clear();

        // Create and shuffle deck
        CreateDeck();
        ShuffleDeck();

        // Deal to tableau
        int cardIndex = 0;
        for (int col = 0; col < 7; col++)
        {
            for (int row = 0; row <= col; row++)
            {
                var card = _deck[cardIndex++];
                card.IsFaceUp = row == col;
                _tableau[col].Add(card);
            }
        }

        // Remaining cards go to stock
        for (int i = cardIndex; i < 52; i++)
        {
            _deck[i].IsFaceUp = false;
            _stock.Add(_deck[i]);
        }

        DrawGame();
    }

    private void CreateDeck()
    {
        _deck.Clear();
        string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
        string[] suitSymbols = { "\u2665", "\u2666", "\u2663", "\u2660" };

        for (int s = 0; s < 4; s++)
        {
            for (int v = 1; v <= 13; v++)
            {
                _deck.Add(new Card
                {
                    Suit = suits[s],
                    SuitSymbol = suitSymbols[s],
                    Value = v,
                    IsRed = s < 2
                });
            }
        }
    }

    private void ShuffleDeck()
    {
        var rng = new Random();
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    private void DrawGame()
    {
        GameCanvas.Children.Clear();

        double startX = 20;
        double startY = 20;

        // Draw stock pile
        DrawPile(_stock, startX, startY, "stock", 0, false);

        // Draw waste pile
        DrawPile(_waste, startX + CardWidth + 15, startY, "waste", 0, true, 3);

        // Draw foundation piles
        for (int i = 0; i < 4; i++)
        {
            double x = startX + (CardWidth + 15) * (i + 3);
            DrawFoundation(i, x, startY);
        }

        // Draw tableau
        double tableauY = startY + CardHeight + 30;
        for (int i = 0; i < 7; i++)
        {
            double x = startX + (CardWidth + 10) * i;
            DrawTableauColumn(i, x, tableauY);
        }
    }

    private void DrawPile(List<Card> pile, double x, double y, string pileType, int pileIndex, bool showTop, int showCount = 1)
    {
        if (pile.Count == 0)
        {
            var empty = CreateEmptyPile(pileType == "stock");
            Canvas.SetLeft(empty, x);
            Canvas.SetTop(empty, y);
            empty.Tag = new PileInfo { Type = pileType, Index = pileIndex };
            empty.MouseLeftButtonDown += Pile_Click;
            GameCanvas.Children.Add(empty);
        }
        else if (showTop)
        {
            int start = Math.Max(0, pile.Count - showCount);
            for (int i = start; i < pile.Count; i++)
            {
                var card = pile[i];
                var cardElement = CreateCardElement(card);
                double offsetX = (i - start) * 20;
                Canvas.SetLeft(cardElement, x + offsetX);
                Canvas.SetTop(cardElement, y);
                cardElement.Tag = new PileInfo { Type = pileType, Index = pileIndex, Card = card };
                cardElement.MouseLeftButtonDown += Card_Click;
                GameCanvas.Children.Add(cardElement);
            }
        }
        else
        {
            var cardBack = CreateCardBack();
            Canvas.SetLeft(cardBack, x);
            Canvas.SetTop(cardBack, y);
            cardBack.Tag = new PileInfo { Type = pileType, Index = pileIndex };
            cardBack.MouseLeftButtonDown += Pile_Click;
            GameCanvas.Children.Add(cardBack);
        }
    }

    private void DrawFoundation(int index, double x, double y)
    {
        var pile = _foundations[index];
        string[] suits = { "\u2665", "\u2666", "\u2663", "\u2660" };

        if (pile.Count == 0)
        {
            var empty = CreateEmptyPile(false, suits[index]);
            Canvas.SetLeft(empty, x);
            Canvas.SetTop(empty, y);
            empty.Tag = new PileInfo { Type = "foundation", Index = index };
            empty.MouseLeftButtonDown += Pile_Click;
            GameCanvas.Children.Add(empty);
        }
        else
        {
            var card = pile[^1];
            var cardElement = CreateCardElement(card);
            Canvas.SetLeft(cardElement, x);
            Canvas.SetTop(cardElement, y);
            cardElement.Tag = new PileInfo { Type = "foundation", Index = index, Card = card };
            cardElement.MouseLeftButtonDown += Card_Click;
            GameCanvas.Children.Add(cardElement);
        }
    }

    private void DrawTableauColumn(int colIndex, double x, double y)
    {
        var pile = _tableau[colIndex];

        if (pile.Count == 0)
        {
            var empty = CreateEmptyPile(false);
            Canvas.SetLeft(empty, x);
            Canvas.SetTop(empty, y);
            empty.Tag = new PileInfo { Type = "tableau", Index = colIndex };
            empty.MouseLeftButtonDown += Pile_Click;
            GameCanvas.Children.Add(empty);
        }
        else
        {
            double currentY = y;
            for (int i = 0; i < pile.Count; i++)
            {
                var card = pile[i];
                FrameworkElement cardElement;

                if (card.IsFaceUp)
                {
                    cardElement = CreateCardElement(card);
                    bool isSelected = card == _selectedCard;
                    if (isSelected)
                    {
                        ((Border)cardElement).BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                        ((Border)cardElement).BorderThickness = new Thickness(3);
                    }
                }
                else
                {
                    cardElement = CreateCardBack();
                }

                Canvas.SetLeft(cardElement, x);
                Canvas.SetTop(cardElement, currentY);
                cardElement.Tag = new PileInfo { Type = "tableau", Index = colIndex, Card = card, CardIndex = i };
                cardElement.MouseLeftButtonDown += Card_Click;
                GameCanvas.Children.Add(cardElement);

                currentY += card.IsFaceUp ? CardOverlap : FaceDownOverlap;
            }
        }
    }

    private Border CreateCardElement(Card card)
    {
        var border = new Border
        {
            Width = CardWidth,
            Height = CardHeight,
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };

        var grid = new Grid();

        var brush = card.IsRed
            ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
            : new SolidColorBrush(Color.FromRgb(31, 41, 55));

        // Top left value
        var topLeft = new StackPanel { Margin = new Thickness(4, 2, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        topLeft.Children.Add(new TextBlock { Text = card.DisplayValue, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = brush });
        topLeft.Children.Add(new TextBlock { Text = card.SuitSymbol, FontSize = 14, Foreground = brush, Margin = new Thickness(2, -4, 0, 0) });
        grid.Children.Add(topLeft);

        // Center suit
        var center = new TextBlock
        {
            Text = card.SuitSymbol,
            FontSize = 36,
            Foreground = brush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(center);

        // Bottom right value (rotated)
        var bottomRight = new StackPanel
        {
            Margin = new Thickness(0, 0, 4, 2),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(180)
        };
        bottomRight.Children.Add(new TextBlock { Text = card.DisplayValue, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = brush });
        bottomRight.Children.Add(new TextBlock { Text = card.SuitSymbol, FontSize = 14, Foreground = brush, Margin = new Thickness(2, -4, 0, 0) });
        grid.Children.Add(bottomRight);

        border.Child = grid;
        return border;
    }

    private Border CreateCardBack()
    {
        var border = new Border
        {
            Width = CardWidth,
            Height = CardHeight,
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };

        var grid = new Grid();
        var pattern = new Border
        {
            Margin = new Thickness(6),
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromRgb(59, 130, 246))
        };
        grid.Children.Add(pattern);
        border.Child = grid;

        return border;
    }

    private Border CreateEmptyPile(bool isStock, string? symbol = null)
    {
        var border = new Border
        {
            Width = CardWidth,
            Height = CardHeight,
            CornerRadius = new CornerRadius(6),
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
            BorderThickness = new Thickness(2),
            Cursor = Cursors.Hand
        };

        if (isStock)
        {
            border.Child = new TextBlock
            {
                Text = "\u21BB",
                FontSize = 32,
                Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        else if (symbol != null)
        {
            border.Child = new TextBlock
            {
                Text = symbol,
                FontSize = 32,
                Foreground = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        return border;
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not PileInfo info)
            return;

        if (info.Type == "stock")
        {
            DrawFromStock();
            return;
        }

        if (info.Card == null) return;

        // If card is face down in tableau, flip it
        if (info.Type == "tableau" && !info.Card.IsFaceUp)
        {
            return;
        }

        if (_selectedCard == null)
        {
            // Select this card
            if (info.Type == "waste" || info.Type == "tableau" || info.Type == "foundation")
            {
                _selectedCard = info.Card;
                _selectedPileType = info.Type;
                _selectedPileIndex = info.Index;
                DrawGame();
            }
        }
        else
        {
            // Try to move selected card to this pile
            TryMove(info.Type, info.Index);
        }
    }

    private void Pile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not PileInfo info)
            return;

        if (info.Type == "stock")
        {
            DrawFromStock();
            return;
        }

        if (_selectedCard != null)
        {
            TryMove(info.Type, info.Index);
        }
    }

    private void DrawFromStock()
    {
        if (_stock.Count == 0)
        {
            // Reset stock from waste
            if (_waste.Count > 0)
            {
                _undoStack.Push(new GameMove { Type = "reset_stock", WasteCount = _waste.Count });
                while (_waste.Count > 0)
                {
                    var card = _waste[^1];
                    card.IsFaceUp = false;
                    _waste.RemoveAt(_waste.Count - 1);
                    _stock.Add(card);
                }
                _moves++;
                MovesText.Text = _moves.ToString();
            }
        }
        else
        {
            // Draw card from stock
            var card = _stock[^1];
            _stock.RemoveAt(_stock.Count - 1);
            card.IsFaceUp = true;
            _waste.Add(card);
            _undoStack.Push(new GameMove { Type = "draw" });
            _moves++;
            MovesText.Text = _moves.ToString();
        }

        _selectedCard = null;
        DrawGame();
    }

    private void TryMove(string targetType, int targetIndex)
    {
        if (_selectedCard == null) return;

        bool moved = false;
        var move = new GameMove
        {
            Type = "move",
            FromType = _selectedPileType,
            FromIndex = _selectedPileIndex,
            ToType = targetType,
            ToIndex = targetIndex
        };

        if (targetType == "foundation")
        {
            moved = TryMoveToFoundation(targetIndex, move);
        }
        else if (targetType == "tableau")
        {
            moved = TryMoveToTableau(targetIndex, move);
        }

        if (moved)
        {
            _undoStack.Push(move);
            _moves++;
            MovesText.Text = _moves.ToString();
            CheckWin();
        }

        _selectedCard = null;
        DrawGame();
    }

    private bool TryMoveToFoundation(int foundationIndex, GameMove move)
    {
        if (_selectedCard == null) return false;

        var foundation = _foundations[foundationIndex];
        string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };

        // Check if card matches foundation suit
        if (_selectedCard.Suit != suits[foundationIndex])
            return false;

        // Check if valid move (Ace on empty, or next value)
        if (foundation.Count == 0 && _selectedCard.Value != 1)
            return false;
        if (foundation.Count > 0 && _selectedCard.Value != foundation[^1].Value + 1)
            return false;

        // Only allow single card moves to foundation
        if (_selectedPileType == "tableau")
        {
            var pile = _tableau[_selectedPileIndex];
            if (pile[^1] != _selectedCard)
                return false;

            pile.Remove(_selectedCard);
            move.FlippedCard = FlipTopCard(_selectedPileIndex);
        }
        else if (_selectedPileType == "waste")
        {
            _waste.Remove(_selectedCard);
        }
        else if (_selectedPileType == "foundation")
        {
            _foundations[_selectedPileIndex].Remove(_selectedCard);
        }

        foundation.Add(_selectedCard);
        move.CardsMoved = 1;
        return true;
    }

    private bool TryMoveToTableau(int tableauIndex, GameMove move)
    {
        if (_selectedCard == null) return false;

        var targetPile = _tableau[tableauIndex];

        // Check valid move
        if (targetPile.Count == 0)
        {
            if (_selectedCard.Value != 13) return false; // Only kings on empty
        }
        else
        {
            var topCard = targetPile[^1];
            if (!topCard.IsFaceUp) return false;
            if (_selectedCard.Value != topCard.Value - 1) return false;
            if (_selectedCard.IsRed == topCard.IsRed) return false; // Must alternate colors
        }

        // Move card(s)
        if (_selectedPileType == "tableau")
        {
            var sourcePile = _tableau[_selectedPileIndex];
            int cardIndex = sourcePile.IndexOf(_selectedCard);
            var cardsToMove = sourcePile.Skip(cardIndex).ToList();

            foreach (var card in cardsToMove)
                sourcePile.Remove(card);
            targetPile.AddRange(cardsToMove);

            move.CardsMoved = cardsToMove.Count;
            move.FlippedCard = FlipTopCard(_selectedPileIndex);
        }
        else if (_selectedPileType == "waste")
        {
            _waste.Remove(_selectedCard);
            targetPile.Add(_selectedCard);
            move.CardsMoved = 1;
        }
        else if (_selectedPileType == "foundation")
        {
            _foundations[_selectedPileIndex].Remove(_selectedCard);
            targetPile.Add(_selectedCard);
            move.CardsMoved = 1;
        }

        return true;
    }

    private bool FlipTopCard(int tableauIndex)
    {
        var pile = _tableau[tableauIndex];
        if (pile.Count > 0 && !pile[^1].IsFaceUp)
        {
            pile[^1].IsFaceUp = true;
            return true;
        }
        return false;
    }

    private void CheckWin()
    {
        int total = _foundations.Sum(f => f.Count);
        if (total == 52)
        {
            WinMovesText.Text = $"Completed in {_moves} moves";
            WinOverlay.Visibility = Visibility.Visible;
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_undoStack.Count == 0) return;

        var move = _undoStack.Pop();

        if (move.Type == "draw")
        {
            var card = _waste[^1];
            _waste.RemoveAt(_waste.Count - 1);
            card.IsFaceUp = false;
            _stock.Add(card);
        }
        else if (move.Type == "reset_stock")
        {
            for (int i = 0; i < move.WasteCount; i++)
            {
                var card = _stock[^1];
                _stock.RemoveAt(_stock.Count - 1);
                card.IsFaceUp = true;
                _waste.Add(card);
            }
        }
        else if (move.Type == "move")
        {
            // Unflip card if needed
            if (move.FlippedCard && move.FromType == "tableau")
            {
                var pile = _tableau[move.FromIndex];
                if (pile.Count > 0)
                    pile[^1].IsFaceUp = false;
            }

            // Get cards to move back
            List<Card> cardsToMove = new();
            if (move.ToType == "foundation")
            {
                for (int i = 0; i < move.CardsMoved; i++)
                {
                    cardsToMove.Insert(0, _foundations[move.ToIndex][^1]);
                    _foundations[move.ToIndex].RemoveAt(_foundations[move.ToIndex].Count - 1);
                }
            }
            else if (move.ToType == "tableau")
            {
                var pile = _tableau[move.ToIndex];
                for (int i = 0; i < move.CardsMoved; i++)
                {
                    cardsToMove.Insert(0, pile[^1]);
                    pile.RemoveAt(pile.Count - 1);
                }
            }

            // Move back to source
            if (move.FromType == "waste")
            {
                _waste.AddRange(cardsToMove);
            }
            else if (move.FromType == "tableau")
            {
                _tableau[move.FromIndex].AddRange(cardsToMove);
            }
            else if (move.FromType == "foundation")
            {
                _foundations[move.FromIndex].AddRange(cardsToMove);
            }
        }

        _moves = Math.Max(0, _moves - 1);
        MovesText.Text = _moves.ToString();
        _selectedCard = null;
        DrawGame();
    }

    private void AutoComplete_Click(object sender, RoutedEventArgs e)
    {
        // Auto-complete if all cards are face up
        bool allFaceUp = _stock.Count == 0 && _waste.Count == 0;
        foreach (var pile in _tableau)
        {
            if (pile.Any(c => !c.IsFaceUp))
            {
                allFaceUp = false;
                break;
            }
        }

        if (!allFaceUp)
        {
            MessageBox.Show("Can only auto-complete when all cards are face up and stock is empty.",
                "Auto Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Auto move cards to foundation
        bool moved;
        do
        {
            moved = false;
            // Check waste
            if (_waste.Count > 0)
            {
                var card = _waste[^1];
                int foundationIndex = GetFoundationIndex(card.Suit);
                if (CanAutoMoveToFoundation(card, foundationIndex))
                {
                    _waste.RemoveAt(_waste.Count - 1);
                    _foundations[foundationIndex].Add(card);
                    _moves++;
                    moved = true;
                }
            }

            // Check tableau
            foreach (var pile in _tableau)
            {
                if (pile.Count > 0)
                {
                    var card = pile[^1];
                    int foundationIndex = GetFoundationIndex(card.Suit);
                    if (CanAutoMoveToFoundation(card, foundationIndex))
                    {
                        pile.RemoveAt(pile.Count - 1);
                        _foundations[foundationIndex].Add(card);
                        _moves++;
                        moved = true;
                    }
                }
            }

            DrawGame();
            System.Threading.Thread.Sleep(50);
        } while (moved);

        MovesText.Text = _moves.ToString();
        CheckWin();
    }

    private int GetFoundationIndex(string suit) => suit switch
    {
        "Hearts" => 0,
        "Diamonds" => 1,
        "Clubs" => 2,
        "Spades" => 3,
        _ => -1
    };

    private bool CanAutoMoveToFoundation(Card card, int foundationIndex)
    {
        if (foundationIndex < 0) return false;
        var foundation = _foundations[foundationIndex];
        if (foundation.Count == 0) return card.Value == 1;
        return card.Value == foundation[^1].Value + 1;
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
        else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Undo_Click(sender, e);
            e.Handled = true;
        }
    }
}

public class Card
{
    public string Suit { get; set; } = "";
    public string SuitSymbol { get; set; } = "";
    public int Value { get; set; }
    public bool IsRed { get; set; }
    public bool IsFaceUp { get; set; }

    public string DisplayValue => Value switch
    {
        1 => "A",
        11 => "J",
        12 => "Q",
        13 => "K",
        _ => Value.ToString()
    };
}

public class PileInfo
{
    public string Type { get; set; } = "";
    public int Index { get; set; }
    public Card? Card { get; set; }
    public int CardIndex { get; set; }
}

public class GameMove
{
    public string Type { get; set; } = "";
    public string FromType { get; set; } = "";
    public int FromIndex { get; set; }
    public string ToType { get; set; } = "";
    public int ToIndex { get; set; }
    public int CardsMoved { get; set; }
    public bool FlippedCard { get; set; }
    public int WasteCount { get; set; }
}
