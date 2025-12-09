using System.Collections.Generic;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages shared storage (convoy/storehouse) for items and materials.
    /// Handles item deposits, withdrawals, and material management for repairs/forging.
    /// </summary>
    [RequireComponent(typeof(Brain))]
    [RequireComponent(typeof(LongTermMemory))]
    public class StorehouseBrain : MonoBehaviour
    {
        GameplayGeneralSettings _gameplaySettings;

        private void Awake()
        {
            _ltm = GetComponent<LongTermMemory>();
            _gameplaySettings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<GameplayGeneralSettings>();
            _brain = GetComponent<Brain>();
            _materials = new Dictionary<ObjectItem, int>();
            GoldDisplayNames =
                _gameplaySettings != null
                    ? _gameplaySettings.GoldDisplayNames
                    : new GoldDisplay { OneLetter = "G", FullName = "gold" };
        }

        private Brain _brain;
        private LongTermMemory _ltm;

        [SerializeField, HideInInspector]
        private List<ObjectItemInstance> _storedItems = new();

        private Dictionary<ObjectItem, int> _materials = new();

        [SerializeField, HideInInspector]
        private int PlayerGold { get; set; } = 0;

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
            return OperationResult.SuccessResult();
        }

        public void SaveGoldToLTM() => _ltm.RememberInt("Storehouse_Gold", PlayerGold);

        #endregion

        #region Storehouse Operations

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
            _brain?.PublishItemDeposited(item);

            Debug.Log($"Deposited {item.Template.name} into storehouse.");
            return OperationResult.SuccessResult();
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

            _storedItems.Remove(item);

            targetInventory?.AddToInventory(item);

            _brain?.PublishItemWithdrawn(item, targetInventory);

            Debug.Log($"Withdrew {item.Template.name} from storehouse.");
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Check if the storehouse has sufficient materials for an operation.
        /// </summary>
        public bool HasMaterials(ObjectItem material, int amount) =>
            material != null
            && amount > 0
            && _materials.ContainsKey(material)
            && _materials[material] >= amount;

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
                _materials.Remove(material);
            }

            Debug.Log($"Consumed {amount}x {material.name} from storehouse.");
            return OperationResult.SuccessResult();
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

            if (!_materials.ContainsKey(material))
            {
                _materials[material] = 0;
            }

            _materials[material] += amount;
            Debug.Log($"Added {amount}x {material.name} to storehouse.");
        }

        /// <summary>
        /// Get the count of a specific material.
        /// </summary>
        public int GetMaterialCount(ObjectItem material) =>
            material == null ? 0
            : _materials.ContainsKey(material) ? _materials[material]
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
