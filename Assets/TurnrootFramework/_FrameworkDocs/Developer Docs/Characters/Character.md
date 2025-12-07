# CharacterData

ScriptableObject template defining a character's base configuration. Stores identity, visuals, base stats, starting inventory, skills, and relationships.

## Overview

`CharacterData` is the template asset for characters. It defines defaults that are copied into `CharacterInstance` at runtime. Multiple instances can share the same template.

**Namespace**: `Turnroot.Characters`  
**Source**: `Assets/TurnrootFramework/Characters/CharacterData.cs`

## Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `DisplayName` | `string` | Short character name |
| `FullName` | `string` | Full character name |
| `Which` | `CharacterWhich` | Character type (Avatar, Ally, Enemy, NPC) |
| `IsUnique` | `bool` | If true, only one runtime instance allowed |
| `Level` | `int` | Starting level |
| `BoundedStats` | `List<BoundedCharacterStat>` | Base bounded stats (HP, Stamina) |
| `UnboundedStats` | `List<CharacterStat>` | Base unbounded stats (Strength, Speed) |
| `PersonalGrowthRates` | `List<UnboundedStatModifier>` | Growth rate percentages for level up |
| `StartingClass` | `CharacterClassData` | Initial class |
| `StartingInventory` | `List<InventorySlot>` | Initial equipment |
| `Portraits` | `SerializableDictionary<string, Portrait>` | Named portrait configurations |
| `AccentColor1/2/3` | `Color` | Colors for tinting portraits and outfits |

## Stat Access

Implements `IHasStats` interface:

```csharp
// Get specific stats
BoundedCharacterStat hp = characterData.GetBoundedStat(BoundedStatType.Health);
CharacterStat strength = characterData.GetUnboundedStat(UnboundedStatType.Strength);

// Extension methods work on templates too
characterData.ForEachStat(stat => Debug.Log(stat.DisplayName));
```

## Creation

Create via Unity menu: **Turnroot → Character → CharacterData**

## Instance Creation

```csharp
// Factory method respects IsUnique flag
CharacterInstance instance = CharacterInstance.Create(characterData);
```

## Editor Helpers

```csharp
// Save portrait layer defaults (tagged layers)
characterData.SaveDefaults();

// Load saved defaults back to portrait layers
characterData.LoadDefaults();

// Invalidate cached portrait array (after edits)
characterData.InvalidatePortraitArrayCache();
```

## Validation

`OnValidate()` automatically:
- Sanitizes self-referencing support relationships
- Initializes stats from `DefaultCharacterStats` if empty
- Validates rigging properties

## See Also

- [CharacterInstance](CharacterInstance.md) - Runtime state container
- [CharacterStats](CharacterStats.md) - Stat system
- [IHasStats](IHasStats.md) - Stat interface
- [Portrait](Portraits/Portrait.md) - Portrait system
- [CharacterInventory](CharacterInventory.md) - Inventory system
- [UniqueInstances](UniqueInstances.md) - Unique instance handling
