using System.Collections.Generic;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages shared storage (convoy/storehouse) for items and materials.
    /// Handles item deposits, withdrawals, and material management for repairs/forging.
    /// </summary>
    [RequireComponent(typeof(Brain))]
    [RequireComponent(typeof(LongTermMemory))]
    public class StorehouseBrain : BrainComponent
    {
        private GameplayGeneralSettings _gameplaySettings;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnGoldGained += HandleGoldGained;
            _brain.OnGoldSpent += HandleGoldSpent;
            _brain.OnLongTermMemoryInitialized += InitializeLTMDependentData;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnGoldGained -= HandleGoldGained;
            _brain.OnGoldSpent -= HandleGoldSpent;
            _brain.OnLongTermMemoryInitialized -= InitializeLTMDependentData;
        }

        private void HandleGoldGained(int amount) => AddGold(amount);

        private void HandleGoldSpent(int amount) => SpendGold(amount);

        private void Start()
        {
            _ltm = GetComponent<LongTermMemory>();
            _gameplaySettings = GameplayGeneralSettings.Instance;
            _materials = new Dictionary<ObjectItem, int>();
            GoldDisplayNames =
                _gameplaySettings != null
                    ? _gameplaySettings.GoldDisplayNames
                    : new GoldDisplay { OneLetter = "G", FullName = "gold" };
        }

        private void InitializeLTMDependentData()
        {
            int tryLoadGold = GetGoldFromLTM();
            if (tryLoadGold <= 0)
            {
                PlayerGold = 0;
                SaveGoldToLTM();
            }
            else
            {
                PlayerGold = tryLoadGold;
            }

            LoadStorehouse();
        }

        private LongTermMemory _ltm;

        private Dictionary<ObjectItem, int> _materials = new();

        [HideInInspector]
        public int PlayerGold;

        [HideInInspector]
        public GoldDisplay GoldDisplayNames;

        #region Gold Operations

        public void AddGold(int amount)
        {
            PlayerGold += amount;
            SaveGoldToLTM();
        }

        public bool CanAfford(int amount) => PlayerGold >= amount;

        public OperationResult SpendGold(int amount)
        {
            if (!CanAfford(amount))
            {
                return OperationResult.Failure("Insufficient gold.");
            }

            PlayerGold -= amount;
            SaveGoldToLTM();
            return OperationResult.Successful();
        }

        public void SaveGoldToLTM()
        {
            var encoded = _brain.EncodeString(PlayerGold.ToString());
            _ltm.Remember(LtmKeys.StorehousePurchasingPower, encoded.ToString());
        }

        public int GetGoldFromLTM()
        {
            var recalled = _ltm.Recall(LtmKeys.StorehousePurchasingPower);
            if (recalled == null)
            {
                return GameplayGeneralSettings.Instance.StartingGold;
            }

            var decoded = _brain.DecodeString(recalled);
            return int.Parse(decoded);
        }
        #endregion

        #region Storehouse Operations

        public void SaveCurrentStorehouse()
        {
            foreach (var material in _materials)
            {
                _ = _ltm.RememberInt(
                    LtmKeys.StorehouseMaterialKey(material.Key.name),
                    material.Value
                );
            }
        }

        public void LoadStorehouse()
        {
            // Load gold amount
            PlayerGold =
                int.TryParse(_ltm.Recall(LtmKeys.StorehousePurchasingPower), out int recalledGold)
                && recalledGold >= 0
                    ? recalledGold
                    : GameplayGeneralSettings.Instance.StartingGold;

            // loop through all known materials and load their counts
            _materials.Clear();
            var allMaterialKeys = _ltm.RecallKeysByPrefix(LtmKeys.StorehouseMaterialPrefix)
                .FindAll(k => k.StartsWith(LtmKeys.StorehouseMaterialPrefix));
            foreach (var key in allMaterialKeys)
            {
                var materialName = key.Replace(LtmKeys.StorehouseMaterialPrefix, "");
                var materialCount = _ltm.RecallInt(key);
                var materialItem = Resources.Load<ObjectItem>($"Items/{materialName}");
                if (materialItem != null && materialCount > 0)
                {
                    _materials[materialItem] = materialCount;
                }
            }
        }

        public OperationResult DepositItem(ObjectItemInstance item)
        {
            if (item == null || item.Template == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            var material = item.Template;
            _materials.TryGetValue(material, out var existingCount);
            _materials[material] = existingCount + 1;
            SaveCurrentStorehouse();

            Brain.PublishItemDeposited(item);
            $"Deposited {item.Template.name} into storehouse (total {GetItemCountInStorehouse(material)}).".LogInfo();

            return OperationResult.Successful();
        }

        public OperationResult WithdrawItem(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        )
        {
            if (item == null || item.Template == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            var material = item.Template;
            if (!_materials.TryGetValue(material, out var count) || count <= 0)
            {
                return OperationResult.Failure("Item not in storehouse.");
            }

            if (targetInventory != null && targetInventory.IsFull)
            {
                return OperationResult.Failure("Target inventory is full.");
            }

            if (targetInventory != null)
            {
                var addRes = targetInventory.AddToInventory(item);
                if (!addRes.Success)
                {
                    return addRes;
                }
            }

            if (count <= 1)
            {
                _materials.Remove(material);
            }
            else
            {
                _materials[material] = count - 1;
            }

            SaveCurrentStorehouse();
            Brain.PublishItemWithdrawn(item, targetInventory);

            $"Withdrew {item.Template.name} from storehouse (remaining {GetItemCountInStorehouse(material)}).".LogInfo();
            return OperationResult.Successful();
        }

        public bool HasMaterials(ObjectItem material, int amount) =>
            material != null
            && amount > 0
            && _materials.TryGetValue(material, out var count)
            && count >= amount;

        public OperationResult ConsumeMaterials(ObjectItem material, int amount)
        {
            if (!HasMaterials(material, amount))
            {
                return OperationResult.Failure("Insufficient materials.");
            }

            _materials[material] -= amount;

            if (_materials[material] <= 0)
            {
                _ = _materials.Remove(material);
            }
            SaveCurrentStorehouse();
            $"Consumed {amount}x {material.name} from storehouse.".LogInfo();
            return OperationResult.Successful();
        }

        public void AddMaterials(ObjectItem material, int amount)
        {
            if (material == null || amount <= 0)
            {
                return;
            }

            // Use TryGetValue to avoid double lookup
            if (!_materials.TryGetValue(material, out var currentCount))
            {
                currentCount = 0;
            }

            _materials[material] = currentCount + amount;
            SaveCurrentStorehouse();
            $"Added {amount}x {material.name} to storehouse.".LogInfo();
        }

        public int GetMaterialCount(ObjectItem material) =>
            material == null ? 0
            : _materials.TryGetValue(material, out var count) ? count
            : 0;

        #endregion

        #region Queries

        public Dictionary<ObjectItem, int> GetAllMaterials() => new(_materials);

        public int GetItemCountInStorehouse(ObjectItem item) =>
            item == null ? 0 : (_materials.TryGetValue(item, out var count) ? count : 0);

        public List<ObjectItemInstance> GetStoredItems() => new(); // old semantics removed, use material counts instead.
        #endregion
    }
}
