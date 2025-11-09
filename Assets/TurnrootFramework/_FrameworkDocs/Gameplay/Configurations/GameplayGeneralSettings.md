# GameplayGeneralSettings
**Type:** `ScriptableObject`
**Location:** `Assets/TurnrootFramework/Gameplay/Configurations/GameplayGeneralSettings.cs`

Controls global gameplay feature toggles and settings, including which item subtypes are enabled.

## Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `_itemsCanBeGifts` | `bool` | If true, Gift item subtype is enabled |
| `_itemsCanBeLostItems` | `bool` | If true, LostItem item subtype is enabled |

## Methods

```csharp
public bool UseItemsCanBeGifts() // Returns _itemsCanBeGifts
public bool UseItemsCanBeLostItems() // Returns _itemsCanBeLostItems
```

## Integration
- **ObjectSubtype**: Uses these settings to determine which subtypes are valid/enabled
- **ObjectItem**: Subtype dropdown and validation respects these toggles

```
