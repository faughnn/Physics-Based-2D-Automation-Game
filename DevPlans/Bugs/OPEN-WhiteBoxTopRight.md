# Bug: White Box in Top Right Corner

## Summary
An empty white/bordered box appears in the top right corner of the screen, overlapping the game view.

## Symptoms
- White rectangular box visible in the top right corner
- Box appears to have a light border with empty/dark interior
- Persists on screen during gameplay
- Appears alongside the Debug Overlay (which displays correctly on the left)

## Root Cause
Unknown. Possibly:
- An OnGUI element rendering an empty box
- A UI panel that was created but has no content
- A debug visualization that should be hidden or removed

## Affected Code
- Unknown - needs investigation
- Likely candidates:
  - Debug overlay system in `Assets/Scripts/Debug/`
  - Any script with `OnGUI()` method
  - UI Canvas elements

## Potential Solutions
### 1. Find and Remove Stray GUI Element
Search for OnGUI methods or UI elements that might be drawing this box and either fix or remove them.

### 2. Check Debug Sections
Verify all debug sections are rendering correctly and none are drawing empty boxes.

## Priority
Low

## Related Files
- `Assets/Scripts/Debug/DebugOverlay.cs`
- Any files containing `OnGUI()` or `GUI.Box`
