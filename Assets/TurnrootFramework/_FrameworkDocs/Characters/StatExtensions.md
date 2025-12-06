# StatExtensions

Extension methods for `IHasStats`. Provides batch operations and eliminates repetitive stat iteration patterns.

## Overview

Consolidates "apply to bounded stats, then unbounded stats" logic into reusable extension methods.

**Namespace**: `Turnroot.Characters.Stats`  
**Source**: `Assets/TurnrootFramework/Characters/Components/Stats/StatExtensions.cs`

## Iteration Methods

```csharp
// Execute action on all stats (bounded + unbounded)
void ForEachStat(this IHasStats stats, Action<BaseCharacterStat> action)

// Execute action on bounded stats only
void ForEachBoundedStat(this IHasStats stats, Action<BoundedCharacterStat> action)

// Execute action on unbounded stats only
void ForEachUnboundedStat(this IHasStats stats, Action<CharacterStat> action)
```

### Example: Display All Stats

```csharp
character.ForEachStat(stat => {
    Debug.Log($"{stat.DisplayName}: {stat.Current} + {stat.Bonus} = {stat.Get()}");
});
```

## Bonus Methods

```csharp
// Apply temporary bonuses
void ApplyBoundedBonuses(this IHasStats stats, IEnumerable<StatModifier> modifiers)
void ApplyUnboundedBonuses(this IHasStats stats, IEnumerable<UnboundedStatModifier> modifiers)

// Remove temporary bonuses
void RemoveBoundedBonuses(this IHasStats stats, IEnumerable<StatModifier> modifiers)
void RemoveUnboundedBonuses(this IHasStats stats, IEnumerable<UnboundedStatModifier> modifiers)
```

### Example: Class Stat Bonuses

```csharp
// When equipping class
character.ApplyBoundedBonuses(classData.statBonuses);
character.ApplyUnboundedBonuses(classData.unboundedStatBonuses);

// When removing class
character.RemoveBoundedBonuses(classData.statBonuses);
character.RemoveUnboundedBonuses(classData.unboundedStatBonuses);
```

## Custom Modifier Methods

```csharp
// Apply formula to bounded stats
void ApplyBoundedModifiers(
    this IHasStats stats,
    IEnumerable<StatModifier> modifiers,
    Func<BoundedCharacterStat, float, float> modifier)

// Apply formula to unbounded stats
void ApplyUnboundedModifiers(
    this IHasStats stats,
    IEnumerable<UnboundedStatModifier> modifiers,
    Func<CharacterStat, float, float> modifier)
```

### Example: Scaling Stats

```csharp
// Double all bonuses
character.ApplyBoundedModifiers(modifiers, (stat, value) => {
    return stat.Current + (value * 2);
});

// Apply percentage increase
character.ApplyUnboundedModifiers(modifiers, (stat, value) => {
    return stat.Current * (1 + value / 100f);
});
```

## Best Practices

**DO** ✅
```csharp
// Use extension methods for batch operations
character.ForEachBoundedStat(stat => stat.SetBonus(0));
character.ApplyBoundedBonuses(modifiers);
```

**DON'T** ❌
```csharp
// Avoid manual loops
foreach (var modifier in modifiers) {
    var stat = character.GetBoundedStat(modifier.boundedStatType);
    if (stat != null) {
        stat.SetBonus(stat.Bonus + modifier.value);
    }
}
```

## Performance

- Extension methods are inline-optimized by JIT
- No additional allocations beyond standard foreach
- Same performance as manual loops

## See Also

- [IHasStats](IHasStats.md) - Interface definition
- [CharacterStats](CharacterStats.md) - Stat classes
- [StatApplicationHelper](StatApplicationHelper.md) - Legacy wrapper (delegates to extensions)
- [CharacterInstance](CharacterInstance.md) - Usage examples
