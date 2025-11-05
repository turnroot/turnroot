# Character Components

## Pronouns

**Namespace:** `Assets.Prototypes.Characters.Subclasses`  
**Type:** `[Serializable]`

Pronoun set system for character dialogue and text generation.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Singular` | `string` | Subject pronoun (they/she/he) |
| `PossessiveAdjective` | `string` | Possessive adjective (their/her/his) |
| `PossessivePronoun` | `string` | Possessive pronoun (theirs/hers/his) |
| `Objective` | `string` | Object pronoun (them/her/him) |

### Constructors

```csharp
Pronouns(string pronounType = "they")
```
Creates pronoun set from type: "they", "she", or "he".

```csharp
Pronouns()
```
Default constructor (uses "they" pronouns).

### Methods

```csharp
void SetPronounType(string pronounType)
```
Changes pronoun set. Accepts: "they", "she", "he" (case-insensitive).

```csharp
string Get(string pronounCase)
```
Gets specific pronoun by case name.
- **Parameters:** `pronounCase` - Case name or pronoun type
- **Accepts:** "singular", "they", "possessiveadjective", "their", "possessivepronoun", "theirs", "objective", "them"
- **Returns:** Requested pronoun

```csharp
string Use(string text)
```
Replaces pronoun placeholders in text.
- **Placeholders:** `{they}`, `{them}`, `{their}`, `{theirs}`
- **Example:** `"I saw {them} with {their} friend"` → `"I saw him with his friend"`

### Built-in Pronoun Sets

| Type | Singular | Possessive Adj. | Possessive Pronoun | Objective |
|------|----------|-----------------|-------------------|-----------|
| they | they | their | theirs | them |
| she | she | her | hers | her |
| he | he | his | his | him |

### Usage

```csharp
var pronouns = new Pronouns("she");
string text = pronouns.Use("{they} found {their} sword");
// Result: "she found her sword"

// Direct access
string subject = pronouns.Singular; // "she"
string possessive = pronouns.PossessiveAdjective; // "her"
```

---

## SupportRelationship

**Namespace:** `Assets.Prototypes.Characters.Subclasses`  
**Type:** `[Serializable]`

Tracks relationship between two characters with support levels and progression.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Character` | `Character` | `null` | Related character |
| `CurrentLevel` | `SupportLevels.Level` | `None` | Current support rank |
| `MaxLevel` | `SupportLevels.Level` | `S` | Maximum achievable rank |
| `SupportPoints` | `int` | `0` | Progress points toward next level |
| `SupportSpeed` | `int` | `1` | Point gain rate multiplier |

### Support Levels

**SupportLevels.Level** enum:
- `None` - No relationship
- `E` - Initial acquaintance
- `D` - Casual friends
- `C` - Close friends
- `B` - Growing relationship
- `A` - Strong bond
- `S` - Special/romantic relationship

### Usage

```csharp
// Create relationship
var support = new SupportRelationship {
    Character = otherCharacter,
    CurrentLevel = SupportLevels.Level.C,
    MaxLevel = SupportLevels.Level.A,
    SupportPoints = 50,
    SupportSpeed = 2
};

// On Character
character.AddSupportRelationship(otherCharacter);
var rel = character.GetSupportRelationship(otherCharacter);
rel.SupportPoints += 10 * rel.SupportSpeed;
```

---

## HereditaryTraits

**Namespace:** `Assets.Prototypes.Characters.Configuration`  
**Type:** `[Serializable]`

Configuration for which traits and characteristics can be inherited from parent to child units.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HairColor` | `bool` | `true` | Whether hair color is inherited |
| `FaceShape` | `bool` | `false` | Whether face shape is inherited |
| `EyeColor` | `bool` | `true` | Whether eye color is inherited |
| `SkinColor` | `bool` | `true` | Whether skin color is inherited |
| `Height` | `bool` | `true` | Whether height is inherited |
| `Aptitudes` | `bool` | `false` | Whether aptitudes are inherited |
| `StatGrowths` | `bool` | `false` | Whether stat growths are inherited |

### Notes
- These are boolean flags indicating which traits **can** be passed down
- The actual trait values (colors, growths, etc.) are stored on the Character class
- Accessed via `Character.PassedDownTraits` property

---

## CharacterWhich

**Namespace:** `Assets.Prototypes.Characters.Configuration`  
**Type:** `enum`

Character type/allegiance identifier.

### Values

| Value | Description |
|-------|-------------|
| `Avatar` | Player avatar/main character |
| `NPC` | Non-player character |
| `Enemy` | Enemy/hostile unit |
| `Friend` | Friendly/allied unit |

### Usage

```csharp
if (character.Which == CharacterWhich.Enemy) {
    // Enemy behavior
}
```

---

## SerializableDictionary<TKey, TValue>

**Namespace:** Global  
**Type:** `[Serializable]`  
**Source:** `Assets/AbstractScripts/SerializableDictionary.cs`

Unity-serializable dictionary wrapper for inspector editing.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Dictionary` | `Dictionary<TKey, TValue>` | Lazy-initialized dictionary from serialized lists |
| `Count` | `int` | Number of key-value pairs |
| `Keys` | `IEnumerable<TKey>` | Collection of keys |
| `Values` | `IEnumerable<TValue>` | Collection of values |

### Indexer

```csharp
TValue this[TKey key] { get; set; }
```
Access values by key (returns default if key not found).

### Methods

```csharp
void Add(TKey key, TValue value)
```
Add key-value pair (does nothing if key exists).

```csharp
bool Remove(TKey key)
```
Remove key-value pair.
- **Returns:** `true` if removed, `false` if key not found

```csharp
bool ContainsKey(TKey key)
```
Check if key exists.

```csharp
bool TryGetValue(TKey key, out TValue value)
```
Try to get value for key.

```csharp
void Clear()
```
Remove all entries.

### Usage

```csharp
[SerializeField]
private SerializableDictionary<string, int> classExps;

// Note: Properties are read-only in Character class
// Access via Character.ClassExps property
```

### Notes
- Serializes as two parallel lists (`_keys` and `_values`)
- Dictionary is lazily reconstructed from lists on first access
- Custom property drawer provides inspector editing
- Maintains dictionary semantics (unique keys)
