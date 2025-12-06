using System;
using Turnroot.Serialization;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    [Serializable]
    public class ObjectItemInstance : IPostDeserialize
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private ObjectItem _template;

        [SerializeField]
        private CharacterInventoryInstance _ownerInventory;
        private int currentUses;
        public ObjectItem Template => _template;

        public ObjectItemInstance(ObjectItem template)
        {
            _template = template;
            _id = Guid.NewGuid().ToString();
            currentUses = 0;
        }

        /// <summary>
        /// Use the item once, reducing its durability if applicable.
        /// </summary>
        /// <returns>
        /// Remaining uses left. -1 if the item is not durable.
        /// </returns>
        public int Use()
        {
            if (!_template.Durability)
            {
                return -1;
            }
            else
            {
                currentUses++;
                return _template.MaxUses - currentUses > 0 ? _template.MaxUses - currentUses : 0;
            }
        }

        /// <summary>
        /// Transfer this item to another inventory.
        /// </summary>
        /// <param name="targetInventory">The inventory to transfer the item to.</param>
        /// <returns>True if the transfer was successful, false otherwise.</returns>
        public bool Transfer(CharacterInventoryInstance targetInventory)
        {
            if (_template.IsUnequippable)
            {
                Debug.LogWarning("Cannot transfer an unequippable item.");
                return false;
            }
            if (targetInventory.IsFull)
            {
                Debug.LogWarning("Target inventory is full. Cannot transfer item.");
                return false;
            }
            _ownerInventory.RemoveFromInventory(this);
            targetInventory.AddToInventory(this);
            _ownerInventory = targetInventory;
            return true;
        }

        /// <summary>
        /// Discard this item from its owner's inventory.
        /// </summary>
        /// <returns>
        /// True if the item was successfully discarded, false otherwise.
        /// </returns>
        public bool Discard()
        {
            if (_template.IsUnequippable)
            {
                Debug.LogWarning("Cannot discard an unequippable item.");
                return false;
            }
            _ownerInventory.RemoveFromInventory(this);
            return true;
        }

        /// <summary>
        /// Sell this item from its owner's inventory.
        /// </summary>
        /// <returns>
        /// True if the item was successfully sold, false otherwise.
        /// </returns>
        public bool Sell()
        {
            if (_template.IsUnequippable || _template.Sellable)
            {
                Debug.LogWarning("Cannot sell an unequippable item.");
                return false;
            }
            int deduction = _template.SellPriceDeductedPerUse * currentUses;
            int finalPrice = Math.Max(0, _template.BasePrice - deduction);
            _ownerInventory.RemoveFromInventory(this);
            // TODO: Add gold to player (brains)
            return true;
        }

        /// <summary>
        /// Buy this item into the specified inventory.
        /// </summary>
        /// <param name="buyerInventory">The inventory to buy the item into.</param>
        /// <returns>True if the item was successfully bought, false otherwise.</returns>
        public bool Buy(CharacterInventoryInstance buyerInventory)
        {
            if (_template.IsUnequippable || !_template.Buyable)
            {
                Debug.LogWarning("Cannot buy an unequippable item.");
                return false;
            }
            if (buyerInventory.IsFull)
            {
                Debug.LogWarning("Buyer inventory is full. Cannot buy item.");
                return false;
            }
            buyerInventory.AddToInventory(this);
            _ownerInventory = buyerInventory;
            // TODO: Deduct gold from player (brains)
            return true;
        }

        /// <summary>
        /// Repair this item, restoring its durability.
        /// </summary>
        /// <param name="repairUses">The number of uses to repair.</param>
        /// <returns>True if the item was successfully repaired, false otherwise.</returns>
        public bool Repair(int repairUses)
        {
            if (!_template.Repairable || !_template.Durability)
            {
                Debug.LogWarning("Cannot repair a non-repairable item.");
                return false;
            }
            if (_template.RepairNeedsItems)
            {
                // TODO: Get items from storehouse
                return true;
            }
            if (repairUses <= 0 || currentUses - repairUses < 0)
            {
                Debug.LogWarning("Invalid repair uses specified.");
                return false;
            }
            var repairCost = _template.RepairItemAmountPerUse * repairUses;
            // TODO: Check if player can pay the cost
            return true;
        }

        public void OnAfterDeserialize()
        {
            // there is nothing here yet
        }
    }
}
