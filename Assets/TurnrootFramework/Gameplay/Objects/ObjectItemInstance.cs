using System;
using Assets.Turnroot.Gameplay.Brain;
using Turnroot.Serialization;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    [Serializable]
    public class ObjectItemInstance : IPostDeserialize
    {
        [NaughtyAttributes.ReadOnly]
        public StorehouseBrain sb;

        [NaughtyAttributes.ReadOnly]
        public InventoryBrain ib;

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

        private void Start()
        {
            sb = Utilities.GetBrain.Get()?.storehouseBrain;
            ib = Utilities.GetBrain.Get()?.inventoryBrain;
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
                ib.UseItem(this);
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
            ib.TransferItem(this, targetInventory);
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
            ib.DiscardItem(this);
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
            sb.AddGold(finalPrice);
            ib.SellItem(this);
            return OperationResult.SuccessResult();
        }

        public bool CanBuy(CharacterInventoryInstance buyerInventory) =>
            !_template.IsUnequippable
            && _template.Buyable
            && !buyerInventory.IsFull
            && sb.CanAfford(_template.BasePrice);

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

            if (!sb.CanAfford(_template.BasePrice))
            {
                return OperationResult.Failure("Insufficient gold to buy item.");
            }

            buyerInventory.AddToInventory(this);
            _ownerInventory = buyerInventory;
            sb.SpendGold(_template.BasePrice);
            ib.BuyItem(this, buyerInventory);
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
                return sb.HasMaterials(
                    _template.RepairItem,
                    _template.RepairItemAmountPerUse * repairUses
                );
            }
            if (repairUses <= 0 || currentUses - repairUses < 0)
            {
                return false;
            }
            var repairCost = _template.RepairItemAmountPerUse * repairUses;
            return sb.CanAfford(repairCost);
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
            if (!sb.CanAfford(repairCost))
            {
                return OperationResult.Failure("Insufficient gold to repair item.");
            }
            if (!sb.HasMaterials(_template.RepairItem, repairCost))
            {
                return OperationResult.Failure("Insufficient materials to repair item.");
            }
            ib.RepairItem(this, repairUses);
            sb.SpendGold(repairCost);
            sb.ConsumeMaterials(_template.RepairItem, repairCost);

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
