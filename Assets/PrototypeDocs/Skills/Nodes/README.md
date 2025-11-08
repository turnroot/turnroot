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
- `Execute(BattleContext)` - Starts graph execution at entry nodes
- `Validate(out string)` - Validates graph structure

### SkillNode
**Type**: Abstract `Node`  
**Source**: `Core/SkillNode.cs`

Base class for all skill nodes. All custom nodes inherit from this.

**Ports**:
- `ExecutionFlow execIn` - Input execution flow (hidden in subclasses)
- Subclasses add custom `execOut` and data ports

**Methods**:
- `Execute(BattleContext)` - Override for node logic (takes BattleContext, not SkillExecutionContext)
- `GetContextFromGraph(SkillGraph)` - Retrieves BattleContext from graph via reflection
- `GetInputFloat(string, float)` - Helper to get float input or test value
- `GetInputBool(string, bool)` - Helper to get bool input or test value

**Events**:
- `OnNodeExecute` - UnityEvent fired when node executes

### BattleContext
**Type**: `class`  
**Source**: `Assets/Prototypes/Gameplay/Combat/FundamentalComponents/Battles/BattleContext.cs`  
**Namespace**: `Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles`

Runtime data passed between nodes during battle execution. Replaces the old `SkillExecutionContext`.

**Properties**:
- `CurrentSkill` - Skill currently being executed
- `CurrentSkillGraph` - SkillGraph currently being executed
- `ActiveSkills` - List of all skills available in this battle
- `ActiveSkillGraphs` - List of all skill graphs available
- `SkillUseCount` - Dictionary tracking uses per skill
- `UnitInstance` - CharacterInstance that cast the skill (caster)
- `Targets` - List<CharacterInstance> receiving skill effects (enemies)
- `Allies` - List<CharacterInstance> of allied characters
- `AdjacentUnits` - Adjacency object with direction-based unit lookup
- `EnvironmentalConditions` - Weather, terrain, and environmental effects
- `CustomData` - Dictionary<string, object> for shared state between nodes
  - Example keys: `"IsCriticalHit"`, `"FollowUpDisabled"`, `"BattleOrderModified"`
- `IsInterrupted` - Flag to stop execution early

**Methods**:
- `GetCustomData<T>(string key, T defaultValue)` - Safely retrieve typed custom data
- `SetCustomData(string key, object value)` - Store custom data for later nodes

### SkillGraphExecutor
**Type**: Static utility  
**Source**: `Core/SkillGraphExecutor.cs`

Executes skill graphs by traversing ExecutionFlow connections.

**Methods**:
- `Execute(SkillGraph, BattleContext)` - Starts graph execution

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

**Menu Organization**: Event nodes are organized into three submenus:
- **Events/Offensive/** - Damage and debuff enemies
- **Events/Defensive/** - Protect and buff self/allies  
- **Events/Neutral/** - Positioning, utility, resources

### Offensive Events (Attack/Debuff)
- **Area of Effect Damage** - Deal damage to all targeted enemies in radius
- **Break Weapon** - Instantly break enemy's equipped weapon (multi-target)
- **Critical Hit** - Set `IsCriticalHit` flag in CustomData for current attack
- **Deal Additional Damage** - Deal damage to target(s) (multi-target support)
- **Deal Debuff** - Apply status debuff to target(s) (multi-target, uses debuffTypePlaceholder)
- **Deal Debuff Area of Effect** - Apply debuff to all targeted enemies in radius (uses debuffTypePlaceholder)
- **Disable Enemy Followup** - Prevent enemy from performing follow-up attack (multi-target)
- **First Strike** - Attack first, prevent counterattack
- **Kill Target** - Instantly kill target(s) by setting Health to 0 (multi-target support)
- **Steal** - Attempt to steal item from target (placeholder for RNG integration)
- **Unmount Enemy** - Force enemy to dismount (cavalry units, multi-target)

### Defensive Events (Protect/Buff)
- **Affect Adjacent Ally Stat** - Modify adjacent ally stats (uses AdjacentUnits from BattleContext)
- **Affect Unit Stat** - Modify caster's stats (BoolValue input, stat dropdown)
- **Area Of Effect Buff** - Buff adjacent allies within radius (configurable stat/amount/duration)
- **Cure Debuff** - Remove status effects from allies (AllDebuffs or SpecificDebuff modes)
- **Damage Reflection** - Reflect percentage of damage back to attacker
- **Negate Next Attack** - Complete damage negation on self (Miracle, Pavise, Aegis skills)
- **Negate Next Attack On Allies** - Complete damage negation on all adjacent allies
- **Reduce Damage** - Flat or percentage damage reduction

### Neutral Events (Utility/Positioning/Resources)
- **Adjust Advantage Percents** - Modify weapon triangle advantage/disadvantage values
- **Affect Enemy Stat** - Modify target stats with optional "all targeted enemies" mode (BoolValue affectAllTargets input)
- **Affect Unit Weapon Uses** - Modify weapon durability (increase/decrease uses)
- **Change Battle Order** - Modify follow-up attack mechanics
  - OrderEffectType: GuaranteeFollowup, PreventFollowup, ModifySpeedThreshold, AttackFirst
  - Integrates with speed-based follow-up attack system
- **Gain Gold** - Add gold to player's resources
- **Move Unit** - Move caster to target location (placeholder for pathfinding integration)
- **Reposition** - Move ally to adjacent tile (uses AdjacentUnits dictionary)
- **Swap Unit With Target** - Swap positions with target (placeholder for tile system)
- **Take Another Turn** - Set flag to grant caster an extra turn
- **Warp** - Teleport ally to/from caster position (uses AdjacentUnits dictionary)

### Multi-Target Pattern
Many Event nodes support the "affect all targeted enemies" pattern:
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
- DisableEnemyFollowup
- UnmountEnemy
- BreakWeapon

## Condition Nodes Reference

Condition nodes evaluate game state and return boolean results. Located in `Assets/Prototypes/Skills/Nodes/Nodes/Conditions/`.

### Stat Comparison Conditions
- **Unit Stat** - Compare caster's stat against threshold
- **Enemy Stat** - Compare target's stat against threshold
- **Enemy Would Kill Unit** - Check if enemy attack would be lethal

### Unit State Conditions
- **Has Buff** - Check if unit has specific buff active
- **Has Debuff** - Check if unit has specific debuff active
- **Is Armored** - Check if unit has armor class
- **Is Flying** - Check if unit has flying movement type
- **Is Riding** - Check if unit is mounted (cavalry)
- **Unit Kill Count** - Compare number of kills by unit

### Combat State Conditions
- **Is Initiating Combat** - True if unit started the combat
- **Is First Combat Of Turn** - True if this is unit's first combat this turn
- **Skill Use Count** - Compare how many times skill has been used

### Spatial Conditions
- **Adjacent Allies** - Check/count allies in adjacent tiles
- **Adjacent Enemies** - Check/count enemies in adjacent tiles  
- **Enemy Distance** - Compare distance to enemy target
- **Enemy Terrain Type** - Check terrain type enemy is standing on

### Classification Conditions
- **Enemy Class** - Check if enemy matches specific class
- **Environmental Conditions** - Check weather/terrain/time of day

### Turn/Time Conditions
- **Turn Count** - Compare current turn number
- **Turns Alive** - Compare how many turns unit has survived

**Pattern**: All condition nodes output `BoolValue` that can be used by **FlowIf** or other logic nodes.

**Usage Example**:
```
UnitStat (HP < 50%) → FlowIf → [If True: Desperate Strike]
                             → [If False: Normal Attack]
```

## Creating Custom Nodes

```csharp
using Assets.Prototypes.Skills.Nodes;
using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
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

    public override void Execute(BattleContext context)
    {
        float aVal = GetInputValue<FloatValue>("a", new FloatValue()).Value;
        float bVal = GetInputValue<FloatValue>("b", new FloatValue()).Value;
        result = new FloatValue { Value = aVal + bVal };
        
        // Continue execution through output port
        var flow = GetOutputValue("execOut", execOut);
        if (flow != null && flow.node != null)
        {
            ((SkillNode)flow.node).Execute(context);
        }
    }

    public override object GetValue(NodePort port)
    {
        return port.fieldName == "result" ? result : null;
    }
}
```

## Execution Flow

1. **Entry**: Graph executor finds nodes with no `execIn` connections
2. **Execute**: Calls `node.Execute(context)` with BattleContext
3. **Continue**: Node retrieves output ExecutionFlow and calls Execute on connected node
4. **Data Flow**: Output ports provide values via `GetValue(NodePort)` when queried
5. **Complete**: All execution paths finish or `context.IsInterrupted` is set

## Integration

Link graph to skill:
1. Create SkillGraph asset
2. Add nodes and connect them
3. Assign graph to `Skill.BehaviorGraph`
4. Call `skill.BehaviorGraph.Execute(battleContext)` at runtime

---

## See Also

- **[Skill](../Skill.md)** - Main skill asset
- **[BattleContext](BattleContext.md)** - Full context API reference (if detailed doc exists)
- **Adjacency System** - `Assets/Prototypes/Gameplay/Combat/FundamentalComponents/Battles/Locations/Adjacency.cs`
- **Direction Enum** - 9-direction grid system for unit positioning
