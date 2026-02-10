# Project Banshee-Idle

A deep, polished incremental RPG built with C# and Godot 4.3. Inspired by *Melvor Idle* and *RuneScape*.

## Features (Current)
- **Godot 4 GUI:** Modern sidebar navigation and real-time progress bars.
- **Woodcutting:** Chop trees from normal to Magic.
- **Mining:** Mine ores from Copper to Runite.
- **Combat System:** Fight monsters (Goblins, Skeletons, Dragons) with proper HP and loot tables.
- **Crafting & Equipment:** Smithing, Fletching, and 9 equipment slots with stat bonuses.
- **Exponential Leveling:** Proper RuneScape-style XP curves.
- **Inventory/Bank:** Manage your loot and stash items.
- **Save/Load:** JSON-based persistence (saved to `user://savegame.json`).

## Prerequisites
- **Godot 4.3 (Mono/C# version)** installed.
- **.NET 8.0 SDK** or later.

## How to Build & Run

### 1. Build the solutions
Open your terminal in the `BansheeIdle` directory and run:
```bash
/snap/bin/godot-4 --headless --path . --build-solutions --quit
```
*(Or use your local Godot binary path)*

### 2. Run the game
Open the project in Godot 4.3 and press **F5**, or run:
```bash
/snap/bin/godot-4 --path .
```

## Game Controls
- Use the **Sidebar** to navigate between Skills, Combat, Equipment, Inventory, and the Bank.
- Click on an action (e.g., "Normal Tree") to start gathering.
- The **Combat** view allows you to fight monsters for XP and loot.
- **Equipment** lets you equip weapons and armor from your inventory.
- Your progress is saved automatically when you click the **Save** button.

## Project Structure
- `Core/`: The engine guts (Combat, Crafting, Skills, Inventory, Data Structures).
- `Data/`: JSON database of items, monsters, and actions.
- `scenes/`: Godot UI scenes (Main interface).
- `scripts/`: C# controllers and bridge scripts for Godot.
- `Program.cs`: Legacy console game loop (deprecated).

## Status
- **Phase 1-4:** Complete. The core game loop, GUI, Combat, and Crafting systems are fully implemented.
