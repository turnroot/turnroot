# Stacked Image Editor Window — short ref

Generic base editor for `StackedImage<TOwner>`. Provides layer management, tinting, live preview and rendering. Derive and implement a few members to support custom assets (Portraits etc).

## Type Parameters

- `TOwner` (constraint: `UnityEngine.Object`) - Owner type (CharacterData, ItemData, etc.)
- `TStackedImage` (constraint: `StackedImage<TOwner>`) - Concrete StackedImage implementation

## Quick start
Derive `PortraitEditorWindow : StackedImageEditorWindow<CharacterData, Portrait>` and override `GetImagesFromOwner` plus title strings. Use `GetWindow<T>` to open.
## Required overrides
- `WindowTitle`, `OwnerFieldLabel` (strings)
- `GetImagesFromOwner(TOwner owner)`

## Protected Fields (Available to Derived Classes)

| Field | Type | Description |
|-------|------|-------------|
| `_currentOwner` | `TOwner` | Selected owner object |
| `_currentImage` | `TStackedImage` | Selected image being edited |
| `_selectedImageIndex` | `int` | Selected image index |
| `_autoRefresh` | `bool` | Auto-refresh preview on changes |
| `_selectedLayerIndex` | `int` | Selected layer index (-1 = none) |

## Virtual Methods (Optional Overrides)

Override these to customize behavior:

- `OnGUI()` - Main GUI rendering
- `DrawControlPanel()` - Left controls panel
- `DrawPreviewPanel()` - Right preview panel
- `DrawImageMetadataSection()` - ID/key editing
- `DrawImageStackSection()` - ImageStack assignment
- `DrawOwnerSection()` - Owner info
- `DrawTintingSection()` - Tint colors
- `DrawLayerManagementSection()` - Layer list

## Protected Helper Methods

- `UpdateCurrentImage()` - Refresh current image from owner
- `RefreshPreview()` - Recomposite and update preview


In-built features: selection, preview, layer tint/order/offset, default management, render to file.

**UI Layout:**
```
+-------------------------------+
� Window Title                   �
+-------------------------------�
� Controls    � Preview         �
� - Metadata  �  [Save Defaults] �
� - Stack     �  [Load Defaults] �
� - Owner     �  [Image]        �
� - Tints     �  [Refresh]      �
� - Layers    �  [Saved Sprite] �
�             �  [Render]       �
+-------------------------------+
```

## Customization Examples

### Add Custom Controls
``csharp
protected override void DrawControlPanel()
{
    base.DrawControlPanel();
    EditorGUILayout.Space(10);
    if (GUILayout.Button("Custom Action"))
    {
        // Your logic
    }
}
``

### Add Static Opener
``csharp
public static void OpenPortrait(CharacterData character, int index = 0)
{
    var window = GetWindow<PortraitEditorWindow>();
    window._currentOwner = character;
    window._selectedImageIndex = index;
    window.UpdateCurrentImage();
    window.RefreshPreview();
}
``

## Architecture

**Template Method Pattern:**
- Base class provides workflow (template)
- Abstract methods are customization points
- Derived classes implement type-specific details

**Benefits:**
- ~600 lines shared, ~30 lines per editor
- Bug fixes apply to all editors
- New features added once
- Type-safe with compile-time checking

---

## Where to look
- `Assets/TurnrootFramework/Graphics2D/Editor/StackedImageEditorWindow.cs`

Key methods (public/protected)
- `GetImagesFromOwner(TOwner owner)` — abstract: return image array for editor
- `SetImagesToOwner(TOwner owner, TStackedImage[] images)` — optional override to update owner images
- `UpdateCurrentImage()` — refresh selection and preview
- `RefreshPreview()` — trigger recomposition and update UI
- `DrawControlPanel()` / `DrawPreviewPanel()` — layout hooks for derived classes

See also
- [Portrait](../Characters/Portraits/Portrait.md) — concrete use-case
- [StackedImage](../Graphics2D/StackedImage.md) — base image API
- [PortraitEditorWindow source](../Tools/PortraitEditorWindow.md)
