# StackedImage

**Namespace**: `Assets.Prototypes.Graphics2D`  
**Type**: `[Serializable]` abstract class with generic parameter  
**Generic**: `StackedImage<TOwner> where TOwner : UnityEngine.Object`  
**Source**: `Assets/Prototypes/Graphics2D/Components/StackedImage.cs`

## Description

Abstract base class for compositable layered images with owner reference and tint color support. Manages image stack composition, file saving, and sprite asset generation. Generic owner type enables type-safe integration with various asset types.

## Generic Parameter

| Parameter | Constraint | Description |
|-----------|------------|-------------|
| `TOwner` | `: UnityEngine.Object` | Type of asset that owns this image (e.g., `Skill`, `CharacterData`) |

## Implementations

- **[SkillBadge](../Skills/SkillBadge.md)** - `StackedImage<Skill>` for skill badge graphics
- **Portrait** - `StackedImage<CharacterData>` for character portraits

## Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Owner` | `TOwner` | Read-only | Asset that owns this image |
| `ImageStack` | `ImageStack` | Read-only | Container of layers to composite |
| `Key` | `string` | Read-only | Unique filename for saved sprite |
| `RuntimeSprite` | `Sprite` | Read-only | Non-serialized preview sprite (not saved) |
| `SavedSprite` | `Sprite` | Read-only | Serialized sprite asset reference |
| `Id` | `Guid` | Read-only | Unique identifier (persisted via `_idString`) |
| `TintColors` | `Color[]` | Read-only | Array of 3 tint colors for RGB mask channels |

**Note**: All properties read-only. Use methods (`SetOwner`, `SetKey`) to modify.

## Methods

### Render()

```csharp
public void Render()
```

Main rendering pipeline: composites layers, saves to file, loads sprite asset.

**Validation**:
- Checks `_imageStack` is assigned
- Ensures `_key` is valid (auto-generates if empty)

**Pipeline**:
```
1. CompositeLayers()   → Creates Texture2D
2. Create RuntimeSprite → Sprite.Create() for preview
3. SaveToFile()        → Writes PNG to disk
4. LoadSavedSprite()   → Configures importer, loads sprite
```

**Editor-Only**: `SaveToFile()` and `LoadSavedSprite()` use `#if UNITY_EDITOR` for AssetDatabase operations.

### CompositeLayers()

```csharp
public Texture2D CompositeLayers()
```

Composites ImageStack layers into single texture.

**Returns**: `Texture2D` with all layers composited, or `null` if no layers.

**Process**:
1. Validates ImageStack and layers exist
2. Ensures key is set (auto-generates if needed)
3. Creates 512x512 base texture with transparent background
4. Extracts layer array and mask array
5. Calls `ImageCompositor.CompositeImageStackLayers()`
6. Returns composited texture

**Note**: Creates new texture each call (not cached).

### ToString()

```csharp
public override string ToString()
```

**Returns**: `"p{Guid}"` format (e.g., `"p12345678-1234-1234-1234-123456789abc"`)

### Identify()

```csharp
public string Identify()
```

**Returns**: Detailed debug string with ID, owner name, and key.

**Example**: `"StackedImage(ID: 12345678-..., Owner: Fireball, Key: skillBadge_guid)"`

## Lifecycle

### Deserialization

`OnAfterDeserialize()` called by Unity after loading:

1. **Parse GUID**: Converts `_idString` to `_id` (generates new if empty)
2. **Auto-generate Key**: Sets `_key` if empty using GUID
3. **Initialize Tint Colors**: Ensures array has 3 elements (white default)
4. **Sync Colors**: Calls `UpdateTintColorsFromOwner()`

## Technical Details

### File Paths

**Save Directory Structure**:
```
Assets/
  Resources/
    GameContent/
      Graphics/
        {GetSaveSubdirectory()}/
          {Key}.png
```

**Example Paths**:
- `Assets/Resources/GameContent/Graphics/SkillBadges/fireball_badge.png`
- `Assets/Resources/GameContent/Graphics/Portraits/hero_portrait_001.png`

### Tint Color System

Three-color tinting via RGB mask channels:

| Mask Channel | TintColors Index | Description |
|--------------|------------------|-------------|
| Red (R) | `TintColors[0]` | Primary tint color |
| Green (G) | `TintColors[1]` | Secondary tint color |
| Blue (B) | `TintColors[2]` | Tertiary tint color |

Each layer's mask sprite defines color intensity per channel. See **[ImageCompositor](../Tools/ImageCompositor.md)** for algorithm.

### Serialization Fields

| Field | Type | Attributes | Purpose |
|-------|------|------------|---------|
| `_owner` | `TOwner` | `[SerializeField]` | Owner reference |
| `_imageStack` | `ImageStack` | `[SerializeField]` | Layer container |
| `_key` | `string` | `[SerializeField, HideInInspector]` | Filename |
| `_runtimeSprite` | `Sprite` | `[NonSerialized]` | Preview sprite (not saved) |
| `_savedSprite` | `Sprite` | `[SerializeField, HideInInspector]` | Saved sprite asset |
| `_idString` | `string` | `[SerializeField, HideInInspector]` | GUID string for persistence |
| `_tintColors` | `Color[]` | `[SerializeField, HideInInspector]` | Tint colors (3 elements) |

**GUID Persistence**: `Guid` type not serializable, so persisted as `_idString` and parsed in `OnAfterDeserialize()`.

---

## See Also

- **[SkillBadge](../Skills/SkillBadge.md)** - Skill badge implementation
- **[ImageStack](../Characters/Portraits/ImageStack.md)** - Layer container
- **[ImageCompositor](../Tools/ImageCompositor.md)** - Compositing and tinting
- **[StackedImageEditorWindow](../Tools/StackedImageEditorWindow.md)** - Base editor window
