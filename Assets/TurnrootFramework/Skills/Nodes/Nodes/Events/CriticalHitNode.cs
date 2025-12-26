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
                    context?.Unit.UnitInstance,
                    nameof(context.Unit.UnitInstance)
                )
            )
            {
                return;
            }

            context.Brain?.PublishCriticalHit(context.Unit.UnitInstance);
            Debug.Log(
                $"CriticalHit: {context.Unit.UnitInstance.CharacterTemplate.DisplayName} triggered a critical hit"
            );
        }
    }
}
