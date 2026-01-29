# Bug: AoE Circle Only Visible Near Edge of Range

## Summary
The dig and grab area-of-effect circle indicator is only visible when the cursor is between 80%-100% of the tool's max range. It should be visible at all times when the tool is selected and the cursor is within range.

## Symptoms
- Select the shovel or grabber tool
- Move cursor near the player (well within range) — no AoE circle is shown
- Move cursor far from the player (near the edge of range) — AoE circle appears
- The circle only appears in a narrow band at 80%-100% of max range

## Root Cause
In `ToolRangeIndicator.LateUpdate()`, an early return at line ~117-121 hides **both** the range arc and the AoE circle when the cursor is below 80% of max range:

```csharp
float ratio = distance / range;

// Hidden below 80% of range
if (ratio < FadeStartRatio)
{
    Hide();
    return;
}
```

The `Hide()` method disables both `lineRenderer` (the range arc) and `aoeLineRenderer` (the AoE circle) indiscriminately. The early return also prevents the AoE drawing code (lines ~162-203) from ever executing when the cursor is close to the player.

The 80% fade-in threshold is intentional for the **range arc** (it shows the boundary of your reach), but the **AoE circle** (showing dig/grab area at cursor) should be independent and always visible when within range.

## Affected Code
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs:15` — `FadeStartRatio = 0.8f` constant
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs:114-121` — early return hides everything below 80%
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs:206-212` — `Hide()` disables both renderers

## Potential Solutions
### 1. Separate AoE visibility from arc visibility
Move the AoE circle logic before/independent of the `ratio < FadeStartRatio` early return. The early return should only hide `lineRenderer` (the arc), not `aoeLineRenderer` (the AoE circle). The AoE circle should have its own visibility rule: show when tool is active, cursor is within range (`ratio <= 1.0`), and grabber is not holding cells.

### 2. Remove early return entirely, use per-element visibility
Replace the single `Hide()` early return with per-element visibility checks:
- Arc: only visible when `ratio >= 0.8`
- AoE circle: visible when `ratio <= 1.0` and tool has a non-zero `cellRadius`

## Priority
Medium

## Related Files
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs`
- `Assets/Scripts/Game/Digging/DiggingController.cs`
- `Assets/Scripts/Game/CellGrabSystem.cs`
