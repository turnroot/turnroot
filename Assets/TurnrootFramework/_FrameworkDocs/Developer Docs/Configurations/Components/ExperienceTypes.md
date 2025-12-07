# Experience Types

## Classes

### ExperienceType

Defines a type of experience that units can gain (e.g., Sword, Axe, Riding, Authority).

**Location:** `Assets/TurnrootFramework/Gameplay/Combat/FundamentalComponents/ExperienceType.cs`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name of the experience type. Setting this auto-generates the `Id`. |
| `Id` | `string` | Unique identifier (lowercase, no spaces). **Auto-generated** from `Name`. Read-only in inspector. |
| `Enabled` | `bool` | Whether this experience type is active in the game. |
| `HasWeaponType` | `bool` | Whether this experience type is associated with a weapon type. |
| `AssociatedWeaponType` | `WeaponType` | The weapon type linked to this experience. Only visible when `HasWeaponType` is true. |

#### Inspector Behavior

- **Name Field**: Editable text field
- **Id Field**: Hidden (auto-generated)
- **Enabled**: Checkbox
- **HasWeaponType**: Checkbox
- **AssociatedWeaponType**: Only shown when HasWeaponType is checked

#### ID generation

The `Id` is automatically generated from the `Name` when it is set. The rule used in code is:
- Convert to lowercase
- Remove spaces

Example: "Long Sword" -> `longsword`

Note: `Id` has a private setter and cannot be written to in the inspector; it's computed from `Name`.

---

### WeaponType

Defines a weapon type with its characteristics and combat ranges.

**Location:** `Assets/TurnrootFramework/Gameplay/Objects/Components/WeaponType.cs`  
**Inherits:** `ScriptableObject`

#### Creation

Create via: Assets → Create → Turnroot → Game Settings → Gameplay → Weapon Type (`CreateAssetMenu` configured in code).

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name of the weapon type (e.g., "Sword", "Bow"). |
| `Icon` | `Sprite` | Visual icon for the weapon type. |
| `Id` | `string` | Unique identifier for lookups (editable). |
| `Ranges` | `int[]` | Array of valid combat ranges (e.g., [1] for melee, [2,3] for 2-3 range). |
| `DefaultRange` | `int` | Default/preferred range for this weapon type. |

---

## See Also

- **[GameplayGeneralSettings](../GameplayGeneralSettings.md)** - Main gameplay configuration
- **[Settings](../Settings.md)** - Other prototype settings (graphics, character defaults)
- `Assets/TurnrootFramework/Skills/Nodes/Nodes/Conditions/WeaponTypeNode.cs` — example node that queries weapon type in a node graph
