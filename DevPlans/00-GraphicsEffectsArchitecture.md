# Graphics Effects System Architecture

## Goal

Create an extensible system for adding visual effects to the game. The architecture should make it easy to add new effects without modifying existing code.

---

## What This System Does

Think of this like a plugin system for graphics. Each effect is a self-contained module that:
- Can be turned on/off independently
- Knows how to configure itself
- Communicates with the shader through a central manager

---

## File Structure

```
Assets/Scripts/Graphics/
├── GraphicsManager.cs           # The central hub - manages all effects
├── IGraphicsEffect.cs           # Interface - defines what every effect must implement
├── GraphicsEffectBase.cs        # Base class - common code shared by all effects
└── Effects/
    ├── NoiseVariationEffect.cs  # (See 01-NoiseVariationEffect.md)
    ├── LightingEffect.cs        # (See 02-LightingEffect.md)
    ├── SoftEdgesEffect.cs       # (See 03-SoftEdgesEffect.md)
    └── GlowEffect.cs            # (See 04-GlowEffect.md)

Assets/Scripts/UI/
└── SettingsMenu.cs              # UI for toggling effects (already exists)
```

---

## Core Components

### 1. IGraphicsEffect (Interface)

This defines the "contract" that every effect must fulfill:

| Property/Method | Purpose |
|----------------|---------|
| `EffectName` | Display name for UI (e.g., "Noise Variation") |
| `Description` | Tooltip text explaining what it does |
| `IsEnabled` | Whether the effect is currently active |
| `OnEnable()` | Called when effect is turned on |
| `OnDisable()` | Called when effect is turned off |
| `UpdateShaderProperties()` | Push settings to the shader |

### 2. GraphicsEffectBase (Abstract Class)

Provides default implementations so effects don't repeat common code:
- PlayerPrefs save/load for persistence
- Default enable/disable behavior
- Reference to GraphicsManager

### 3. GraphicsManager (Singleton)

The central coordinator:
- Holds a list of all registered effects
- Provides access to shared resources (shader, textures)
- Called by effects to push global shader properties
- Initialized once at game start

---

## How Effects Communicate with the Shader

Effects don't modify the shader directly. Instead:

1. Effect calls `Shader.SetGlobalFloat("_EffectName", value)`
2. Shader reads this global property
3. Shader applies the effect based on the value

This means:
- Effects are decoupled from shader implementation
- Multiple effects can coexist without conflicts
- Shader can be optimized independently

---

## How to Add a New Effect

1. **Create the effect class** in `Assets/Scripts/Graphics/Effects/`
   - Inherit from `GraphicsEffectBase`
   - Implement required interface members

2. **Register it** in `GraphicsManager.Start()`
   - Add one line: `RegisterEffect(new YourEffect());`

3. **Add shader support** in `WorldRender.shader`
   - Add the uniform property
   - Add the visual logic in the fragment shader

4. **Done** - Settings menu auto-discovers registered effects

---

## Shader Integration

The `WorldRender.shader` will need these additions:

```
Properties block:
    _NoiseEnabled ("Noise Enabled", Float) = 1
    _LightingEnabled ("Lighting Enabled", Float) = 1
    _SoftEdgesEnabled ("Soft Edges Enabled", Float) = 0
    _GlowIntensity ("Glow Intensity", Float) = 0.5
```

Each effect's shader logic is documented in its respective plan file.

---

## Settings Persistence

Effects save their enabled state to PlayerPrefs:
- Key format: `"Graphics_{EffectName}_Enabled"`
- Loaded automatically on game start
- Saved immediately when toggled

---

## Initialization Order

1. `SandboxController.Start()` creates `GraphicsManager`
2. `GraphicsManager.Start()` registers all effects
3. Each effect loads its saved state from PlayerPrefs
4. Each effect calls `UpdateShaderProperties()` to apply initial state
5. Settings menu queries `GraphicsManager` to build toggles

---

## Design Principles

Following the project's architecture philosophy:

- **Single source of truth**: GraphicsManager owns effect state
- **No special cases**: All effects follow the same interface
- **Extensible**: Adding effects doesn't modify existing effect code
- **Decoupled**: Effects don't know about each other or the UI
