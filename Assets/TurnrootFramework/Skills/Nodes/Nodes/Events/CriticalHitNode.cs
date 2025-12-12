using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

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
            if (context?.UnitInstance == null)
            {
                Debug.LogWarning("CriticalHit: No unit instance in context");
                return;
            }

            var brain = GetBrain.Get();
            if (brain != null)
            {
                brain.InvokeCriticalHit(context.UnitInstance);
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
