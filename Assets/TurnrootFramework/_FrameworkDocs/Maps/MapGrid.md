# MapGrid — short ref

World grid component used to create, edit, and serialize the map surface and feature layer.

What it is
- A MonoBehaviour which owns a 2D grid of `MapGridPoint` child GameObjects. Stores grid meta (width/height, map name, scale) and a serialized feature layer used by the editor.

What it does
- Create and manage grid points, add/remove rows/columns, connect to 3D height meshes, and save/load feature layers.
- Provides helper operations to get a `MapGridPoint` at a row/column and to apply persisted feature data at load time.

Public methods
- `CreateChildrenPoints()` — create all grid points for the configured width/height
- `AddRow()` / `AddColumn()` / `RemoveRow()` / `RemoveColumn()` — adjust grid size
- `ConnectTo3DMapObject()` — compute 3D raycast points and store them for runtime or editing
- `ClearGrid()` — delete all child points and clear the dictionary
- `RebuildGridDictionary()` — rebuild internal lookup from existing children
- `SaveFeatureLayer()` / `LoadFeatureLayer()` — (de)serialize the feature layer and per-point properties
- `EnsureGridPoints()` — create or re-sync grid points to match width/height
- `GetGridPoint(int row, int col)` — returns the `MapGridPoint` at coordinates or null

Where to look
- Source: `Assets/TurnrootFramework/Maps/Components/Grids/MapGrid.cs`

See also
- [MapGridPoint](./MapGridPoint.md) — details for the single tile data model
- [MapGridEditorWindow](./MapGridEditorWindow.md) — editor that paints terrain and features
- [MapGridFeatureProperties](./Property Components/MapGridFeatureProperties.md) — defaults applied to features
