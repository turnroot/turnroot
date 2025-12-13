using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
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

            var brain = GetBrain.Get();
            if (brain != null)
            {
                brain.PublishCriticalHit(context.UnitInstance);
                Debug.Log(
                    $"CriticalHit: {context.UnitInstance.CharacterTemplate.DisplayName} triggered a critical hit"
                );
            }
            else
            {
                Debug.LogWarning("CriticalHit: Could not find Brain to invoke event");
            }
        }
    }
}
