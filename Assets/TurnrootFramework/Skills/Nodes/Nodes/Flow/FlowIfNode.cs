using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Conditional flow node that interrupts execution if the input condition is false.
    /// </summary>
    [NodeLabel("Proceed if condition is true")]
    [CreateNodeMenu("Flow/Flow If")]
    public class FlowIfNode : SkillNode
    {
        [Input(ShowBackingValue.Never, ConnectionType.Override)]
        public ExecutionFlow InFlow;

        [Input(ShowBackingValue.Always, ConnectionType.Override)]
        public BoolValue condition;

        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow OutFlow;

        public override void Execute(BattleContext context)
        {
            // GetInputValue uses the backing field as fallback when nothing is connected.
            BoolValue conditionValue = GetInputValue("condition", condition);

            if (!conditionValue.value)
            {
                // Mark the flow as interrupted so ContinueFromNode stops here.
                context.Flags.IsInterrupted = true;
            }
            // If true, do nothing — the executor's recursive ContinueFromNode handles
            // continuation automatically.
        }
    }
}
