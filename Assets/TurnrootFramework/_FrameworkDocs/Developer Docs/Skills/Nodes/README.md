
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

Condition nodes evaluate game state and return boolean results. Located in `Assets/TurnrootFramework/Skills/Nodes/Nodes/Conditions/`.

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
using Turnroot.Skills.Nodes;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
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

# Skill Node System — short ref

Node-based execution (xNode) for skills. Use `SkillGraph` to store connected `SkillNode`s executed with a `BattleContext`.

Key pieces
- `SkillGraph.Execute(BattleContext)` — runs nodes starting at flow entry points
- `SkillNode.Execute(BattleContext)` — override for node logic; use `GetInput*` helpers
- `BattleContext` — runtime data (caster, targets, Allies, AdjacentUnits, CustomData)

Port types
- `ExecutionFlow`, `BoolValue`, `FloatValue`, `StringValue` — typed data ports

Typical pattern
- Connect event and flow nodes to model skill effects; assign `SkillGraph` to `Skill.BehaviorGraph` and call Execute at runtime

Where to look
- Source: `Assets/TurnrootFramework/Skills/Nodes` and `Assets/TurnrootFramework/Gameplay/Combat/FundamentalComponents/Battles/BattleContext.cs`

Public methods (quick list)
- `SkillGraph.Execute(BattleContext)` — execute the graph with a battle context
- `SkillGraph.Proceed()` — continue execution from current node(s)
- `SkillNode.Execute(BattleContext)` — override to implement node behavior
- `SkillNode.GetContextFromGraph(SkillGraph)` — helper to access execution context
- `SkillNode.GetInputFloat(string, float)` / `GetInputBool(string, bool)` — input helpers
- `SkillGraphExecutor.Execute(BattleContext)` / `Proceed()` / `ContinueFromNode(SkillNode)` / `GetContext()` — runtime executor API
- `BattleContext.GetCustomData<T>(string, T)` / `SetCustomData(string, object)` — store ad-hoc data for nodes

See also
- [Skill](../Skill.md) — skill asset and runtime usage
- [SkillBadge](../SkillBadge.md) — badge graphics and composition
- `Assets/TurnrootFramework/Gameplay/Combat/FundamentalComponents/Battles/BattleContext.cs` — full API of BattleContext
