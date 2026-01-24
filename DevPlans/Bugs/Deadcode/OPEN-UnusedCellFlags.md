# Dead Code: Unused CellFlags Constants

## Summary
Four of the five CellFlags constants are defined but never used. Only `CellFlags.None` is referenced.

## Location
- **File:** `Assets/Scripts/Simulation/Cell.cs`
- **Lines:** 22-27

## Unused Flags
| Flag | Line | Intended Purpose | Status |
|------|------|------------------|--------|
| `OnBelt` | 23 | Being moved by a belt | **USED** in SimulateBeltsJob |
| `OnLift` | 24 | Being moved by a lift | **UNUSED** - Lift not implemented |
| `Burning` | 25 | Currently on fire | **UNUSED** - Fire not implemented |
| `Wet` | 26 | In contact with liquid | **UNUSED** - Wetness not implemented |
| `Settled` | 27 | Powder can't move, skip simulation | **UNUSED** - Optimization not implemented |

## Recommended Action
- Keep `OnBelt` (actively used)
- Remove `OnLift`, `Burning`, `Wet`, `Settled` or mark with `// TODO: Future feature` comments
