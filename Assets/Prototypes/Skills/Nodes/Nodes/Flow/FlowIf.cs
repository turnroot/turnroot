using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[NodeLabel("Proceed if condition is true")]
[CreateNodeMenu("Flow/Flow If")]
public class FlowIf : SkillNode
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)]
    public ExecutionFlow InFlow;

    [Input(ShowBackingValue.Always, ConnectionType.Override)]
    public BoolValue condition;

    [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
    public ExecutionFlow OutFlow;

    public override void Execute(BattleContext context)
    {
        // Get the condition value from connected node
        BoolValue conditionValue = GetInputValue<BoolValue>("condition", new BoolValue());

        // If condition is false, interrupt execution (don't proceed)
        if (!conditionValue.value)
        {
            context.IsInterrupted = true;
            Debug.Log($"FlowIf condition is false, stopping execution.");
        }
        // If true, execution will proceed normally when Proceed() is called
    }
}
