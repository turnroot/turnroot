Generic base for layered compositable images. Handles composition (ImageStack), key generation and sprite rendering for `SkillBadge`, `Portrait`, and other `StackedImage<T>` types.

## Generic Parameter

| Parameter | Constraint | Description |
|-----------|------------|-------------|
| `TOwner` | `: UnityEngine.Object` | Type of asset that owns this image (e.g., `Skill`, `CharacterData`) |

Implementations: `Portrait` (Character), `SkillBadge` (Skill)

Key properties: `ImageStack`, `Key`, `SavedSprite`, `TintColors[3]`

Main methods
- `SetOwner(owner)` — set owner
- `SetImageStack(stack)` — assign ImageStack
- `SetKey(key)` — set filename/auto-generate
- `Render()` — composite layers → save sprite

### SetImageStack()

```csharp
public void SetImageStack(ImageStack stack)
```

Assigns an ImageStack to this StackedImage instance.

**Parameters**:
- `stack` - ImageStack containing layers to composite

**Effects**:
- Assigns `_imageStack` field

### SetKey()

```csharp
public void SetKey(string key)
```

Sets the filename key for saving sprite assets.

**Parameters**:
- `key` - Filename key (without extension), or `null`/empty to auto-generate

**Behavior**:
- If `key` is not empty: Sets `_key` directly
- If `key` is null/empty: Calls `EnsureKeyInitialized()` to generate `stackedImage_{Guid}`

### Render()

```csharp
public void Render()
```

Main rendering pipeline: composites layers, saves to file, loads sprite asset.

**Validation**:
- Checks `_imageStack` is assigned
- Ensures `_key` is valid (auto-generates if empty via `EnsureKeyInitialized()`)

**Pipeline**:
```
1. CompositeLayers()   → Creates Texture2D with proper dimensions
2. Create RuntimeSprite → Sprite.Create() for preview
3. SaveToFile()        → Writes PNG to disk
4. LoadSavedSprite()   → Configures importer, loads sprite
```

**Editor-Only**: `SaveToFile()` and `LoadSavedSprite()` use `#if UNITY_EDITOR` for AssetDatabase operations.

Composition: Composites layers to Texture2D using `ImageCompositor` (editor-only file writes + AssetDatabase calls — only when rendering in editor).

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

## Internal Helper

### EnsureKeyInitialized()

```csharp
private void EnsureKeyInitialized()
```

Internal helper that ensures `_key` is set to `"stackedImage_{Guid}"`.

**Behavior**:
- Generates new GUID if `_id` is empty
- Updates `_idString` for serialization
- Sets `_key` to `stackedImage_{_id}` format

**Used By**: Constructor, `SetKey()`, `Render()`, `CompositeLayers()`, `OnAfterDeserialize()`

**Note**: Consolidates key initialization logic to avoid duplication.

## Lifecycle

### Deserialization

`OnAfterDeserialize()` called by Unity after loading:

1. **Parse GUID**: Converts `_idString` to `_id` (generates new if empty)
2. **Auto-generate Key**: Calls `EnsureKeyInitialized()` if `_key` is empty
3. **Initialize Tint Colors**: Ensures array has 3 elements (white default)
4. **Sync Colors**: Calls `UpdateTintColorsFromOwner()`

### Constructor

`StackedImage()` constructor:

1. **Generate GUID**: Creates new `_id` and stores as `_idString`
2. **Initialize Key**: Calls `EnsureKeyInitialized()` to set default key format

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


Public methods
- `SetOwner(TOwner owner)` — set owner and update tint colors
- `SetImageStack(ImageStack stack)` — assign ImageStack asset
- `SetKey(string)` — set the saved key (filename) or auto-generate
- `Render()` — composites layers and saves sprite to Resources
- `CompositeLayers()` — returns a composited Texture2D for preview
- `Identify()` / `ToString()` — debugging helpers

See also
- [ImageCompositor](../Tools/ImageCompositor.md) — low-level pixel compositor
- [StackedImageEditorWindow](../Tools/StackedImageEditorWindow.md) — editor window for live preview and rendering
- [Portrait](../Characters/Portraits/Portrait.md) — portrait implementation (concrete `StackedImage`)
# StackedImage — short ref

---

Where to look
- `Assets/TurnrootFramework/Graphics2D/Components/StackedImage.cs`
- `Assets/TurnrootFramework/Tools/ImageCompositor.cs`
