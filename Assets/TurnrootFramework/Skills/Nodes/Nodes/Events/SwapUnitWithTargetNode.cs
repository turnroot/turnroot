using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Swap Unit With Target")]
    [NodeLabel("Swaps the position of the unit with the target")]
    public class SwapUnitWithTargetNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(BattleContext context)
        {
            if (
                !ValidationHelper.ValidateNotNull(
                    context?.UnitInstance,
                    nameof(context.UnitInstance)
                )
            )
            {
                return;
            }

            if (!ValidationHelper.ValidateNotNullOrEmpty(context.Targets, nameof(context.Targets)))
            {
                return;
            }

            var target = context.Targets[0];
            if (!ValidationHelper.ValidateNotNull(target, nameof(target)))
            {
                return;
            }

            // Store swap command in CustomData
            var swapData = new { UnitId = context.UnitInstance.Id, TargetId = target.Id };
            context.SetCustomData("SwapPositions", swapData);

            Debug.Log("SwapUnitWithTarget: Will swap positions with target");
        }
    }
}
