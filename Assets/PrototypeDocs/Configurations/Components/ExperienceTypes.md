# Experience Types

## Classes

### ExperienceType

Defines a type of experience that units can gain (e.g., Sword, Axe, Riding, Authority).

**Location:** `Assets/Prototypes/Gameplay/Combat/FundamentalComponents/ExperienceType.cs`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name of the experience type. Setting this auto-generates the `Id`. |
| `Id` | `string` | Unique identifier (lowercase, no spaces). **Auto-generated** from `Name`. Read-only in inspector. |
| `Enabled` | `bool` | Whether this experience type is active in the game. |
| `HasWeaponType` | `bool` | Whether this experience type is associated with a weapon type. |
| `AssociatedWeaponType` | `WeaponType` | The weapon type linked to this experience. Only visible when `HasWeaponType` is true. |

#### Usage

```csharp
// Create via object initializer
var swordExp = new ExperienceType
{
    Name = "Sword",           // Id will auto-generate as "sword"
    Enabled = true,
    HasWeaponType = true,
    AssociatedWeaponType = swordWeapon
};

// Access properties
string id = swordExp.Id;      // "sword" (auto-generated)
string name = swordExp.Name;  // "Sword"

// Change name (updates Id automatically)
swordExp.Name = "Long Sword"; // Id becomes "longsword"
```

#### Inspector Behavior

- **Name Field**: Editable text field
- **Id Field**: Hidden (auto-generated)
- **Enabled**: Checkbox
- **HasWeaponType**: Checkbox
- **AssociatedWeaponType**: Only shown when HasWeaponType is checked

#### ID Generation

The `Id` is automatically generated when the `Name` is set:
- Converts to lowercase
- Removes all spaces
- Example: `"Long Sword"` → `"longsword"`

**Note:** The `Id` property has a private setter and cannot be directly assigned.

---

### WeaponType

Defines a weapon type with its characteristics and combat ranges.

**Location:** `Assets/Prototypes/Gameplay/Combat/Objects/Components/WeaponType.cs`  
**Inherits:** `ScriptableObject`

#### Creation

```csharp
Assets > Create > Game Settings > Gameplay > Weapon Type
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name of the weapon type (e.g., "Sword", "Bow"). |
| `Icon` | `Sprite` | Visual icon for the weapon type. |
| `Id` | `string` | Unique identifier for lookups. |
| `Ranges` | `int[]` | Array of valid combat ranges (e.g., [1] for melee, [2,3] for 2-3 range). |
| `DefaultRange` | `int` | Default/preferred range for this weapon type. |

#### Usage

```csharp
// Create asset via Unity menu
// Assets > Create > Game Settings > Gameplay > Weapon Type

// Configure in inspector or via code
WeaponType bow = bowAsset;
bow.Name = "Bow";
bow.Id = "bow";
bow.Ranges = new int[] { 2, 3 };  // Can attack at range 2 or 3
bow.DefaultRange = 2;             // Prefers range 2

// Check valid ranges at runtime
bool canAttackAt2 = bow.Ranges.Contains(2); // true
int preferredRange = bow.DefaultRange;      // 2
```

#### Serialization

Both `ExperienceType` and `WeaponType` use Unity serialization:
- WeaponType is a **ScriptableObject** (create as asset)
- ExperienceType is **[Serializable]** (embeds in other assets)
- Both use properties wrapping `[SerializeField]` private backing fields
- Fully compatible with Unity Inspector

---

## Custom Property Drawer

### ExperienceTypeDrawer

Custom property drawer for `ExperienceType` in the Unity Inspector.

**Location:** `Assets/Prototypes/Gameplay/Combat/FundamentalComponents/Editor/ExperienceTypeDrawer.cs`

#### Features

1. **Auto-ID Generation**
   - Automatically updates `Id` when `Name` changes in inspector
   - ID generation logic matches the property setter

2. **Conditional Visibility**
   - `AssociatedWeaponType` field only appears when `HasWeaponType` is checked
   - Dynamic height calculation based on visible fields

3. **Hidden Fields**
   - `Id` field is not displayed in inspector (read-only, auto-generated)

#### Layout

```
┌─────────────────────────┐
│ Name: [____________]    │  ← Text field
├─────────────────────────┤
│ ☑ Enabled              │  ← Toggle
├─────────────────────────┤
│ ☑ HasWeaponType        │  ← Toggle
├─────────────────────────┤
│ AssociatedWeaponType:   │  ← Only shown if HasWeaponType
│   [WeaponType]          │
└─────────────────────────┘
```

---

## Integration

### GameplayGeneralSettings

Experience types are configured in `GameplayGeneralSettings.cs`:

```csharp
[SerializeField, BoxGroup("Experience Types")]
private ExperienceType[] ExperienceWeaponTypes;

[SerializeField, BoxGroup("Extra Experience Types")]
private ExperienceType RidingExperienceType = new ExperienceType
{
    Name = "Riding",      // Id auto-generates as "riding"
    Enabled = false,
    HasWeaponType = false
};
```

### Best Practices

1. **Always set Name first** when creating ExperienceType instances
2. **Don't try to set Id directly** - it's auto-generated
3. **Check HasWeaponType** before accessing AssociatedWeaponType
4. **Use Id for lookups**, Name for display

### Common Patterns

```csharp
// Finding experience types by ID
ExperienceType FindExperienceType(string id)
{
    return experienceTypes.FirstOrDefault(e => e.Id == id);
}

// Filtering enabled types
var enabledTypes = experienceTypes.Where(e => e.Enabled).ToArray();

// Getting weapon-based experience types
var weaponExperience = experienceTypes
    .Where(e => e.HasWeaponType && e.AssociatedWeaponType != null)
    .ToArray();

// Validating weapon type
if (expType.HasWeaponType && expType.AssociatedWeaponType == null)
{
    Debug.LogWarning($"Experience type '{expType.Name}' requires a weapon type!");
}
```

---

## Technical Notes

### Serialization Architecture

Both classes use property wrappers around serialized backing fields:

```csharp
[SerializeField]
private string _name;

public string Name
{
    get => _name;
    set => _name = value;
}
```

This pattern provides:
- Unity Inspector compatibility
- Property setter logic (for auto-ID generation)
- C# property syntax for clean API

### Why Private Id Setter?

The `Id` property has a private setter to prevent accidental assignment:

```csharp
public string Id
{
    get => _id;
    private set => _id = value;  // Private!
}
```

This ensures `Id` is only updated via the `Name` setter, maintaining consistency between name and ID.

---

## See Also

- **[GameplayGeneralSettings](GameplayGeneralSettings.md)** - Main gameplay configuration
- **[Settings](Settings.md)** - Other prototype settings
