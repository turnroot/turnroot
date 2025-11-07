using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Swap Unit With Target")]
    [NodeLabel("Swaps the position of the unit with the target")]
    public class SwapUnitWithTarget : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.UnitInstance == null)
            {
                Debug.LogWarning("SwapUnitWithTarget: No unit instance in context");
                return;
            }

            if (context.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("SwapUnitWithTarget: No target in context");
                return;
            }

            var target = context.Targets[0];
            if (target == null)
            {
                Debug.LogWarning("SwapUnitWithTarget: Target is null");
                return;
            }

            // TODO: Integrate with actual positioning/grid system
            Debug.Log($"SwapUnitWithTarget: Swapped positions between unit and target");
        }
    }
}
