# MapGrid Editor Window — short ref

Editor window used to paint terrain, features and test movement on `MapGrid` assets.

What it is
- Unity EditorWindow (`MapGridEditorWindow`) with paint tools, hotkeys, a right-side properties panel and a test movement/debug panel. Opens from `Turnroot/Editors/Map Grid Editor`.

What it does
- Allows painting of terrain tile types and features (treasure, doors, warps, etc.).
- Displays per-tile properties; supports saving/loading features to the serialized feature layer on the `MapGrid`.
- Provides movement testing (walk/fly/ride) and special grid tools (3D height connection, add/remove rows).

Public methods / entry points
- `Open()` — static menu entry to open the window
- Editor UI hooks (protected/private) for drawing controls and preview
- Responds to `MapGrid.CreateChildrenPoints`, `MapGrid.SaveFeatureLayer`, etc., via editor and UI commands

Where to look
- Source: `Assets/TurnrootFramework/Maps/Editor/MapGridEditorWindow.cs`
- Inspector: `Assets/TurnrootFramework/Maps/Editor/MapGridInspector.cs`

See also
- [MapGrid](./MapGrid.md) — grid operations and save/load
- [MapGridPoint](./MapGridPoint.md) — per-tile property access and defaults
- [MapGridEditorSettings](./MapGridEditorSettings.md) — editor color and layout settings
