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
                    context?.Unit.UnitInstance,
                    nameof(context.Unit.UnitInstance)
                )
            )
            {
                return;
            }

            if (
                !ValidationHelper.ValidateNotNullOrEmpty(
                    context.Participants.Targets,
                    nameof(context.Participants.Targets)
                )
            )
            {
                return;
            }

            var target = context.Participants.Targets[0];
            if (!ValidationHelper.ValidateNotNull(target, nameof(target)))
            {
                return;
            }

            // Store swap command in CustomData
            var swapData = new { UnitId = context.Unit.UnitInstance.Id, TargetId = target.Id };
            context.SetCustomData("SwapPositions", swapData);

#if UNITY_EDITOR
            TurnrootLogger.Log("SwapUnitWithTarget: Will swap positions with target");
#endif
        }
    }
}
