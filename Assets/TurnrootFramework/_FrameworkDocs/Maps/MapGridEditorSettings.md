# MapGridEditorSettings — short ref

Editor settings ScriptableObject used by the Map Grid Editor Window.

What it is
- A `ScriptableObject` providing per-editor settings such as grid color/thickness, UI layout indentation, border highlight colors for selection and modified tiles, and header accent colors for property sections.

What it does
- Centralizes editor look-and-feel for `MapGridEditorWindow` so designers can configure selection colors, header accents, feature display modes and grid rendering.

Public fields (editor-set values)
- `gridThickness`, `gridColor` — grid line thickness and color
- `featureDisplay` — `FeatureDisplay.Icon` or `FeatureDisplay.Initial` used in the overlay
- `propertyIndent` — indentation in property panels
- `selectedFeatureBorderColor`, `selectedTileBorderColor`, `modifiedPropertyBorderColor` — selection & border colors
- `headerAccentColor` and per-type header accent colors for different property types

Where to look
Source: `Assets/TurnrootFramework/Maps/Editor/MapGridEditorSettings.cs`

See also
- [MapGridEditorWindow](./MapGridEditorWindow.md) — how settings are consumed
- [MapGridPoint](./MapGridPoint.md) — which properties get header accents in the editor
