# Skill — short ref

`Skill` is a ScriptableObject representing gameplay abilities with metadata, accent colors, an optional badge (StackedImage), and a `SkillGraph` (xNode) for behavior.

Key fields
- `AccentColor1/2/3` — drives badge tinting
- `BehaviorGraph` — skill logic graph (executed with a BattleContext)
- `UnityEvent`s` — hooks for editor/runtime events (ReadyToFire, SkillTriggered, ActionCompleted)

Workflow
- Create skill, configure accents, generate Badge via editor tool, edit badge layers, render sprite

Where to look
- Source: `Assets/TurnrootFramework/Skills/Skill.cs`
- Nodes: `Assets/TurnrootFramework/Skills/Nodes` for skill graph execution

Public methods
- `CreateNewBadge()` — creates a `SkillBadge` and opens editor (editor-only)
- `ExecuteSkill(BattleContext)` — run assigned behavior graph for this skill
- `TriggerSkillEvents()` — fire UnityEvent hooks related to skill

See also
- [SkillBadge](./SkillBadge.md) — badge composition
- [Skill Node System](./Nodes/README.md) — node-based skill execution
