using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to use an item.
    /// </summary>
    public class UseItemCommand : CommandBase
    {
        public string UserId { get; }
        public string ItemId { get; }
        public string TargetId { get; }

        public UseItemCommand(string userId, string itemId, string targetId, int turn)
            : base(turn)
        {
            UserId = userId;
            ItemId = itemId;
            TargetId = targetId;
        }

        public override bool Execute(BattleContext context)
        {
            var user = FindUnit(context, UserId);
            var item = user?.InventoryInstance?.InventoryItems?.Find(i =>
                i != null && i.InstanceID == ItemId
            );
            if (item == null || context?.Brain?.inventoryBrain == null)
            {
                $"[UseItemCommand] Could not resolve user {UserId} or item {ItemId}".LogWarning();
                return false;
            }

            UndoState[UndoStateKeys.PrevUses] = item.CurrentUses;

            // InventoryBrain.UseItem decrements durability and publishes ItemUsed/ItemBroken
            context.Brain.inventoryBrain.UseItem(item);

            var target = TargetId != null ? FindUnit(context, TargetId) : null;
            context.Brain.PublishUseItemLogicCompleted(user, item);

            $"[UseItemCommand] {user.CharacterTemplate.DisplayName} used {item.Template.name} on {(target != null ? target.CharacterTemplate.DisplayName : "self")}".LogInfo();
            return true;
        }

        public override bool Undo(BattleContext context)
        {
            var user = FindUnit(context, UserId);
            var item = user?.InventoryInstance?.InventoryItems?.Find(i =>
                i != null && i.InstanceID == ItemId
            );
            if (
                item == null
                || !UndoState.TryGetValue(UndoStateKeys.PrevUses, out var prevUses)
                || item.Template?.Durability != true
            )
            {
                "[UseItemCommand] Item use cannot be undone".LogWarning();
                return false;
            }

            item.ReplenishUses(item.CurrentUses - (int)prevUses);
            context.Brain.PublishItemUsed(item, item.RemainingUses);
            return true;
        }
    }
}

