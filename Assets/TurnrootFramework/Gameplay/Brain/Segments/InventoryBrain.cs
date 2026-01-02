using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages inventory operations and publishes item-related events.
    /// Handles item transfers, usage, buying, selling, and repairs.
    /// </summary>
    public class InventoryBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            Debug.Log("InventoryBrain is ready.");
#endif
        }

        protected override void SubscribeToBrainEvents()
        {
            // InventoryBrain primarily publishes events rather than subscribing
            // Add any event subscriptions here if needed in the future
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            // No subscriptions to clean up
        }

        #region Item Operations

        /// <summary>
        /// Use an item and publish the usage event.
        /// </summary>
        public int UseItem(ObjectItemInstance item)
        {
            if (item == null)
            {
                return -1;
            }

            int remainingUses = item.Use();
            _brain?.PublishItemUsed(item, remainingUses);

            if (remainingUses == 0)
            {
                _brain?.PublishItemBroken(item);
#if UNITY_EDITOR
                Debug.Log($"{item.Template.name} has broken!");
#endif
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
            if (item == null || user == null)
            {
                return false;
            }

            if (context?.Brain == null)
            {
                throw new System.InvalidOperationException(
                    "UseItemInBattle requires BattleContext.Brain to be set."
                );
            }

            // Always use command pattern
            var command = new UseItemCommand(
                user.Id,
                item.InstanceID,
                target?.Id,
                context.Brain.CurrentTurnNumber
            );
            return context.Brain.ExecuteCommand(command);
        }

        /// <summary>
        /// Transfer an item between inventories and publish the event.
        /// </summary>
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
                _brain?.PublishItemTransferred(item, targetInventory);
            }

            return result;
        }

        /// <summary>
        /// Discard an item and publish the event.
        /// </summary>
        public OperationResult DiscardItem(ObjectItemInstance item)
        {
            if (item == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            var result = item.Discard();
            if (result.Success)
            {
                _brain?.PublishItemDiscarded(item);
            }

            return result;
        }

        /// <summary>
        /// Sell an item and publish the event.
        /// </summary>
        public OperationResult SellItem(ObjectItemInstance item)
        {
            if (item == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            var result = item.Sell();
            if (result.Success)
            {
                _brain?.PublishItemSold(item);
            }

            return result;
        }

        /// <summary>
        /// Buy an item and publish the event.
        /// </summary>
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
                _brain?.PublishItemBought(item, buyerInventory);
            }

            return result;
        }

        /// <summary>
        /// Repair an item and publish the event.
        /// </summary>
        public OperationResult RepairItem(ObjectItemInstance item, int repairUses)
        {
            if (item == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            var result = item.Repair(repairUses);
            if (result.Success)
            {
                _brain?.PublishItemRepaired(item, repairUses);
            }

            return result;
        }

        /// <summary>
        /// Forge an item into a new item and publish the event.
        /// </summary>
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
            var storehouseBrain = _brain.storehouseBrain;

            // Perform the forg
            var result = item.Forger.ForgeItem(storehouseBrain, targetOption.Value);
            if (result.Success)
            {
                _brain?.PublishItemForged(item, targetItem);
            }

            return result;
        }

        #endregion

        /// <summary>
        /// Equip an item on a character and publish an equipped event.
        /// </summary>
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

            character.InventoryInstance.EquipItem(inventoryIndex);
            var item = character.InventoryInstance.InventoryItems[inventoryIndex];
            _brain?.PublishItemEquipped(character, item);
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Unequip an item on a character and publish an unequipped event.
        /// </summary>
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
            character.InventoryInstance.UnequipItem(inventoryIndex);
            _brain?.PublishItemUnequipped(character, item);
            return OperationResult.SuccessResult();
        }

        #region Queries

        /// <summary>
        /// Get all items across all character inventories.
        /// </summary>
        public List<ObjectItemInstance> GetAllItems()
        {
            // Gather items from both an active battle and gamewide context to support both modes
            var items = new List<ObjectItemInstance>();
            var seenCharacters = new System.Collections.Generic.HashSet<string>();

            // 1) From active BattleBrain (in-battle instances)
            var battleBrain = _brain.battleBrain;
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
            var gw = _brain?.gamewideContextBrain;
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

        /// <summary>
        /// Find items by template across all inventories.
        /// </summary>
        public List<ObjectItemInstance> FindItemsByTemplate(ObjectItem template)
        {
            var items = GetAllItems();
            return items.FindAll(i => i.Template == template);
        }

        #endregion
    }
}
