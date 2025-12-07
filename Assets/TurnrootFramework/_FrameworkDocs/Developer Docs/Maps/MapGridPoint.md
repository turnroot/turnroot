# MapGridPoint — short ref

Represents a single grid tile with typed properties and an optional feature.

What it is
- MonoBehaviour attached to grid child GameObjects that stores typed property lists (string, bool, int, float, unit templates, object-item instances, UnityEvents) for both the point and its active feature.
- Also contains an optional starting `SpawnPoint` and first-class fields for defaults (starting unit, friendly/enemy enters events).

What it does
- Provides typed getters and setters for point- and feature-level properties using `MapGridPropertyBase` value containers.
- Holds and applies feature defaults from `MapGridFeatureProperties` when a feature is applied.
- Grid editor uses this component to allow painting features and editing per-tile properties.

Public methods
- `Initialize(int row, int col)` — set row/col and initialize defaults
- `SetFeatureTypeId(string)` / `ApplyFeature(string id, string name, bool toggle)` / `ClearFeature()` — manage the feature assigned to this point
- `ApplyDefaultsForFeature(string featureId)` — apply defaults from `MapGridFeatureProperties` asset
- `Set/Get` property methods for typed properties:
  - `SetStringPointProperty/GetStringPointProperty`, `GetAllStringPointProperties()`
  - `SetBoolPointProperty/GetBoolPointProperty`, `GetAllBoolPointProperties()`
  - `SetIntPointProperty/GetIntPointProperty`, `GetAllIntPointProperties()`
  - `SetFloatPointProperty/GetFloatPointProperty`, `GetAllFloatPointProperties()`
  - `SetUnitPointProperty/GetUnitPointProperty`, `GetAllUnitPointProperties()`
  - `SetObjectItemPointProperty/GetObjectItemPointProperty`, `GetAllObjectItemPointProperties()`
  - Event variants for `SetEventPointProperty/GetEventPointProperty` (UnityEvent)
- `SetTerrainTypeId(string)` — assign terrain type on the tile
 - `GetNeighbors(bool cardinal = false)` — returns a dictionary of neighbor `MapGridPoint`s keyed by direction (N/NE/E/...); `cardinal=true` returns only cardinal neighbors (N/E/S/W). Used by navigation (A*).
- Getter helpers for the defaults: `GetStartingUnit()`, `GetFriendlyEntersEvent()`, `GetEnemyEntersEvent()`

Where to look
- Source: `Assets/TurnrootFramework/Maps/Components/Grids/MapGridPoint.cs`
- Feature defaults: `Assets/TurnrootFramework/Maps/Components/Grids/Property Components/MapGridFeatureProperties.cs`

See also
- [MapGrid](./MapGrid.md) — grid-level helpers and scene utilities
- [MapGridEditorWindow](./MapGridEditorWindow.md) — editor that exposes these properties via a UI
- [MapGridPropertyBase](./Property Components/MapGridPropertyBase.md) — underlying typed property containers
