using System.Collections.Generic;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages shared storage (convoy/storehouse) for items and materials.
    /// Handles item deposits, withdrawals, and material management for repairs/forging.
    /// </summary>
    [RequireComponent(typeof(Brain))]
    public class StorehouseBrain : MonoBehaviour
    {
        private Brain _brain;

        [SerializeField]
        private List<ObjectItemInstance> _storedItems = new();

        private Dictionary<ObjectItem, int> _materials = new();

        private void Awake()
        {
            _brain = GetComponent<Brain>();
            _materials = new Dictionary<ObjectItem, int>();
        }

        #region Storehouse Operations

        /// <summary>
        /// Deposit an item into the storehouse.
        /// </summary>
        public OperationResult DepositItem(ObjectItemInstance item)
        {
            if (item == null)
                return OperationResult.Failure("Invalid item.");

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
                return OperationResult.Failure("Invalid item.");

            if (!_storedItems.Contains(item))
                return OperationResult.Failure("Item not in storehouse.");

            if (targetInventory != null && targetInventory.IsFull)
                return OperationResult.Failure("Target inventory is full.");

            _storedItems.Remove(item);

            if (targetInventory != null)
            {
                targetInventory.AddToInventory(item);
            }

            _brain?.PublishItemWithdrawn(item, targetInventory);

            Debug.Log($"Withdrew {item.Template.name} from storehouse.");
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Check if the storehouse has sufficient materials for an operation.
        /// </summary>
        public bool HasMaterials(ObjectItem material, int amount)
        {
            if (material == null || amount <= 0)
                return false;

            return _materials.ContainsKey(material) && _materials[material] >= amount;
        }

        /// <summary>
        /// Consume materials from the storehouse.
        /// </summary>
        public OperationResult ConsumeMaterials(ObjectItem material, int amount)
        {
            if (!HasMaterials(material, amount))
                return OperationResult.Failure("Insufficient materials.");

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
                return;

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
        public int GetMaterialCount(ObjectItem material)
        {
            if (material == null)
                return 0;

            return _materials.ContainsKey(material) ? _materials[material] : 0;
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get all items currently in the storehouse.
        /// </summary>
        public List<ObjectItemInstance> GetStoredItems()
        {
            return new List<ObjectItemInstance>(_storedItems);
        }

        /// <summary>
        /// Get all available materials and their counts.
        /// </summary>
        public Dictionary<ObjectItem, int> GetAllMaterials()
        {
            return new Dictionary<ObjectItem, int>(_materials);
        }

        #endregion
    }
}
