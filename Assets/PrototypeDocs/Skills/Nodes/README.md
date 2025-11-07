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
- `ThisUnit` - GameObject that initiated skill
- `Target` - GameObject receiving skill effect
- `DamageValue` / `HealingValue` - Numeric effect values
- `StatusEffect` - Status effect to apply
- `Duration` - Effect duration in seconds
- `CustomData` - Dictionary for custom node data

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
| `Nodes/Flow/` | Flow | Indigo #3730a3 | Entry points, event listeners |
| `Nodes/Math/` | Math | Blue #1e40af | Calculations, operations |
| `Nodes/Events/` | Events | Purple #6b21a8 | Event firing, callbacks |
| `Nodes/Conditions/` | Conditions | Violet #5b21b6 | Branching logic, comparisons |

**Override**: Add `[NodeCategory(NodeCategory.Flow)]` to manually set category.

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
