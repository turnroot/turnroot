# Portrait — short ref

Composable portrait asset. Uses an `ImageStack` (layers) and `AccentColor*` from its owner to produce a sprite.

Key responsibilities
- `UpdateTintColorsFromOwner()` pulls owner colors
- `CompositeLayers()` builds Texture2D, `Render()` saves and generates sprite
- `Key` defines filename; `SavedSprite` stores created asset

Integration
- `CharacterData` owns Portraits; editor tools call `Render()` to update saved sprites
- Tinting done by `ImageCompositor`

Where to look
- Source: `Assets/TurnrootFramework/Characters/Portraits/Portrait.cs`
