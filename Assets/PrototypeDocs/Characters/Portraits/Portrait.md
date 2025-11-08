# Portrait

**Namespace:** `Assets.Prototypes.Characters.Subclasses`  
**Type:** `[Serializable]`

Represents a compositable character portrait with layer-based rendering and color tinting.

## Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Owner` | `Character` | Read-only | Owning character |
| `ImageStack` | `ImageStack` | Read-only | Layer stack for compositing |
| `Key` | `string` | Read-only | Unique filename key for saved sprite |
| `RuntimeSprite` | `Sprite` | Read-only | Generated sprite (not serialized) |
| `SavedSprite` | `Sprite` | Read-only | Persistent sprite asset reference |
| `Id` | `Guid` | Read-only | Unique portrait identifier |
| `TintColors` | `Color[]` | Read-only | Array of 3 tint colors for layer masking |

## Constructor

```csharp
public Portrait()
```
Creates a new portrait with a unique GUID and generated key.

## Methods

### Configuration
```csharp
void SetOwner(Character owner)
```
Assigns the owning character and updates tint colors from character's accent colors.

```csharp
void SetKey(string key)
```
Sets the filename key for saved sprite. If empty/null, generates a new GUID-based key.

### Color Management
```csharp
void UpdateTintColorsFromOwner()
```
Synchronizes tint colors from owner's `AccentColor1/2/3` properties. Called automatically on deserialization and owner assignment.

### Rendering
```csharp
void Render()
```
Composites all layers, saves PNG to disk, and creates sprite asset.
- Saves to: `Assets/Resources/GameContent/Graphics/Portraits/{Key}.png`
- Configures texture as "Single Sprite" type
- Updates `SavedSprite` property
- Generates `RuntimeSprite` for preview

```csharp
Texture2D CompositeLayers()
```
Performs layer compositing with tinting and transformations.
- **Returns:** Composited 512x512 RGBA32 texture

### Utility
```csharp
string ToString()
```
Returns compact identifier: `p{GUID}`

```csharp
string Identify()
```
Returns detailed string: `Portrait(ID: {guid}, Owner: {name}, Key: {key})`

## Lifecycle

### OnAfterDeserialize
Automatically called by Unity after asset load:
- Parses GUID from stored string
- Generates key if empty
- Initializes tint color array
- Updates tint colors from owner

## Rendering Pipeline

1. **Composite Layers** - Blends sprites with masks, tints, scale, offset, rotation
2. **Apply Tinting** - Uses RGB channels in mask to blend 3 tint colors
3. **Save PNG** - Encodes to PNG and writes to Resources folder
4. **Configure Import** - Sets texture type to Sprite (Single mode)
5. **Load Asset** - Loads sprite from AssetDatabase
6. **Assign Reference** - Stores in `SavedSprite` for runtime use

## Tinting System

Tint colors map to mask RGB channels:
- **Red Channel** → `TintColors[0]` (AccentColor1)
- **Green Channel** → `TintColors[1]` (AccentColor2)
- **Blue Channel** → `TintColors[2]` (AccentColor3)

Higher channel values = stronger tint. Multiple channels blend proportionally.

## Notes
- Only `Owner` and `ImageStack` are editable in inspector
- Other fields hidden with `[HideInInspector]`
- Use **Portrait Editor Window** (Tools > Portrait Editor) for full editing
- Tint colors default to white if owner not set
