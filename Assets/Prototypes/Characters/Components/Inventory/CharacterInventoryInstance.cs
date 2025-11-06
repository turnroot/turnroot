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

    [SerializeField]
    private int[] _equippedItemIndices = new int[3] { -1, -1, -1 };

    [SerializeField]
    private bool _isWeaponEquipped;

    [SerializeField]
    private bool _isShieldEquipped;

    [SerializeField]
    private bool _isAccessoryEquipped;

    public List<ObjectItem> InventoryItems => _inventoryItems;
    public int Capacity => _capacity;
    public int CurrentItemCount => _inventoryItems.Count;

    public int CurrentWeight
    {
        get
        {
            int weight = 0;
            foreach (var item in _inventoryItems)
            {
                if (item != null)
                    weight += item.Weight;
            }
            return weight;
        }
    }

    public int[] EquippedItemIndices => _equippedItemIndices;

    public bool IsWeaponEquipped => _isWeaponEquipped;
    public bool IsShieldEquipped => _isShieldEquipped;
    public bool IsAccessoryEquipped => _isAccessoryEquipped;

    /// <summary>
    /// Create a new empty inventory with specified capacity
    /// </summary>
    public CharacterInventoryInstance(int capacity = 6)
    {
        _capacity = capacity;
        _inventoryItems = new List<ObjectItem>();
        _equippedItemIndices = new int[3] { -1, -1, -1 };
    }

    /// <summary>
    /// Create inventory with starting items
    /// </summary>
    public CharacterInventoryInstance(int capacity, List<ObjectItem> startingItems)
    {
        _capacity = capacity;
        _inventoryItems = new List<ObjectItem>(startingItems);
        _equippedItemIndices = new int[3] { -1, -1, -1 };
    }

    public int GetEquippedItemIndex(ObjectItemType itemType)
    {
        return itemType switch
        {
            ObjectItemType.Weapon => _equippedItemIndices[0],
            ObjectItemType.Shield => _equippedItemIndices[1],
            ObjectItemType.Accessory => _equippedItemIndices[2],
            _ => -1,
        };
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
                switch (i)
                {
                    case 0:
                        _isWeaponEquipped = false;
                        break;
                    case 1:
                        _isShieldEquipped = false;
                        break;
                    case 2:
                        _isAccessoryEquipped = false;
                        break;
                }
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

        int slotIndex = itemToEquip.ItemType switch
        {
            ObjectItemType.Weapon => 0,
            ObjectItemType.Shield => 1,
            ObjectItemType.Accessory => 2,
            _ => -1,
        };

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

        switch (itemToEquip.ItemType)
        {
            case ObjectItemType.Weapon:
                _isWeaponEquipped = true;
                break;
            case ObjectItemType.Shield:
                _isShieldEquipped = true;
                break;
            case ObjectItemType.Accessory:
                _isAccessoryEquipped = true;
                break;
        }
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

        switch (slotIndex)
        {
            case 0:
                _isWeaponEquipped = false;
                break;
            case 1:
                _isShieldEquipped = false;
                break;
            case 2:
                _isAccessoryEquipped = false;
                break;
        }
    }

    public void UnequipAllItems()
    {
        _equippedItemIndices[0] = -1;
        _equippedItemIndices[1] = -1;
        _equippedItemIndices[2] = -1;
        _isWeaponEquipped = false;
        _isShieldEquipped = false;
        _isAccessoryEquipped = false;
    }
}
