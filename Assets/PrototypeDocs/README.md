# Prototype Systems Documentation

Complete API reference for Assets/Prototypes systems.

## Core Systems

### Character System
- **[Character](Characters/Character.md)** - Main character configuration asset
- **[CharacterStats](Characters/CharacterStats.md)** - Stat system (bounded and unbounded)
- **[CharacterComponents](Characters/CharacterComponents.md)** - Pronouns, relationships, traits

### Portrait System
- **[Portrait](Characters/Portraits/Portrait.md)** - Compositable character portraits
- **[ImageStack](Characters/Portraits/ImageStack.md)** - Layer container and ImageStackLayer
- **[ImageCompositor](Tools/ImageCompositor.md)** - Static compositor utility

### Configuration
- **[Settings](Configurations/Settings.md)** - CharacterPrototypeSettings, GraphicsPrototypesSettings
- **[ExperienceTypes](Configurations/Components/ExperienceTypes.md)** - Combat experience and weapon types

### Editor Tools
- **[PortraitEditorWindow](Tools/PortraitEditorWindow.md)** - Portrait editing interface

---

## Quick Reference

### Creating a Character

```csharp
// Create asset via Unity menu
Assets > Create > Character > Character

// Configure properties in the Unity Inspector
// All properties are read-only at runtime
```

### Creating a Portrait

```csharp
// Via Portrait Editor Window (recommended)
Tools > Portrait Editor
```

### Using Tinting

```csharp
// Note: AccentColor properties are read-only
// Set them in the Unity Inspector on the Character asset

// In Portrait Editor, click "Update from Character Colors"
// to sync tint colors from character

// Add masked layer in ImageStack
var layer = new ImageStackLayer {
    Sprite = hairSprite,
    Mask = hairMask, // R=red regions, G=blue regions, B=yellow regions
    Order = 5
};
```

### Querying Stats

```csharp
// Get stat by type
BoundedCharacterStat hp = character.GetBoundedStat(BoundedStatType.Health);
CharacterStat str = character.GetUnboundedStat(UnboundedStatType.Strength);

// Use values
int currentHp = hp; // Implicit conversion
int totalStr = str.Get();

// Check bounded stat ratio
if (hp.Ratio < 0.5f) {
    // Low health
}
```

### Working with Pronouns

```csharp
var pronouns = new Pronouns("she");

// Template text
string text = "{they} found {their} sword and gave it to {them}";
string result = pronouns.Use(text);
// Result: "she found her sword and gave it to her"

// Direct access
string subject = pronouns.Singular; // "she"
```

### Support Relationships

```csharp
// Add relationship
character.AddSupportRelationship(otherCharacter);

// Query
var support = character.GetSupportRelationship(otherCharacter);
if (support != null) {
    support.SupportPoints += 10;
    
    if (support.CurrentLevel < support.MaxLevel) {
        // Check for level up
    }
}
```

---

## System Architecture

### Character Data Flow
```
CharacterPrototypeSettings
        ↓
    Character
        ↓
   Portrait[] ─→ ImageStack ─→ ImageStackLayer[]
        ↓                           ↓
  RuntimeSprite              Sprite + Mask
  SavedSprite                     ↓
        ↓                   ImageCompositor
   Game Runtime        ←─────────┘
```

### Portrait Rendering Pipeline
```
1. Portrait.Render()
2. ├─→ UpdateTintColorsFromOwner() [Skipped on render]
3. ├─→ CompositeLayers()
4. │   └─→ ImageCompositor.CompositeImageStackLayers()
5. │       ├─→ TintSpritePixels() [per layer with mask]
6. │       ├─→ Scale + Offset transformations
7. │       └─→ Alpha blend to base texture
8. ├─→ SaveToFile() [PNG encode and write]
9. └─→ LoadSavedSprite()
10.     ├─→ Configure TextureImporter (Sprite type)
11.     └─→ Load from AssetDatabase
```

### Stat System Hierarchy
```
CharacterStat (unbounded)
    ├─ StatType: UnboundedStatType
    ├─ Current: float
    └─ Bonus: float

BoundedCharacterStat (clamped)
    ├─ StatType: BoundedStatType
    ├─ Current: float (clamped)
    ├─ Bonus: float
    ├─ Max: float
    └─ Min: float
```

---

## Common Patterns

### Editor Window Pattern
```csharp
public class MyEditorWindow : EditorWindow
{
    [MenuItem("Tools/My Window")]
    static void ShowWindow() {
        GetWindow<MyEditorWindow>("My Window");
    }
    
    void OnGUI() {
        // Draw UI
    }
}
```

### ScriptableObject Settings
```csharp
var settings = Resources.Load<SettingsType>("Path/In/Resources");
if (settings != null) {
    // Apply settings
}
```

### Serializable Class Pattern
```csharp
[Serializable]
public class MyClass
{
    [SerializeField] private int _value;
    public int Value => _value;
}
```

---

## File Locations

### Assets
```
Assets/
  Prototypes/
    Characters/
      Character.cs
      CharacterPrototypeSettings.cs
      Components/
        Portrait.cs
        Pronouns.cs
        SupportRelationship.cs
        HereditaryTraits.cs
        CharacterWhich.cs
        Stats/
          CharacterStat.cs
          BoundedCharacterStat.cs
          StatTypes.cs
        Editor/
          PortraitEditorWindow.cs
          Property Drawers...
    Graphics2D/
      GraphicsPrototypesSettings.cs
      Components/
        ImageStackLayer.cs
        Portrait/
          ImageStack.cs
  AbstractScripts/
    Graphics2D/
      ImageCompositor.cs
```

### Resources
```
Assets/Resources/
  GameSettings/
    CharacterPrototypeSettings.asset
  GraphicsPrototypesSettings.asset
  GameContent/
    Graphics/
      Portraits/
        {key}.png [Generated portraits]
```

---

## Best Practices

### Performance
- Enable Read/Write only on textures used in compositing
- Keep layer counts reasonable (<20 layers)
- Use appropriate texture sizes (512x512 for portraits)
- Disable auto-refresh when making multiple layer changes

### Organization
- One ImageStack per character portrait style
- Consistent layer ordering conventions (0-10: base, 11-20: features, etc.)
- Descriptive portrait keys
- Group related sprites in project folders

### Workflow
- Configure character accent colors before portraits
- Use Portrait Editor Window for all portrait editing
- Test tinting with simple masks first
- Save frequently when editing complex portraits

### Debugging
- Check Console for compositor error messages
- Verify texture Read/Write enabled
- Ensure mask dimensions match sprite dimensions
- Use Portrait.Identify() for debugging identity issues

---

## Extension Points

### Adding Stat Types
1. Add to `BoundedStatType` or `UnboundedStatType` enum
2. Implement `GetDisplayName()` and `GetDescription()` extensions
3. Add to Character stat lists as needed

### Custom Property Drawers
```csharp
[CustomPropertyDrawer(typeof(MyType))]
public class MyTypeDrawer : PropertyDrawer {
    // Implement OnGUI and GetPropertyHeight
}
```

### New Portrait Features
- Extend `ImageStackLayer` with new properties
- Update `ImageCompositor.CompositeImageStackLayers()` logic
- Add UI in `PortraitEditorWindow`

---

## Troubleshooting

### "Texture is not readable"
- Select texture → Inspector → Advanced → Enable Read/Write
- Apply changes

### "Portrait key is empty"
- Click "Generate New Key" in Portrait Editor
- Or manually set a unique key

### Tinting not working
- Verify mask sprite assigned to layer
- Check mask texture has Read/Write enabled
- Ensure tint colors not all black/white
- Verify mask and sprite dimensions match

### SavedSprite is null
- Check PNG was created in `Assets/Resources/GameContent/Graphics/Portraits/`
- Verify texture imported as Sprite (Single) type
- Look for import errors in Console

### Preview not updating
- Enable "Auto Refresh" checkbox
- Click "Refresh Preview" button
- Check Console for compositor errors
