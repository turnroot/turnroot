# CharacterSettings

Centralized cache for character-related settings. Provides safe access with default fallbacks and eliminates repetitive singleton access.

## Overview

Manages settings access with automatic caching and exception handling. Prevents Unity serialization errors during editor operations.

**Namespace**: `Turnroot.Characters`  
**Source**: `Assets/TurnrootFramework/Characters/CharacterSettings.cs`

## Properties

```csharp
// Maximum non-weapon equipment slots (default: 2)
public static int MaxNonWeaponSlots { get; }

// Character prototype settings asset
public static CharacterPrototypeSettings PrototypeSettings { get; }

// Default character stats configuration
public static DefaultCharacterStats DefaultStats { get; }
```

## Usage

```csharp
// Access with automatic caching and error handling
int slots = CharacterSettings.MaxNonWeaponSlots;

// Settings are loaded once and cached
var settings = CharacterSettings.PrototypeSettings;
var defaults = CharacterSettings.DefaultStats;

// Manual cache clear (rarely needed)
CharacterSettings.ClearCache();
```

## Error Handling

Automatically handles:
- **UnityException**: Resources.Load called during serialization (uses defaults)
- **General exceptions**: Logs error and returns safe default
- **Missing assets**: Logs error once, returns null

### Example: Safe Defaults

```csharp
// If MaxNonWeaponSlots can't be loaded, defaults to 2
int slots = CharacterSettings.MaxNonWeaponSlots; // Never throws

// If PrototypeSettings missing, logs error and returns null
var settings = CharacterSettings.PrototypeSettings;
if (settings == null) {
    // Handle missing settings
}
```

## Cache Management

Cache automatically clears:
- **Script reload**: Editor scripts reloaded
- **Play mode**: Entering/exiting play mode

Manual clearing:
```csharp
// Force reload settings from disk
CharacterSettings.ClearCache();
```

## Replacement Pattern

**Before** (manual try-catch):
```csharp
try {
    var settings = GameSettingsLoader.LoadFirst<CharacterPrototypeSettings>("GameSettings");
    if (settings == null) {
        Debug.LogError("Settings not found!");
        return;
    }
    // Use settings
} catch (UnityException) {
    // Handle serialization error
} catch (Exception ex) {
    Debug.LogError($"Error: {ex.Message}");
}
```

**After** (using CharacterSettings):
```csharp
var settings = CharacterSettings.PrototypeSettings;
// Error handling and logging automatic
```

## See Also

- [CharacterPrototypeSettings](../Configurations/Settings.md) - Prototype settings asset
- [DefaultCharacterStats](DefaultCharacterStats.md) - Default stat definitions
- [CharacterInventoryInstance](CharacterInventory.md) - Uses MaxNonWeaponSlots
