using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inventory system for characters. Each character has their own CharacterInventoryInstance
/// that tracks their specific inventory state.
///
/// NOTE: This does NOT use a ScriptableObject template because:
/// - Inventory state is always per-character (never shared)
/// - No benefit to having a "template" inventory
/// - Simpler to just create new instances directly
/// </summary>
[Serializable]
public class CharacterInventoryInstance
{
    [SerializeField]
    private List<ObjectItem> _inventoryItems = new List<ObjectItem>();

    [SerializeField]
    private int _capacity = 6;

    // Equipment slots: [0] = Weapon, [1+] = Non-weapon equipables (shields, accessories, etc.)
    // Size is dynamic based on GameplayGeneralSettings
    [SerializeField]
    private int[] _equippedItemIndices;

    [SerializeField]
    private bool _isWeaponEquipped;

    // Non-weapon equipped flags - size matches non-weapon slot count
    [SerializeField]
    private bool[] _nonWeaponEquippedFlags;

    public List<ObjectItem> InventoryItems => _inventoryItems;
    public int Capacity => _capacity;
    public int CurrentItemCount => _inventoryItems.Count;

    public int CurrentWeight
    {
        get
        {
            float weight = 0;
            foreach (var item in _inventoryItems)
            {
                if (item != null)
                    weight += item.Weight;
            }
            return (int)weight;
        }
    }

    public int[] EquippedItemIndices => _equippedItemIndices;

    public bool IsWeaponEquipped => _isWeaponEquipped;

    /// <summary>
    /// Gets the maximum number of non-weapon items that can be equipped from settings.
    /// </summary>
    private int MaxNonWeaponSlots =>
        GameplayGeneralSettings.Instance.GetMaxEquippedNonWeaponItems();

    /// <summary>
    /// Ensures equipment arrays are properly sized based on current settings.
    /// </summary>
    private void EnsureEquipmentArraysInitialized()
    {
        int totalSlots = 1 + MaxNonWeaponSlots; // 1 weapon slot + N non-weapon slots

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

        if (_nonWeaponEquippedFlags == null || _nonWeaponEquippedFlags.Length != MaxNonWeaponSlots)
        {
            var oldFlags = _nonWeaponEquippedFlags;
            _nonWeaponEquippedFlags = new bool[MaxNonWeaponSlots];
            if (oldFlags != null)
            {
                for (
                    int i = 0;
                    i < System.Math.Min(oldFlags.Length, _nonWeaponEquippedFlags.Length);
                    i++
                )
                {
                    _nonWeaponEquippedFlags[i] = oldFlags[i];
                }
            }
        }
    }

    public bool IsShieldEquipped => GetNonWeaponSlotEquipped(0);
    public bool IsAccessoryEquipped => GetNonWeaponSlotEquipped(1);

    private bool GetNonWeaponSlotEquipped(int slotIndex)
    {
        EnsureEquipmentArraysInitialized();
        return slotIndex >= 0
            && slotIndex < _nonWeaponEquippedFlags.Length
            && _nonWeaponEquippedFlags[slotIndex];
    }

    /// <summary>
    /// Create a new empty inventory with specified capacity
    /// </summary>
    public CharacterInventoryInstance(int capacity = 6)
    {
        _capacity = capacity;
        _inventoryItems = new List<ObjectItem>();
        EnsureEquipmentArraysInitialized();
    }

    /// <summary>
    /// Create inventory with starting items
    /// </summary>
    public CharacterInventoryInstance(int capacity, List<ObjectItem> startingItems)
    {
        _capacity = capacity;
        _inventoryItems = new List<ObjectItem>(startingItems);
        EnsureEquipmentArraysInitialized();
    }

    /// <summary>
    /// Gets the equipment slot index for an item based on its type.
    /// Returns: 0=Weapon, 1=Shield, 2=Accessory, -1=Not Equipable
    /// </summary>
    private int GetSlotIndexForItem(ObjectItem item)
    {
        // Weapons (including magic/staff) go in weapon slot
        if (item.Subtype == ObjectSubtype.Weapon)
        {
            return 0;
        }

        // Equipable items use their EquipableObjectType
        if (item.Subtype == ObjectSubtype.Equipable)
        {
            return item.EquipableType switch
            {
                EquipableObjectType.Shield => 1,
                EquipableObjectType.Accessory => 2,
                EquipableObjectType.Ring => 2, // Rings also use accessory slot
                EquipableObjectType.Staff => 0, // Staves can go in weapon slot
                _ => -1,
            };
        }

        // Other subtypes (Consumable, Gift, etc.) are not equipable
        return -1;
    }

    /// <summary>
    /// Updates the equipped flag for a specific slot.
    /// </summary>
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

    public int GetEquippedItemIndex(EquipableObjectType itemType)
    {
        // For equipable types, find first item matching that type
        for (int i = 0; i < _inventoryItems.Count; i++)
        {
            if (
                _inventoryItems[i].Subtype == ObjectSubtype.Equipable
                && _inventoryItems[i].EquipableType == itemType
                && IsItemEquipped(_inventoryItems[i])
            )
            {
                return i;
            }
        }
        return -1;
    }

    public bool IsItemEquipped(ObjectItem item)
    {
        int index = _inventoryItems.IndexOf(item);
        if (index < 0)
            return false;

        return Array.IndexOf(_equippedItemIndices, index) >= 0;
    }

    public bool CanAddItem(ObjectItem item)
    {
        return _inventoryItems.Count < _capacity;
    }

    public void AddToInventory(ObjectItem item)
    {
        if (!CanAddItem(item))
        {
            Debug.LogWarning("Inventory is full. Cannot add item.");
            return;
        }

        _inventoryItems.Add(item);
    }

    public void RemoveFromInventory(ObjectItem item)
    {
        int index = _inventoryItems.IndexOf(item);
        if (index < 0)
        {
            Debug.LogWarning("Item not found in inventory. Cannot remove item.");
            return;
        }

        _inventoryItems.RemoveAt(index);

        // Update equipped indices if the removed item was equipped or affects indices
        for (int i = 0; i < _equippedItemIndices.Length; i++)
        {
            if (_equippedItemIndices[i] == index)
            {
                // Item was equipped, unequip it
                _equippedItemIndices[i] = -1;
                SetEquippedFlag(i, false);
            }
            else if (_equippedItemIndices[i] > index)
            {
                // Adjust equipped index if necessary
                _equippedItemIndices[i]--;
            }
        }
    }

    public void EquipItem(int index)
    {
        if (index < 0 || index >= _inventoryItems.Count)
        {
            Debug.LogWarning("Invalid inventory index. Cannot equip item.");
            return;
        }

        ObjectItem itemToEquip = _inventoryItems[index];

        int slotIndex = GetSlotIndexForItem(itemToEquip);

        if (slotIndex == -1)
        {
            Debug.LogWarning("Item type cannot be equipped.");
            return;
        }

        // Unequip currently equipped item in this slot if any
        if (_equippedItemIndices[slotIndex] != -1)
        {
            UnequipItemFromSlot(slotIndex);
        }

        _equippedItemIndices[slotIndex] = index;
        SetEquippedFlag(slotIndex, true);
    }

    public void UnequipItem(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= _inventoryItems.Count)
        {
            Debug.LogWarning("Invalid inventory index. Cannot unequip item.");
            return;
        }

        for (int i = 0; i < _equippedItemIndices.Length; i++)
        {
            if (_equippedItemIndices[i] == inventoryIndex)
            {
                UnequipItemFromSlot(i);
                return;
            }
        }

        Debug.LogWarning("Item is not currently equipped.");
    }

    private void UnequipItemFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _equippedItemIndices.Length)
            return;

        _equippedItemIndices[slotIndex] = -1;
        SetEquippedFlag(slotIndex, false);
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
    /// <summary>
    /// Called when the object is validated in the Unity Editor.
    /// Ensures equipment arrays are resized when settings change.
    /// </summary>
    public void OnValidate()
    {
        // Ensure arrays are properly sized based on current settings
        EnsureEquipmentArraysInitialized();
    }
#endif
}
