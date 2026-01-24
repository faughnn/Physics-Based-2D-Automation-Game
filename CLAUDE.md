# Claude Code Settings

**Always use the Opus model for any subagents that are launched.**

**Never commit to git without asking the user first.** Always confirm before running `git commit`, `git push`, or other commands that modify git history.

---

# Development Guidelines

## Architecture Philosophy

**Systems, Not Patches**

- Build unified systems that handle all cases, not individual fixes for individual problems
- One source of truth - if logic exists, it lives in ONE place
- No special-case rules for specific scenarios
- If a "fix" only addresses one situation, step back and design a system that handles ALL similar situations
- When something doesn't work, ask: "What system is missing?" not "What patch can I add?"

**Example:**
- BAD: Adding `GetClusterNetForce()` to ClusterManager that loops over cells and calls ForceZoneManager
- GOOD: Adding `GetNetForceInArea()` to ForceZoneManager itself, so ANY system needing area queries uses the same logic

**Questions to ask before implementing:**
1. Does this logic already exist somewhere? (Don't duplicate)
2. Where should this logic live? (Single responsibility)
3. Will other systems need this? (Design for reuse)
4. Am I adding a special case or extending a system? (Prefer the latter)

---

## Separation of Concerns: Game vs Simulation

The codebase has two distinct layers that must remain separate:

**Simulation Layer** (`Assets/Scripts/Simulation/`)
- Cell world, materials, physics
- Clusters and rigid body simulation
- Belts, structures, terrain colliders
- Rendering of the cell world
- Reusable by any scene (Sandbox, Game, etc.)

**Sandbox Layer** (`Assets/Scripts/` - SandboxController, etc.)
- Material painting and brush tools
- Debug cluster spawning
- Belt placement tools
- Development/testing utilities

**Game Layer** (`Assets/Scripts/Game/`)
- Player character and controls
- Game-specific mechanics and rules
- Camera follow, UI, scoring, etc.

**Rules:**
- Changes to materials, cell physics, belts, clusters, or world simulation → `Assets/Scripts/Simulation/`
- Changes to painting tools, debug spawning, or sandbox features → Sandbox layer
- Changes to player, game mechanics, or game-specific features → `Assets/Scripts/Game/`
- Simulation code must NEVER reference Sandbox or Game code
- Sandbox and Game use `SimulationManager` to access simulation systems

**Scene Controllers:**
- `SandboxController` - Sandbox scene, painting tools, debug spawning
- `GameController` - Game scene, player spawning, game logic

---

## Chunk Threading Architecture

The simulation uses a **4-pass checkerboard pattern** to enable parallel chunk processing without race conditions.

**Chunk Groups:**
```
A B A B A B A B
C D C D C D C D
A B A B A B A B
```

Group assignment: `group = (chunkX & 1) + ((chunkY & 1) << 1)`
- Group A (0): even X, even Y
- Group B (1): odd X, even Y
- Group C (2): even X, odd Y
- Group D (3): odd X, odd Y

**Why 4 Groups?**
- Each chunk (64x64 cells) processes an extended region with a 32-cell buffer
- Same-group chunks are 2 chunks apart (128 cells)
- Their extended regions (64 + 32 + 32 = 128 cells wide) leave no overlap
- This ensures same-group chunks can READ but not WRITE adjacent cells
- Cell velocity is capped at 16 cells/frame (plus max spread of 11), safely within the 64-cell half-gap

**Execution:**
1. Groups A, B, C, D are scheduled sequentially (with dependencies)
2. Within each group, chunks run in parallel across worker threads
3. Each chunk processes bottom-to-top with alternating X direction per row

**Dirty Chunk Optimization:**
Only "active" chunks are simulated. A chunk is active if:
- It has the `IsDirty` flag set (something moved into/within it)
- It was active last frame (`activeLastFrame != 0`)
- It contains a structure (`HasStructure` flag)

When a cell moves, `MarkDirtyInternal()` is called for both old and new positions. Cells that don't move do NOT mark dirty, allowing chunks to go inactive.

**Key Files:**
- `CellSimulatorJobbed.cs` - Orchestrates the 4-pass scheduling
- `CellWorld.cs` - `CollectChunkGroups()` assigns chunks to groups
- `SimulateChunksJob.cs` - Burst-compiled job that processes each chunk

---

## Debug Overlay System

All debug visualization and metrics display goes through the unified `DebugOverlay` system in `Assets/Scripts/Debug/`.

**To add new debug information:**
1. Create a new class in `Assets/Scripts/Debug/Sections/` that extends `DebugSectionBase`
2. Implement `SectionName`, `Priority`, `DrawGUI()`, and optionally `DrawGizmos()` and `UpdateCachedValues()`
3. Register it in `SandboxController.Start()` via `debugOverlay.RegisterSection(new YourSection(...))`

**DO NOT:**
- Create standalone `OnGUI()` methods for debug display
- Use `Debug.DrawLine()` directly for visualization (use gizmos in a debug section)
- Create new MonoBehaviours just for debug visualization

**Controls:**
- F3: Toggle overlay visibility
- F4: Toggle gizmos visibility

---

## Bug Tracking

When requested to log a bug, create a markdown file in `G:\Sandy\DevPlans\Bugs\`.

**Naming convention:** `{STATUS}-{BugName}.md`
- `OPEN-BugName.md` - Active bug, not yet fixed
- `FIXED-BugName.md` - Bug has been resolved
- `REJECTED-BugName.md` - Not a bug, or won't fix

Rename the file when status changes.
