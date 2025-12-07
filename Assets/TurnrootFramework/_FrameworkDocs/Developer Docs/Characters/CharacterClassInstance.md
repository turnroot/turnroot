# CharacterClassDataInstance

Runtime instance of a character class. Manages material rendering and applies stat modifiers to a `CharacterInstance`.

## Overview

Created when a character equips a class. Tracks mastery progress, applies bonuses, and renders the class outfit visuals.

**Namespace**: `Turnroot.Characters.CharacterClass`  
**Source**: `Assets/TurnrootFramework/Characters/CharacterClass/CharacterClassDataInstance.cs`

## Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `ClassData` | `CharacterClassData` | Reference to class template |
| `CharacterData` | `CharacterData` | Reference to character template |
| `IsFirstTimeEquipped` | `bool` | True if character hasn't used this class before |
| `BattlesCompleted` | `int` | Battle count for mastery tracking |
| `LevelWhenEquipped` | `int` | Character level when class was equipped |

## Stat Operations

```csharp
// Apply temporary class bonuses
classInstance.ApplyClassBonuses(character);

// Remove class bonuses (when changing classes)
classInstance.RemoveClassBonuses(character);

// Apply one-time permanent bonuses (first equip only)
classInstance.ApplyClassChangeBonuses(character);

// Enforce class restrictions
classInstance.EnforceStatMinimums(character);
classInstance.ApplyStatCaps(character);

// Check if stats exceed caps
bool above = classInstance.IsAboveCaps(character);
```

## Mastery Tracking

```csharp
// Increment battle count
classInstance.IncrementBattleCount();

// Check mastery conditions and learn skills
bool learnedNew = classInstance.CheckMasteryConditions(character);
```

Mastery criteria:
- **LevelBased**: Character reaches `LevelWhenEquipped + target` levels
- **BattleBased**: Character completes `target` battles in this class

## Visual Rendering

```csharp
// Initialize material for class outfit
bool success = classInstance.Initialize();

// Material uses character's accent colors:
// - AccentColor1, AccentColor2, AccentColor3
// Applied via tint mask to class outfit mesh
```

## Lifecycle

```csharp
// Create instance
var classInstance = new CharacterClassDataInstance(
    characterData,
    classData,
    meshRenderer
);

// Initialize visuals
classInstance.Initialize();

// Apply to character
classInstance.ApplyClassBonuses(character);

// Cleanup when done
classInstance.Dispose(); // Destroys material instance
```

## See Also

- [CharacterClassData](CharacterClassData.md) - Class template definition
- [CharacterInstance](CharacterInstance.md) - Runtime character state
- [StatApplicationHelper](StatApplicationHelper.md) - Stat modification utilities
- [IHasStats](IHasStats.md) - Stat interface
