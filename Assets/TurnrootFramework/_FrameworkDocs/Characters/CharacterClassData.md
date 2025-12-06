# CharacterClassData

ScriptableObject defining a character class (job/vocation). Specifies stat modifiers, visual appearance, weapon restrictions, and mastery skills.

## Overview

Character classes provide temporary bonuses, enforce stat minimums/caps, and grant skills through mastery. Examples: Knight, Mage, Cavalier.

**Namespace**: `Turnroot.Characters.CharacterClass`  
**Source**: `Assets/TurnrootFramework/Characters/CharacterClass/CharacterClassData.cs`

## Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `className` | `string` | Display name for the class |
| `classTier` | `ProgressionLevel` | Base/Intermediate/Advanced/Master |
| `statBonuses` | `List<StatModifier>` | Temporary bounded stat bonuses while equipped |
| `unboundedStatBonuses` | `List<UnboundedStatModifier>` | Temporary unbounded stat bonuses |
| `classChangeBonuses` | `List<StatModifier>` | One-time permanent stat increases (first equip) |
| `growthRateModifiers` | `List<UnboundedStatModifier>` | Added to personal growth rates on level up |
| `statMinimums` | `List<StatModifier>` | Minimum stat values enforced by class |
| `statCaps` | `List<StatModifier>` | Maximum bounded stat values |
| `allowedWeaponTypes` | `List<WeaponType>` | Equippable weapon types (empty = all allowed) |
| `promotionPaths` | `List<CharacterClassData>` | Classes this can promote to/from |

## Visuals

```csharp
// Class outfit mesh and materials
public Mesh ClassOutfit;
public Shader ShaderGraph;
public Texture2D Base, MSE, TintMask;
```

Rendered onto character model when class is equipped.

## Mastery System

```csharp
[System.Serializable]
public struct Mastery
{
    public Skill skill;
    public MasteryCriteria criteria; // None, LevelBased, BattleBased
    public int target;               // Levels/battles required
}
```

Characters learn mastery skills after meeting criteria while using the class.

## Class Restrictions

```csharp
// Level requirement to change into this class
public int requiredLevelToChange;

// Species restrictions
public List<SpeciesType> allowedSpecies;

// Pronoun restrictions (gender-locked classes)
public List<string> allowedPronounKeys;

// Experience rank requirements (e.g., Sword rank B)
public List<ExperienceRequirement> experienceRequirements;
```

## Stat Application

Classes modify stats through `CharacterClassDataInstance`:

```csharp
// Stat modifiers applied in this order:
1. Remove old class bonuses
2. Apply new class bonuses (temporary)
3. Apply class change bonuses (permanent, first time only)
4. Enforce stat minimums
5. Apply stat caps
```

## See Also

- [CharacterClassDataInstance](CharacterClassInstance.md) - Runtime class instance
- [CharacterData](Character.md) - Character template
- [CharacterInstance](CharacterInstance.md) - Runtime character state
- [CharacterStats](CharacterStats.md) - Stat system
