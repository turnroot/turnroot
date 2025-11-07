# Skill Node System

**Framework**: xNode 1.8+  
**Namespace**: `Assets.Prototypes.Skills.Nodes`  
**Location**: `Assets/Prototypes/Skills/Nodes/`

## Overview

Visual node-based execution system for skill behavior. Supports both execution flow and data passing through typed ports.

## Core Architecture

### SkillGraph
**Type**: `NodeGraph`  
**Source**: `Core/SkillGraph.cs`

Container asset for skill behavior nodes. Creates via **Right-click → Create → Skill Graph**.

**Methods**:
- `Execute(SkillExecutionContext)` - Starts graph execution at entry nodes
- `Validate(out string)` - Validates graph structure

### SkillNode
**Type**: Abstract `Node`  
**Source**: `Core/SkillNode.cs`

Base class for all skill nodes. All custom nodes inherit from this.

**Ports**:
- `ExecutionFlow execIn` - Input execution flow (hidden in subclasses)
- Subclasses add custom `execOut` and data ports

**Methods**:
- `Execute(SkillExecutionContext)` - Override for node logic
- `SignalComplete(SkillExecutionContext)` - Call when node finishes

**Events**:
- `OnNodeExecute` - UnityEvent fired when node executes

### SkillExecutionContext
**Type**: `class`  
**Source**: `Core/SkillExecutionContext.cs`

Runtime data passed between nodes during execution.

**Properties**:
- `UnitInstance` - CharacterInstance that cast the skill (caster)
- `Targets` - List<CharacterInstance> receiving skill effects (enemies)
- `CustomData` - Dictionary<string, object> for shared state between nodes
  - Example keys: `"IsCriticalHit"`, `"FollowUpDisabled"`, `"BattleOrderModified"`

### SkillGraphExecutor
**Type**: Static utility  
**Source**: `Core/SkillGraphExecutor.cs`

Executes skill graphs by traversing ExecutionFlow connections.

**Methods**:
- `Execute(SkillGraph, SkillExecutionContext)` - Starts graph execution

## Socket Types

All socket types are serializable structs with `Value` property:

| Type | Description | Port Color |
|------|-------------|------------|
| `ExecutionFlow` | Execution sequencing | Violet (#8b5cf6) |
| `BoolValue` | Boolean data | Sky (#0ea5e9) |
| `FloatValue` | Numeric data | Teal (#14b8a6) |
| `StringValue` | Text data | Orange (#f97316) |

## Node Categories

Nodes auto-tint based on subfolder:

| Folder | Category | Color | Purpose |
|--------|----------|-------|---------|
| `Nodes/Flow/` | Flow | Indigo #3730a3 | Entry points, execution control |
| `Nodes/Math/` | Math | Blue #1e40af | Calculations, operations |
| `Nodes/Events/` | Events | Purple #6b21a8 | Stat modification, damage, effects |
| `Nodes/Conditions/` | Conditions | Violet #5b21b6 | Branching logic, comparisons |
| `Nodes/Values/` | Values | Cyan | Data providers (constants, variables) |

**Override**: Add `[NodeCategory(NodeCategory.Flow)]` to manually set category.

## Event Nodes Reference

Event nodes perform actions during skill execution. Located in `Assets/Prototypes/Skills/Nodes/Nodes/Events/`.

### Stat Modification
- **AffectUnitStat** - Modify caster's stats (BoolValue input, stat dropdown)
- **AffectEnemyStat** - Modify target stats with optional "all enemies" mode (BoolValue affectAllTargets input)
- **AffectAdjacentAllyStat** - Modify adjacent ally stats (placeholder)

### Combat Actions  
- **DealAdditionalDamage** - Deal damage to target(s) (multi-target support)
- **KillTarget** - Instantly kill target(s) by setting Health to 0 (multi-target support)
- **CriticalHit** - Set `IsCriticalHit` flag in CustomData for current attack
- **DisableEnemyFollowup** - Prevent enemy from performing follow-up attack
- **DealDebuff** - Apply status debuff to target(s) (multi-target, uses debuffTypePlaceholder)
- **DealDebuffAreaOfEffect** - Apply debuff to all targets in radius (uses debuffTypePlaceholder)
- **AreaOfEffectDamage** - Deal damage to all targets in radius

### Battle Order & Turn Mechanics
- **ChangeBattleOrder** - Modify follow-up attack mechanics
  - OrderEffectType: GuaranteeFollowup, PreventFollowup, ModifySpeedThreshold, AttackFirst
  - Integrates with speed-based follow-up attack system
- **TakeAnotherTurn** - Set flag to grant caster an extra turn

### Movement & Positioning
- **MoveUnit** - Move caster to target location (placeholder for pathfinding integration)
- **SwapUnitWithTarget** - Swap positions with target (placeholder for tile system)
- **UnmountEnemy** - Force enemy to dismount (cavalry units)

### Resources & Items
- **GainGold** - Add gold to player's resources
- **Steal** - Attempt to steal item from target (placeholder for RNG integration)
- **AffectUnitWeaponUses** - Modify weapon durability (increase/decrease uses)

### Multi-Target Pattern
Many Event nodes support the "affect all targets" pattern:
```csharp
[Input(connectionType: ConnectionType.Override)] 
public BoolValue affectAllTargets;

[Tooltip("Test: affect all when input unconnected")]
public bool testAffectAll = false;
```
When `affectAllTargets` is true, the node loops through all entries in `context.Targets`. Otherwise, only affects first target.

**Nodes with Multi-Target**:
- AffectEnemyStat
- KillTarget
- DealAdditionalDamage
- DealDebuff
- DealDebuffAreaOfEffect (radius-based instead)

## Creating Custom Nodes

```csharp
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

// Auto-tints based on file location
[CreateNodeMenu("Math/Add Numbers")]
public class AddNumbers : SkillNode
{
    [Input] public FloatValue a;
    [Input] public FloatValue b;
    [Output] public FloatValue result;
    [Output] public ExecutionFlow execOut;

    public override void Execute(SkillExecutionContext context)
    {
        float aVal = GetInputValue<FloatValue>("a", new FloatValue()).Value;
        float bVal = GetInputValue<FloatValue>("b", new FloatValue()).Value;
        result = new FloatValue { Value = aVal + bVal };
        
        SignalComplete(context); // Continue execution
    }

    public override object GetValue(NodePort port)
    {
        return port.fieldName == "result" ? result : null;
    }
}
```

**File Location**: Save in `Assets/Prototypes/Skills/Nodes/Nodes/Math/` for blue tint.

## Execution Flow

1. **Entry**: Graph executor finds nodes with no `execIn` connections
2. **Execute**: Calls `node.Execute(context)` 
3. **Signal**: Node calls `SignalComplete(context)` when done
4. **Continue**: Executor follows `execOut` connections
5. **Complete**: All execution paths finish

## Integration

Link graph to skill:
1. Create SkillGraph asset
2. Add nodes and connect them
3. Assign graph to `Skill.BehaviorGraph`
4. Call `skill.BehaviorGraph.Execute(context)` at runtime

---

## See Also

- **[Skill](../Skill.md)** - Main skill asset
- **[SkillExecutionContext API](SkillExecutionContext.md)** - Context properties reference
