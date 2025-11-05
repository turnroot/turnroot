# Character Stats System

## CharacterStat

**Namespace:** `Assets.Prototypes.Characters.Stats`  
**Type:** `[Serializable]`

Unbounded stat with current value and bonus modifier.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `StatType` | `UnboundedStatType` | Stat type identifier |
| `Name` | `string` | Type name as string |
| `DisplayName` | `string` | Formatted display name |
| `Description` | `string` | Stat description |
| `Current` | `float` | Base value (rounded) |
| `Bonus` | `float` | Bonus modifier (rounded) |
| `CurrentInt` | `int` | Base value as integer |
| `BonusInt` | `int` | Bonus as integer |

### Constructors

```csharp
CharacterStat()
```
Default constructor (Strength, 0 value).

```csharp
CharacterStat(float current = 0, UnboundedStatType statType = UnboundedStatType.Strength)
```
- **Parameters:**
  - `current` - Initial value
  - `statType` - Stat type

### Methods

```csharp
int Get()
```
Returns total value: `Current + Bonus` (rounded to int).

```csharp
void SetCurrent(float value)
```
Sets base current value.

```csharp
void SetBonus(float value)
```
Sets bonus modifier.

### Implicit Conversion
```csharp
int damage = characterStat; // Automatically calls Get()
```

---

## BoundedCharacterStat

**Namespace:** `Assets.Prototypes.Characters.Stats`  
**Type:** `[Serializable]`

Stat with min/max bounds (e.g., Health, Stamina).

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `StatType` | `BoundedStatType` | Stat type identifier |
| `Name` | `string` | Type name as string |
| `DisplayName` | `string` | Formatted display name |
| `Description` | `string` | Stat description |
| `Current` | `float` | Current value (clamped, rounded) |
| `Bonus` | `float` | Bonus modifier (rounded) |
| `Max` | `float` | Maximum value (rounded) |
| `Min` | `float` | Minimum value (rounded) |
| `Ratio` | `float` | Current/Max ratio (0-1) for progress bars |
| `CurrentInt` | `int` | Current as integer |
| `BonusInt` | `int` | Bonus as integer |
| `MaxInt` | `int` | Max as integer |
| `MinInt` | `int` | Min as integer |

### Constructors

```csharp
BoundedCharacterStat()
```
Default constructor (Health, 100/100).

```csharp
BoundedCharacterStat(
    float max, 
    float current = -1, 
    float min = 0,
    BoundedStatType statType = BoundedStatType.Health
)
```
- **Parameters:**
  - `max` - Maximum value
  - `current` - Initial value (defaults to max if -1)
  - `min` - Minimum value
  - `statType` - Stat type

### Methods

```csharp
int Get()
```
Returns total value: `Current + Bonus` (clamped and rounded).

```csharp
void SetMax(float value)
```
Sets maximum. Clamps current if needed.

```csharp
void SetMin(float value)
```
Sets minimum. Clamps current if needed.

```csharp
void SetCurrent(float value)
```
Sets current value (automatically clamped to min/max).

```csharp
void SetBonus(float value)
```
Sets bonus modifier.

```csharp
void SetBonusPercent(float percent)
```
Sets bonus as percentage of max: `bonus = max * percent / 100`

### Implicit Conversion
```csharp
int hp = healthStat; // Automatically calls Get()
```

---

## StatTypes

### UnboundedStatType
Enum for stats without bounds:
- `Strength` - Physical power and melee damage

### BoundedStatType
Enum for stats with min/max:
- `Health` - Character's life force

Each type has extension methods:
```csharp
string displayName = statType.GetDisplayName();
string description = statType.GetDescription();
```

---

## Usage Examples

### Creating Stats
```csharp
// Unbounded
var strength = new CharacterStat(10, UnboundedStatType.Strength);
strength.SetBonus(5);
int totalStr = strength.Get(); // 15

// Bounded
var health = new BoundedCharacterStat(100, 80, 0, BoundedStatType.Health);
health.SetCurrent(50);
int currentHp = health; // Implicit conversion to 50
```

### In Character
```csharp
Character character = GetCharacter();

// Query by type
BoundedCharacterStat hp = character.GetBoundedStat(BoundedStatType.Health);
CharacterStat str = character.GetUnboundedStat(UnboundedStatType.Strength);

// Note: Character properties are read-only
// Stats list manipulation must be done in Unity Inspector
```

### Progress Bars
```csharp
BoundedCharacterStat health = character.GetBoundedStat(BoundedStatType.Health);
if (health != null) {
    float fillAmount = health.Ratio; // 0.0 to 1.0
    progressBar.fillAmount = fillAmount;
}
```

## Notes
- All values rounded to nearest integer on access
- Bounded stats automatically clamp on set
- Implicit int conversion for convenient arithmetic
- Bonus modifiers separate from base values
