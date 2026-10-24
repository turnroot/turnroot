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

            // The transfer itself is performed synchronously by the Brain's OnItemStolen
            // handler (BattleBrain.HandleItemStolenLogic), which picks the most valuable
            // stealable item and moves it via InventoryBrain.TransferItem.
            context.Brain.PublishItemStolen(context.Unit.UnitInstance, target);

            $"Steal: {context.Unit.UnitInstance.CharacterTemplate.DisplayName} attempted to steal {itemType} from {target.CharacterTemplate.DisplayName}".LogInfo();
        }
    }
}
