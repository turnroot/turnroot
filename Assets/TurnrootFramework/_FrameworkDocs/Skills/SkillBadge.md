# SkillBadge — short ref

SkillBadge is a `StackedImage<Skill>` used to generate badge sprites from an `ImageStack` and the skill's accent colors.

Key points
- Accent colors map to mask RGB channels for badge tinting
- `Render()` composites layers and writes a sprite asset
- Edited via `SkillBadgeEditorWindow` for preview and layered edits

Where to look
- Source: `Assets/TurnrootFramework/Skills/Components/Badges/SkillBadge.cs`

Public methods
- `UpdateTintColorsFromOwner()` — syncs AccentColor1/2/3 to tints
- `Render()` / `CompositeLayers()` (inherited from `StackedImage<T>`) — composite & save

See also
- [Skill](./Skill.md) — Skill asset using a SkillBadge
- [StackedImageEditorWindow](../Tools/StackedImageEditorWindow.md) — base editor for editing stacked images
