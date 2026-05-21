using System;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.Serialization;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    /// <summary>
    /// Runtime instance of an ObjectItem with usage tracking, inventory management, and transaction operations.
    /// </summary>
    [Serializable]
    public class ObjectItemInstance : IPostDeserialize
    {
        [SerializeField]
        private string _id;

        public string InstanceID
        {
            get => _id;
            internal set => _id = value;
        }

        [SerializeField]
        private ObjectItem _template;

        public ObjectItem Template => _template;

        public int Slot = -1;
        public bool IsEquipped = false;

        [NonSerialized]
        private CharacterInventoryInstance _ownerInventory;

        private int currentUses;

        internal void SetOwnerInventory(CharacterInventoryInstance owner) => _ownerInventory = owner;

        internal void ClearOwnerInventory()
        {
            _ownerInventory = null;
            Slot = -1;
            IsEquipped = false;
        }

        public int CurrentUses
        {
            get => currentUses;
            internal set => currentUses = value;
        }
        public int RemainingUses =>
            _template?.Durability == true ? _template.MaxUses - currentUses : -1;

        [NonSerialized]
        private Brain.Brain _brain;

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

        internal int Use()
        {
            if (_template == null)
            {
                return -1;
            }

            if (!_template.Durability)
            {
                return -1;
            }

            currentUses++;

            int remaining = _template.MaxUses - currentUses;
            if (remaining < 0)
            {
                remaining = 0;
            }

            return remaining;
        }

        /// <summary>Restores up to <paramref name="amount"/> uses on this item (clamped so uses never go below 0).</summary>
        public void ReplenishUses(int amount)
        {
            if (_template == null || !_template.Durability || amount <= 0)
            {
                return;
            }

            currentUses = Mathf.Max(0, currentUses - amount);
        }

        /// <summary>
        /// Validates whether this item can be transferred to a target inventory.
        /// Use for UI binding (e.g., enabling/disabling transfer buttons).
        /// </summary>
        public bool CanTransfer(CharacterInventoryInstance targetInventory) =>
            !_template.IsUnequippable && !targetInventory.IsFull;

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

            var resRemove = _ownerInventory.RemoveFromInventory(this);
            if (!resRemove.Success)
            {
                return resRemove;
            }

            var resAdd = targetInventory.AddToInventory(this);
            if (!resAdd.Success)
            {
                // Try to restore to owner on failure (best-effort)
                _ownerInventory.AddToInventory(this);
                return resAdd;
            }

            // owner & slot set by CharacterInventoryInstance.AddToInventory
            InventoryBrain?.TransferItem(this, targetInventory);
            return OperationResult.Successful();
        }

        public bool CanDiscard() => !_template.IsUnequippable;

        internal OperationResult Discard()
        {
            if (_template.IsUnequippable)
            {
                return OperationResult.Failure("Cannot discard an unequippable item.");
            }

            var res = _ownerInventory.RemoveFromInventory(this);
            if (!res.Success)
            {
                return res;
            }
            InventoryBrain?.DiscardItem(this);
            return OperationResult.Successful();
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
            var res = _ownerInventory.RemoveFromInventory(this);
            if (!res.Success)
            {
                return res;
            }
            StorehouseBrain?.AddGold(finalPrice);
            InventoryBrain?.SellItem(this);
            return OperationResult.Successful();
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

            var res = buyerInventory.AddToInventory(this);
            if (!res.Success)
            {
                return res;
            }
            // owner & slot set by CharacterInventoryInstance.AddToInventory
            StorehouseBrain?.SpendGold(_template.BasePrice);
            InventoryBrain?.BuyItem(this, buyerInventory);
            return OperationResult.Successful();
        }

        public bool CanRepair(int repairUses, StorehouseBrain overrideStorehouseBrain = null)
        {
            try
            {
                if (_template == null)
                {
                    "ObjectItemInstance.CanRepair: _template is null".LogWarning(
                        "ObjectItemInstance"
                    );
                    return false;
                }

                if (!_template.Repairable || !_template.Durability)
                {
                    return false;
                }

                if (repairUses <= 0 || currentUses - repairUses < 0)
                {
                    return false;
                }

                var repairStorehouseBrain = overrideStorehouseBrain ?? StorehouseBrain;
                if (repairStorehouseBrain == null)
                {
                    "ObjectItemInstance.CanRepair: storehouse brain reference missing".LogWarning(
                        "ObjectItemInstance"
                    );
                    return false;
                }

                int repairCost = _template.RepairPricePerUse * repairUses;
                bool canAfford =
                    _template.RepairPricePerUse <= 0 || repairStorehouseBrain.CanAfford(repairCost);

                if (_template.RepairNeedsItems)
                {
                    if (_template.RepairItem == null)
                    {
                        "ObjectItemInstance.CanRepair: RepairNeedsItems true but RepairItem is null".LogWarning(
                            "ObjectItemInstance"
                        );
                        return false;
                    }

                    int requiredItems;
                    if (_template.OneRepairItemCoversFullRepair)
                    {
                        requiredItems = 1;
                    }
                    else if (_template.RepairItemAmountPerUse > 0)
                    {
                        requiredItems = _template.RepairItemAmountPerUse * repairUses;
                    }
                    else
                    {
                        return false;
                    }

                    bool hasMaterials = repairStorehouseBrain.HasMaterials(
                        _template.RepairItem,
                        requiredItems
                    );
                    bool canRepair = hasMaterials && canAfford;

                    if (!canRepair)
                    {
                        $"ObjectItemInstance.CanRepair: cannot repair '{_template.Name}', hasMaterials={hasMaterials}, canAfford={canAfford}, requiredItems={requiredItems}, repairCost={repairCost}, currentUses={currentUses}".LogInfo(
                            "ObjectItemInstance"
                        );
                    }
                    return canRepair;
                }

                if (!canAfford)
                {
                    $"ObjectItemInstance.CanRepair: cannot repair '{_template.Name}', insufficient gold (cost={repairCost}, currentGold={repairStorehouseBrain.PlayerGold})".LogInfo(
                        "ObjectItemInstance"
                    );
                }

                return canAfford;
            }
            catch (Exception ex)
            {
                $"ObjectItemInstance.CanRepair: crashed with exception: {ex}".LogWarning(
                    "ObjectItemInstance"
                );
                return false;
            }
        }

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

                return OperationResult.Successful();
            }

            int repairItemCost;

            if (repairUses <= 0 || currentUses - repairUses < 0)
            {
                return OperationResult.Failure("Invalid repair uses specified.");
            }

            if (!CanRepair(repairUses, StorehouseBrain))
            {
                return OperationResult.Failure("Cannot afford repair.");
            }
            else
            {
                repairItemCost = _template.OneRepairItemCoversFullRepair ? 1 : _template.RepairItemAmountPerUse * repairUses;
            }
            InventoryBrain?.RepairItem(this, repairUses);
            StorehouseBrain?.SpendGold(_template.RepairPricePerUse * repairUses);
            if (_template.RepairNeedsItems)
            {
                StorehouseBrain?.ConsumeMaterials(_template.RepairItem, repairItemCost);
            }

            currentUses -= repairUses;
            if (currentUses < 0)
            {
                currentUses = 0;
            }

            return OperationResult.Successful();
        }

        public void OnAfterDeserialize()
        {
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

            // If this item was deserialized without an owning inventory, clear slot
            if (_ownerInventory == null)
            {
                Slot = -1;
            }
        }

        public bool IsRepairableWeaponAccessoryOrShield()
        {
            if (_template == null)
            {
                return false;
            }

            if (!_template.Durability || !_template.Repairable)
            {
                return false;
            }

            var subtype = _template.Subtype;
            bool isWeapon = subtype?.IsWeapon == true;
            bool isShield = subtype?.IsShield == true;
            bool isAccessory = subtype?.IsAccessory == true;

            if (!isWeapon && !isShield && !isAccessory)
            {
                return false;
            }

            int maxUses = _template.MaxUses;
            return maxUses <= 0 ? false : RemainingUses < maxUses;
        }

        public bool IsForgeableWeaponOrMagic()
        {
            if (_template == null)
            {
                return false;
            }

            if (!_template.Forgeable)
            {
                return false;
            }

            var subtype = _template.Subtype;
            return subtype?.IsWeapon == true || subtype?.IsMagic == true;
        }

        public float GetDurabilityPercentage()
        {
            return _template == null || !_template.Durability || _template.MaxUses <= 0 ? 1f : (float)RemainingUses / _template.MaxUses;
        }
    }
}
