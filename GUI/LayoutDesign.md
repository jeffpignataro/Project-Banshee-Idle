# GUI Architecture Design: Project Banshee-Idle

## Layout Overview (16:9 Desktop)
1. **Sidebar (Left, 20% Width)**
   - Player Profile (Gold, Level).
   - Navigation List:
     - Bank
     - Woodcutting
     - Mining
     - Settings
2. **Header (Top, 10% Height)**
   - Breadcrumbs (e.g., "Skills > Woodcutting").
   - Active Action Progress Bar (Universal).
3. **Main Content (Center/Right)**
   - Dynamic view based on Sidebar selection.
   - Grid layout for Bank.
   - Action buttons for Skills.
4. **Footer (Bottom, 5% Height)**
   - Recent Action Log (e.g., "You received 1x Logs").

## Godot Scene Tree Structure
- Main (CanvasLayer)
    - HBoxContainer
        - Sidebar (Panel)
        - VBoxContainer (MainArea)
            - Header (Panel)
            - ViewContainer (ScrollContainer)
            - Footer (Panel)
