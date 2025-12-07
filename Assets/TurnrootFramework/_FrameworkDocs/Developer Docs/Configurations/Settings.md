# Settings — short ref

Global scriptable settings that tune runtime and editor behavior.

CharacterPrototypeSettings
- Location: `Resources/GameSettings/*/CharacterPrototypeSettings`
- Used by `CharacterData.OnEnable()` to initialize defaults and by editor utilities.
- Editor hook: `OnValidate()` — updates `CharacterData` assets after changes.

GraphicsPrototypesSettings
- Location: `Resources/GraphicsPrototypesSettings`
- Controls portrait render width/height used by `StackedImage.CompositeLayers()` and `ImageStack.PreRender()`.
- Editor hook: `OnValidate()` — updates `ImageStack` assets for new render sizes.

See also
- [StackedImage](../Graphics2D/StackedImage.md) — rendering pipeline for portraits/badges
- [DefaultCharacterStats](../Characters/DefaultCharacterStats.md) — default stat initialization