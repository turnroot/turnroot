using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Objects;
using Turnroot.Gameplay.Objects.Components;
using UnityEngine;

/// <summary>
/// Inventory system for characters. Each character has their own CharacterInventoryInstance
/// that tracks their specific inventory state.
/// </summary>
[Serializable]
public class CharacterInventoryInstance
{
    [SerializeField]
    private List<ObjectItemInstance> _inventoryItems = new List<ObjectItemInstance>();

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

    // Cached settings value to avoid repeated singleton lookups
    private int _cachedMaxNonWeaponSlots = -1;
    private bool _isInitialized = false;

    public List<ObjectItemInstance> InventoryItems => _inventoryItems;
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
                    weight += item.Template.Weight;
            }
            return (int)weight;
        }
    }

    public int[] EquippedItemIndices => _equippedItemIndices;

    public bool IsWeaponEquipped => _isWeaponEquipped;

    /// <summary>
    /// Gets the maximum number of non-weapon items that can be equipped from settings.
    /// Cached to avoid repeated singleton lookups.
    /// </summary>
    private int MaxNonWeaponSlots
    {
        get
        {
            // Get current value from settings
            int currentValue = GameplayGeneralSettings.Instance.GetMaxEquippedNonWeaponItems();

            // If cached value changed, invalidate initialization
            if (_cachedMaxNonWeaponSlots != currentValue)
            {
                _cachedMaxNonWeaponSlots = currentValue;
                _isInitialized = false;
            }

            return _cachedMaxNonWeaponSlots;
        }
    }

    /// <summary>
    /// Ensures equipment arrays are properly sized based on current settings.
    /// Uses caching and a flag to avoid redundant reallocations.
    /// </summary>
    private void EnsureEquipmentArraysInitialized()
    {
        int maxNonWeapon = MaxNonWeaponSlots; // Cache the value for this method call
        int totalSlots = 1 + maxNonWeapon; // 1 weapon slot + N non-weapon slots

        // Check if we need to initialize/resize
        bool needsResize =
            !_isInitialized
            || _equippedItemIndices == null
            || _equippedItemIndices.Length != totalSlots
            || _nonWeaponEquippedFlags == null
            || _nonWeaponEquippedFlags.Length != maxNonWeapon;

        if (!needsResize)
            return;

        // Resize equipped item indices
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
                    i < System.Math.Min(oldFlags.Length, _nonWeaponEquippedFlags.Length);
                    i++
                )
                {
                    _nonWeaponEquippedFlags[i] = oldFlags[i];
                }
            }
        }

        _isInitialized = true;
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
        _inventoryItems = new List<ObjectItemInstance>();
        EnsureEquipmentArraysInitialized();
    }

    /// <summary>
    /// Create inventory with starting items
    /// </summary>
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

    /// <summary>
    /// Gets the inventory index of an equipped item by its equipable type.
    /// </summary>
    /// <param name="itemType">The equipable type to search for.</param>
    /// <returns>The inventory index of the equipped item, or -1 if not found.</returns>
    public int GetEquippedItemIndex(EquipableObjectType itemType)
    {
        // For equipable types, find first item matching that type
        for (int i = 0; i < _inventoryItems.Count; i++)
        {
            if (
                _inventoryItems[i].Template.Subtype == ObjectSubtype.Equipable
                && _inventoryItems[i].Template.EquipableType == itemType
                && IsItemEquipped(_inventoryItems[i])
            )
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets the inventory index of an equipped weapon.
    /// </summary>
    /// <returns>The inventory index of the equipped weapon, or -1 if no weapon is equipped.</returns>
    public int GetEquippedWeaponIndex()
    {
        if (!_isWeaponEquipped || _equippedItemIndices == null || _equippedItemIndices.Length == 0)
            return -1;

        return _equippedItemIndices[0];
    }

    /// <summary>
    /// Gets the inventory index of an equipped item by subtype (weapon, equipable, etc).
    /// For weapons, returns the equipped weapon index. For equipables, searches by equipable type.
    /// </summary>
    /// <param name="subtype">The subtype to search for.</param>
    /// <param name="equipableType">The equipable type (only used if subtype is Equipable).</param>
    /// <returns>The inventory index of the equipped item, or -1 if not found.</returns>
    public int GetEquippedItemIndex(
        ObjectSubtype subtype,
        EquipableObjectType equipableType = default
    )
    {
        if (subtype == ObjectSubtype.Weapon)
        {
            return GetEquippedWeaponIndex();
        }
        else if (subtype == ObjectSubtype.Equipable)
        {
            return GetEquippedItemIndex(equipableType);
        }

        return -1; // Other subtypes (Consumable, Gift, etc.) cannot be equipped
    }

    public bool IsItemEquipped(ObjectItemInstance item)
    {
        int index = _inventoryItems.IndexOf(item);
        if (index < 0)
            return false;

        return Array.IndexOf(_equippedItemIndices, index) >= 0;
    }

    public bool CanAddItem()
    {
        return _inventoryItems.Count < _capacity;
    }

    public void AddToInventory(ObjectItemInstance item)
    {
        if (!CanAddItem())
        {
            Debug.LogWarning("Inventory is full. Cannot add item.");
            return;
        }

        _inventoryItems.Add(item);
    }

    public void RemoveFromInventory(ObjectItemInstance item)
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

        ObjectItemInstance itemToEquip = _inventoryItems[index];

        int slotIndex = GetSlotIndexForItem(itemToEquip.Template);

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
