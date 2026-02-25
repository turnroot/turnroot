using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills; // for SkillDebug and executor access
using Turnroot.Utilities;
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
            // Get the condition value from connected node
            object raw = GetInputValue("condition", new BoolValue());
            BoolValue conditionValue;
            if (raw is BoolValue bv)
            {
                conditionValue = bv;
            }
            else
            {
                "FlowIfNode: condition input was not a BoolValue, treating as false".LogWarning();
                conditionValue = new BoolValue();
            }

            if (!conditionValue.value)
            {
                context.Flags.IsInterrupted = true;
            }
            else
            {
                // advance immediately rather than waiting for external Proceed call
                var executor = context.GetCustomData<SkillGraphExecutor>("_executor");
                executor?.Proceed();
            }
        }
    }
}
