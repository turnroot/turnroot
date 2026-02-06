using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Objects;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.GameSettings;
using Turnroot.Serialization;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.Components
{
    /// <summary>
    /// Character inventory with automatic weapon slot management and LTM persistence.
    /// Slot 0 is reserved for weapons and auto-equips next weapon when emptied.
    /// </summary>
    [Serializable]
    public class CharacterInventoryInstance : IPostDeserialize
    {
        [SerializeField]
        private List<ObjectItemInstance> _inventoryItems = new();

        [SerializeField]
        private int _capacity = 6;

        [SerializeField]
        private int[] _equippedItemIndices;

        [SerializeField]
        private bool _isWeaponEquipped;

        [SerializeField]
        private bool[] _nonWeaponEquippedFlags;

        private bool _isInitialized = false;

        public List<ObjectItemInstance> InventoryItems => _inventoryItems;
        public int Capacity => _capacity;
        public int CurrentItemCount => _inventoryItems.Count;
        public bool IsFull => _inventoryItems.Count >= _capacity;

        public ObjectItemInstance[] Items() => _inventoryItems.ToArray();

        public int[] EquippedItemIndices => _equippedItemIndices;
        public bool IsWeaponEquipped => _isWeaponEquipped;
        public bool IsShieldEquipped => GetNonWeaponSlotEquipped(0);
        public bool IsAccessoryEquipped => GetNonWeaponSlotEquipped(1);

        public int CurrentWeight
        {
            get
            {
                float weight = 0;
                foreach (var item in _inventoryItems)
                {
                    if (item != null)
                    {
                        weight += item.Template.Weight;
                    }
                }
                return (int)weight;
            }
        }

        private int MaxNonWeaponSlots => CharacterSettings.MaxNonWeaponSlots;

        public CharacterInventoryInstance(int capacity = 6)
        {
            _capacity = capacity;
            _inventoryItems = new List<ObjectItemInstance>();
            EnsureEquipmentArraysInitialized();
        }

        public CharacterInventoryInstance()
            : this(6) { }

        public CharacterInventoryInstance(int capacity, List<ObjectItem> startingItems)
        {
            _capacity = capacity;
            _inventoryItems = new List<ObjectItemInstance>();
            foreach (var item in startingItems)
            {
                _inventoryItems.Add(new ObjectItemInstance(item));
            }
            EnsureEquipmentArraysInitialized();
        }

        public void OnAfterDeserialize() => EnsureEquipmentArraysInitialized();

        private void EnsureEquipmentArraysInitialized()
        {
            int maxNonWeapon = MaxNonWeaponSlots;
            int totalSlots = 1 + maxNonWeapon;

            bool needsResize =
                !_isInitialized
                || _equippedItemIndices == null
                || _equippedItemIndices.Length != totalSlots
                || _nonWeaponEquippedFlags == null
                || _nonWeaponEquippedFlags.Length != maxNonWeapon;

            if (!needsResize)
            {
                return;
            }

            if (_equippedItemIndices == null || _equippedItemIndices.Length != totalSlots)
            {
                var oldIndices = _equippedItemIndices;
                _equippedItemIndices = new int[totalSlots];
                for (int i = 0; i < _equippedItemIndices.Length; i++)
                {
                    _equippedItemIndices[i] =
                        (oldIndices != null && i < oldIndices.Length) ? oldIndices[i] : -1;
                }
            }

            if (_nonWeaponEquippedFlags == null || _nonWeaponEquippedFlags.Length != maxNonWeapon)
            {
                var oldFlags = _nonWeaponEquippedFlags;
                _nonWeaponEquippedFlags = new bool[maxNonWeapon];
                if (oldFlags != null)
                {
                    for (
                        int i = 0;
                        i < Math.Min(oldFlags.Length, _nonWeaponEquippedFlags.Length);
                        i++
                    )
                    {
                        _nonWeaponEquippedFlags[i] = oldFlags[i];
                    }
                }
            }

            _isInitialized = true;
        }

        private bool GetNonWeaponSlotEquipped(int slotIndex)
        {
            EnsureEquipmentArraysInitialized();
            return slotIndex >= 0
                && slotIndex < _nonWeaponEquippedFlags.Length
                && _nonWeaponEquippedFlags[slotIndex];
        }

        private int GetSlotIndexForItem(ObjectItem item)
        {
            return item.Subtype == ObjectSubtype.Weapon ? 0
                : item.Subtype == ObjectSubtype.Shield ? 1
                : item.Subtype == ObjectSubtype.Equipable
                    ? item.EquipableType switch
                    {
                        EquipableObjectType.Shield => 1,
                        EquipableObjectType.Accessory => 2,
                        EquipableObjectType.Ring => 2,
                        EquipableObjectType.Staff => 0,
                        _ => -1,
                    }
                : -1;
        }

        private void SetEquippedFlag(int slotIndex, bool isEquipped)
        {
            EnsureEquipmentArraysInitialized();

            if (slotIndex == 0)
            {
                _isWeaponEquipped = isEquipped;
            }
            else if (slotIndex > 0 && slotIndex <= MaxNonWeaponSlots)
            {
                _nonWeaponEquippedFlags[slotIndex - 1] = isEquipped;
            }
        }

        public int GetEquippedWeaponIndex() =>
            !_isWeaponEquipped || _equippedItemIndices == null || _equippedItemIndices.Length == 0
                ? -1
                : _equippedItemIndices[0];

        public bool IsItemEquipped(ObjectItemInstance item)
        {
            int index = _inventoryItems.IndexOf(item);
            return index >= 0 && Array.IndexOf(_equippedItemIndices, index) >= 0;
        }

        public bool CanAddItem() => _inventoryItems.Count < _capacity;

        public OperationResult AddToInventory(ObjectItemInstance item)
        {
            if (!CanAddItem())
            {
                return OperationResult.Failure("Inventory is full. Cannot add item.");
            }

            _inventoryItems.Add(item);
            int newIndex = _inventoryItems.Count - 1;
            item.Slot = newIndex;
            item.SetOwnerInventory(this);
            return OperationResult.Successful();
        }

        public OperationResult RemoveFromInventory(ObjectItemInstance item)
        {
            int index = _inventoryItems.IndexOf(item);
            if (index < 0)
            {
                return OperationResult.Failure("Item not found in inventory. Cannot remove item.");
            }

            var removedItem = _inventoryItems[index];
            bool wasEquippedWeaponInSlot0 = removedItem.Slot == 0 && removedItem.IsEquipped;

            _inventoryItems.RemoveAt(index);
            removedItem.ClearOwnerInventory();

            for (int i = index; i < _inventoryItems.Count; i++)
            {
                _inventoryItems[i].Slot = i;
            }

            for (int i = 0; i < _equippedItemIndices.Length; i++)
            {
                if (_equippedItemIndices[i] == index)
                {
                    _equippedItemIndices[i] = -1;
                    SetEquippedFlag(i, false);
                }
                else if (_equippedItemIndices[i] > index)
                {
                    _equippedItemIndices[i]--;
                }
            }

            if (wasEquippedWeaponInSlot0)
            {
                AutoEquipNextWeapon();
            }

            return OperationResult.Successful();
        }

        private void AutoEquipNextWeapon()
        {
            // Find next available weapon and equip it
            foreach (var item in _inventoryItems)
            {
                if (
                    item.Template != null
                    && !item.IsEquipped
                    && item.Template.Subtype == ObjectSubtype.Weapon
                )
                {
                    item.IsEquipped = true;
                    item.Slot = 0; // Move to slot 0 for visual organization
                    return;
                }
            }
        }

        public OperationResult EquipItem(int index)
        {
            if (index < 0 || index >= _inventoryItems.Count)
            {
                return OperationResult.Failure("Invalid inventory index. Cannot equip item.");
            }

            ObjectItemInstance itemToEquip = _inventoryItems[index];
            int slotIndex = GetSlotIndexForItem(itemToEquip.Template);

            if (slotIndex == -1)
            {
                return OperationResult.Failure("Item type cannot be equipped.");
            }

            if (_equippedItemIndices[slotIndex] != -1)
            {
                var res = UnequipItemFromSlot(slotIndex);
                if (!res.Success)
                {
                    return res;
                }
            }

            _equippedItemIndices[slotIndex] = index;
            SetEquippedFlag(slotIndex, true);
            itemToEquip.IsEquipped = true;
            itemToEquip.Slot = slotIndex; // Move to slot for visual organization
            return OperationResult.Successful();
        }

        public OperationResult UnequipItem(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= _inventoryItems.Count)
            {
                return OperationResult.Failure("Invalid inventory index. Cannot unequip item.");
            }

            for (int i = 0; i < _equippedItemIndices.Length; i++)
            {
                if (_equippedItemIndices[i] == inventoryIndex)
                {
                    return UnequipItemFromSlot(i);
                }
            }

            return OperationResult.Failure("Item is not currently equipped.");
        }

        private OperationResult UnequipItemFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _equippedItemIndices.Length)
            {
                return OperationResult.Failure("Invalid slot index for unequip.");
            }

            int itemIndex = _equippedItemIndices[slotIndex];
            if (itemIndex >= 0 && itemIndex < _inventoryItems.Count)
            {
                _inventoryItems[itemIndex].IsEquipped = false;
            }

            _equippedItemIndices[slotIndex] = -1;
            SetEquippedFlag(slotIndex, false);
            return OperationResult.Successful();
        }

        public void UnequipAllItems()
        {
            EnsureEquipmentArraysInitialized();

            for (int i = 0; i < _equippedItemIndices.Length; i++)
            {
                _equippedItemIndices[i] = -1;
            }

            _isWeaponEquipped = false;
            for (int i = 0; i < _nonWeaponEquippedFlags.Length; i++)
            {
                _nonWeaponEquippedFlags[i] = false;
            }
        }

#if UNITY_EDITOR
        public void OnValidate() => EnsureEquipmentArraysInitialized();
#endif
    }
}
