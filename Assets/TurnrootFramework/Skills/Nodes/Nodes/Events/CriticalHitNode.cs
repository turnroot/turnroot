using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Critical Hit")]
    [NodeLabel("Triggers a critical hit")]
    public class CriticalHitNode : SkillNode
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

            context.Brain?.PublishCriticalHit(context.UnitInstance);
            Debug.Log(
                $"CriticalHit: {context.UnitInstance.CharacterTemplate.DisplayName} triggered a critical hit"
            );
        }
    }
}
