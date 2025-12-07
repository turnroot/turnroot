# Portrait Editor Window

**Namespace:** `Turnroot.Characters.Subclasses.Editor`  
**Inherits:** `StackedImageEditorWindow<CharacterData, Portrait>`  
**Menu:** `Window > Portrait Editor`

Editor tool for creating and editing character portraits with real-time preview. This is a concrete implementation of the generic `StackedImageEditorWindow` base class.

## Architecture

The `PortraitEditorWindow` is a minimal implementation (~30 lines) that extends `StackedImageEditorWindow<CharacterData, Portrait>`. All UI and functionality is inherited from the base class.

### Implementation

## Window Layout

### When No Portraits Exist
+-------------------------------------+
| Character: [Dropdown]               |
|                                     |
| Info: This Character has no portraits. |
|                                     |
| [Portrait Name Text Field] [Create] |
+-------------------------------------+

# Portrait Editor Window — short ref

Editor for managing `Portrait` assets (composition, preview, defaults). Built on `StackedImageEditorWindow<CharacterData, Portrait>`.

Key features
- Live preview and auto-refresh
- Map portrait layers to `ImageStack` and adjust offset/scale/tint/order
- Save/load character defaults (tagged layer presets)

When to use
- Create or edit portrait assets; preview output before saving sprites to Resources

Extend
- Use `StackedImageEditorWindow<TOwner,TImage>` to create editors for other stacked image types

Where to look
- Source: `Assets/TurnrootFramework/Characters/Components/Editor/PortraitEditorWindow.cs`

Key methods
- `SaveDefaults()` / `LoadDefaults()` — save load tagged layer defaults on current character
- `UpdateCurrentImage()` — set the selected image and refresh the preview
- `RefreshPreview()` — recomposite current ImageStack for live preview

See also
- [StackedImage](../Graphics2D/StackedImage.md) — base methods used by rendering
- [ImageStack](../Characters/Portraits/ImageStack.md) — layering format used by portraits