using System;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.Serialization;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    [Serializable]
    public class ObjectItemInstance : IPostDeserialize
    {
        [SerializeField]
        private string _id;

        public string InstanceID => _id;

        [SerializeField]
        private ObjectItem _template;

        [NonSerialized]
        private CharacterInventoryInstance _ownerInventory;
        private int currentUses;
        public ObjectItem Template => _template;

        /// <summary>
        /// The number of times this item has been used.
        /// </summary>
        public int CurrentUses => currentUses;

        /// <summary>
        /// The remaining uses before the item breaks.
        /// Returns -1 if the item has no durability (infinite uses).
        /// </summary>
        public int RemainingUses =>
            _template?.Durability == true ? _template.MaxUses - currentUses : -1;

        /// <summary>
        /// Reference to the Brain for accessing brain segments.
        /// Must be set via SetBrain() after deserialization or creation.
        /// </summary>
        [NonSerialized]
        private Brain.Brain _brain;

        /// <summary>
        /// Sets the Brain reference. Call this after creating or deserializing the item.
        /// </summary>
        public void SetBrain(Brain.Brain brain) => _brain = brain;

        private StorehouseBrain StorehouseBrain => _brain?.storehouseBrain;
        private InventoryBrain InventoryBrain => _brain?.inventoryBrain;

        private readonly ObjectForgerHelper ForgerHelper;

        public ObjectForgerHelper Forger => ForgerHelper;

        public ObjectItemInstance(ObjectItem template)
        {
            _template = template;
            if (_template.Forgeable)
            {
                ForgerHelper = new ObjectForgerHelper { ThisItem = template };
            }
            _id = Guid.NewGuid().ToString();
            currentUses = 0;
        }

        /// <summary>
        /// Use the item once, reducing its durability if applicable.
        /// </summary>
        /// <returns>
        /// Remaining uses left. -1 if the item is not durable.
        /// </returns>
        internal int Use()
        {
            if (!_template.Durability)
            {
                return -1;
            }
            else
            {
                currentUses++;
                InventoryBrain?.UseItem(this);
                return _template.MaxUses - currentUses > 0 ? _template.MaxUses - currentUses : 0;
            }
        }

        /// <summary>
        /// Validates whether this item can be transferred to a target inventory.
        /// Use for UI binding (e.g., enabling/disabling transfer buttons).
        /// </summary>
        public bool CanTransfer(CharacterInventoryInstance targetInventory) =>
            !_template.IsUnequippable && !targetInventory.IsFull;

        /// <summary>
        /// Transfers this item to a target inventory.
        /// </summary>
        internal OperationResult Transfer(CharacterInventoryInstance targetInventory)
        {
            if (_template.IsUnequippable)
            {
                return OperationResult.Failure("Cannot transfer an unequippable item.");
            }

            if (targetInventory.IsFull)
            {
                return OperationResult.Failure("Target inventory is full. Cannot transfer item.");
            }

            _ownerInventory.RemoveFromInventory(this);
            targetInventory.AddToInventory(this);
            _ownerInventory = targetInventory;
            InventoryBrain?.TransferItem(this, targetInventory);
            return OperationResult.SuccessResult();
        }

        public bool CanDiscard() => !_template.IsUnequippable;

        internal OperationResult Discard()
        {
            if (_template.IsUnequippable)
            {
                return OperationResult.Failure("Cannot discard an unequippable item.");
            }

            _ownerInventory.RemoveFromInventory(this);
            InventoryBrain?.DiscardItem(this);
            return OperationResult.SuccessResult();
        }

        public bool CanSell() => !_template.IsUnequippable && _template.Sellable;

        internal OperationResult Sell()
        {
            if (_template.IsUnequippable || !_template.Sellable)
            {
                return OperationResult.Failure("Cannot sell this item.");
            }

            int deduction = _template.SellPriceDeductedPerUse * currentUses;
            int finalPrice = Math.Max(0, _template.BasePrice - deduction);
            _ownerInventory.RemoveFromInventory(this);
            StorehouseBrain?.AddGold(finalPrice);
            InventoryBrain?.SellItem(this);
            return OperationResult.SuccessResult();
        }

        public bool CanBuy(CharacterInventoryInstance buyerInventory) =>
            !_template.IsUnequippable
            && _template.Buyable
            && !buyerInventory.IsFull
            && (StorehouseBrain?.CanAfford(_template.BasePrice) ?? false);

        internal OperationResult Buy(CharacterInventoryInstance buyerInventory)
        {
            if (_template.IsUnequippable || !_template.Buyable)
            {
                return OperationResult.Failure("Cannot buy this item.");
            }

            if (buyerInventory.IsFull)
            {
                return OperationResult.Failure("Buyer inventory is full. Cannot buy item.");
            }

            if (!(StorehouseBrain?.CanAfford(_template.BasePrice) ?? false))
            {
                return OperationResult.Failure("Insufficient gold to buy item.");
            }

            buyerInventory.AddToInventory(this);
            _ownerInventory = buyerInventory;
            StorehouseBrain?.SpendGold(_template.BasePrice);
            InventoryBrain?.BuyItem(this, buyerInventory);
            return OperationResult.SuccessResult();
        }

        public bool CanRepair(int repairUses)
        {
            if (!_template.Repairable || !_template.Durability)
            {
                return false;
            }

            if (_template.RepairNeedsItems)
            {
                return StorehouseBrain?.HasMaterials(
                        _template.RepairItem,
                        _template.RepairItemAmountPerUse * repairUses
                    ) ?? false;
            }
            if (repairUses <= 0 || currentUses - repairUses < 0)
            {
                return false;
            }
            var repairCost = _template.RepairItemAmountPerUse * repairUses;
            return StorehouseBrain?.CanAfford(repairCost) ?? false;
        }

        /// <summary>
        /// Repairs this item by restoring the specified number of uses.
        /// </summary>
        internal OperationResult Repair(int repairUses)
        {
            if (!_template.Repairable || !_template.Durability)
            {
                return OperationResult.Failure("Cannot repair a non-repairable item.");
            }

            if (_template.RepairNeedsItems)
            {
                currentUses -= repairUses;
                if (currentUses < 0)
                {
                    currentUses = 0;
                }

                return OperationResult.SuccessResult();
            }

            if (repairUses <= 0 || currentUses - repairUses < 0)
            {
                return OperationResult.Failure("Invalid repair uses specified.");
            }

            var repairCost = _template.RepairItemAmountPerUse * repairUses;
            if (!(StorehouseBrain?.CanAfford(repairCost) ?? false))
            {
                return OperationResult.Failure("Insufficient gold to repair item.");
            }
            if (!(StorehouseBrain?.HasMaterials(_template.RepairItem, repairCost) ?? false))
            {
                return OperationResult.Failure("Insufficient materials to repair item.");
            }
            InventoryBrain?.RepairItem(this, repairUses);
            StorehouseBrain?.SpendGold(repairCost);
            StorehouseBrain?.ConsumeMaterials(_template.RepairItem, repairCost);

            currentUses -= repairUses;
            if (currentUses < 0)
            {
                currentUses = 0;
            }

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
                {
                    currentUses = 0;
                }

                if (currentUses > _template.MaxUses)
                {
                    currentUses = _template.MaxUses;
                }
            }
        }
    }
}
