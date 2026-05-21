using System.Collections.Generic;
using System.Linq;
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
    ///
    /// Two parallel collections track different item kinds:
    /// - <see cref="_materials"/>: stackable/generic quantities, keyed by <see cref="ObjectItem"/> template.
    ///   Written by <see cref="AddMaterials"/> (vendor purchases, crafting drops, etc.).
    ///   Used for quantity checks like repair costs and gift availability.
    /// - <see cref="_storedItems"/>: unique <see cref="ObjectItemInstance"/> objects with individual
    ///   state (durability, InstanceID, owner). Written by <see cref="DepositItem"/> when a character
    ///   stows a specific owned item so it can be withdrawn and assigned later intact.
    ///   Also writes a corresponding count into <see cref="_materials"/> so both collections stay in sync.
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
            if (tryLoadGold == int.MinValue)
            {
                PlayerGold = GameplayGeneralSettings.Instance.StartingGold;
                SaveGoldToLTM();
            }
            else
            {
                PlayerGold = tryLoadGold;
            }

            LoadStorehouse();
        }

        private LongTermMemory _ltm;

        // Stackable/generic item counts keyed by template. Covers all items added via AddMaterials
        // (shop purchases, quest rewards, etc.) and is kept in sync when items are deposited/withdrawn.
        private Dictionary<ObjectItem, int> _materials = new();

        // Unique item instances with per-item state (durability, InstanceID). Only populated via
        // DepositItem — use GetStoredItems() when you need to withdraw or inspect a specific instance.
        private List<ObjectItemInstance> _storedItems = new();

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

        public OperationResult SpendGold(int amount, bool shouldSave = true)
        {
            if (!CanAfford(amount))
            {
                return OperationResult.Failure("Insufficient gold.");
            }

            PlayerGold -= amount;
            if (shouldSave)
            {
                SaveGoldToLTM();
            }
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
                // No value stored yet.
                return int.MinValue;
            }

            var decoded = _brain.DecodeString(recalled);
            return !int.TryParse(decoded, out var parsedGold) || parsedGold < 0 ? int.MinValue : parsedGold;
        }
        #endregion

        #region Storehouse Operations

        public void SaveCurrentStorehouse()
        {
            foreach (var material in _materials)
            {
                if (material.Key != null)
                {
                    _ = _ltm.RememberInt(
                        LtmKeys.StorehouseMaterialIdKey(material.Key.Id),
                        material.Value
                    );

                    _ = _ltm.RememberInt(
                        LtmKeys.StorehouseMaterialKey(material.Key.name),
                        material.Value
                    );
                }
            }

            SaveStoredItems();
        }

        private void SaveStoredItems()
        {
            try
            {
                var settings = GamewideContextBrainHelpers.GetJsonSerializerSettings();
                var payload = Newtonsoft.Json.JsonConvert.SerializeObject(_storedItems, settings);
                _ltm.Remember(LtmKeys.StorehouseStoredItems, payload);
            }
            catch (System.Exception e)
            {
                $"StorehouseBrain.SaveStoredItems failed: {e.Message}".LogError("StorehouseBrain");
            }
        }

        public void LoadStorehouse()
        {
            // Load gold amount
            int tryLoadGold = GetGoldFromLTM();
            PlayerGold =
                tryLoadGold == int.MinValue
                    ? GameplayGeneralSettings.Instance.StartingGold
                    : tryLoadGold;

            // loop through all known materials and load their counts
            _materials.Clear();
            var allMaterialKeys = _ltm.RecallKeysByPrefix(LtmKeys.StorehouseMaterialPrefix)
                .FindAll(k => k.StartsWith(LtmKeys.StorehouseMaterialPrefix));

            foreach (var key in allMaterialKeys)
            {
                int materialCount = _ltm.RecallInt(key);
                if (materialCount <= 0)
                {
                    continue;
                }

                ObjectItem materialItem = null;

                if (key.StartsWith(LtmKeys.StorehouseMaterialIdPrefix))
                {
                    var materialId = key.Substring(LtmKeys.StorehouseMaterialIdPrefix.Length);
                    materialItem =
                        _materials.Keys.FirstOrDefault(mi => mi.Id == materialId)
                        ?? Resources
                            .FindObjectsOfTypeAll<ObjectItem>()
                            .FirstOrDefault(mi => mi.Id == materialId);

                    if (materialItem == null)
                    {
                        $"StorehouseBrain.LoadStorehouse: unresolved material ID '{materialId}' from key '{key}', skipping".LogWarning(
                            "StorehouseBrain"
                        );
                        continue;
                    }
                }
                else if (key.StartsWith(LtmKeys.StorehouseMaterialPrefix))
                {
                    var materialName = key.Substring(LtmKeys.StorehouseMaterialPrefix.Length);

                    if (materialName.StartsWith("Id_"))
                    {
                        // Skip old-style duplicate of ID key guard.
                        continue;
                    }

                    materialItem =
                        _materials.Keys.FirstOrDefault(mi => mi.name == materialName)
                        ?? Resources.Load<ObjectItem>($"Items/{materialName}")
                        ?? Resources
                            .FindObjectsOfTypeAll<ObjectItem>()
                            .FirstOrDefault(mi => mi.name == materialName);

                    if (materialItem == null)
                    {
                        $"StorehouseBrain.LoadStorehouse: unresolved material name '{materialName}' from key '{key}', skipping".LogWarning(
                            "StorehouseBrain"
                        );
                        continue;
                    }
                }

                if (materialItem != null)
                {
                    if (_materials.ContainsKey(materialItem))
                    {
                        _materials[materialItem] += materialCount;
                    }
                    else
                    {
                        _materials[materialItem] = materialCount;
                    }
                }
            }

            // Load stored instances (for durability/status tracking)
            _storedItems.Clear();
            var storedItemsJson = _ltm.Recall(LtmKeys.StorehouseStoredItems);
            if (!string.IsNullOrEmpty(storedItemsJson))
            {
                try
                {
                    var settings = GamewideContextBrainHelpers.GetJsonSerializerSettings();
                    var parsedList = Newtonsoft.Json.JsonConvert.DeserializeObject<
                        List<ObjectItemInstance>
                    >(storedItemsJson, settings);
                    if (parsedList != null)
                    {
                        _storedItems = parsedList;

                        var parsedItemCounts = new Dictionary<ObjectItem, int>();
                        foreach (var item in _storedItems)
                        {
                            if (item?.Template == null)
                            {
                                continue;
                            }

                            item.SetBrain(Brain);

                            if (!parsedItemCounts.TryGetValue(item.Template, out var parsedCount))
                            {
                                parsedCount = 0;
                            }
                            parsedItemCounts[item.Template] = parsedCount + 1;
                        }

                        // Keep existing material counts from LTM; only add templates that were not present.
                        foreach (var kv in parsedItemCounts)
                        {
                            if (!_materials.ContainsKey(kv.Key))
                            {
                                _materials[kv.Key] = kv.Value;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    $"StorehouseBrain.LoadStorehouse: failed to deserialize stored items: {e.Message}".LogWarning(
                        "StorehouseBrain"
                    );
                }
            }
        }

        public OperationResult DepositItem(ObjectItemInstance item)
        {
            if (item == null || item.Template == null)
            {
                return OperationResult.Failure("Invalid item.");
            }

            item.ClearOwnerInventory();
            item.SetBrain(Brain);

            var material = item.Template;
            _materials.TryGetValue(material, out var existingCount);
            _materials[material] = existingCount + 1;
            _storedItems.Add(item);
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
                item.SetBrain(Brain);
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

            // remove matching instance by ID first; fallback on template match.
            var storedInstance = _storedItems.FirstOrDefault(i =>
                i != null && (i.InstanceID == item.InstanceID || i.Template == item.Template)
            );
            if (storedInstance != null)
            {
                _storedItems.Remove(storedInstance);
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
                // Explicitly zero out LTM keys before removing so SaveCurrentStorehouse
                // doesn't leave a stale non-zero count behind.
                _ = _ltm.RememberInt(LtmKeys.StorehouseMaterialIdKey(material.Id), 0);
                _ = _ltm.RememberInt(LtmKeys.StorehouseMaterialKey(material.name), 0);
                _ = _materials.Remove(material);
            }
            SaveCurrentStorehouse();
            $"Consumed {amount}x {material.name} from storehouse.".LogInfo();
            return OperationResult.Successful();
        }

        public void AddMaterials(ObjectItem material, int amount, bool shouldSave = true)
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
            if (shouldSave)
            {
                SaveCurrentStorehouse();
            }
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

        public List<ObjectItemInstance> GetStoredItems() => new(_storedItems);

        #endregion
    }
}
