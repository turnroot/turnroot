# MapGridPropertyBase — short ref

Base typed property system used by `MapGridPoint` and `FeatureProperties` for flexible, typed properties.

What it is
- A `ScriptableObject` base with several serializable typed property containers: `StringProperty`, `EventProperty`, `UnitProperty`, `ObjectItemProperty`, `BoolProperty`, `IntProperty`, `FloatProperty`.

What it does
- Provides `IProperty` typed containers with `key` and `GetValue/SetValue` semantics, allowing MapGrid editor and runtime code to interact with point/feature properties generically while still keeping typed accessors.
- `MapGridFeatureProperties` inherits this to expose default properties for features.

Public API
- `GetProperty<T>(string key)` — generic getter to fetch a typed property container by key
- `SetProperty<T>(string key, object value)` — generic setter to create or update a typed property
- Property lists exposed for editor serialization: `stringProperties`, `eventProperties`, `unitProperties`, `objectItemProperties`, `boolProperties`, `intProperties`, `floatProperties`

Where to look
- Source: `Assets/TurnrootFramework/Maps/Components/Grids/Property Components/MapGridPropertyBase.cs`

See also
- [MapGridPoint](../MapGridPoint.md) — how MapGridPoint uses these typed properties
- [MapGridFeatureProperties](./MapGridFeatureProperties.md) — default values for features
