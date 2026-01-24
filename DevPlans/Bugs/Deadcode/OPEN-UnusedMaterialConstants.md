# Dead Code: Unused Material Constants

## Summary
Several material ID constants are defined but the materials either have no definition or are never used in gameplay.

## Location
- **File:** `Assets/Scripts/Simulation/MaterialDef.cs`
- **Lines:** 57-69

## Unused Materials
| Constant | Line | Status |
|----------|------|--------|
| `Materials.IronOre` | 57 | ID defined, no MaterialDef created |
| `Materials.MoltenIron` | 58 | ID defined, no MaterialDef created |
| `Materials.Iron` | 59 | ID defined, no MaterialDef created |
| `Materials.Coal` | 60 | ID defined, no MaterialDef created |
| `Materials.Ash` | 61 | ID defined, no MaterialDef created |
| `Materials.Smoke` | 62 | Only referenced as `materialOnBurn` (unused thermal system) |
| `Materials.Ground` | 69 | MaterialDef created but never placed/used |

## Reason
These are placeholder materials for planned features:
- Smelting system (IronOre, MoltenIron, Iron)
- Combustion system (Coal, Ash, Smoke)
- Terrain digging (Ground)

## Recommended Action
Remove unused material IDs or move to a separate "PlannedMaterials" section with clear TODO comments.
