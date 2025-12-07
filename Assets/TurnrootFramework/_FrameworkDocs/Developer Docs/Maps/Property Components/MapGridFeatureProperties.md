# MapGridFeatureProperties — short ref

ScriptableObject that stores default property values for features (e.g., treasure, doors).

What it is
- Inherits from `MapGridPropertyBase` and adds `featureId` + `featureName` for matching the defaults to a named feature.
- `MapGridPointFeatureProperties` is an alias type specifically used for point features.

What it does
- When a feature is applied in the editor, `MapGridPoint.ApplyDefaultsForFeature` loads `MapGridFeatureProperties` assets from `Resources/GameSettings`, finds a matching `featureId`, and applies typed defaults to the point's feature property lists.

Public fields
- `featureId`, `featureName` — used for matching and display in the editor
- Typed property lists inherited from `MapGridPropertyBase`

Where to look
- Source: `Assets/TurnrootFramework/Maps/Components/Grids/Property Components/MapGridFeatureProperties.cs`

See also
- [MapGridPoint](../MapGridPoint.md) — `ApplyDefaultsForFeature` and feature property usage
- [MapGridFeaturePropertiesEditor](../Editor/MapGridFeaturePropertiesEditor.md) — editor for creating/maintaining feature defaults
