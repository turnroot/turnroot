# CharacterInventoryInstance — short ref

Serializable per-character inventory with 3 equipment slots: Weapon, Shield, Accessory (Accessory also includes Rings). Items use `ObjectSubtype` + `EquipableObjectType` to determine slots.

Key behaviors
- Add/Remove items with capacity checks
- Equip determines slot and replaces previous equipment in that slot
- Auto-adjusts equipped indices on remove/reorder

Useful methods
- `AddToInventory(item)`, `RemoveFromInventory(item)`
- `EquipItem(index)`, `UnequipItem(index)`, `UnequipAllItems()`
- `GetEquippedItemIndex(subtype)` to query current equipment

Look here
- Source: `Assets/TurnrootFramework/Characters/Components/Inventory/CharacterInventoryInstance.cs`

See also
- [ObjectItem](../Gameplay/Objects/ObjectItem.md) — items and subtype system
- [GameplayGeneralSettings](../Configurations/GameplayGeneralSettings.md) — toggles for gift/lost items
