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
