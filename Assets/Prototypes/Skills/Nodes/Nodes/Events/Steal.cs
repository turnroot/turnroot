using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Steal")]
    [NodeLabel("Steal an object from the enemy")]
    public class Steal : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Tooltip("Type of item to steal (weapon, item, etc.)")]
        public string itemType = "Item";

        public override void Execute(BattleContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
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

            // TODO: Integrate with actual inventory system
            context.SetCustomData("StolenItemType", itemType);
            Debug.Log($"Steal: Attempted to steal {itemType} from target");
        }
    }
}
