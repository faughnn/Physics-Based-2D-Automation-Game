# Level System Extensions

## Goal

Expand the level system's vocabulary so it can express a wider variety of levels without requiring new systems for each variation.

## Current State

The level architecture is clean. `LevelData` is a generic data class, `LevelLoader` is fully decoupled from specific levels, and `GameController` consumes `LevelData` generically with only 2 lines of coupling to the specific level class.

Creating a new level with different terrain layouts, objective counts, and bucket positions is straightforward — write a new static factory class and point `GameController` at it.

The limitations are in what `LevelData` can express:

- **Terrain is rectangles only** — `TerrainRegion` is axis-aligned min/max rectangles. No curves, caves, noise-based generation, or heightmaps.
- **Objectives are "collect N of material X" only** — `ObjectiveData` supports a target material, a count, and a reward ability. No other objective types (reach a location, build N structures, survive for time, etc.).
- **World objects are buckets and a shovel** — no system for placing arbitrary object types (enemies, switches, decorations, material sources) through level data.
- **No level selection mechanism** — `GameController` hard-codes which level to load. No menu, no parameter, no scene-based selection.
- **No per-level configuration** — starting abilities, available structures, world visual theme, etc. are all global.

## Work Required

### Terrain Generation
- Add support for non-rectangular terrain: heightmap-based surfaces, noise fills, or a callback-based generator that `LevelLoader` can invoke
- Consider a `ITerrainGenerator` interface so levels can mix rectangular regions with procedural generation

### Level Selection
- Add a level selection mechanism — could be as simple as an enum field on a scene GameObject, or a proper level select screen
- Decouple world size from the level data class (TutorialLevelData currently exposes WorldWidth/WorldHeight as static fields, which `GameController` reads before calling Create())

### World Object Vocabulary
- Extend `LevelData` with typed spawn lists for new object categories as they're needed (don't pre-build for hypothetical objects)
- Keep the current pattern where `GameController` iterates spawn lists and calls factory methods

### Objective Types
- This can wait until there's a concrete need for a non-collection objective. The current system handles the tutorial level's needs.

## Scope

Most of this is future-facing. The immediate priority is terrain generation flexibility (rectangles are limiting for interesting level design) and level selection (so multiple levels can coexist). Objective types and world object vocabulary can be extended when specific gameplay needs arise.
