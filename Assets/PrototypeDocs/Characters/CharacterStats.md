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
- `Defense` - Resistance to physical attacks
- `Magic` - Magical power and spell damage
- `Resistance` - Resistance to magical attacks
- `Skill` - Accuracy and critical hit chance
- `Speed` - Determines turn order and evasion
- `Luck` - Affects various chance-based outcomes
- `Dexterity` - Affects ranged attack accuracy and dodging
- `Charm` - Influences interactions with NPCs
- `Movement` - Number of tiles a character can move
- `Endurance` - Affects stamina and resistance to fatigue
- `Authority` - Influences leadership and command abilities
- `CriticalAvoidance` - Reduces chance of receiving critical hits

### BoundedStatType
Enum for stats with min/max:
- `Level` - Character's current level
- `Health` - Character's life force
- `LevelExperience` - Experience points toward next level
- `ClassExperience` - Experience points in current class

Each type has extension methods:
```csharp
string displayName = statType.GetDisplayName();
string description = statType.GetDescription();
```