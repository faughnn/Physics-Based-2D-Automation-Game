# Insufficient Buffer for Multi-Cell Movements

**Status:** FIXED (2026-01-23)
**Severity:** Was CRITICAL

---

## Summary

The 15-cell buffer zone was designed for velocity-based movement, but liquid and gas spreading could exceed this limit, potentially causing data races between same-group chunks.

---

## Resolution

**Fix: Core-Only Simulation**

Chunks now only simulate cells within their 32x32 core region. The buffer zone concept is eliminated - cells can write anywhere, but only core cells are simulated.

With this change:
- Same-group chunk cores are 32 cells apart (64 cell spacing - 32 cell core)
- Max liquid spread: 11 cells
- Max gas spread: 4 cells
- Max velocity: 16 cells
- All movement < 32 cells = **no overlap between same-group write zones**

---

## Previous Concern (No Longer Applicable)

The old extended region approach created a 2-cell gap that horizontal spread could exceed:

| Movement Type | Max Distance | Old Gap | Exceeded? |
|---------------|-------------|---------|-----------|
| Liquid spread | 11 cells | 2 cells | YES |
| Gas spread | 4 cells | 2 cells | YES |
| Velocity | 15 cells | 2 cells | No |

---

## Files Modified
- `Assets/Scripts/Simulation/Jobs/SimulateChunksJob.cs` - Removed buffer zone iteration, core-only simulation
- `Assets/Scripts/Simulation/PhysicsSettings.cs` - MaxVelocity now 16, updated comment
