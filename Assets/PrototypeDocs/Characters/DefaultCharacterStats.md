# DefaultCharacterStats

**Namespace:** `Assets.Prototypes.Characters`  
**Inherits:** `ScriptableObject`  
**Location:** `Resources/GameSettings/DefaultCharacterStats`

Defines default stat configurations that are automatically applied to new Character assets.

## Creation

```csharp
Assets > Create > Game Settings > Default Character Stats
```

**Important:** Save the asset at `Assets/Resources/GameSettings/DefaultCharacterStats.asset` for automatic loading.

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `DefaultBoundedStats` | `List<DefaultBoundedStat>` | List of bounded stats to initialize |
| `DefaultUnboundedStats` | `List<DefaultUnboundedStat>` | List of unbounded stats to initialize |

## Nested Classes

### DefaultBoundedStat

Defines a default bounded stat configuration.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StatType` | `BoundedStatType` | - | Type of bounded stat |
| `Max` | `float` | `100` | Maximum value |
| `Current` | `float` | `100` | Starting current value |
| `Min` | `float` | `0` | Minimum value |

### DefaultUnboundedStat

Defines a default unbounded stat configuration.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StatType` | `UnboundedStatType` | - | Type of unbounded stat |
| `Current` | `float` | `10` | Starting value |

## Methods

```csharp
List<BoundedCharacterStat> CreateBoundedStats()
```
Creates instances of bounded stats from the default configuration.
- **Returns:** List of initialized BoundedCharacterStat objects

```csharp
List<CharacterStat> CreateUnboundedStats()
```
Creates instances of unbounded stats from the default configuration.
- **Returns:** List of initialized CharacterStat objects

## Integration

### Automatic Initialization

Characters automatically load and apply defaults in `OnEnable()`:
- Checks if character has no stats (both lists empty)
- Loads `Resources/GameSettings/DefaultCharacterStats`
- Initializes stats using `CreateBoundedStats()` and `CreateUnboundedStats()`

### Character.cs Integration

```csharp
private void OnEnable()
{
    // ... other initialization ...
    
    // Initialize stats from defaults if stats are empty
    if (_boundedStats.Count == 0 && _unboundedStats.Count == 0)
    {
        var defaultStats = Resources.Load<DefaultCharacterStats>(
            "GameSettings/DefaultCharacterStats"
        );
        if (defaultStats != null)
        {
            _boundedStats = defaultStats.CreateBoundedStats();
            _unboundedStats = defaultStats.CreateUnboundedStats();
        }
    }
}
```

## Usage

### Setup

1. Create the asset: `Assets > Create > Game Settings > Default Character Stats`
2. Save it at: `Assets/Resources/GameSettings/DefaultCharacterStats.asset`
3. Configure default stats in the Inspector

### When Stats Are Initialized

Stats are automatically initialized when:
- A new Character asset is created
- An existing Character asset is loaded that has no stats
- The asset's `OnEnable()` method is called

### Modifying Defaults

Changes to the DefaultCharacterStats asset:
- ✅ Apply to new Character assets automatically
- ❌ Do NOT affect existing Characters with stats already set
- To reset a Character's stats to defaults, clear both stat lists

## Notes

- Initialization only happens if BOTH stat lists are empty
- If a Character has any stats, defaults are not applied
- The asset must be in `Resources/GameSettings/` to be loaded automatically
- Each stat type can only appear once in each list
- Changes to defaults require Unity to reload the asset

---

## See Also

- **[CharacterStats](../Characters/CharacterStats.md)** - Stat system details
- **[Character](../Characters/Character.md)** - Character asset
- **[Settings](Settings.md)** - Other settings assets
