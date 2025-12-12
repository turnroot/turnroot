using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Steal")]
    [NodeLabel("Steal an object from the enemy")]
    public class StealNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Tooltip("Type of item to steal (weapon, item, etc.)")]
        public string itemType = "Item";

        public override void Execute(BattleContext context)
        {
            if (context?.UnitInstance == null)
            {
                Debug.LogWarning("Steal: No unit instance in context");
                return;
            }

            if (context.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("Steal: No target in context");
                return;
            }

            var target = context.Targets[0];
            if (target == null)
            {
                Debug.LogWarning("Steal: Target is null");
                return;
            }

            var brain = GetBrain.Get();
            if (brain != null)
            {
                brain.InvokeItemStolen(context.UnitInstance, target);
                Debug.Log(
                    $"Steal: {context.UnitInstance.CharacterTemplate.DisplayName} attempted to steal {itemType} from {target.CharacterTemplate.DisplayName}"
                );
            }
            else
            {
                Debug.LogWarning("Steal: Could not find Brain to invoke event");
            }
        }
    }
}
