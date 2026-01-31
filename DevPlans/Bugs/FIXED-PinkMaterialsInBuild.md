# Bug: Most Things Render Pink/Magenta in Build

## Summary
When the project is built and run as a standalone executable, most visuals appear pink/magenta. This is Unity's indicator for missing or broken shaders. The project works fine in the editor because the editor has access to all shaders regardless of static references, but builds strip unreferenced shaders.

## Symptoms
- Cell world (terrain, sand, water, all materials) renders pink
- Tool range indicator lines render pink
- Everything using `Shader.Find()` at runtime fails
- Works correctly in the Unity Editor

## Root Cause
The project uses **URP (2D Renderer)** but relies on `Shader.Find()` at runtime to locate shaders. In builds, Unity strips shaders that aren't statically referenced by a material asset in a scene or the Resources folder, or listed in "Always Included Shaders." All four `Shader.Find()` calls fail in builds:

### 1. Custom world shader stripped from build (CRITICAL)
`CellRenderer.cs:96` calls `Shader.Find("FallingSand/WorldRender")`. The shader exists at `Assets/Shaders/WorldRender.shader` and is correctly written for URP, but no `.mat` asset references it and it's not in the Always Included Shaders list. Unity strips it from the build. **This makes the entire cell world pink.**

### 2. Fallback shader doesn't exist in URP (CRITICAL)
`CellRenderer.cs:102` falls back to `Shader.Find("Unlit/Color")`, which is a built-in render pipeline shader that does not exist under URP. This fallback also returns null.

### 3. Legacy sprite shader in URP (MEDIUM)
`ToolRangeIndicator.cs:60,74` uses `Shader.Find("Sprites/Default")`, which is a built-in pipeline shader. Under URP it may be stripped or incompatible, making the range/AoE indicators pink.

## Affected Code
- `Assets/Scripts/Rendering/CellRenderer.cs:96` — `Shader.Find("FallingSand/WorldRender")` — custom shader stripped from build
- `Assets/Scripts/Rendering/CellRenderer.cs:102` — `Shader.Find("Unlit/Color")` — doesn't exist in URP
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs:60` — `Shader.Find("Sprites/Default")` — legacy shader, may be stripped in URP
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs:74` — `Shader.Find("Sprites/Default")` — same issue
- `Assets/Shaders/WorldRender.shader` — the custom shader itself (correctly written for URP, just not referenced)
- `ProjectSettings/GraphicsSettings.asset` — Always Included Shaders list does not include the custom shader

## Potential Solutions
### 1. Add shaders to Always Included Shaders list
In Unity Editor: **Project Settings > Graphics > Always Included Shaders**, add:
- `FallingSand/WorldRender`
- `Sprites/Default` (or the URP equivalent)

This is the quickest fix and ensures the shaders survive build stripping regardless of how they're referenced.

### 2. Replace Shader.Find() with serialized references (recommended long-term)
Eliminate all `Shader.Find()` calls. Instead:
- `CellRenderer` already has `[SerializeField] private Shader worldShader` — assign it in the scene/prefab Inspector, or load it from a `Resources` folder
- `ToolRangeIndicator` — add a `[SerializeField] private Material lineMaterial` field and assign a URP-compatible sprite material in the Inspector, or use `Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")` with the shader added to Always Included Shaders
- Remove the `Unlit/Color` fallback entirely (it serves no purpose under URP)

### 3. Create a material asset referencing the custom shader
Create `Assets/Materials/WorldRender.mat` that uses the `FallingSand/WorldRender` shader. Place it in a scene or `Resources/` folder. Unity will then include the shader in builds automatically. This avoids editing Graphics Settings but still requires the material to be reachable.

## Priority
Critical — the game is unplayable in built form.

## Related Files
- `Assets/Scripts/Rendering/CellRenderer.cs`
- `Assets/Scripts/Game/UI/ToolRangeIndicator.cs`
- `Assets/Shaders/WorldRender.shader`
- `Assets/Settings/UniversalRP.asset`
- `Assets/Settings/Renderer2D.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/QualitySettings.asset`
