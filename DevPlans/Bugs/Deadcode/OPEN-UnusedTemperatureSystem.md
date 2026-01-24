# Dead Code: Unused Temperature/Thermal System

## Summary
The codebase contains an entire temperature simulation system that was designed but never implemented. Fields are initialized but never read.

## Locations

### Cell.cs (Line 12)
```csharp
public byte temperature;     // 0-255 for heat simulation
```
- Set to default value (20) in CellWorld.cs and ClusterManager.cs
- **Never read anywhere**

### MaterialDef.cs (Lines 31-40)
```csharp
public short ignitionTemp;      // Temperature at which material catches fire
public short meltTemp;          // Temperature at which solid melts
public short freezeTemp;        // Temperature at which liquid freezes
public short boilTemp;          // Temperature at which liquid vaporizes
public byte materialOnMelt;     // What material this becomes when melted
public byte materialOnFreeze;   // What material this becomes when frozen
public byte materialOnBurn;     // What material this becomes when burned
public byte materialOnBoil;     // What material this becomes when boiled
```
- All fields are assigned values in `Materials.CreateDefaults()`
- **None are ever read anywhere**

### MaterialFlags (Lines 17-20)
```csharp
public const byte ConductsHeat = 1 << 0;  // Assigned but never checked
public const byte Flammable = 1 << 1;     // Assigned to Oil but never checked
public const byte Conductive = 1 << 2;    // Never assigned or checked
public const byte Corrodes = 1 << 3;      // Never assigned or checked
```

## Memory Impact
- `Cell.temperature`: 1 byte per cell (could save ~4MB on large worlds)
- `MaterialDef` thermal fields: 12 bytes per material definition

## Recommended Action
**Option A:** Remove the temperature system entirely if not planned for near-term implementation.

**Option B:** Keep as placeholder if thermal simulation is on the roadmap, but add `// TODO: Implement thermal simulation` comments.
