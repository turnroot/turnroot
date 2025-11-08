# Portrait Editor Window

**Namespace:** `Assets.Prototypes.Characters.Subclasses.Editor`  
**Inherits:** `StackedImageEditorWindow<CharacterData, Portrait>`  
**Menu:** `Window > Portrait Editor`

Editor tool for creating and editing character portraits with real-time preview. This is a concrete implementation of the generic `StackedImageEditorWindow` base class.

## Architecture

The `PortraitEditorWindow` is a minimal implementation (~30 lines) that extends `StackedImageEditorWindow<CharacterData, Portrait>`. All UI and functionality is inherited from the base class.

### Implementation

```csharp
public class PortraitEditorWindow : StackedImageEditorWindow<CharacterData, Portrait>
{
    protected override string WindowTitle => "Portrait Editor";
    protected override string OwnerFieldLabel => "Character";
    
    protected override Portrait[] GetImagesFromOwner(CharacterData owner)
    {
        return owner?.Portraits;
    }
}
```

## Window Layout

```
┌─────────────────────────────────────┐
│ Character: [Dropdown]               │
│ Portrait Index: [0]                 │
│                                     │
│ ┌─────────────────────────────────┐ │
│ │      Live Preview (256x256)     │ │
│ │          [Preview Image]        │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Portrait ID: p{guid}                │
│ Owner: CharacterName                │
│                                     │
│ ImageStack: [ObjectField]           │
│ [Create New ImageStack]             │
│ [Initialize Portrait]               │
│                                     │
│ Key: [text field]                   │
│ [Generate New Key]                  │
│                                     │
│ === Layers ===                      │
│ [Add Layer] [Refresh Preview]       │
│ ┌─────────────────────────────────┐ │
│ │ Layer 0:                        │ │
│ │   Sprite: [ObjectField]         │ │
│ │   Mask: [ObjectField]           │ │
│ │   Offset: (0, 0)                │ │
│ │   Scale: 1.0                    │ │
│ │   Order: 0                      │ │
│ │   [Delete]                      │ │
│ └─────────────────────────────────┘ │
│                                     │
│ === Tint Colors ===                 │
│ Tint 1 (Red): [Color]               │
│ Tint 2 (Green): [Color]             │
│ Tint 3 (Blue): [Color]              │
│ [Update from Character Colors]      │
│                                     │
│ [Save & Render Portrait]            │
│                                     │
│ Auto Refresh: [✓]                   │
└─────────────────────────────────────┘
```

## Workflow

### Creating New Portrait

1. Select character from dropdown
2. Click "Initialize Portrait" at unused index
3. Assign or create ImageStack
4. Set portrait key (or generate)
5. Add layers (sprites + masks)
6. Adjust layer transforms (offset, scale, order)
7. Set tint colors
8. Preview updates automatically
9. Click "Save & Render Portrait"
10. PNG saved and sprite asset created

### Editing Existing Portrait

1. Select character
2. Select portrait index
3. Modify layers/colors/transforms
4. Preview updates
5. Save when satisfied

### Layer Ordering

- Layers rendered by `Order` property (low to high)
- Lower order = rendered first (behind)
- Higher order = rendered last (in front)
- Negative orders allowed

## Technical Details

### Inherited Functionality

All features are provided by `StackedImageEditorWindow<TOwner, TStackedImage>`:

- **Image Selection**: Dropdown to select from owner's image array
- **Metadata Editing**: Key (filename) editing and generation
- **Image Stack Management**: Assign ImageStack, view layers
- **Owner Management**: View/update owner reference
- **Tint Colors**: Edit 3 tint colors, reset, update from owner
- **Layer Inspection**: View layer properties (sprite, mask, order, offset, scale, rotation)
- **Preview**: Real-time compositing with auto-refresh toggle
- **Render & Save**: Export to PNG and create sprite asset

### Creating Custom Editors

To create a new stacked image editor for a different owner type:

```csharp
public class MyImageEditorWindow : StackedImageEditorWindow<MyOwnerType, MyImageType>
{
    protected override string WindowTitle => "My Image Editor";
    protected override string OwnerFieldLabel => "My Owner";
    
    [MenuItem("Window/My Image Editor")]
    public static void ShowWindow()
    {
        GetWindow<MyImageEditorWindow>("My Image Editor");
    }
    
    protected override MyImageType[] GetImagesFromOwner(MyOwnerType owner)
    {
        return owner?.Images;
    }
}
```

---

## See Also

- **[StackedImage](../Characters/Portraits/StackedImage.md)** - Base stacked image system
- **[Portrait](../Characters/Portraits/Portrait.md)** - Character-specific implementation
- **[ImageStack](../Characters/Portraits/ImageStack.md)** - Layer container
- **[ImageCompositor](ImageCompositor.md)** - Compositing utility
- `ImageCompositor.CompositeImageStackLayers()` - Core rendering
- `AssetDatabase` - Asset management

## Limitations

- Rotation not yet implemented in compositor
- Single undo operation per save
- Preview quality lower than final render
- No zoom/pan in preview window