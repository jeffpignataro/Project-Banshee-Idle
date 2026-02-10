# Project Banshee-Idle

A deep, polished incremental RPG built with C# and Godot (CLI-version for now). Inspired by *Melvor Idle* and *RuneScape*.

## Features (Current)
- **Woodcutting:** Chop trees from normal to Magic.
- **Mining:** Mine ores from Copper to Runite.
- **Exponential Leveling:** Proper RuneScape-style XP curves.
- **Inventory/Bank:** Manage your loot and stash items.
- **Save/Load:** JSON-based persistence.
- **Console Interface:** Functional game loop for testing logic.

## Prerequisites
- **.NET 8.0 SDK** or later.

## How to Build & Run

### 1. Build the project
Open your terminal in the `BansheeIdle` directory and run:
```bash
dotnet build
```

### 2. Run the game
To start the console-based game loop:
```bash
dotnet run
```

## Game Controls (CLI)
- Use numbers **1-8** to navigate menus.
- When performing a skill action (gathering), press **Enter** at any time to stop and return to the menu.
- Your progress is saved to `savegame.json` when you select the save or quit options.

## Project Structure
- `Core/`: The engine guts (Skills, Inventory, Data Structures).
- `Data/`: JSON database of items and actions.
- `GUI/`: Blueprints for the upcoming Godot interface.
- `Program.cs`: The console-based game loop for testing.

## Next Steps
- **Phase 3:** Implementing the Godot GUI.
- **Phase 4:** Combat systems and equipment.
