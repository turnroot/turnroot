using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

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

            context.Brain?.PublishItemStolen(context.UnitInstance, target);
            Debug.Log(
                $"Steal: {context.UnitInstance.CharacterTemplate.DisplayName} attempted to steal {itemType} from {target.CharacterTemplate.DisplayName}"
            );
        }
    }
}
