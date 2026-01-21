# Game Progression Trees

Three interconnected progression trees that unlock new materials, machines, and capabilities. Players advance by processing materials, building infrastructure, and discovering new combinations.

---

## Tree 1: Materials & Metallurgy (Industrial)

*Focus: Mining, smelting, and refining raw materials into useful components.*

This tree represents the core industrial backbone of automation. Players discover and process increasingly complex materials.

```
                         ┌─────────────────────┐
                         │   RAW MATERIALS     │
                         │   (Starting Point)  │
                         └─────────┬───────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │    SAND     │      │   STONE     │      │   WATER     │
       │  (Powder)   │      │  (Static)   │      │  (Liquid)   │
       └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │   GLASS     │      │  IRON ORE   │      │   STEAM     │
       │ (heat sand) │      │  (mining)   │      │ (heat water)│
       └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
              │                    │                    │
              │             ┌──────┴──────┐            │
              │             ▼             ▼            │
              │      ┌─────────────┐ ┌─────────────┐   │
              │      │MOLTEN IRON  │ │   COAL      │   │
              │      │(furnace ore)│ │ (mining)    │   │
              │      └──────┬──────┘ └──────┬──────┘   │
              │             │               │          │
              │             └───────┬───────┘          │
              │                     ▼                  │
              │              ┌─────────────┐           │
              │              │   STEEL     │           │
              │              │(iron + coal)│           │
              │              └──────┬──────┘           │
              │                     │                  │
              └──────────┬──────────┴─────────┬────────┘
                         ▼                    ▼
                  ┌─────────────┐      ┌─────────────┐
                  │ ALLOY FORGE │      │   BOILER    │
                  │  (machine)  │      │  (machine)  │
                  └──────┬──────┘      └──────┬──────┘
                         │                    │
         ┌───────────────┼────────────────────┤
         ▼               ▼                    ▼
  ┌─────────────┐ ┌─────────────┐      ┌─────────────┐
  │   BRONZE    │ │  TITANIUM   │      │ PRESSURIZED │
  │(copper+tin) │ │  (rare ore) │      │   STEAM     │
  └─────────────┘ └─────────────┘      └─────────────┘
```

### Tier 1: Primitives
| Material | Type | Source | Properties |
|----------|------|--------|------------|
| Sand | Powder | Surface deposits | Falls, piles; base for glass |
| Stone | Static | Everywhere | Immovable terrain |
| Water | Liquid | Springs, rain | Flows, evaporates at 100°C |
| Dirt | Powder | Surface layer | Absorbs water, becomes mud |

### Tier 2: Basic Processing
| Material | Type | Recipe | Properties |
|----------|------|--------|------------|
| Glass | Static | Sand + Heat (800°C) | Transparent, fragile clusters |
| Steam | Gas | Water + Heat (100°C) | Rises, condenses when cooled |
| Mud | Liquid | Dirt + Water | Slow-flowing, dries to clay |
| Clay | Powder | Mud - Water (evaporate) | Moldable, fires to ceramic |

### Tier 3: Ores & Extraction
| Material | Type | Source | Properties |
|----------|------|--------|------------|
| Iron Ore | Powder | Underground veins | Heavy, magnetic |
| Copper Ore | Powder | Underground veins | Conductive when processed |
| Coal | Powder | Underground seams | Burns hot, produces ash |
| Tin Ore | Powder | Shallow deposits | Light, low melting point |

### Tier 4: Metallurgy
| Material | Type | Recipe | Properties |
|----------|------|--------|------------|
| Molten Iron | Liquid | Iron Ore + Heat (1500°C) | Glowing, burns on contact |
| Iron Ingot | Static | Molten Iron (cooled) | Strong, rusts in water |
| Steel | Static | Molten Iron + Coal | Stronger than iron, no rust |
| Bronze | Static | Copper + Tin (molten) | First alloy, corrosion-resistant |

### Tier 5: Advanced Materials
| Material | Type | Recipe | Properties |
|----------|------|--------|------------|
| Titanium | Static | Rare ore + extreme heat | Lightweight, very strong |
| Tempered Glass | Static | Glass + controlled cooling | Strong, heat-resistant |
| Ceramic | Static | Clay + Heat (1200°C) | Heat-insulating |
| Carbon Fiber | Static | Coal + pressure + heat | Extremely strong, light |

---

## Tree 2: Automation & Logistics

*Focus: Moving, sorting, and processing materials efficiently.*

This tree unlocks machines and systems that handle materials automatically.

```
                         ┌─────────────────────┐
                         │   MANUAL LABOR      │
                         │   (Starting Point)  │
                         └─────────┬───────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │   BASIC BELT        │
                         │  (moves materials)  │
                         └─────────┬───────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │   CHUTE     │      │    LIFT     │      │   HOPPER    │
       │(gravity fed)│      │ (vertical)  │      │ (collector) │
       └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
              │                    │                    │
              └────────────────────┼────────────────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │   BASIC FURNACE     │
                         │   (applies heat)    │
                         └─────────┬───────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │   GRINDER   │      │   PRESS     │      │   MIXER     │
       │(breaks down)│      │ (compacts)  │      │ (combines)  │
       └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
              │                    │                    │
              └────────────────────┼────────────────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │   FAST BELT         │
                         │  (2x speed)         │
                         └─────────┬───────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │  SPLITTER   │      │   FILTER    │      │   VALVE     │
       │ (divides)   │      │  (by type)  │      │(liquid gate)│
       └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
              │                    │                    │
              └────────────────────┼────────────────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │   ARC FURNACE       │
                         │  (extreme heat)     │
                         └─────────┬───────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │  CENTRIFUGE │      │  CRYOGENIC  │      │   PUMP      │
       │(separates)  │      │  CHAMBER    │      │ (liquids)   │
       └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
              │                    │                    │
              └────────────────────┼────────────────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │   EXPRESS BELT      │
                         │   (4x speed)        │
                         └─────────┬───────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │   TELEPORTER        │
                         │  (instant move)     │
                         └─────────────────────┘
```

### Tier 1: Basic Transport
| Machine | Function | Unlocks With |
|---------|----------|--------------|
| Basic Belt | Moves materials horizontally (2 cells/sec) | Starting |
| Chute | Gravity-fed diagonal transport | Stone |
| Lift | Moves materials vertically | Iron |
| Hopper | Collects falling materials | Iron |

### Tier 2: Processing
| Machine | Function | Unlocks With |
|---------|----------|--------------|
| Basic Furnace | Heats interior to 500°C | Stone + Coal |
| Grinder | Breaks clusters into powder | Iron |
| Press | Compacts powder into solid blocks | Steel |
| Mixer | Combines two material streams | Bronze |

### Tier 3: Routing
| Machine | Function | Unlocks With |
|---------|----------|--------------|
| Fast Belt | 2x belt speed | Steel |
| Splitter | Divides stream into two outputs | Steel |
| Filter | Routes specific materials | Steel + Glass |
| Valve | Controls liquid flow | Bronze |

### Tier 4: Advanced Processing
| Machine | Function | Unlocks With |
|---------|----------|--------------|
| Arc Furnace | Heats to 2000°C (for titanium) | Steel + Carbon |
| Centrifuge | Separates mixed materials by density | Steel |
| Cryogenic Chamber | Cools to -100°C | Titanium |
| Pump | Moves liquids against gravity | Bronze |

### Tier 5: Mastery
| Machine | Function | Unlocks With |
|---------|----------|--------------|
| Express Belt | 4x belt speed | Titanium |
| Teleporter | Instant point-to-point transport | Void Crystal |
| Assembler | Combines materials into structures | All Tier 4 |

---

## Tree 3: Arcana & Transmutation (Fantasy + Physics)

*Focus: Magical manipulation of matter - bending physics rules through arcane means.*

This tree introduces fantasy elements that work alongside physics. Magic doesn't break physics - it extends it with new rules that still respect physical consistency.

```
                         ┌─────────────────────┐
                         │   MUNDANE WORLD     │
                         │   (Starting Point)  │
                         └─────────┬───────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │   ESSENCE CRYSTAL   │
                         │ (found deep below)  │
                         └─────────┬───────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │ FIRE ESSENCE│      │WATER ESSENCE│      │EARTH ESSENCE│
       │   (ember)   │      │   (dew)     │      │   (loam)    │
       └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
              │                    │                    │
              │                    ▼                    │
              │             ┌─────────────┐            │
              │             │ AIR ESSENCE │            │
              │             │  (zephyr)   │            │
              │             └──────┬──────┘            │
              │                    │                   │
              └────────────────────┼───────────────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │   ALCHEMICAL STILL  │
                         │ (essence extractor) │
                         └─────────┬───────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │ TRANSMUTER  │      │  ENCHANTER  │      │  CONDENSER  │
       │(change mat.)│      │(buff machines)│    │(essence→mat)│
       └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
              │                    │                    │
              └────────────────────┼────────────────────┘
                                   │
          ┌────────────────────────┼────────────────────────┐
          │                        │                        │
          ▼                        ▼                        ▼
   ┌─────────────┐          ┌─────────────┐          ┌─────────────┐
   │ PHILOSOPHERS│          │   VOID      │          │  TEMPORAL   │
   │   STONE     │          │  CRYSTAL    │          │   SHARD     │
   │(lead→gold)  │          │(nullifies)  │          │(time manip.)│
   └──────┬──────┘          └──────┬──────┘          └──────┬──────┘
          │                        │                        │
          │                        ▼                        │
          │                 ┌─────────────┐                 │
          │                 │ ELEMENTAL   │                 │
          │                 │   FORGE     │                 │
          │                 └──────┬──────┘                 │
          │                        │                        │
          └────────────────────────┼────────────────────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
       ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
       │  PHLOGISTON │      │    AETHER   │      │  QUINTESS-  │
       │ (magic fire)│      │(anti-gravity)│     │   ENCE      │
       └─────────────┘      └─────────────┘      └─────────────┘
```

### Tier 1: Discovery
| Material | Type | Source | Properties |
|----------|------|--------|------------|
| Essence Crystal | Static | Deep underground | Glows faintly, resonates near elements |

### Tier 2: Elemental Essences
Essences are extracted from materials using an Alchemical Still. They follow physical rules but have unique properties:

| Essence | Extracted From | Physical Behavior | Effect |
|---------|----------------|-------------------|--------|
| Fire Essence (Ember) | Coal, Lava | Gas, rises slowly | Heats adjacent cells +5°C/frame |
| Water Essence (Dew) | Pure Water | Liquid, very light | Cools adjacent cells -5°C/frame |
| Earth Essence (Loam) | Stone, Ore | Powder, heavy | Increases density of materials |
| Air Essence (Zephyr) | Steam | Gas, rises fast | Reduces density of materials |

### Tier 3: Arcane Machines
| Machine | Function | Physics Integration |
|---------|----------|---------------------|
| Alchemical Still | Extracts essences from base materials | Works like furnace but outputs essence based on heat patterns |
| Transmuter | Converts one material to another | Requires exact essence ratios; respects mass conservation |
| Enchanter | Applies essence effects to machines | Belt + Fire Essence = heated belt surface |
| Condenser | Converts essences back to materials | Reverse extraction; essence → base material |

### Tier 4: Arcane Materials
| Material | Type | Recipe | Properties |
|----------|------|--------|------------|
| Philosopher's Stone | Static | All 4 essences + Gold trace | Transmutes adjacent lead → gold (slowly) |
| Void Crystal | Static | Essence Crystal + extreme cold (-200°C) | Nullifies gravity in radius |
| Temporal Shard | Static | Essence Crystal + extreme heat (2500°C) | Slows time for cells in radius (1/2 sim speed) |
| Mithril | Static | Silver + Air Essence + Moon phase | Lightweight metal, immune to rust |

### Tier 5: Fundamental Forces
These materials manipulate the physics simulation itself:

| Material | Type | Creation | Effect |
|----------|------|----------|--------|
| Phlogiston | Liquid | Fire Essence + Void Crystal | Burns underwater, heats without consuming itself |
| Aether | Gas | Air Essence + Temporal Shard | Anti-gravity - pushes materials upward |
| Quintessence | Liquid | All essences combined | Perfect transmutation catalyst - instant material conversion |

### Enchantment System
Essences can be applied to automation machines to modify their behavior:

| Enchantment | Essence Used | Effect on Belt | Effect on Furnace | Effect on Lift |
|-------------|--------------|----------------|-------------------|----------------|
| Ignition | Fire | Heats materials as they pass | +200°C max temp | Lifts faster when hot |
| Cooling | Water | Cools materials as they pass | Becomes cooling chamber | Condenses steam to water |
| Weight | Earth | Moves heavier materials | Compacts materials | Increased lifting force |
| Levity | Air | Moves lighter materials faster | Materials float inside | Anti-gravity lift |

### Transmutation Recipes
The Transmuter converts materials using essence ratios:

| Input | Essence Recipe | Output | Notes |
|-------|----------------|--------|-------|
| Lead | Fire × 3 | Gold | Classic alchemy; slow process |
| Sand | Earth × 2 + Fire × 1 | Ruby | Crystallizes under pressure+heat |
| Water | Air × 2 + Cold | Ice (permanent) | Unlike natural ice, won't melt |
| Iron | Fire × 1 + Earth × 1 | Steel | Faster than conventional smelting |
| Stone | Water × 2 | Clay | Softens stone structure |
| Coal | Fire × 4 + Air × 4 | Diamond | Extreme essence compression |

### Physics Integration Notes

The fantasy tree follows these principles to maintain physical consistency:

1. **Conservation of Mass**: Transmutation preserves total mass. Lead→Gold creates slightly less gold.
2. **Energy Requirements**: Powerful effects require essence input, which must be gathered/produced.
3. **Spatial Consistency**: Effects work within the cell grid - no teleportation without infrastructure.
4. **Predictable Interactions**: Aether (anti-gravity) still respects collision; it just inverts the gravity vector.
5. **No Free Energy**: Phlogiston burns bright but must be crafted from essences that require extraction.

---

## Cross-Tree Synergies

The trees interlock in meaningful ways:

### Materials → Automation
- Better materials unlock better machines
- Steel belts move faster than iron
- Titanium enables extreme-temperature processing

### Automation → Arcana
- Automated essence extraction at scale
- Enchanted machines for efficiency
- Mass transmutation production lines

### Arcana → Materials
- Transmutation creates rare materials
- Essence-enhanced processing yields purer outputs
- Void Crystals enable anti-gravity material transport

---

## Suggested Starting Experience

1. **Hour 0-2**: Discover sand, water, stone. Build first furnace to make glass.
2. **Hour 2-5**: Mine iron ore, smelt iron. Build first belt system.
3. **Hour 5-10**: Establish coal supply, produce steel. Automate ore processing.
4. **Hour 10-15**: Discover Essence Crystal. Begin arcane experimentation.
5. **Hour 15+**: Combine all three trees to build complex automated transmutation factories.

---

## Implementation Priority

### Phase 1: Core Materials
- [ ] Sand, Stone, Water, Steam (already exists)
- [ ] Iron Ore, Coal, Molten Iron, Iron
- [ ] Basic material palette expansion

### Phase 2: Basic Automation
- [ ] Belt structure (horizontal transport)
- [ ] Lift structure (vertical transport)
- [ ] Furnace structure (heating)

### Phase 3: Advanced Materials
- [ ] Copper, Tin, Bronze
- [ ] Steel (iron + coal smelting)
- [ ] Glass (sand + heat)

### Phase 4: Routing & Processing
- [ ] Splitter, Filter machines
- [ ] Grinder, Press machines
- [ ] Fast belt variants

### Phase 5: Arcane Foundation
- [ ] Essence Crystal material
- [ ] Alchemical Still machine
- [ ] Four base essences

### Phase 6: Advanced Arcana
- [ ] Transmuter machine
- [ ] Enchantment system
- [ ] Advanced arcane materials

---

*These progression trees provide 50+ hours of content with natural discovery curves and satisfying cross-system synergies.*
