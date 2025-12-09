using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages inventory operations and publishes item-related events.
    /// Handles item transfers, usage, buying, selling, and repairs.
    /// </summary>
    [RequireComponent(typeof(Brain))]
    public class InventoryBrain : MonoBehaviour
    {
        private Brain _brain;

        private void Awake()
        {
            _brain = GetComponent<Brain>();
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
                Debug.Log($"{item.Template.name} has broken!");
            }

            return remainingUses;
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

            // TODO: Implement forging logic
            _brain?.PublishItemForged(item, targetItem);

            return OperationResult.SuccessResult();
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get all items across all character inventories.
        /// </summary>
        public List<ObjectItemInstance> GetAllItems()
        {
            var items = new List<ObjectItemInstance>();
            var gamewideContext = _brain.gamewideContextBrain;

            if (gamewideContext != null)
            {
                var characters = gamewideContext.GetAllActiveInstances();
                foreach (var character in characters)
                {
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
