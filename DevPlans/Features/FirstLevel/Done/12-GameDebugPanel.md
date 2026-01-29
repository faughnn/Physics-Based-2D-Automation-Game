# Game Debug Panel

## Overview
Add a debug overlay to the Game scene similar to the existing Sandbox debug overlay. Shows FPS, simulation metrics, and game-specific debug info.

## Requirements
- Toggle with F3 (same as Sandbox)
- Show FPS with color-coded performance (green/yellow/red)
- Show simulation time per frame
- Reuse existing `DebugOverlay` and `DebugSectionBase` infrastructure

## Implementation

### Step 1: Add DebugOverlay to GameController
In `GameController.Start()`, after creating the ProgressionUI:

```csharp
// Add using statement at top
using FallingSand.Debugging;

// In Start(), add:
// 14. Create debug overlay
CreateDebugOverlay();
```

### Step 2: Create the Debug Overlay Setup Method
Add to `GameController.cs`:

```csharp
private void CreateDebugOverlay()
{
    if (DebugOverlay.Instance != null) return;

    GameObject debugObj = new GameObject("DebugOverlay");
    var overlay = debugObj.AddComponent<DebugOverlay>();

    // Register simulation section (FPS, sim time, worker threads)
    overlay.RegisterSection(new SimulationDebugSection(simulation));
}
```

### Step 3: Add Game-Specific Debug Section
Create `Assets/Scripts/Debug/Sections/GameDebugSection.cs` showing:
- Player position (cell coordinates)
- Current objective progress
- Equipped item
- Unlocked abilities

Register it in `CreateDebugOverlay()`:
```csharp
overlay.RegisterSection(new GameDebugSection(playerController, progressionManager));
```

## Files to Modify
- `Assets/Scripts/Game/GameController.cs` - Add debug overlay creation

## Files to Create
- `Assets/Scripts/Debug/Sections/GameDebugSection.cs` - Game-specific metrics

## Existing Infrastructure
- `Assets/Scripts/Debug/DebugOverlay.cs` - Core overlay manager
- `Assets/Scripts/Debug/DebugSectionBase.cs` - Base class for sections
- `Assets/Scripts/Debug/Sections/SimulationDebugSection.cs` - Already has FPS display

## Controls
- F3: Toggle overlay visibility
- F4: Toggle gizmos visibility
