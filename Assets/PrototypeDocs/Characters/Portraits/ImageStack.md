# ImageStack

**Namespace:** `Assets.Prototypes.Graphics.Portrait`  
**Inherits:** `ScriptableObject`

Container for ordered sprite layers used in portrait composition.

## Creation

```csharp
// Create via Unity menu
Assets > Create > Graphics > Portrait > ImageStack
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Layers` | `List<ImageStackLayer>` | Ordered list of compositable layers |
| `OwnerCharacter` | `CharacterData` | Optional character reference for editor auto-assignment |

## Usage

```csharp
// Create and configure
ImageStack stack = CreateInstance<ImageStack>();
stack.Layers.Add(new ImageStackLayer {
    Sprite = baseSprite,
    Order = 0
});

// Use with Portrait
portrait.ImageStack = stack;
portrait.CompositeLayers(); // Composites all layers
```

## Notes
- Rendering handled by `StackedImage<TOwner>.CompositeLayers()` (Portrait, SkillBadge, etc.)
- Layers composited in order of `Order` property (low to high)
- Owner character reference used by editor for auto-assignment
- Render dimensions determined by `GraphicsPrototypesSettings`

---

# ImageStackLayer

**Type:** `[Serializable]`

Individual sprite layer with transform and tinting data.

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Sprite` | `Sprite` | `null` | Source sprite to composite |
| `Mask` | `Sprite` | `null` | Tint mask (RGB channels define tint regions) |
| `Offset` | `Vector2` | `(0,0)` | Position offset in pixels |
| `Scale` | `float` | `1.0` | Scale multiplier |
| `Rotation` | `float` | `0.0` | Rotation in degrees (not yet implemented) |
| `Order` | `int` | `0` | Render order (lower = behind) |

## Tint Masking

Mask sprite uses RGB channels to define tinting regions:
- **Red Channel** - Tinted by `TintColors[0]`
- **Green Channel** - Tinted by `TintColors[1]`
- **Blue Channel** - Tinted by `TintColors[2]`

Channel intensity (0-1) controls blend strength. Multiple channels blend proportionally.

## Transformations

Applied in order:
1. **Tinting** - If mask present, applies color tinting
2. **Scaling** - Resizes sprite by scale factor
3. **Offset** - Translates position
4. **Alpha Blending** - Composites with lower layers

## Example

```csharp
var layer = new ImageStackLayer {
    Sprite = hairSprite,
    Mask = hairMask,
    Offset = new Vector2(0, 10),
    Scale = 1.2f,
    Order = 5
};
```

## Notes
- Rotation property exists but not yet implemented in compositor
- Mask must be same dimensions as sprite
- Both sprite and mask textures require Read/Write enabled
