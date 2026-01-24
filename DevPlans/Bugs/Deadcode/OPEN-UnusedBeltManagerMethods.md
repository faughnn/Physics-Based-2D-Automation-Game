# Dead Code: Unused BeltManager Methods

## Summary
Several public methods in BeltManager are defined but never called from anywhere in the codebase.

## Location
- **File:** `Assets/Scripts/Structures/BeltManager.cs`

## Unused Methods
| Method | Line | Purpose |
|--------|------|---------|
| `HasBeltAt(int x, int y)` | 335 | Check if belt exists at position |
| `TryGetBeltTile(int x, int y, out BeltTile tile)` | 343 | Get belt tile data at position |
| `TryGetBelt(ushort beltId, out BeltStructure belt)` | 351 | Get belt by ID |
| `GetBelts()` | 359 | Get all belt structures |
| `SimulateBelts(ushort currentFrame)` | 376 | Single-threaded belt simulation |

## Notes
- `SimulateBelts` is the non-Burst single-threaded version; `ScheduleSimulateBelts` (the parallel Burst job) is used instead
- The query methods (`HasBeltAt`, `TryGetBeltTile`, etc.) appear to be API methods that were never needed

## Recommended Action
- Remove `SimulateBelts` (superseded by job version)
- Evaluate if query methods should be kept for future use or removed
