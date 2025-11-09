# ObjectItem
**Inherits:** `ScriptableObject`
**Location:** `Assets/TurnrootFramework/Gameplay/Objects/ObjectItem.cs`
**Namespace:** `Turnroot.Gameplay.Objects`

Represents a gameplay item that can be equipped, used, gifted, or traded. Supports weapons, magic, consumables, equipable items, gifts, and lost items with dynamic subtype validation.

## Creation
```csharp
Assets > Create > Turnroot > Objects > Gameplay Item
```

## Overview
ObjectItem provides a flexible item system with:
- **Dynamic Subtype System** - Item types that respect GameplayGeneralSettings
- **Equipment Support** - Weapons, shields, accessories, staves, rings
- **Durability System** - Optional use tracking and repair mechanics
- **Gift System** - Character-specific gift preferences
- **Lost Item System** - Items belonging to specific characters
- **Range System** - Weapon/magic attack ranges with stat bonuses

## Properties

### Identity
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_name` | `string` | Private | Display name of the item |
| `_id` | `string` | Private, Readonly | Unique GUID identifier |
| `_flavorText` | `string` | Private | Description text for the item |
| `_icon` | `Sprite` | Private | Item icon sprite |
| `Icon` | `Sprite` | Public | Public accessor for `_icon` |

### Type
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_subtype` | `ObjectSubtype` | Private | Item subtype (Weapon, Magic, Consumable, etc.) |
| `Subtype` | `ObjectSubtype` | Public | Public accessor for `_subtype` |
| `_equipableType` | `EquipableObjectType` | Private | Type if subtype is Equipable |
| `EquipableType` | `EquipableObjectType` | Public | Public accessor for `_equipableType` |
| `_weaponType` | `WeaponType` | Private | Weapon type reference (shown if Weapon subtype) |
| `IsEquippable` | `bool` | Public | True if Weapon or Equipable subtype |

### Pricing
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_basePrice` | `int` | Private | Base purchase price (default: 100) |
| `_sellable` | `bool` | Private | Whether item can be sold (default: true) |
| `_buyable` | `bool` | Private | Whether item can be purchased (default: true) |
| `_sellPriceDeductedPerUse` | `int` | Private | Value reduction per use (default: 2) |

### Repair
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_repairable` | `bool` | Private | Whether item can be repaired (default: true) |
| `_repairPricePerUse` | `int` | Private | Repair cost per use point (default: 10) |
| `_repairNeedsItems` | `bool` | Private | Whether repair requires items (default: true) |
| `_repairItem` | `ObjectItem` | Private | Item required for repair |
| `_repairItemAmountPerUse` | `int` | Private | Quantity of repair item needed (default: 1) |
| `_forgeable` | `bool` | Private | Whether item can be forged (default: false) |
| `_forgeInto` | `ObjectItem[]` | Private | Items this can be forged into |
| `_forgePrices` | `int[]` | Private | Forge costs for each target item |
| `_forgeNeedsItems` | `bool` | Private | Whether forging requires items (default: false) |
| `_forgeItems` | `ObjectItem[]` | Private | Items required for forging |

### Lost Items
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_lostItem` | `bool` | Private | Whether this is a lost item |
| `_belongsTo` | `CharacterData` | Private | Character who owns this lost item |
| `BelongsTo` | `CharacterData` | Public | Public accessor for `_belongsTo` |

### Gift
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_giftRank` | `int` | Private | Gift quality rank (1-5, default: 1) |
| `_unitsLove` | `string[]` | Private | Character names who love this gift |
| `_unitsHate` | `string[]` | Private | Character names who hate this gift |

### Attack Range (Weapon/Magic Only)
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_lowerRange` | `int` | Private | Minimum attack range (default: 0) |
| `_upperRange` | `int` | Private | Maximum attack range (default: 0) |
| `_rangeAdjustedByStat` | `bool` | Private | Whether range scales with stats (default: false) |
| `_rangeAdjustedByStatName` | `UnboundedStatType` | Private | Stat used for range bonus (default: Strength) |
| `_rangeAdjustedByStatAmount` | `int` | Private | Range bonus per stat point (default: 0) |

### Durability (Weapon/Magic Only)
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_durability` | `bool` | Private | Whether item has durability (default: true) |
| `_uses` | `int` | Private | Current use count (default: 100) |
| `_maxUses` | `int` | Private | Maximum use count (default: 100) |
| `_replenishUsesAfterBattle` | `bool` | Private | Auto-repair after battle (default: false) |
| `_replenishUsesAfterBattleAmount` | `ReplenishUseType` | Private | How much to repair (default: None) |

### Stats
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_weight` | `float` | Private | Item weight (default: 1.0) |
| `Weight` | `float` | Public | Public accessor for `_weight` |

### Aptitude (Weapon/Magic Only)
| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `_minAptitude` | `Aptitude` | Private | Minimum aptitude to use (default: E) |

## Helper Methods

--
## See Also
- **[CharacterInventoryInstance](../Characters/CharacterInventory.md)** - Inventory management
- **[WeaponType](./WeaponType.md)** - Weapon classification
- **[Aptitude](./Aptitude.md)** - Weapon proficiency requirements
- **[GameplayGeneralSettings](../Configurations/GameplayGeneralSettings.md)** - Feature toggles
