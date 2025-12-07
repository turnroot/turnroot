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

        [NonSerialized]
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
        /// Validates whether this item can be transferred to a target inventory.
        /// Use for UI binding (e.g., enabling/disabling transfer buttons).
        /// </summary>
        public bool CanTransfer(CharacterInventoryInstance targetInventory)
        {
            if (_template.IsUnequippable)
                return false;
            if (targetInventory.IsFull)
                return false;
            return true;
        }

        /// <summary>
        /// Transfers this item to a target inventory.
        /// </summary>
        public OperationResult Transfer(CharacterInventoryInstance targetInventory)
        {
            if (_template.IsUnequippable)
                return OperationResult.Failure("Cannot transfer an unequippable item.");

            if (targetInventory.IsFull)
                return OperationResult.Failure("Target inventory is full. Cannot transfer item.");

            _ownerInventory.RemoveFromInventory(this);
            targetInventory.AddToInventory(this);
            _ownerInventory = targetInventory;
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Validates whether this item can be discarded.
        /// Use for UI binding (e.g., enabling/disabling discard buttons).
        /// </summary>
        public bool CanDiscard()
        {
            if (_template.IsUnequippable)
                return false;
            return true;
        }

        /// <summary>
        /// Discards this item from the owner's inventory.
        /// </summary>
        public OperationResult Discard()
        {
            if (_template.IsUnequippable)
                return OperationResult.Failure("Cannot discard an unequippable item.");

            _ownerInventory.RemoveFromInventory(this);
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Validates whether this item can be sold.
        /// Use for UI binding (e.g., enabling/disabling sell buttons).
        /// </summary>
        public bool CanSell()
        {
            if (_template.IsUnequippable || !_template.Sellable)
                return false;
            return true;
        }

        /// <summary>
        /// Sells this item and removes it from inventory.
        /// </summary>
        public OperationResult Sell()
        {
            if (_template.IsUnequippable || !_template.Sellable)
                return OperationResult.Failure("Cannot sell this item.");

            int deduction = _template.SellPriceDeductedPerUse * currentUses;
            int finalPrice = Math.Max(0, _template.BasePrice - deduction);
            _ownerInventory.RemoveFromInventory(this);
            // TODO: Add gold to player (brains)
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Validates whether this item can be bought for a buyer's inventory.
        /// Use for UI binding (e.g., enabling/disabling buy buttons).
        /// </summary>
        public bool CanBuy(CharacterInventoryInstance buyerInventory)
        {
            if (_template.IsUnequippable || !_template.Buyable)
                return false;
            if (buyerInventory.IsFull)
                return false;
            return true;
        }

        /// <summary>
        /// Buys this item and adds it to the buyer's inventory.
        /// </summary>
        public OperationResult Buy(CharacterInventoryInstance buyerInventory)
        {
            if (_template.IsUnequippable || !_template.Buyable)
                return OperationResult.Failure("Cannot buy this item.");

            if (buyerInventory.IsFull)
                return OperationResult.Failure("Buyer inventory is full. Cannot buy item.");

            buyerInventory.AddToInventory(this);
            _ownerInventory = buyerInventory;
            // TODO: Deduct gold from player (brains)
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Validates whether this item can be repaired.
        /// Use for UI binding (e.g., enabling/disabling repair buttons).
        /// </summary>
        public bool CanRepair(int repairUses)
        {
            if (!_template.Repairable || !_template.Durability)
                return false;
            if (_template.RepairNeedsItems)
            {
                // TODO: Get items from storehouse
                return true;
            }
            if (repairUses <= 0 || currentUses - repairUses < 0)
                return false;
            // TODO: Check if player can pay the cost
            return true;
        }

        /// <summary>
        /// Repairs this item by restoring the specified number of uses.
        /// </summary>
        public OperationResult Repair(int repairUses)
        {
            if (!_template.Repairable || !_template.Durability)
                return OperationResult.Failure("Cannot repair a non-repairable item.");

            if (_template.RepairNeedsItems)
            {
                // TODO: Get items from storehouse
                currentUses -= repairUses;
                if (currentUses < 0)
                    currentUses = 0;
                return OperationResult.SuccessResult();
            }

            if (repairUses <= 0 || currentUses - repairUses < 0)
                return OperationResult.Failure("Invalid repair uses specified.");

            var repairCost = _template.RepairItemAmountPerUse * repairUses;
            // TODO: Check if player can pay the cost

            currentUses -= repairUses;
            if (currentUses < 0)
                currentUses = 0;

            return OperationResult.SuccessResult();
        }

        public void OnAfterDeserialize()
        {
            // Ensure _ownerInventory reference is maintained after deserialization
            // If this item was deserialized without an owner, it will remain null
            // which is valid for items in shops or as loot

            // Clamp currentUses to valid range based on template
            if (_template != null && _template.Durability)
            {
                if (currentUses < 0)
                    currentUses = 0;
                if (currentUses > _template.MaxUses)
                    currentUses = _template.MaxUses;
            }
        }
    }
}
