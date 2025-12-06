# Character Stats

Two serializable stat containers: `CharacterStat` (unbounded) and `BoundedCharacterStat` (min/max). Both inherit from `BaseCharacterStat`.

## Overview

Stats represent character attributes. Unbounded stats (Strength, Speed) grow without limits. Bounded stats (HP, Stamina) have min/max values for UI bars.

**Namespace**: `Turnroot.Characters.Stats`  
**Source**: `Assets/TurnrootFramework/Characters/Components/Stats/`

## CharacterStat (Unbounded)

Used for: Strength, Speed, Defense, Magic, Resistance, Skill, Luck

```csharp
public class CharacterStat : BaseCharacterStat
{
    public UnboundedStatType StatType { get; }
    public override float Current { get; }
    public override float Bonus { get; }
    public override int Get() { } // Returns Current + Bonus
}
```

### Usage

```csharp
var strength = new CharacterStat(10, UnboundedStatType.Strength);
strength.SetCurrent(12);
strength.SetBonus(3);
int total = strength.Get(); // 15

// Implicit int conversion
int value = strength; // Calls Get() automatically
```

## BoundedCharacterStat

Used for: Health, Stamina, shields, experience bars

```csharp
public class BoundedCharacterStat : BaseCharacterStat
{
    public BoundedStatType StatType { get; }
    public float Max { get; }
    public float Min { get; }
    public float Ratio { get; } // (Current + Bonus) / Max
    
    public void SetMax(float value) { }
    public void SetMin(float value) { }
    public void SetBonusPercent(float percent) { } // Bonus = Max * percent / 100
}
```

### Usage

```csharp
var hp = new BoundedCharacterStat(max: 100, current: 80, min: 0, BoundedStatType.Health);
hp.SetCurrent(hp.Current - 20); // Takes 20 damage
float hpRatio = hp.Ratio;        // For UI bar: 0.6 (60/100)

int currentHP = hp.CurrentInt;   // 60
int maxHP = hp.MaxInt;           // 100
```

## Stat Types

```csharp
// Unbounded
public enum UnboundedStatType
{
    Strength, Magic, Skill, Speed, 
    Luck, Defense, Resistance, 
    Build, Movement, ConstitutionHP
}

// Bounded
public enum BoundedStatType
{
    Health, Stamina
}
```

## BaseCharacterStat

Common interface for all stats:

```csharp
public abstract class BaseCharacterStat
{
    public abstract string Name { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public virtual float Current { get; }
    public virtual float Bonus { get; }
    public virtual int Get() { }                    // Current + Bonus
    public abstract void SetCurrent(float value);
    public virtual void SetBonus(float value) { }
}
```

## Stat Helpers

```csharp
// Find stat by type
BoundedCharacterStat hp = StatHelpers.GetBoundedStat(stats, BoundedStatType.Health);
CharacterStat str = StatHelpers.GetUnboundedStat(stats, UnboundedStatType.Strength);
```

## Extension Methods

Via `IHasStats` interface:

```csharp
// Iterate all stats
character.ForEachStat(stat => Debug.Log($"{stat.Name}: {stat.Get()}"));

// Apply modifiers
character.ApplyBoundedBonuses(modifiers);
character.ApplyUnboundedBonuses(modifiers);
```

## See Also

- [IHasStats](IHasStats.md) - Stat interface
- [StatExtensions](StatExtensions.md) - Extension methods
- [CharacterData](Character.md) - Template stats
- [CharacterInstance](CharacterInstance.md) - Runtime stats
- [DefaultCharacterStats](DefaultCharacterStats.md) - Default initialization