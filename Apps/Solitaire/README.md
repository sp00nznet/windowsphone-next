# Solitaire

Classic Klondike solitaire card game.

## Features

- **Classic Klondike** - Standard solitaire rules
- **Drag & Drop** - Move cards between piles
- **Undo** - Reverse your last move
- **Move Counter** - Tracks game progress
- **Auto-Complete** - Coming soon
- **Win Detection** - Celebrates your victory

## Game Rules

### Objective
Move all 52 cards to the four foundation piles, sorted by suit from Ace to King.

### Setup
- 7 tableau columns with increasing cards (1-7)
- Top card of each column is face up
- Remaining 24 cards in the stock pile

### Valid Moves
- **Tableau to Tableau**: Place card on another of opposite color, one rank higher
- **Tableau to Foundation**: Place card on matching suit, one rank higher (start with Ace)
- **Stock to Waste**: Draw cards from stock
- **Waste to Tableau/Foundation**: Move top waste card

### Cards
- **Red Suits**: Hearts (♥), Diamonds (♦)
- **Black Suits**: Clubs (♣), Spades (♠)
- **Ranks**: A, 2, 3, 4, 5, 6, 7, 8, 9, 10, J, Q, K

## Usage

### Moving Cards
1. Tap/click a card to select it
2. Tap/click the destination
3. Card moves if valid

### Drawing from Stock
- Tap the stock pile (face-down cards)
- Card moves to waste pile

### Undoing Moves
- Tap the **Undo** button
- Reverses last move

### New Game
- Tap **New Game**
- Deck is reshuffled

## Winning

When all 52 cards are on the foundation piles (13 cards each, A through K), you win!

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Exit game |

## Building

```powershell
cd Apps/Solitaire
dotnet build
```

## Project Structure

```
Solitaire/
├── MainWindow.xaml        # Game board UI
├── MainWindow.xaml.cs     # Card game logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── SolitaireTheme.xaml
└── WindowsPhoneSolitaire.csproj
```
