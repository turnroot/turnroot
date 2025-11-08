using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Swap Unit With Target")]
    [NodeLabel("Swaps the position of the unit with the target")]
    public class SwapUnitWithTargetNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(BattleContext context)
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

            // Store swap command in CustomData
            var swapData = new { UnitId = context.UnitInstance.Id, TargetId = target.Id };
            context.SetCustomData("SwapPositions", swapData);

            Debug.Log("SwapUnitWithTarget: Will swap positions with target");
        }
    }
}
