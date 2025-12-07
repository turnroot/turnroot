# SkillBadgeEditorWindow — short ref

Editor for SkillBadge assets; uses `StackedImageEditorWindow<Skill, SkillBadge>`.

Open
- Menu: Window → Turnroot → Skill Badge Editor
- Button on Skill asset: Create Badge

Common actions
- Generate key, assign ImageStack, update or reset tints, render sprite

Where to look
- Source: `Assets/TurnrootFramework/Skills/Components/Badges/Editor/SkillBadgeEditorWindow.cs`

Key methods
- Overrides `GetImagesFromOwner(Skill)` to return badge assets for a skill
- Uses base editor helpers: `UpdateCurrentImage()`, `RefreshPreview()`, and `SetImageStack()` on the badge

See also
- [StackedImageEditorWindow](../Tools/StackedImageEditorWindow.md) — base window
- [SkillBadge](./SkillBadge.md) — badge implementation
