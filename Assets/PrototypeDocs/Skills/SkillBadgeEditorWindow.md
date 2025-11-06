# SkillBadgeEditorWindow

**Namespace**: `Assets.Prototypes.Skills.Components.Badges.Editor`  
**Type**: `EditorWindow`  
**Inherits**: `StackedImageEditorWindow<Skill, SkillBadge>`  
**Source**: `Assets/Prototypes/Skills/Components/Badges/Editor/SkillBadgeEditorWindow.cs`

## Description

Editor window for creating and editing skill badge graphics. Specialized implementation of `StackedImageEditorWindow` configured for skill badges.

## Opening the Window

### Via Menu

**Window → Turnroot → Skill Badge Editor**

Opens empty window ready for skill selection.

### Via Skill Asset

Click **"Create Badge"** button on `Skill` asset in inspector. Opens window with skill and badge pre-loaded.

### Controls

| Button | Action |
|--------|--------|
| Generate Short Key | Creates key like `stackedImage_12345678` |
| Generate Full GUID Key | Creates full GUID key |
| + Create New Image Stack | Opens save dialog for new ImageStack asset |
| Set Owner to Current Skill | Links badge to current skill |
| Reset to White | Sets all tint colors to white |
| Update from Owner | Syncs tint colors from skill accent colors |
| Refresh Preview | Manually regenerates preview |
| Render and Save to File | Exports badge to file system |

-----

## See Also

- **[Skill](Skill.md)** - Parent skill asset
- **[SkillBadge](SkillBadge.md)** - Badge data class
- **[StackedImageEditorWindow](../Tools/StackedImageEditorWindow.md)** - Base editor window class
- **[ImageStack](../Characters/Portraits/ImageStack.md)** - Layer container
- **[PortraitEditorWindow](../Tools/PortraitEditorWindow.md)** - Similar editor for character portraits
