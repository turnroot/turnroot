# CharacterInventoryInstance

**Type:** `Serializable Class`  
**Location:** `Assets/TurnrootFramework/Characters/Components/Inventory/CharacterInventoryInstance.cs`

Manages character inventory with support for simultaneous equipment of weapon, shield, and accessory items. Each character has their own instance (not a shared ScriptableObject template).

## Equipment System

The inventory supports **3 simultaneous equipment slots**:

| Slot Index | Item Subtype | Equipable Types | Flag Property |
|------------|--------------|-----------------|---------------|
| 0 | `ObjectSubtype.Weapon` | Weapon, Staff (equipable) | `IsWeaponEquipped` |
| 1 | `ObjectSubtype.Equipable` (Shield) | Shield | `IsShieldEquipped` |
| 2 | `ObjectSubtype.Equipable` (Accessory/Ring) | Accessory, Ring | `IsAccessoryEquipped` |

**Slot Determination Logic:**
- Items with `Subtype == ObjectSubtype.Weapon` → Slot 0
- Items with `Subtype == ObjectSubtype.Equipable`:
  - `EquipableType == Shield` → Slot 1
  - `EquipableType == Accessory` → Slot 2
  - `EquipableType == Ring` → Slot 2 (shares with Accessory)
  - `EquipableType == Staff` → Slot 0 (shares with Weapon)
- Other subtypes (Consumable, Gift, LostItem) → Not equipable

## Properties

### Inventory Data

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_inventoryItems` | `List<ObjectItem>` | Private | List of items in inventory |
| `_capacity` | `int` | Private | Maximum number of items (default: 6) |
| `CurrentItemCount` | `int` | Public | Current number of items in inventory |
| `CurrentWeight` | `float` | Public | Total weight of all items |

### Equipment State

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_equippedItemIndices` | `int[]` | Private | Array of 3 indices (-1 if slot empty) |
| `IsWeaponEquipped` | `bool` | Public | Whether weapon slot is occupied |
| `IsShieldEquipped` | `bool` | Public | Whether shield slot is occupied |
| `IsAccessoryEquipped` | `bool` | Public | Whether accessory slot is occupied |

## Methods

### GetEquippedItemIndex (by Subtype)

```csharp
int GetEquippedItemIndex(ObjectSubtype subtype, EquipableObjectType equipableType = default)
```

### GetEquippedItemIndex (by Subtype)

```csharp
int GetEquippedItemIndex(ObjectSubtype subtype, EquipableObjectType equipableType = default)
```

Returns the inventory index of the equipped item by subtype and equipable type.

**Parameters:**
- `subtype` - The ObjectSubtype to query (Weapon, Equipable, etc.)
- `equipableType` - The EquipableObjectType (only used if subtype is Equipable)

**Returns:** Inventory index (0-based), or -1 if no item of that type is equipped

### IsItemEquipped

```csharp
bool IsItemEquipped(ObjectItem item)
```

Checks if a specific item is currently equipped in any slot.

**Parameters:**
- `item` - The item to check

**Returns:** `true` if item is equipped, `false` otherwise

### AddToInventory

```csharp
void AddToInventory(ObjectItem item)
```

Adds an item to the inventory.

**Parameters:**
- `item` - The item to add

**Behavior:**
- Fails with warning if inventory is at capacity
- Automatically increments `CurrentItemCount` and `CurrentWeight`
- Appends to the `_inventoryItems` list

### CanAddItem

```csharp
bool CanAddItem(ObjectItem item)
```

Checks if there is room to add an item.

**Returns:** `true` if inventory has space, `false` if at capacity

### RemoveFromInventory

```csharp
void RemoveFromInventory(ObjectItem item)
```

Removes an item from the inventory.

**Parameters:**
- `item` - The item to remove

**Behavior:**
- Fails with warning if item not found
- Automatically decrements `CurrentItemCount` and `CurrentWeight`
- If item was equipped, unequips it and clears appropriate flag
- Adjusts all equipped indices to account for array shift

### EquipItem

```csharp
void EquipItem(int index)
```

Equips an item from the inventory into the appropriate slot based on its type.

**Parameters:**
- `index` - Inventory index of the item to equip

**Behavior:**
- Determines slot from item's `Subtype` and `EquipableType`:
  - `Subtype.Weapon` → Slot 0
  - `Subtype.Equipable` with `EquipableType.Shield` → Slot 1
  - `Subtype.Equipable` with `EquipableType.Accessory/Ring` → Slot 2
  - `Subtype.Equipable` with `EquipableType.Staff` → Slot 0
- Automatically unequips previously equipped item in that slot
- Sets appropriate equipped flag (`IsWeaponEquipped`, etc.)
- Fails with warning if index is invalid or item type cannot be equipped
- Only items with `Weapon` or `Equipable` subtypes can be equipped

### UnequipItem

```csharp
void UnequipItem(int inventoryIndex)
```

Unequips a specific item from its equipment slot.

**Parameters:**
- `inventoryIndex` - Inventory index of the item to unequip

**Behavior:**
- Finds which slot has the item equipped
- Clears that slot and its corresponding flag
- Item remains in inventory
- Fails with warning if index is invalid or item is not equipped

### UnequipAllItems

```csharp
void UnequipAllItems()
```

Unequips all items from all slots.

**Behavior:**
- Sets all 3 equipment slots to -1
- Sets all equipped flags to `false`
- Items remain in inventory, only equipment state changes

### ReorderItem

```csharp
void ReorderItem(int oldIndex, int newIndex)
```

Moves an item from one inventory position to another.

**Parameters:**
- `oldIndex` - Current inventory index
- `newIndex` - Desired inventory index
---

## See Also

- **[Character](Character.md)** - Character system
- **[CharacterComponents](CharacterComponents.md)** - Other character components
- **[ObjectItem](../Gameplay/ObjectItem.md)** - Item definition
- **[ObjectSubtype](../Gameplay/ObjectSubtype.md)** - Dynamic item type system
