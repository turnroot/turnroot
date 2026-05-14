using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Attempts to steal an item or weapon from the target enemy.
    /// </summary>
    [CreateNodeMenu("Events/Offensive/Steal")]
    [NodeLabel("Steal an object from the enemy")]
    public class StealNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

        [Tooltip("Type of item to steal (weapon, item, etc.)")]
        public string itemType = "Item";

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

            // TODO: Actually transfer the item before publishing the event.
            // Need an inventory transfer API, e.g.:
            //   var stolen = target.InventoryInstance.TakeItem(itemType);
            //   if (stolen != null) context.Unit.UnitInstance.InventoryInstance.AddItem(stolen);
            // PublishItemStolen should only fire after the transfer succeeds.
            context.Brain.PublishItemStolen(context.Unit.UnitInstance, target);

            $"Steal: {context.Unit.UnitInstance.CharacterTemplate.DisplayName} attempted to steal {itemType} from {target.CharacterTemplate.DisplayName}".LogInfo();
        }
    }
}
