#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public class MapGridEditorWindow : EditorWindow
{
    private MapGrid _grid;
    private TerrainTypes _terrainAsset;
    private int _selectedTerrainIndex = 0;
    private Vector2 _scroll = Vector2.zero;
    private float _zoom = 1f;
    private int _baseCellSize = 24;
    private bool _isDragging = false;
    private Vector2Int _dragStart;
    private Vector2Int _dragEnd;
    private Vector2Int _hoveredCell = new(-1, -1);

    private enum Mode
    {
        Paint = 0,
        TestMovement = 1,
    }

    private Mode _mode = Mode.Paint;

    // A* options
    private bool _asWalk = true;
    private bool _asFly = false;
    private bool _asRide = false;
    private bool _asMagic = false;
    private bool _asArmor = false;
    private float _sameDirectionMultiplier = 0.95f;

    // Movement tester
    private int _testMovementValue = 5;
    private MapGridPoint _testMovementStart = null;
    private Dictionary<MapGridPoint, float> _testMovementResults = null;

    // Icon cache for terrain palette
    private Dictionary<string, Texture2D> _terrainIcons = new();

    [MenuItem("Turnroot/Editors/Map Grid Editor")]
    public static void Open()
    {
        GetWindow<MapGridEditorWindow>("Map Grid Editor");
    }

    private void OnEnable()
    {
        _terrainAsset = TerrainTypes.LoadDefault();
        this.minSize = new Vector2(600, 480);
        this.maxSize = new Vector2(2000, 900);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _grid = (MapGrid)EditorGUILayout.ObjectField(_grid, typeof(MapGrid), true);
        if (GUILayout.Button("Refresh"))
        {
            _terrainAsset = TerrainTypes.LoadDefault();
            Repaint();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Auto-ensure grid points if the assigned MapGrid has an empty index.
        if (_grid != null && _grid.GetGridPoint(0, 0) == null)
        {
            _grid.EnsureGridPoints();
            EditorUtility.SetDirty(_grid);
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
            SceneView.RepaintAll();
        }

        if (_grid == null)
        {
            EditorGUILayout.HelpBox("Assign a MapGrid to edit.", MessageType.Info);
            return;
        }

        // If the grid's internal index appears empty, offer to ensure points exist.
        if (_grid.GetGridPoint(0, 0) == null)
        {
            EditorGUILayout.HelpBox(
                "No labels, no colors. Grid points appear missing.",
                MessageType.Warning
            );
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ensure Grid Points"))
            {
                _grid.EnsureGridPoints();
                EditorUtility.SetDirty(_grid);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Rebuild Index"))
            {
                _grid.RebuildGridDictionary();
                EditorUtility.SetDirty(_grid);
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        if (_terrainAsset == null)
        {
            EditorGUILayout.HelpBox("No TerrainTypes asset found.", MessageType.Warning);
        }

        _mode = (Mode)GUILayout.Toolbar((int)_mode, new string[] { "Paint", "Test Movement" });

        // Test Movement controls
        if (_mode == Mode.TestMovement)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Movement:", GUILayout.Width(70));
            _testMovementValue = EditorGUILayout.IntSlider(
                _testMovementValue,
                1,
                10,
                GUILayout.Width(300)
            );
            if (GUILayout.Button("Clear Test", GUILayout.Width(90)))
            {
                _testMovementStart = null;
                _testMovementResults = null;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            // Movement type toggles (same as A* tab) so TestMovement uses the same cost flags
            EditorGUILayout.BeginHorizontal();
            _asWalk = GUILayout.Toggle(_asWalk, "Walk", "Button");
            _asFly = GUILayout.Toggle(_asFly, "Fly", "Button");
            _asRide = GUILayout.Toggle(_asRide, "Ride", "Button");
            _asMagic = GUILayout.Toggle(_asMagic, "Magic", "Button");
            _asArmor = GUILayout.Toggle(_asArmor, "Armor", "Button");
            GUILayout.Label("Same-dir:", GUILayout.Width(70));
            _sameDirectionMultiplier = EditorGUILayout.Slider(
                _sameDirectionMultiplier,
                0.5f,
                1.1f,
                GUILayout.Width(150)
            );
            EditorGUILayout.EndHorizontal();
        }

        // Terrain selector (only shown in Paint mode)
        if (_mode == Mode.Paint)
        {
            DrawTerrainSelector();
            DrawTerrainPalette();
        }

        // Zoom controls
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Zoom:", GUILayout.Width(40));
        _zoom = EditorGUILayout.Slider(_zoom, 0.25f, 3f);
        if (GUILayout.Button("Reset Zoom", GUILayout.Width(90)))
        {
            _zoom = 1f;
        }

        EditorGUILayout.EndHorizontal();

        float statusBarH = 24f;
        Rect area = GUILayoutUtility.GetRect(position.width, position.height - 120 - statusBarH);
        DrawGridArea(area);

        // Status bar showing hovered tile (row/col)
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.FlexibleSpace();
        string hoverText =
            _hoveredCell.x >= 0
                ? $"Hovered: Row {_hoveredCell.x}, Col {_hoveredCell.y}"
                : "Hovered: (none)";
        GUILayout.Label(hoverText);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTerrainSelector()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Terrain:", GUILayout.Width(60));
        if (_terrainAsset != null && _terrainAsset.Types != null)
        {
            string[] names = new string[_terrainAsset.Types.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = _terrainAsset.Types[i].Name ?? i.ToString();
            }

            _selectedTerrainIndex = EditorGUILayout.Popup(_selectedTerrainIndex, names);
            var col = _terrainAsset.Types[_selectedTerrainIndex].EditorColor;
            Rect cRect = GUILayoutUtility.GetRect(18, 18);
            EditorGUI.DrawRect(cRect, col);
        }
        else
        {
            EditorGUILayout.LabelField("(none)");
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTerrainPalette()
    {
        if (_terrainAsset == null || _terrainAsset.Types == null)
        {
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Palette:", GUILayout.Width(60));
        foreach (var t in _terrainAsset.Types)
        {
            if (t == null)
            {
                continue;
            }

            Texture2D icon = GetTerrainIcon(t);
            GUIContent content;
            if (icon != null)
            {
                content = new GUIContent(icon, t.Name);
            }
            else
            {
                content = new GUIContent(t.Name);
            }

            GUIStyle style = new(GUI.skin.button);
            style.fixedWidth = 40;
            style.fixedHeight = 24;

            int idx = System.Array.IndexOf(_terrainAsset.Types, t);
            if (GUILayout.Button(content, style))
            {
                _selectedTerrainIndex = idx;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private Texture2D GetTerrainIcon(TerrainType t)
    {
        if (t == null)
        {
            return null;
        }
        // Use a stable cache key (prefer Id, otherwise Name)
        string key = !string.IsNullOrEmpty(t.Id) ? t.Id : t.Name;
        if (_terrainIcons.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var NameWithoutSpaces = t.Name.Replace(" ", "").ToLower();

        // Try multiple Resources subfolders and both Id and Name lookups.
        Texture2D tex = null;
        string[] tryPaths = new string[]
        {
            "TerrainIcons/" + t.Id,
            "TerrainIcons/" + NameWithoutSpaces,
            "EditorSettings/MapGridEditorIcons/" + t.Id,
            "EditorSettings/MapGridEditorIcons/" + NameWithoutSpaces,
        };
        foreach (var p in tryPaths)
        {
            if (string.IsNullOrEmpty(p))
            {
                continue;
            }

            tex = Resources.Load<Texture2D>(p);
            if (tex != null)
            {
                break;
            }
        }

        _terrainIcons[key] = tex;
        return tex;
    }

    private void DrawGridArea(Rect area)
    {
        // Editor window renders cell colors and overlays directly.

        float cellSize = _baseCellSize * _zoom;
        int width = _grid.GridWidth;
        int height = _grid.GridHeight;

        // Scrollable view
        _scroll = GUI.BeginScrollView(
            area,
            _scroll,
            new Rect(0, 0, width * cellSize, height * cellSize)
        );

        // Background
        var contentRect = new Rect(0, 0, width * cellSize, height * cellSize);
        EditorGUI.DrawRect(contentRect, Color.grey * 0.2f);

        Event e = Event.current;
        // After BeginScrollView the GUI coordinate space is already translated into
        // the scroll content. Use Event.mousePosition plus the scroll offset to
        // get content-local coordinates (do not subtract the window `area` position).
        Vector2 localMouse = e.mousePosition + _scroll;

        float contentW = width * cellSize;
        float contentH = height * cellSize;
        if (
            localMouse.x >= 0
            && localMouse.y >= 0
            && localMouse.x <= contentW
            && localMouse.y <= contentH
        )
        {
            Vector2Int cell = MouseToCell(localMouse, cellSize);
            _hoveredCell = ClampCell(cell, width, height);
        }
        else
        {
            _hoveredCell = new Vector2Int(-1, -1);
        }

        if (e.type == EventType.MouseMove)
        {
            Repaint();
        }

        HandleMouse(e, localMouse, cellSize, width, height);

        // Determine farthest reachable cost for TestMovement visualization
        float _testMaxCost = float.NegativeInfinity;
        if (_mode == Mode.TestMovement && _testMovementResults != null)
        {
            foreach (var kv in _testMovementResults)
            {
                if (kv.Value > _testMaxCost)
                {
                    _testMaxCost = kv.Value;
                }
            }
            if (float.IsNegativeInfinity(_testMaxCost))
            {
                _testMaxCost = float.MinValue;
            }
        }

        // Draw cells
        for (int r = 0; r < width; r++)
        {
            for (int c = 0; c < height; c++)
            {
                Rect cellRect = new(r * cellSize, c * cellSize, cellSize, cellSize);
                // Terrain color
                var point = _grid.GetGridPoint(r, c);
                Color fill = Color.white;
                if (point != null)
                {
                    TerrainType tt = null;
                    if (_terrainAsset != null)
                    {
                        tt = _terrainAsset.GetTypeById(point.TerrainTypeId);
                    }
                    tt ??= point.SelectedTerrainType;

                    if (tt != null)
                    {
                        fill = tt.EditorColor;
                    }
                }
                EditorGUI.DrawRect(cellRect, fill);

                // Selection overlay (paint mode)
                if (_mode == Mode.Paint && _isDragging)
                {
                    int minR = Mathf.Min(_dragStart.x, _dragEnd.x);
                    int maxR = Mathf.Max(_dragStart.x, _dragEnd.x);
                    int minC = Mathf.Min(_dragStart.y, _dragEnd.y);
                    int maxC = Mathf.Max(_dragStart.y, _dragEnd.y);
                    if (r >= minR && r <= maxR && c >= minC && c <= maxC)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(0, 0, 0, 0.25f));
                    }
                }

                // Test Movement overlays (drawn when TestMovement mode is active)
                if (_mode == Mode.TestMovement && _testMovementResults != null)
                {
                    if (point != null && _testMovementResults.TryGetValue(point, out var cost))
                    {
                        EditorGUI.DrawRect(cellRect, new Color(0.4f, 0f, 0.4f, 1f));

                        // Draw cost overlay centered on the tile for debugging/inspection.
                        var costText = cost.ToString("0");
                        var txtStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = Mathf.Max(10, Mathf.FloorToInt(cellSize / 3f)),
                        };
                        txtStyle.normal.textColor = Color.white;
                        EditorGUI.LabelField(cellRect, costText, txtStyle);
                    }
                }

                // 1px black lines
                EditorGUI.DrawRect(
                    new Rect(cellRect.x, cellRect.y, cellRect.width, 1f),
                    Color.black
                );
                EditorGUI.DrawRect(
                    new Rect(cellRect.x, cellRect.y + cellRect.height - 1f, cellRect.width, 1f),
                    Color.black
                );
                EditorGUI.DrawRect(
                    new Rect(cellRect.x, cellRect.y, 1f, cellRect.height),
                    Color.black
                );
                EditorGUI.DrawRect(
                    new Rect(cellRect.x + cellRect.width - 1f, cellRect.y, 1f, cellRect.height),
                    Color.black
                );
            }
        }

        GUI.EndScrollView();

        if (GUI.changed)
        {
            Repaint();
        }
    }

    private void HandleMouse(Event e, Vector2 localMouse, float cellSize, int width, int height)
    {
        float contentW = width * cellSize;
        float contentH = height * cellSize;
        bool inside =
            localMouse.x >= 0
            && localMouse.y >= 0
            && localMouse.x <= contentW
            && localMouse.y <= contentH;

        if (_mode == Mode.Paint)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && inside)
            {
                GUI.FocusControl(null);
                Vector2Int cell = MouseToCell(localMouse, cellSize);
                _dragStart = ClampCell(cell, width, height);
                _dragEnd = _dragStart;
                _isDragging = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isDragging)
            {
                Vector2Int cell = MouseToCell(localMouse, cellSize);
                _dragEnd = ClampCell(cell, width, height);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && _isDragging)
            {
                _isDragging = false;
                ApplyTerrainToSelection();
                e.Use();
            }
        }
        else if (_mode == Mode.TestMovement)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (!inside)
                {
                    e.Use();
                    return;
                }
                Vector2Int cell = MouseToCell(localMouse, cellSize);
                Vector2Int cellClamped = ClampCell(cell, width, height);
                var clicked = _grid.GetGridPoint(cellClamped.x, cellClamped.y);
                if (clicked != null)
                {
                    _testMovementStart = clicked;
                    var aStar = new AStarModified();
                    _testMovementResults = aStar.GetReachable(
                        _grid,
                        _testMovementStart,
                        _testMovementValue,
                        _asWalk,
                        _asFly,
                        _asRide,
                        _asMagic,
                        _asArmor,
                        _sameDirectionMultiplier
                    );
                    Repaint();
                }
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                _testMovementStart = null;
                _testMovementResults = null;
                e.Use();
            }
        }
    }

    private Vector2Int MouseToCell(Vector2 localMouse, float cellSize)
    {
        int r = Mathf.FloorToInt(localMouse.x / cellSize);
        int c = Mathf.FloorToInt(localMouse.y / cellSize);
        return new Vector2Int(r, c);
    }

    private Vector2Int ClampCell(Vector2Int cell, int width, int height)
    {
        cell.x = Mathf.Clamp(cell.x, 0, Mathf.Max(0, width - 1));
        cell.y = Mathf.Clamp(cell.y, 0, Mathf.Max(0, height - 1));
        return cell;
    }

    private void ApplyTerrainToSelection()
    {
        if (_grid == null || _terrainAsset == null || _terrainAsset.Types == null)
        {
            return;
        }

        int minR = Mathf.Min(_dragStart.x, _dragEnd.x);
        int maxR = Mathf.Max(_dragStart.x, _dragEnd.x);
        int minC = Mathf.Min(_dragStart.y, _dragEnd.y);
        int maxC = Mathf.Max(_dragStart.y, _dragEnd.y);
        var chosen = _terrainAsset.Types[_selectedTerrainIndex];
#if UNITY_EDITOR
        // Batch apply: record undo per-point and mark each point dirty, then mark scene dirty once.
        int painted = 0;
        for (int r = minR; r <= maxR; r++)
        {
            for (int c = minC; c <= maxC; c++)
            {
                var p = _grid.GetGridPoint(r, c);
                if (p != null)
                {
                    Undo.RecordObject(p, "MapGrid Edit");
                    p.SetTerrainTypeId(chosen.Id);
                    EditorUtility.SetDirty(p);
                    painted++;
                }
            }
        }
        EditorUtility.SetDirty(_grid);
        // Mark the active scene dirty so changes persist
        var _scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(_scene);
        SceneView.RepaintAll();
#endif
    }
}
#endif
