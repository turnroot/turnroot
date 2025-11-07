# Skill Node System Architecture

## Overview
This system uses xNode to create visual node graphs that represent skill behavior. Nodes can be connected by **execution flow** (sequence order) AND **data connections** (bool, float, string values).

## Core Concepts

### 1. Socket Types
- **ExecutionFlow**: White ports - represents sequence/order (what runs next), no data passed
- **BoolValue**: For boolean data connections between nodes
- **FloatValue**: For numeric data connections between nodes
- **StringValue**: For text data connections between nodes

ExecutionFlow sockets are purely for visual sequencing. Data sockets actually pass values between nodes at runtime.

### 2. Key Classes

#### SkillNode (Base Class)
- Located in: `Assets.Prototypes.Skills.Nodes` namespace
- Inherit from this for all skill nodes
- Has `OnNodeExecute` UnityEvent that fires when node runs
- Override `Execute(SkillExecutionContext context)` to implement behavior
- Override `SignalComplete(context)` if node needs to wait for animations/timing

#### SkillExecutionContext
- Runtime data bag passed through node execution
- Contains: ThisUnit, Target(s), DamageValue, HealingValue, etc.
- Has CustomData dictionary for any extra values you need

#### SkillGraph
- NodeGraph asset - one per skill
- Call `graph.Execute(context)` to run it at runtime

#### SkillGraphExecutor  
- Handles executing nodes in sequence
- Finds entry points (nodes with no incoming execution)
- Executes each node, waits for `SignalComplete()`, then continues

### 3. How to Create a Node

```csharp
using UnityEngine;
using XNode;
using Assets.Prototypes.Skills.Nodes;

[CreateNodeMenu("Actions/My Custom Node")]
public class MyCustomNode : SkillNode
{
    // Execution flow ports
    [Input(ShowBackingValue.Never, ConnectionType.Override)]
    public ExecutionFlow execIn;
    
    [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
    public ExecutionFlow execOut;
    
    // Configuration (set in editor, evaluated at runtime)
    [SerializeField]
    private float damageMultiplier = 1.5f;
    
    public override void Execute(SkillExecutionContext context)
    {
        // Evaluate runtime data here
        // Example: Get ThisUnit's strength and calculate damage
        float damage = GetThisUnitStrength(context.ThisUnit) * damageMultiplier;
        context.DamageValue += damage;
        
        // If node completes immediately:
        SignalComplete(context);
        
        // If node needs to wait (animation, etc), call SignalComplete later
    }
    
    public override object GetValue(NodePort port)
    {
        return null; // xNode requirement
    }
}
```

### 4. Nodes with Timing/Animation

If a node Flow an animation that needs to complete before continuing:

```csharp
public override void Execute(SkillExecutionContext context)
{
    // Trigger animation
    AnimationController.PlayAttack(onComplete: () => {
        // Call this when animation finishes
        SignalComplete(context);
    });
    
    // DON'T call SignalComplete here - let animation callback do it
}
```

### 5. Usage in Skill Asset

The `Skill` ScriptableObject now has a `BehaviorGraph` field:

```csharp
// In your game code:
Skill mySkill = // ... get skill reference
SkillExecutionContext context = new SkillExecutionContext {
    ThisUnit = playerCharacter,
    Target = enemy
};
mySkill.ExecuteSkill(context);
```

## Node Examples

### Pure Sequence Node
Just Flow something and continues (like "Play SFX"):
- Has execIn and execOut ports
- Execute() does work, calls SignalComplete() immediately

### Data Evaluation Node  
Evaluates runtime data (like "Deal N% of Strength as Damage"):
- Has execIn and execOut ports
- Has serialized fields for configuration (N%)
- Execute() reads context data, calculates, stores result in context
- Calls SignalComplete()

### Branch Node
Makes a decision (like "If Target HP < 50%"):
- Has execIn port
- Has multiple execOut ports ("True", "False")
- Execute() evaluates condition
- Calls Continue FromNode() on appropriate output port

## Current Status

✅ **Implemented:**
- Base SkillNode class
- Execution context with customizable data
- SkillGraphExecutor for running graphs
- Integration with Skill asset
- Socket type definitions (ExecutionFlow, BoolValue, FloatValue, StringValue)

⚠️ **To Do:**
- Create specific node implementations (damage, healing, conditionals, etc.)
- Test with actual skill graphs
- Handle animation timing integration
- Implement data port reading/writing (GetValue override for data sockets)
