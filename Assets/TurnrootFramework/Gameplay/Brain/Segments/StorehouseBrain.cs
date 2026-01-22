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
        GameplayGeneralSettings _gameplaySettings;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnGoldGained += HandleGoldGained;
            _brain.OnGoldSpent += HandleGoldSpent;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnGoldGained -= HandleGoldGained;
            _brain.OnGoldSpent -= HandleGoldSpent;
        }

        private void HandleGoldGained(int amount) => AddGold(amount);

        private void HandleGoldSpent(int amount) => SpendGold(amount);

        private void Start()
        {
            _ltm = GetComponent<LongTermMemory>();
            _gameplaySettings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>();
            _materials = new Dictionary<ObjectItem, int>();
            GoldDisplayNames =
                _gameplaySettings != null
                    ? _gameplaySettings.GoldDisplayNames
                    : new GoldDisplay { OneLetter = "G", FullName = "gold" };

            // Load saved gold amount
            int tryLoadGold = GetGoldFromLTM();
            if (tryLoadGold <= 0)
            {
                PlayerGold = 0;
                SaveGoldToLTM();
                SaveCurrentStorehouse();
            }
            else
            {
                PlayerGold = tryLoadGold;
            }
        }

        private LongTermMemory _ltm;

        [SerializeField, HideInInspector]
        private List<ObjectItemInstance> _storedItems = new();

        private Dictionary<ObjectItem, int> _materials = new();

        [HideInInspector]
        private int PlayerGold { get; set; } = 0;

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
                return 0;
            }

            var decoded = _brain.DecodeString(recalled);
            return int.Parse(decoded);
        }
        #endregion

        #region Storehouse Operations


        /// <summary>
        /// Save the current state of the storehouse to long-term memory.
        /// </summary>
        public void SaveCurrentStorehouse()
        {
            // loop through _materials and save each material count
            foreach (var material in _materials)
            {
                _ = _ltm.RememberInt(
                    LtmKeys.StorehouseMaterialKey(material.Key.name),
                    material.Value
                );
            }
            // save a single string with all stored item IDs, separated by commas
            var itemIds = string.Join(",", _storedItems.ConvertAll(i => i.InstanceID.ToString()));
            _ltm.Remember(LtmKeys.StorehouseStoredItems, itemIds);
        }

        public void LoadStorehouse()
        {
            // Load gold amount
            PlayerGold =
                int.TryParse(_ltm.Recall(LtmKeys.StorehousePurchasingPower), out int recalledGold)
                && recalledGold >= 0
                    ? recalledGold
                    : 0;
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

            // Load stored items by their IDs
            _storedItems.Clear();
            var storedItemIdsString = _ltm.Recall(LtmKeys.StorehouseStoredItems);
            if (!string.IsNullOrEmpty(storedItemIdsString))
            {
                var itemIds = storedItemIdsString.Split(',');
                var allItems =
                    _brain?.inventoryBrain?.GetAllItems() ?? new List<ObjectItemInstance>();
                foreach (var id in itemIds)
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    var item = allItems.Find(i => i.InstanceID == id);
                    if (item != null)
                    {
                        _storedItems.Add(item);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"StorehouseBrain.LoadStorehouse: Could not find item with ID '{id}'"
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Deposit an item into the storehouse.
        /// </summary>
        ///
        public OperationResult DepositItem(ObjectItemInstance item)
        {
            if (item == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            _storedItems.Add(item);
            SaveCurrentStorehouse();
            _brain?.PublishItemDeposited(item);

#if UNITY_EDITOR
            Debug.Log($"Deposited {item.Template.name} into storehouse.");
#endif
            return OperationResult.Successful();
        }

        /// <summary>
        /// Withdraw an item from the storehouse.
        /// </summary>
        public OperationResult WithdrawItem(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        )
        {
            if (item == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            if (!_storedItems.Contains(item))
            {
                return OperationResult.Failure("Item not in storehouse.");
            }

            if (targetInventory != null && targetInventory.IsFull)
            {
                return OperationResult.Failure("Target inventory is full.");
            }

            // Attempt to add to target inventory first to ensure transfer succeeds
            if (targetInventory != null)
            {
                var addRes = targetInventory.AddToInventory(item);
                if (!addRes.Success)
                {
                    return addRes;
                }
            }

            _ = _storedItems.Remove(item);
            SaveCurrentStorehouse();

            _brain?.PublishItemWithdrawn(item, targetInventory);

#if UNITY_EDITOR
            Debug.Log($"Withdrew {item.Template.name} from storehouse.");
#endif
            return OperationResult.Successful();
        }

        /// <summary>
        /// Check if the storehouse has sufficient materials for an operation.
        /// </summary>
        public bool HasMaterials(ObjectItem material, int amount) =>
            material != null
            && amount > 0
            && _materials.TryGetValue(material, out var count)
            && count >= amount;

        /// <summary>
        /// Consume materials from the storehouse.
        /// </summary>
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
#if UNITY_EDITOR
            Debug.Log($"Consumed {amount}x {material.name} from storehouse.");
#endif
            return OperationResult.Successful();
        }

        /// <summary>
        /// Add materials to the storehouse.
        /// </summary>
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
#if UNITY_EDITOR
            Debug.Log($"Added {amount}x {material.name} to storehouse.");
#endif
        }

        /// <summary>
        /// Get the count of a specific material.
        /// </summary>
        public int GetMaterialCount(ObjectItem material) =>
            material == null ? 0
            : _materials.TryGetValue(material, out var count) ? count
            : 0;

        #endregion

        #region Queries

        /// <summary>
        /// Get all items currently in the storehouse.
        /// </summary>
        public List<ObjectItemInstance> GetStoredItems() => new(_storedItems);

        /// <summary>
        /// Get all available materials and their counts.
        /// </summary>
        public Dictionary<ObjectItem, int> GetAllMaterials() => new(_materials);

        #endregion
    }
}
