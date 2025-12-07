# MapGridPointFeature — short ref

Feature descriptor and convenience helpers for MapGrid features (treasure, door, warp, etc.).

What it is
- Simple serializable class containing the feature ID, display name, and `MapGridPointFeatureProperties` that may include default typed properties.
- Includes an enum `FeatureType` and conversions `TypeFromId` / `IdFromType`.

What it does
- `TypeFromId(string)` / `IdFromType(FeatureType)` map feature IDs to enum cases for editor UI grouping.
- `GetFeatureLetter(string)` returns a single-character marker shown in the editor overlay for a compact representation of the feature.

Public helpers
- `TypeFromId(string)` / `IdFromType(FeatureType)` — id/enum conversions
- `GetFeatureLetter(string)` — single-letter code for editor overlay

Where to look
- Source: `Assets/TurnrootFramework/Maps/Components/Grids/Property Components/MapGridPointFeature.cs`

See also
- [MapGridEditorWindow](../../MapGridEditorWindow.md) — editor overlay and feature palette
- [MapGridFeatureProperties](./MapGridFeatureProperties.md) — feature defaults asset
