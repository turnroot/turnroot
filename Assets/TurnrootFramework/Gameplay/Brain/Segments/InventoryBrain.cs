using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages inventory operations and publishes item-related events.
    /// Handles item transfers, usage, buying, selling, and repairs.
    /// </summary>
    public class InventoryBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        protected override void Awake() => base.Awake();

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnItemEquipped += HandleItemEquippedEvent;
            Brain.OnItemUnequipped += HandleItemUnequippedEvent;
            Brain.OnItemTransferred += HandleItemTransferredEvent;
            Brain.OnItemBought += HandleItemBoughtEvent;
            Brain.OnItemSold += HandleItemSoldEvent;
            Brain.OnItemDiscarded += HandleItemDiscardedEvent;
            Brain.OnBattleCompleted += HandleBattleCompleted;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnItemEquipped -= HandleItemEquippedEvent;
            Brain.OnItemUnequipped -= HandleItemUnequippedEvent;
            Brain.OnItemTransferred -= HandleItemTransferredEvent;
            Brain.OnItemBought -= HandleItemBoughtEvent;
            Brain.OnItemSold -= HandleItemSoldEvent;
            Brain.OnItemDiscarded -= HandleItemDiscardedEvent;
            Brain.OnBattleCompleted -= HandleBattleCompleted;
        }

        private void HandleBattleCompleted(BattleExitType exitType)
        {
            if (exitType == BattleExitType.Defeat)
            {
                return;
            }
            var allUnits = Brain?.gamewideContextBrain?.GetAllActiveInstances();
            if (allUnits == null)
            {
                return;
            }

            foreach (var character in allUnits)
            {
                var items = character?.InventoryInstance?.InventoryItems;
                if (items == null)
                {
                    continue;
                }

                foreach (var item in items)
                {
                    if (item?.Template == null || !item.Template.ReplenishUsesAfterBattle)
                    {
                        continue;
                    }

                    int amount = GetReplenishAmount(item);
                    if (amount > 0)
                    {
                        item.ReplenishUses(amount);
                    }
                }
            }
        }

        private static int GetReplenishAmount(ObjectItemInstance item)
        {
            int maxUses = item.Template.MaxUses;
            return item.Template.ReplenishUsesAfterBattleAmount switch
            {
                ReplenishUseType.Quarter => UnityEngine.Mathf.FloorToInt(maxUses * 0.25f),
                ReplenishUseType.Third => UnityEngine.Mathf.FloorToInt(maxUses * 0.333f),
                ReplenishUseType.Half => UnityEngine.Mathf.FloorToInt(maxUses * 0.5f),
                ReplenishUseType.Full => maxUses,
                ReplenishUseType.One => 1,
                ReplenishUseType.Two => 2,
                ReplenishUseType.Three => 3,
                ReplenishUseType.Four => 4,
                ReplenishUseType.Five => 5,
                ReplenishUseType.Six => 6,
                ReplenishUseType.Seven => 7,
                ReplenishUseType.Eight => 8,
                ReplenishUseType.Nine => 9,
                ReplenishUseType.Ten => 10,
                _ => 0,
            };
        }

        private void HandleItemEquippedEvent(
            CharacterInstance character,
            ObjectItemInstance item
        ) =>
            // Equipped weapon changed — invalidate or refresh the cache for this character
            Brain?.battleBrain?.BattleObject?.Context?.InvalidateUnitWeaponCache(character?.Id);

        private void HandleItemUnequippedEvent(
            CharacterInstance character,
            ObjectItemInstance item
        ) => Brain?.battleBrain?.BattleObject?.Context?.InvalidateUnitWeaponCache(character?.Id);

        private void HandleItemTransferredEvent(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        )
        {
            // Find owner(s) whose inventory contains this item (targetInventory provided)
            var ctx = Brain?.battleBrain?.BattleObject?.Context;
            if (ctx == null)
            {
                return;
            }

            // Invalidate any cached entry for the owner of the target inventory
            var allUnits = Brain?.gamewideContextBrain?.GetAllActiveInstances();
            if (allUnits == null)
            {
                return;
            }

            foreach (var c in allUnits)
            {
                if (
                    c?.InventoryInstance == targetInventory
                    || c?.InventoryInstance?.InventoryItems?.Contains(item) == true
                )
                {
                    ctx.InvalidateUnitWeaponCache(c.Id);
                }
            }
        }

        private void HandleItemBoughtEvent(
            ObjectItemInstance item,
            CharacterInventoryInstance buyerInventory
        )
        {
            Brain?.battleBrain?.BattleObject?.Context?.InvalidateUnitWeaponCache(
                Brain
                    ?.gamewideContextBrain?.GetAllActiveInstances()
                    ?.Find(u => u.InventoryInstance == buyerInventory)
                    ?.Id
            );
        }

        private void HandleItemSoldEvent(ObjectItemInstance item) =>
            // Conservative: invalidate all caches when items are removed from inventories via sell
            Brain?.battleBrain?.BattleObject?.Context?.InvalidateAllWeaponCaches();

        private void HandleItemDiscardedEvent(ObjectItemInstance item) =>
            Brain?.battleBrain?.BattleObject?.Context?.InvalidateAllWeaponCaches();

        #region Item Operations


        public int UseItem(ObjectItemInstance item)
        {
            if (item == null)
            {
                return -1;
            }

            int remainingUses = item.Use();
            Brain.PublishItemUsed(item, remainingUses);

            if (remainingUses == 0)
            {
                Brain.PublishItemBroken(item);
                $"{item.Template.name} has broken!".LogInfo();
            }

            return remainingUses;
        }

        /// <summary>
        /// Use an item in a battle context. Always uses the command pattern.
        /// </summary>
        /// <param name="user">The character using the item.</param>
        /// <param name="item">The item to use.</param>
        /// <param name="context">The battle context (required).</param>
        /// <param name="target">Optional target for the item.</param>
        /// <returns>True if the item was used successfully.</returns>
        public bool UseItemInBattle(
            CharacterInstance user,
            ObjectItemInstance item,
            BattleContext context,
            CharacterInstance target = null
        )
        {
            if (
                item == null
                || user == null
                || context == null
                || context.Brain == null
                || context.Brain.battleBrain == null
            )
            {
                return false;
            }

            // Always use command pattern
            var command = new UseItemCommand(
                user.Id,
                item.InstanceID,
                target?.Id,
                context.Brain.battleBrain.CurrentTurnNumber
            );
            return context.Brain.ExecuteCommand(command);
        }

        public OperationResult TransferItem(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        )
        {
            if (item == null || targetInventory == null)
            {
                return OperationResult.Failure("Invalid item or target inventory.");
            }

            var result = item.Transfer(targetInventory);
            if (result.Success)
            {
                Brain.PublishItemTransferred(item, targetInventory);
            }

            return result;
        }

        public OperationResult DiscardItem(ObjectItemInstance item)
        {
            if (item == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            var result = item.Discard();
            if (result.Success)
            {
                Brain.PublishItemDiscarded(item);
            }

            return result;
        }

        public OperationResult SellItem(ObjectItemInstance item)
        {
            if (item == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            var result = item.Sell();
            if (result.Success)
            {
                Brain.PublishItemSold(item);
            }

            return result;
        }

        public OperationResult BuyItem(
            ObjectItemInstance item,
            CharacterInventoryInstance buyerInventory
        )
        {
            if (item == null || buyerInventory == null)
            {
                return OperationResult.Failure("Invalid item or buyer inventory.");
            }

            var result = item.Buy(buyerInventory);
            if (result.Success)
            {
                Brain.PublishItemBought(item, buyerInventory);
            }

            return result;
        }

        public OperationResult RepairItem(ObjectItemInstance item, int repairUses)
        {
            if (item == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            var result = item.Repair(repairUses);
            if (result.Success)
            {
                Brain.PublishItemRepaired(item, repairUses);
            }

            return result;
        }

        public OperationResult ForgeItem(ObjectItemInstance item, ObjectItem targetItem)
        {
            if (item == null || targetItem == null)
            {
                return OperationResult.Failure("Invalid item or forge target.");
            }

            if (item.Forger == null)
            {
                return OperationResult.Failure("Item cannot be forged.");
            }

            // Get forge options
            var getOptionsResult = item.Forger.GetForgeOptions();
            if (!getOptionsResult.Success)
            {
                return getOptionsResult;
            }

            // Find the matching forge option
            if (item.Forger.forgeOptions == null || item.Forger.forgeOptions.Length == 0)
            {
                return OperationResult.Failure("No forge options available.");
            }

            ForgeOption? targetOption = null;
            foreach (var option in item.Forger.forgeOptions)
            {
                if (option.ForgeInto == targetItem)
                {
                    targetOption = option;
                    break;
                }
            }

            if (!targetOption.HasValue)
            {
                return OperationResult.Failure($"Cannot forge into {targetItem.name}.");
            }

            // Get storehouse brain to consume resources
            var storehouseBrain = Brain.storehouseBrain;

            // Perform the forg
            var result = item.Forger.ForgeItem(storehouseBrain, targetOption.Value);
            if (result.Success)
            {
                Brain.PublishItemForged(item, targetItem);
            }

            return result;
        }

        #endregion


        public OperationResult EquipItem(CharacterInstance character, int inventoryIndex)
        {
            if (character == null || character.InventoryInstance == null)
            {
                return OperationResult.Failure("Invalid character or inventory.");
            }

            if (
                inventoryIndex < 0
                || inventoryIndex >= character.InventoryInstance.InventoryItems.Count
            )
            {
                return OperationResult.Failure("Invalid inventory index.");
            }

            var res = character.InventoryInstance.EquipItem(inventoryIndex);
            if (!res.Success)
            {
                return res;
            }

            var item = character.InventoryInstance.InventoryItems[inventoryIndex];
            Brain.PublishItemEquipped(character, item);
            return OperationResult.Successful();
        }

        public OperationResult UnequipItem(CharacterInstance character, int inventoryIndex)
        {
            if (character == null || character.InventoryInstance == null)
            {
                return OperationResult.Failure("Invalid character or inventory.");
            }

            if (
                inventoryIndex < 0
                || inventoryIndex >= character.InventoryInstance.InventoryItems.Count
            )
            {
                return OperationResult.Failure("Invalid inventory index.");
            }

            var item = character.InventoryInstance.InventoryItems[inventoryIndex];
            var res = character.InventoryInstance.UnequipItem(inventoryIndex);
            if (!res.Success)
            {
                return res;
            }

            Brain.PublishItemUnequipped(character, item);
            return OperationResult.Successful();
        }

        #region Queries

        public List<ObjectItemInstance> GetAllItems()
        {
            // Gather items from both an active battle and gamewide context to support both modes
            var items = new List<ObjectItemInstance>();
            var seenCharacters = new HashSet<string>();

            // 1) From active BattleBrain (in-battle instances)
            var battleBrain = Brain.battleBrain;
            if (battleBrain != null)
            {
                var characters = battleBrain.GetAllActiveInstances();
                foreach (var character in characters)
                {
                    if (character == null || seenCharacters.Contains(character.Id))
                    {
                        continue;
                    }

                    seenCharacters.Add(character.Id);
                    if (character.InventoryInstance?.InventoryItems != null)
                    {
                        items.AddRange(character.InventoryInstance.InventoryItems);
                    }
                }
            }

            // 2) From GamewideContextBrain (persistent/runtime instances outside battle)
            var gw = Brain.gamewideContextBrain;
            if (gw != null)
            {
                var characters = gw.GetAllActiveInstances();
                foreach (var character in characters)
                {
                    if (character == null || seenCharacters.Contains(character.Id))
                    {
                        continue;
                    }

                    seenCharacters.Add(character.Id);
                    if (character.InventoryInstance?.InventoryItems != null)
                    {
                        items.AddRange(character.InventoryInstance.InventoryItems);
                    }
                }
            }

            return items;
        }

        public List<ObjectItemInstance> FindItemsByTemplate(ObjectItem template)
        {
            var items = GetAllItems();
            return items.FindAll(i => i.Template == template);
        }

        #endregion
    }
}
