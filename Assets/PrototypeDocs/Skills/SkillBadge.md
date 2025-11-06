# SkillBadge

**Namespace**: `Assets.Prototypes.Skills.Components.Badges`  
**Type**: `[Serializable]` class  
**Inherits**: `StackedImage<Skill>`  
**Source**: `Assets/Prototypes/Skills/Components/Badges/SkillBadge.cs`

## Description

Specialized `StackedImage` implementation for skill badge graphics. Inherits layered image composition and tinting from `StackedImage<T>`, configured for skills as the owner type.

## Inherited Properties

From `StackedImage<Skill>`:

| Property | Type | Description |
|----------|------|-------------|
| `Owner` | `Skill` | The skill that owns this badge |
| `ImageStack` | `ImageStack` | Container of layers to composite |
| `Key` | `string` | Unique filename for saved sprite |
| `RuntimeSprite` | `Sprite` | Non-serialized preview sprite |
| `SavedSprite` | `Sprite` | Serialized sprite asset reference |
| `Id` | `Guid` | Unique identifier |
| `TintColors` | `Color[]` | Array of 3 tint colors for RGB mask channels |

## Inherited Methods

From `StackedImage<Skill>`:

| Method | Description |
|--------|-------------|
| `SetOwner(Skill)` | Sets owner and updates tint colors |
| `SetKey(string)` | Sets filename key (auto-generates if null/empty) |
| `Render()` | Composites layers, saves PNG, loads sprite asset |
| `CompositeLayers()` | Returns composited Texture2D for preview |
| `Identify()` | Returns debug string with ID, owner, key |

## Tinting System

Badge tinting uses RGB mask channels:

```
Mask Red Channel   → TintColors[0] (Skill.AccentColor1)
Mask Green Channel → TintColors[1] (Skill.AccentColor2)
Mask Blue Channel  → TintColors[2] (Skill.AccentColor3)
```

Each layer's mask sprite defines which regions receive which tint colors. See **[ImageCompositor](../Tools/ImageCompositor.md)** for tinting algorithm details.

## Editor Integration

Edited via `SkillBadgeEditorWindow`, which provides:
- Live preview with tint colors
- Layer management from ImageStack
- Render and save controls
- Tint color manual override

----

## See Also

- **[Skill](Skill.md)** - Parent skill container
- **[SkillBadgeEditorWindow](SkillBadgeEditorWindow.md)** - Editor tool
- **[StackedImage](../Graphics2D/StackedImage.md)** - Base class documentation
- **[ImageStack](../Characters/Portraits/ImageStack.md)** - Layer container
- **[ImageCompositor](../Tools/ImageCompositor.md)** - Tinting and compositing
