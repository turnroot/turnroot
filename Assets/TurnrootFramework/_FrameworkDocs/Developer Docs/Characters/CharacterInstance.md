# CharacterInstance

Runtime state container for a character. Holds current HP, level, experience, equipped items, and active skills. Created from a `CharacterData` template.

## Overview

`CharacterInstance` represents a character during gameplay while `CharacterData` defines the template. Multiple instances can share the same template with different runtime states.

**Namespace**: `Turnroot.Characters`  
**Source**: `Assets/TurnrootFramework/Characters/CharacterInstance.cs`

## Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier (deterministic for unique characters) |
| `CharacterTemplate` | `CharacterData` | Reference to the template asset |
| `CurrentLevel` | `int` | Current character level |
| `CurrentExp` | `int` | Current experience points |
| `BoundedStats` | `List<BoundedCharacterStat>` | Runtime bounded stats (HP, Stamina) |
| `UnboundedStats` | `List<CharacterStat>` | Runtime unbounded stats (Strength, Speed) |
| `CurrentClass` | `CharacterClassDataInstance` | Currently equipped class |
| `InventoryInstance` | `CharacterInventoryInstance` | Runtime inventory state |
| `SkillInstances` | `List<SkillInstance>` | Active skills with cooldown states |

## Creation

Use the factory method to enforce uniqueness for unique character templates:

```csharp
// Respects IsUnique flag on template
CharacterInstance instance = CharacterInstance.Create(characterData);
```

For unique templates, subsequent calls return the existing instance from `UniqueInstanceRegistry`.

## Stat Access

Implements `IHasStats` interface for unified stat operations:

```csharp
// Individual stat access
BoundedCharacterStat hp = instance.GetBoundedStat(BoundedStatType.Health);
CharacterStat strength = instance.GetUnboundedStat(UnboundedStatType.Strength);

// Modify stats
hp.SetCurrent(hp.Current - 10);
strength.SetBonus(strength.Bonus + 5);

// Extension methods (batch operations)
instance.ForEachStat(stat => Debug.Log(stat.DisplayName));
instance.ApplyBoundedBonuses(modifiers);
```

## Level Progression

```csharp
// Level up with stat growth
instance.LevelUp(); // Applies random growth rolls

// Check class requirements
bool canPromote = instance.MeetsClassRequirements(advancedClass);

// Change class
bool success = instance.ChangeClass(newClassData, meshRenderer);
```

## Battle Statistics

```csharp
// Battle tracking
instance.RecordBattleStart();
instance.RecordKill();
instance.IncrementTurnsAlive();
instance.ResetBattleStats();

// Current battle state
bool defeated = instance.IsDefeatedInCurrentBattle;
Vector2Int position = instance.MapGridPosition;
```

## Serialization

Implements `IPostDeserialize` for proper deserialization:

```csharp
// Called automatically after JSON deserialization
instance.OnAfterDeserialize();
```

Re-registers unique instances and initializes runtime structures.

## See Also

- [CharacterData](Character.md) - Template asset definition
- [CharacterStats](CharacterStats.md) - Stat system details
- [CharacterClassDataInstance](CharacterClassInstance.md) - Runtime class state
- [IHasStats](IHasStats.md) - Stat interface and extensions
- [UniqueInstances](UniqueInstances.md) - Unique instance handling
