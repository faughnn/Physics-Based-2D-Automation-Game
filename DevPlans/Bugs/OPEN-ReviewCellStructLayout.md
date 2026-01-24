# OPEN: Review Cell Struct Layout

## Summary

The Cell struct has grown organically and should be reviewed for optimal layout, size, and field usage.

## Current State (12 bytes after fractional velocity addition)

```csharp
public struct Cell
{
    public byte materialId;      // 1 byte
    public byte flags;           // 1 byte (only 5 bits used)
    public ushort frameUpdated;  // 2 bytes
    public sbyte velocityX;      // 1 byte
    public sbyte velocityY;      // 1 byte
    public byte velocityFracY;   // 1 byte (NEW - fractional accumulator)
    public byte temperature;     // 1 byte
    public byte structureId;     // 1 byte
    public ushort ownerId;       // 2 bytes
}
```

## Questions to Consider

1. **frameUpdated** - Is ushort necessary? Could use a single bit or byte if we only need to detect "processed this frame"
2. **flags** - Only 5 of 8 bits used. Could pack other small values here
3. **velocityFracX** - Not added yet. Needed if horizontal forces (wind, explosions) are added
4. **structureId** - Is byte (256 max) enough? Is this field still needed?
5. **ownerId** - Is ushort (65k clusters) overkill?
6. **Alignment** - Current layout may have padding. Could reorder for better cache performance

## Memory Impact

- 1024x1024 world = 1M cells
- 10 bytes = 10 MB, 12 bytes = 12 MB
- Not critical but worth optimizing if easy wins exist

## Action

Review when adding next major feature that touches Cell struct.
