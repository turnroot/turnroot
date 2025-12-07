# IHasStats Interface

Unified interface for accessing character stats. Implemented by `CharacterData` (templates) and `CharacterInstance` (runtime state).

## Overview

Provides consistent stat access patterns and enables extension methods for batch operations.

**Namespace**: `Turnroot.Characters.Stats`  
**Source**: `Assets/TurnrootFramework/Characters/Components/Stats/IHasStats.cs`

## Interface Definition

```csharp
public interface IHasStats
{
    List<BoundedCharacterStat> BoundedStats { get; }
    List<CharacterStat> UnboundedStats { get; }
    BoundedCharacterStat GetBoundedStat(BoundedStatType type);
    CharacterStat GetUnboundedStat(UnboundedStatType type);
}
```

## Implementations

- **CharacterData**: Exposes template stats
- **CharacterInstance**: Exposes runtime stats

## Extension Methods

`StatExtensions` provides batch operations on `IHasStats`:

### Iteration

```csharp
// Iterate all stats as BaseCharacterStat
stats.ForEachStat(stat => {
    Debug.Log($"{stat.DisplayName}: {stat.Get()}");
});

// Iterate only bounded stats
stats.ForEachBoundedStat(stat => {
    Debug.Log($"{stat.DisplayName}: {stat.Current}/{stat.Max}");
});

// Iterate only unbounded stats
stats.ForEachUnboundedStat(stat => {
    Debug.Log($"{stat.DisplayName}: {stat.Current}");
});
```

### Bonus Application

```csharp
// Apply temporary bonuses
character.ApplyBoundedBonuses(modifiers);
character.ApplyUnboundedBonuses(modifiers);

// Remove bonuses
character.RemoveBoundedBonuses(modifiers);
character.RemoveUnboundedBonuses(modifiers);
```

### Custom Modifications

```csharp
// Apply custom formula to stats
character.ApplyBoundedModifiers(modifiers, (stat, value) => {
    return stat.Current + value * 1.5f;
});

character.ApplyUnboundedModifiers(modifiers, (stat, value) => {
    return Mathf.Max(stat.Current + value, 1);
});
```

## Usage Examples

```csharp
// Works with both templates and instances
void ProcessStats(IHasStats entity)
{
    // Get specific stat
    var hp = entity.GetBoundedStat(BoundedStatType.Health);
    
    // Iterate all stats
    entity.ForEachStat(stat => Debug.Log(stat.Name));
    
    // Apply modifiers
    entity.ApplyBoundedBonuses(classModifiers);
}

// Use with template
ProcessStats(characterData);

// Use with instance
ProcessStats(characterInstance);
```

## See Also

- [CharacterStats](CharacterStats.md) - Stat classes
- [StatExtensions](StatExtensions.md) - Extension method details
- [CharacterInstance](CharacterInstance.md) - Runtime implementation
- [CharacterData](Character.md) - Template implementation
