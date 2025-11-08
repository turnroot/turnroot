# Skill

**Namespace**: `(global)`  
**Type**: `ScriptableObject`  
**Source**: `Assets/Prototypes/Skills/Skill.cs`

## Description

Main skill container asset that defines a skill's appearance, metadata, behavior events, and associated badge graphics. Integrates with the StackedImage system for visual badge composition.

## Creation

Create via Unity menu: **Assets → Create → Turnroot → Skills → Skill**

Creates a new ScriptableObject asset named "NewSkill".

## Properties

### Appearance

| Property | Type | Description |
|----------|------|-------------|
| `AccentColor1` | `Color` | Primary accent color for skill theming and badge tinting |
| `AccentColor2` | `Color` | Secondary accent color for badge tinting |
| `AccentColor3` | `Color` | Tertiary accent color for badge tinting |
| `Badge` | `SkillBadge` | Compositable badge graphic (hidden in inspector) |

### Info

| Property | Type | Description |
|----------|------|-------------|
| `SkillName` | `string` | Display name of the skill |
| `Description` | `string` | Multi-line description text |

### Behavior

| Property | Type | Description |
|----------|------|-------------|
| `BehaviorGraph` | `SkillGraph` | xNode graph defining skill execution logic (see Node System below) |
| `ReadyToFire` | `UnityEvent` | Event triggered when skill is ready to use |
| `SkillTriggered` | `UnityEvent` | Event triggered when skill is activated |
| `ActionCompleted` | `UnityEvent` | Event triggered when skill execution finishes |
| `SkillEquipped` | `UnityEvent` | Event triggered when skill is equipped to character |
| `SkillUnequipped` | `UnityEvent` | Event triggered when skill is removed from character |

## Integration with SkillBadge

The skill's three accent colors directly map to the badge's tint colors:
- `AccentColor1` → `SkillBadge.TintColors[0]` (Red mask channel)
- `AccentColor2` → `SkillBadge.TintColors[1]` (Green mask channel)
- `AccentColor3` → `SkillBadge.TintColors[2]` (Blue mask channel)

This allows skills to have visual identity that propagates to their badge graphics.

## Node System

Skills use xNode visual graphs for execution logic. Each skill references a `SkillGraph` that defines its runtime behavior through connected nodes.

### Node Categories

Nodes auto-tint based on subfolder location:
- **Flow** (`/Flow/`) - Indigo tint - Entry points (e.g., UnitAttacks)
- **Math** (`/Math/`) - Blue tint - Calculations and values
- **Events** (`/Events/`) - Purple tint - Event handlers
- **Conditions** (`/Conditions/`) - Violet tint - Branching logic

### Execution Flow

1. Graph starts at entry nodes (nodes with no ExecutionFlow inputs)
2. Each node executes via `Execute(BattleContext)` method
3. Node continues execution by calling Execute on connected output nodes
4. Graph completes when all execution paths finish or `context.IsInterrupted` is set

See **[Node System](Nodes/README.md)** for detailed node architecture and BattleContext documentation.

## Notes

### Badge Workflow

1. Create Skill asset
2. Configure accent colors
3. Click "Create Badge" button
4. Configure badge in Skill Badge Editor window
5. Assign ImageStack with layers
6. Render and save badge sprite

---

## See Also

- **[SkillBadge](SkillBadge.md)** - Badge graphics system
- **[SkillBadgeEditorWindow](SkillBadgeEditorWindow.md)** - Badge editor tool
- **[StackedImage](../Graphics2D/StackedImage.md)** - Base class for compositable graphics
- **[Character](../Characters/Character.md)** - Character system (uses skills)
