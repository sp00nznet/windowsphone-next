# Mahjong

Tile matching game with pyramid layout.

## Features

- **Classic Mahjong** - Match pairs of tiles
- **Pyramid Layout** - Multi-layer tile arrangement
- **Timer** - Track your completion time
- **Tile Count** - Shows remaining tiles
- **Hint System** - Coming soon
- **Shuffle** - Coming soon
- **Win/Stuck Detection** - Automatic game state

## Game Rules

### Objective
Remove all tiles by matching pairs of identical tiles.

### Matching Rules
- Only "free" tiles can be matched
- A tile is free when:
  - No tile is on top of it
  - Left OR right side is clear
- Match two identical tiles to remove both

### Tile Types
| Category | Tiles |
|----------|-------|
| **Dots** | 1-9 circles |
| **Bamboo** | 1-9 bamboo |
| **Characters** | 1-9 万 |
| **Winds** | East, South, West, North |
| **Dragons** | Red, Green, White |
| **Flowers** | Spring, Summer, Autumn, Winter |
| **Seasons** | Plum, Orchid, Bamboo, Chrysanthemum |

### Layout
- 4 layers in pyramid shape
- Bottom: 8x6 tiles
- Second: 6x4 tiles
- Third: 4x2 tiles
- Top: 2x1 tiles

## Usage

### Selecting Tiles
1. Tap a free tile (highlighted)
2. Tap a matching free tile
3. Both tiles disappear

### Game Status
- **Tiles Remaining** - Shown in header
- **Timer** - Running time displayed
- **Win** - All tiles removed
- **No Moves** - Overlay when stuck

### Starting Over
- Tap **New Game** to restart
- Deck is reshuffled

## Winning

Remove all tiles from the board. Fewer moves = better!

## Getting Stuck

If no valid matches remain, the game detects this and shows an overlay. Start a new game to try again.

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Exit game |

## Building

```powershell
cd Apps/Mahjong
dotnet build
```

## Project Structure

```
Mahjong/
├── MainWindow.xaml        # Game board UI
├── MainWindow.xaml.cs     # Tile matching logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── MahjongTheme.xaml
└── WindowsPhoneMahjong.csproj
```
