#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class MapGridEditorWindow : EditorWindow
{
    // Core references
    private MapGrid _grid;
    private TerrainTypes _terrainAsset;

    // State
    private int _selectedTerrainIndex = 0;
    private Vector2 _scroll = Vector2.zero;
    private float _zoom = 1f;
    private bool _isDragging = false;
    private Vector2Int _dragStart,
        _dragEnd,
        _hoveredCell = new(-1, -1);
    private bool _spacePan = false,
        _isPanning = false;
    private Mode _mode = Mode.Paint;

    // Dimension tracking for auto-refresh
    private int _lastKnownWidth = 0;
    private int _lastKnownHeight = 0;

    // Feature editing
    private MapGridPoint _selectedFeaturePoint = null;
    private int _selectedSecondTool = -1;
    private string _selectedSecondToolName = string.Empty;

    // Snapshots for stable UI
    private List<KeyValuePair<string, string>> _detailSnapshotProps = null;
    private List<string> _detailSnapshotPropTypes = null;

    // Movement testing
    private bool _asWalk = true,
        _asFly = false,
        _asRide = false,
        _asMagic = false,
        _asArmor = false;
    private float _sameDirectionMultiplier = 0.95f;
    private int _testMovementValue = 5;
    private MapGridPoint _testMovementStart = null;
    private Dictionary<MapGridPoint, float> _testMovementResults = null;

    // UI constants
    private readonly int _baseCellSize = 24;
    private const int GUI_BUTTON_SIZE = 40;
    private const string PATH_TERRAIN_ICONS = "TerrainIcons/";
    private const string PATH_FEATURE_ICONS = "FeatureIcons/";
    private const string PATH_EDITOR_ICONS = "EditorSettings/MapGridEditorIcons/";

    // Cached resources
    private readonly Dictionary<string, Texture2D> _terrainIcons = new();
    private MapGridEditorSettings _editorSettings;
    private string _editorSettingsPath = string.Empty;
    private DateTime _editorSettingsLastWriteTimeUtc = DateTime.MinValue;
    private GUIStyle _guiStyleButton,
        _guiStyleWrap,
        _guiStyleBoldWrap;
    private Dictionary<KeyCode, string> _featureHotkeys = null;
    private Dictionary<KeyCode, int> _terrainHotkeys = null;
    private Dictionary<KeyCode, string> _shiftHotkeys = null;
    private static Dictionary<string, MapGridPointFeatureProperties> _featureDefaultsCache = new(
        StringComparer.OrdinalIgnoreCase
    );

    // Tool definitions
    private readonly string[] _secondToolIds = new[]
    {
        "treasure",
        "door",
        "warp",
        "healing",
        "ranged",
        "mechanism",
        "control",
        "breakable",
        "shelter",
        "village",
        "fortress",
        "underground",
        "cursor",
        "eraser",
    };
    private readonly string[] _secondToolNames = new[]
    {
        "Treasure",
        "Door",
        "Warp",
        "Healing",
        "Ranged",
        "Mechanism",
        "Control",
        "BreakableWall",
        "Shelter",
        "Village",
        "Fortress",
        "UndergroundItem",
        "Cursor",
        "Eraser",
    };

    private enum Mode
    {
        Paint = 0,
        TestMovement = 1,
        TroopsAndConditions = 2,
    }

    [MenuItem("Turnroot/Editors/Map Grid Editor")]
    public static void Open() => GetWindow<MapGridEditorWindow>("Map Grid Editor");

    private void OnEnable()
    {
        _terrainAsset = TerrainTypes.LoadDefault();
        _editorSettings = Resources.Load<MapGridEditorSettings>(
            "EditorSettings/MapGridEditorSettings"
        );
        if (_editorSettings != null)
        {
            _editorSettingsPath = AssetDatabase.GetAssetPath(_editorSettings) ?? string.Empty;
            if (!string.IsNullOrEmpty(_editorSettingsPath) && File.Exists(_editorSettingsPath))
                _editorSettingsLastWriteTimeUtc = File.GetLastWriteTimeUtc(_editorSettingsPath);
        }
        minSize = new Vector2(1000, 600);
        maxSize = new Vector2(1920, 1080);
        wantsMouseMove = true;
        _selectedSecondTool = -1;
        _selectedSecondToolName = string.Empty;
        _zoom = 1f;

        _featureHotkeys = new Dictionary<KeyCode, string>()
        {
            { KeyCode.T, "treasure" },
            { KeyCode.D, "door" },
            { KeyCode.W, "warp" },
            { KeyCode.H, "healing" },
            { KeyCode.R, "ranged" },
            { KeyCode.M, "mechanism" },
            { KeyCode.C, "control" },
            { KeyCode.B, "breakable" },
            { KeyCode.S, "shelter" },
            { KeyCode.V, "village" },
            { KeyCode.F, "fortress" },
            { KeyCode.U, "underground" },
            { KeyCode.E, "eraser" },
        };

        // Shift + terrain keys -> feature shortcuts (Shift+BackQuote -> treasure, Shift+1 -> door, ...)
        _shiftHotkeys = new Dictionary<KeyCode, string>()
        {
            { KeyCode.Tilde, "treasure" },
            { KeyCode.BackQuote, "treasure" },
            { KeyCode.Alpha1, "door" },
            { KeyCode.Alpha2, "warp" },
            { KeyCode.Alpha3, "healing" },
            { KeyCode.Alpha4, "ranged" },
            { KeyCode.Alpha5, "mechanism" },
            { KeyCode.Alpha6, "control" },
            { KeyCode.Alpha7, "breakable" },
            { KeyCode.Alpha8, "shelter" },
            { KeyCode.Alpha9, "village" },
            { KeyCode.Alpha0, "fortress" },
            { KeyCode.Minus, "underground" },
        };

        _terrainHotkeys = new Dictionary<KeyCode, int>()
        {
            { KeyCode.Tilde, 0 },
            { KeyCode.Alpha1, 1 },
            { KeyCode.Alpha2, 2 },
            { KeyCode.Alpha3, 3 },
            { KeyCode.Alpha4, 4 },
            { KeyCode.Alpha5, 5 },
            { KeyCode.Alpha6, 6 },
            { KeyCode.Alpha7, 7 },
            { KeyCode.Alpha8, 8 },
            { KeyCode.Alpha9, 9 },
            { KeyCode.Alpha0, 10 },
            { KeyCode.Minus, 11 },
            { KeyCode.Equals, 12 },
            { KeyCode.Q, 13 },
            { KeyCode.BackQuote, 0 },
        };
    }

    private void OnInspectorUpdate()
    {
        ReloadEditorSettingsIfChanged();
    }

    private void ReloadEditorSettingsIfChanged()
    {
        var loaded = Resources.Load<MapGridEditorSettings>("EditorSettings/MapGridEditorSettings");
        if (loaded == null)
        {
            if (_editorSettings != null)
            {
                _editorSettings = null;
                _editorSettingsPath = string.Empty;
                _editorSettingsLastWriteTimeUtc = DateTime.MinValue;
                Repaint();
            }
            return;
        }

        string path = AssetDatabase.GetAssetPath(loaded) ?? string.Empty;
        DateTime writeTime = DateTime.MinValue;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            writeTime = File.GetLastWriteTimeUtc(path);

        if (
            _editorSettings == null
            || path != _editorSettingsPath
            || writeTime != _editorSettingsLastWriteTimeUtc
        )
        {
            _editorSettings = loaded;
            _editorSettingsPath = path;
            _editorSettingsLastWriteTimeUtc = writeTime;
            Repaint();
        }
    }

    private void EnsureStyles()
    {
        if (_guiStyleButton != null)
            return;

        _guiStyleButton = new GUIStyle(GUI.skin.button)
        {
            fixedWidth = GUI_BUTTON_SIZE,
            fixedHeight = GUI_BUTTON_SIZE,
        };
        _guiStyleWrap = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            stretchWidth = true,
            richText = false,
        };
        _guiStyleBoldWrap = new GUIStyle(EditorStyles.boldLabel)
        {
            wordWrap = true,
            stretchWidth = true,
            richText = false,
        };

        _guiStyleWrap.normal.textColor = EditorStyles.label.normal.textColor;
        _guiStyleBoldWrap.normal.textColor = EditorStyles.boldLabel.normal.textColor;
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();

        if (Event.current?.type == EventType.MouseMove)
            Repaint();

        HandleKeyboardShortcuts();
        EnsureGridPoints();

        if (_grid == null)
        {
            EditorGUILayout.HelpBox("Assign a MapGrid to edit.", MessageType.Info);
            return;
        }

        if (_grid.GetGridPoint(0, 0) == null)
        {
            EditorGUILayout.HelpBox(
                "No labels, no colors. Grid points appear missing.",
                MessageType.Warning
            );
            if (GUILayout.Button("Ensure Grid Points"))
                EnsureGridPoints();
            if (GUILayout.Button("Rebuild Index"))
            {
                _grid.RebuildGridDictionary();
                MarkDirty();
            }
        }

        if (_terrainAsset == null)
            EditorGUILayout.HelpBox("No TerrainTypes asset found.", MessageType.Warning);

        _mode = (Mode)
            GUILayout.Toolbar(
                (int)_mode,
                new[] { "Paint Terrain Types", "Test Movement", "Troops and Conditions" }
            );

        DrawMainLayout();
        DrawStatusBar();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _grid = (MapGrid)EditorGUILayout.ObjectField(_grid, typeof(MapGrid), true);

        if (_grid != null)
        {
            // Check if dimensions changed and refresh if needed
            if (_lastKnownWidth != _grid.GridWidth || _lastKnownHeight != _grid.GridHeight)
            {
                _lastKnownWidth = _grid.GridWidth;
                _lastKnownHeight = _grid.GridHeight;
                _grid.EnsureGridPoints();
                Repaint();
            }

            GUILayout.Label($"{_grid.GridWidth}x{_grid.GridHeight}", GUILayout.Width(60));
            GUILayout.Label("Map Name:", GUILayout.Width(72));
            string newName = EditorGUILayout.TextField(
                _grid.MapName ?? string.Empty,
                GUILayout.Width(180)
            );
            if (newName != _grid.MapName)
            {
                Undo.RecordObject(_grid, "Edit Map Name");
                _grid.MapName = newName;
                MarkDirty();
            }
        }

        if (GUILayout.Button("Refresh"))
        {
            _terrainAsset = TerrainTypes.LoadDefault();
            if (_grid != null)
            {
                _grid.EnsureGridPoints();
                _lastKnownWidth = _grid.GridWidth;
                _lastKnownHeight = _grid.GridHeight;
            }
            Repaint();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;
        if (e?.type == EventType.KeyDown && !EditorGUIUtility.editingTextField)
        {
            if (e.keyCode == KeyCode.P)
            {
                _mode = Mode.Paint;
                _selectedSecondTool = -1;
                _selectedSecondToolName = string.Empty;
                e.Use();
            }
            if (_mode == Mode.Paint)
            {
                // Shift + terrain keys -> feature shortcuts (Shift+1 -> door, Shift+` -> treasure, etc.)
                if (e.shift)
                {
                    string shiftFeat = null;
                    if (_shiftHotkeys != null)
                        _shiftHotkeys.TryGetValue(e.keyCode, out shiftFeat);

                    if (
                        string.IsNullOrEmpty(shiftFeat)
                        && (e.character == '`' || e.character == '~')
                    )
                    {
                        // fallback to tilde/backquote mapping
                        if (_shiftHotkeys != null)
                        {
                            if (!_shiftHotkeys.TryGetValue(KeyCode.Tilde, out shiftFeat))
                                _shiftHotkeys.TryGetValue(KeyCode.BackQuote, out shiftFeat);
                        }
                    }

                    if (!string.IsNullOrEmpty(shiftFeat))
                    {
                        _selectedSecondTool = Array.IndexOf(_secondToolIds, shiftFeat);
                        _mode = Mode.Paint;
                        e.Use();
                        return;
                    }
                }

                if (_featureHotkeys?.TryGetValue(e.keyCode, out var featId) == true)
                {
                    _selectedSecondTool = Array.IndexOf(_secondToolIds, featId);
                    _mode = Mode.Paint;
                    e.Use();
                }
                else if (_terrainHotkeys?.TryGetValue(e.keyCode, out var terrainIdx) == true)
                {
                    if (
                        _terrainAsset?.Types != null
                        && terrainIdx >= 0
                        && terrainIdx < _terrainAsset.Types.Length
                    )
                    {
                        _selectedTerrainIndex = terrainIdx;
                        _selectedSecondTool = -1;
                        _selectedSecondToolName = string.Empty;
                        e.Use();
                        Repaint();
                    }
                }
            }

            bool ctrl = e.control || e.command;
            if (
                ctrl
                && (
                    e.keyCode == KeyCode.Equals
                    || e.keyCode == KeyCode.Plus
                    || e.keyCode == KeyCode.KeypadPlus
                )
            )
            {
                _zoom = Mathf.Min(3f, _zoom + 0.1f);
                e.Use();
                Repaint();
            }
            else if (ctrl && (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus))
            {
                _zoom = Mathf.Max(0.25f, _zoom - 0.1f);
                e.Use();
                Repaint();
            }

            if (e.keyCode == KeyCode.Space)
            {
                _spacePan = true;
                e.Use();
            }
        }
        else if (e?.type == EventType.KeyUp && e.keyCode == KeyCode.Space)
        {
            _spacePan = false;
            e.Use();
        }
    }

    private void EnsureGridPoints()
    {
        if (_grid != null && _grid.GetGridPoint(0, 0) == null)
        {
            _grid.EnsureGridPoints();
            MarkDirty();
            SceneView.RepaintAll();
        }
    }

    private void DrawMainLayout()
    {
        EditorGUILayout.BeginHorizontal();

        // Left toolbars
        EditorGUILayout.BeginVertical(GUILayout.Width(110));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(52));
        if (_mode == Mode.Paint)
            DrawTerrainPalette();
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUILayout.Width(52));
        if (_mode == Mode.Paint)
            DrawToolsPalette();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // Main area
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        if (_mode == Mode.TestMovement)
            DrawTestMovementControls();
        DrawZoomControls();

        float rightPanelW = 270f; // increased to give right panel +50px
        float leftAreaW = Mathf.Max(200f, position.width - 120f - rightPanelW);
        Rect area = GUILayoutUtility.GetRect(leftAreaW, position.height - 120 - 24);
        DrawGridArea(area);
        EditorGUILayout.EndVertical();

        // Right panel
        EditorGUILayout.BeginVertical(GUILayout.Width(rightPanelW));
        if (_selectedSecondTool >= 0 && _selectedSecondTool < _secondToolNames.Length)
            if (_mode != Mode.TestMovement)
            {
                DrawFeatureDetails(_secondToolIds[_selectedSecondTool]);
            }
            else
            {
                _selectedFeaturePoint = null;
                GUILayout.Space(4);
            }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTestMovementControls()
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

    private void DrawZoomControls()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Zoom:", GUILayout.Width(40));
        _zoom = EditorGUILayout.Slider(_zoom, 0.25f, 3f, GUILayout.Width(150));

        // Add Row and Column buttons
        if (_grid != null)
        {
            if (GUILayout.Button("+ Row", GUILayout.Width(60)))
            {
                Undo.RecordObject(_grid, "Add Row");
                _grid.AddRow();
                _lastKnownWidth = _grid.GridWidth;
                _lastKnownHeight = _grid.GridHeight;
                MarkDirty();
                Repaint();
            }

            if (GUILayout.Button("+ Column", GUILayout.Width(70)))
            {
                Undo.RecordObject(_grid, "Add Column");
                _grid.AddColumn();
                _lastKnownWidth = _grid.GridWidth;
                _lastKnownHeight = _grid.GridHeight;
                MarkDirty();
                Repaint();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        string leftAction =
            _mode == Mode.TestMovement
                ? "Left click: Start test"
                : (
                    _selectedSecondTool >= 0
                        ? "Left click: Add/Select feature"
                        : "Left click: Paint"
                );
        string controls =
            $"Ctrl +/- : Zoom | Space + Drag : Pan | {leftAction} | Left click + drag: Paint Area | [`, 1-0, -, =, Q] : Terrain | Shift+[`, 1-0, -, =] : Feature";
        string hoverText =
            _hoveredCell.x >= 0 ? $"Row {_hoveredCell.x}, Col {_hoveredCell.y}" : "(none)";

        GUILayout.Label($"{controls}", GUILayout.ExpandWidth(true));
        GUILayout.FlexibleSpace();
        GUILayout.Label(
            hoverText,
            new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MinWidth(100) }
        );
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTerrainPalette()
    {
        if (_terrainAsset?.Types == null)
            return;

        GUILayout.Label("Palette", EditorStyles.boldLabel);
        for (int i = 0; i < _terrainAsset.Types.Length; i++)
        {
            var t = _terrainAsset.Types[i];
            if (t == null)
                continue;

            Texture2D icon = GetTerrainIcon(t);
            GUIContent content =
                icon != null ? new GUIContent(icon, t.Name) : new GUIContent(t.Name);

            bool isSelected = _selectedTerrainIndex == i;
            bool newState = GUILayout.Toggle(isSelected, content, _guiStyleButton);
            if (newState && !isSelected)
            {
                _selectedTerrainIndex = i;
                _selectedSecondTool = -1;
                _selectedSecondToolName = string.Empty;
                _selectedFeaturePoint = null;
            }
        }

        if (
            _terrainAsset?.Types != null
            && _selectedTerrainIndex >= 0
            && _selectedTerrainIndex < _terrainAsset.Types.Length
        )
        {
            var col = _terrainAsset.Types[_selectedTerrainIndex].EditorColor;
            Rect cRect = GUILayoutUtility.GetRect(
                40,
                40,
                GUILayout.Width(40),
                GUILayout.Height(40)
            );
            EditorGUI.DrawRect(cRect, col);
            if (GUI.Button(cRect, GUIContent.none, GUIStyle.none))
                ShowTerrainPopup(cRect);
        }
    }

    private void DrawToolsPalette()
    {
        GUILayout.Label("Tools", EditorStyles.boldLabel);

        for (int i = 0; i < _secondToolIds.Length; i++)
        {
            string id = _secondToolIds[i];
            string label = _secondToolNames[i];

            Texture2D icon = GetSecondToolIcon(id);
            GUIContent content =
                icon != null ? new GUIContent(icon, label) : new GUIContent(label.Substring(0, 1));

            bool isSelected = _selectedSecondTool == i;
            bool newState = GUILayout.Toggle(isSelected, content, _guiStyleButton);
            if (newState && !isSelected)
            {
                _selectedSecondTool = i;
                _selectedSecondToolName = string.Empty;
                _selectedFeaturePoint = null;
                _selectedTerrainIndex = -1;
            }
            else if (!newState && isSelected)
            {
                _selectedSecondTool = -1;
                _selectedFeaturePoint = null;
            }
        }

        GUILayout.FlexibleSpace();
    }

    private void ShowTerrainPopup(Rect rect)
    {
        if (_terrainAsset?.Types == null)
            return;

        GenericMenu menu = new();
        for (int i = 0; i < _terrainAsset.Types.Length; i++)
        {
            var t = _terrainAsset.Types[i];
            string name = t?.Name ?? i.ToString();
            int idx = i;
            menu.AddItem(
                new GUIContent(name),
                idx == _selectedTerrainIndex,
                () =>
                {
                    _selectedTerrainIndex = idx;
                    Repaint();
                }
            );
        }
        menu.DropDown(rect);
    }

    private Texture2D GetIcon(string key, string[] paths)
    {
        if (_terrainIcons.TryGetValue(key, out var cached))
            return cached;

        Texture2D tex = null;
        foreach (var p in paths)
        {
            if (string.IsNullOrEmpty(p))
                continue;
            tex = Resources.Load<Texture2D>(p);
            if (tex != null)
                break;
        }

        _terrainIcons[key] = tex;
        return tex;
    }

    private Texture2D GetTerrainIcon(TerrainType t)
    {
        if (t == null)
            return null;
        string key = !string.IsNullOrEmpty(t.Id) ? t.Id : t.Name;
        string nameNoSpaces = (t.Name ?? string.Empty).Replace(" ", "").ToLower();
        return GetIcon(
            key,
            new[]
            {
                PATH_TERRAIN_ICONS + t.Id,
                PATH_TERRAIN_ICONS + nameNoSpaces,
                PATH_EDITOR_ICONS + t.Id,
                PATH_EDITOR_ICONS + nameNoSpaces,
            }
        );
    }

    private Texture2D GetSecondToolIcon(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        string key = "feature_" + id;
        string nameNoSpaces = id.Replace(" ", "").ToLower();
        var paths = new List<string>
        {
            PATH_FEATURE_ICONS + id,
            PATH_FEATURE_ICONS + nameNoSpaces,
            PATH_EDITOR_ICONS + id,
            PATH_EDITOR_ICONS + nameNoSpaces,
        };
        if (string.Equals(id, "cursor", StringComparison.OrdinalIgnoreCase))
        {
            paths.Insert(0, PATH_FEATURE_ICONS + "Cursor");
            paths.Add(PATH_EDITOR_ICONS + "Cursor");
        }
        return GetIcon(key, paths.ToArray());
    }

    private void DrawGridArea(Rect area)
    {
        float cellSize = _baseCellSize * _zoom;
        int width = _grid.GridWidth;
        int height = _grid.GridHeight;

        _scroll = GUI.BeginScrollView(
            area,
            _scroll,
            new Rect(0, 0, width * cellSize, height * cellSize)
        );

        var contentRect = new Rect(0, 0, width * cellSize, height * cellSize);
        EditorGUI.DrawRect(contentRect, Color.grey * 0.2f);

        if (_spacePan || _isPanning)
            EditorGUIUtility.AddCursorRect(contentRect, MouseCursor.Pan);

        Event e = Event.current;
        Vector2 localMouse = e.mousePosition + _scroll;

        UpdateHoveredCell(localMouse, cellSize, width, height);
        HandleMouse(e, localMouse, cellSize, width, height);

        DrawCells(cellSize, width, height);

        GUI.EndScrollView();
        if (GUI.changed)
            Repaint();
    }

    private void UpdateHoveredCell(Vector2 localMouse, float cellSize, int width, int height)
    {
        if (
            localMouse.x >= 0
            && localMouse.y >= 0
            && localMouse.x <= width * cellSize
            && localMouse.y <= height * cellSize
        )
        {
            Vector2Int cell = MouseToCell(localMouse, cellSize);
            _hoveredCell = ClampCell(cell, width, height);
        }
        else
            _hoveredCell = new(-1, -1);
    }

    private void DrawCells(float cellSize, int width, int height)
    {
        for (int r = 0; r < width; r++)
        {
            for (int c = 0; c < height; c++)
            {
                Rect cellRect = new(r * cellSize, c * cellSize, cellSize, cellSize);
                var point = _grid.GetGridPoint(r, c);

                Color fill = GetCellColor(point);
                EditorGUI.DrawRect(cellRect, fill);

                DrawCellOverlays(cellRect, r, c, point, fill, cellSize);
                DrawCellBorders(cellRect);
            }
        }
    }

    private Color GetCellColor(MapGridPoint point)
    {
        if (point == null)
            return Color.white;

        TerrainType tt =
            _terrainAsset?.GetTypeById(point.TerrainTypeId) ?? point.SelectedTerrainType;
        return tt?.EditorColor ?? Color.white;
    }

    private void DrawCellOverlays(
        Rect cellRect,
        int r,
        int c,
        MapGridPoint point,
        Color fill,
        float cellSize
    )
    {
        // Paint selection overlay
        if (_mode == Mode.Paint && _isDragging)
        {
            int minR = Mathf.Min(_dragStart.x, _dragEnd.x),
                maxR = Mathf.Max(_dragStart.x, _dragEnd.x);
            int minC = Mathf.Min(_dragStart.y, _dragEnd.y),
                maxC = Mathf.Max(_dragStart.y, _dragEnd.y);
            if (r >= minR && r <= maxR && c >= minC && c <= maxC)
                EditorGUI.DrawRect(cellRect, new Color(0, 0, 0, 0.25f));
        }

        // Test movement overlay
        if (
            _mode == Mode.TestMovement
            && _testMovementResults?.TryGetValue(point, out var cost) == true
        )
        {
            EditorGUI.DrawRect(cellRect, new Color(.8f, 0f, 0.8f, .65f));
        }

        // Feature overlay: icon (preferred) or letter
        if (point != null)
        {
            string featureId = point.FeatureTypeId;
            string letter = MapGridPointFeature.GetFeatureLetter(featureId);

            Texture2D icon = null;
            if (_editorSettings != null && _editorSettings.featureDisplay == FeatureDisplay.Icon)
                icon = GetSecondToolIcon(featureId);

            if (icon != null)
            {
                float pad = Mathf.Max(1f, cellSize * 0.08f);
                Rect iconRect = new Rect(
                    cellRect.x + pad,
                    cellRect.y + pad,
                    cellRect.width - pad * 2f,
                    cellRect.height - pad * 2f
                );
                // Tint icon to white or black depending on underlying tile luminance,
                // unless the feature is selected (magenta).
                float luminance = 0.299f * fill.r + 0.587f * fill.g + 0.114f * fill.b;
                Color tint =
                    point == _selectedFeaturePoint
                        ? Color.magenta
                        : (luminance < 0.5f ? Color.white : Color.black);
                Color prev = GUI.color;
                GUI.color = tint;
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                GUI.color = prev;
            }
            else if (!string.IsNullOrEmpty(letter))
            {
                float luminance = 0.299f * fill.r + 0.587f * fill.g + 0.114f * fill.b;
                var txtStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(10, Mathf.FloorToInt(cellSize * 0.8f)),
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor =
                            point == _selectedFeaturePoint
                                ? Color.magenta
                                : (luminance < 0.5f ? Color.white : Color.black),
                    },
                };
                EditorGUI.LabelField(cellRect, letter, txtStyle);
            }

            // Selected feature border (unchanged color/behavior)
            if (point == _selectedFeaturePoint)
            {
                float t = Mathf.Max(2f, Mathf.Round(cellSize * 0.08f));
                Color borderCol = Color.magenta;
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, t), borderCol);
                EditorGUI.DrawRect(
                    new Rect(cellRect.x, cellRect.y + cellRect.height - t, cellRect.width, t),
                    borderCol
                );
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, t, cellRect.height), borderCol);
                EditorGUI.DrawRect(
                    new Rect(cellRect.x + cellRect.width - t, cellRect.y, t, cellRect.height),
                    borderCol
                );
            }
        }
    }

    private void DrawCellBorders(Rect cellRect)
    {
        float t = (_editorSettings != null) ? _editorSettings.gridThickness : 1f;
        Color col = (_editorSettings != null) ? _editorSettings.gridColor : Color.black;
        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, t), col);
        EditorGUI.DrawRect(
            new Rect(cellRect.x, cellRect.y + cellRect.height - t, cellRect.width, t),
            col
        );
        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, t, cellRect.height), col);
        EditorGUI.DrawRect(
            new Rect(cellRect.x + cellRect.width - t, cellRect.y, t, cellRect.height),
            col
        );
    }

    private void HandleMouse(Event e, Vector2 localMouse, float cellSize, int width, int height)
    {
        float contentW = width * cellSize,
            contentH = height * cellSize;
        bool inside =
            localMouse.x >= 0
            && localMouse.y >= 0
            && localMouse.x <= contentW
            && localMouse.y <= contentH;

        if (_spacePan)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && inside)
            {
                GUI.FocusControl(null);
                _isPanning = true;
                e.Use();
                return;
            }
            if (e.type == EventType.MouseDrag && _isPanning)
            {
                _scroll -= e.delta;
                _scroll = Vector2.Max(_scroll, Vector2.zero);
                Repaint();
                e.Use();
                return;
            }
            if (e.type == EventType.MouseUp && e.button == 0 && _isPanning)
            {
                _isPanning = false;
                e.Use();
                return;
            }
        }

        if (_mode == Mode.Paint)
        {
            if (e.type == EventType.MouseDown && e.button == 1 && inside)
            {
                HandleRightClick(localMouse, cellSize, width, height);
                e.Use();
                return;
            }
            if (e.type == EventType.MouseDown && e.button == 0 && inside)
            {
                HandlePaintStart(localMouse, cellSize, width, height);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isDragging)
            {
                _dragEnd = ClampCell(MouseToCell(localMouse, cellSize), width, height);
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
                var clicked = _grid.GetGridPoint(
                    ClampCell(MouseToCell(localMouse, cellSize), width, height).x,
                    ClampCell(MouseToCell(localMouse, cellSize), width, height).y
                );
                if (clicked != null)
                {
                    _testMovementStart = clicked;
                    _testMovementResults = new AStarModified().GetReachable(
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

    private void HandleRightClick(Vector2 localMouse, float cellSize, int width, int height)
    {
        Vector2Int cellClamped = ClampCell(MouseToCell(localMouse, cellSize), width, height);
        var clickedPoint = _grid.GetGridPoint(cellClamped.x, cellClamped.y);
        if (clickedPoint != null && !string.IsNullOrEmpty(clickedPoint.FeatureTypeId))
        {
            int idx = Array.IndexOf(_secondToolIds, clickedPoint.FeatureTypeId);
            _selectedSecondTool = idx >= 0 ? idx : Array.IndexOf(_secondToolIds, "cursor");
            _selectedSecondToolName = string.Empty;
            _selectedFeaturePoint = clickedPoint;
            _selectedTerrainIndex = -1;
            Repaint();
        }
    }

    private void HandlePaintStart(Vector2 localMouse, float cellSize, int width, int height)
    {
        GUI.FocusControl(null);
        _dragStart = ClampCell(MouseToCell(localMouse, cellSize), width, height);
        _dragEnd = _dragStart;
        _isDragging = true;
    }

    private Vector2Int MouseToCell(Vector2 localMouse, float cellSize) =>
        new(Mathf.FloorToInt(localMouse.x / cellSize), Mathf.FloorToInt(localMouse.y / cellSize));

    private Vector2Int ClampCell(Vector2Int cell, int width, int height) =>
        new(
            Mathf.Clamp(cell.x, 0, Mathf.Max(0, width - 1)),
            Mathf.Clamp(cell.y, 0, Mathf.Max(0, height - 1))
        );

    private void ApplyTerrainToSelection()
    {
        if (_grid == null || _terrainAsset?.Types == null)
            return;

        int minR = Mathf.Min(_dragStart.x, _dragEnd.x),
            maxR = Mathf.Max(_dragStart.x, _dragEnd.x);
        int minC = Mathf.Min(_dragStart.y, _dragEnd.y),
            maxC = Mathf.Max(_dragStart.y, _dragEnd.y);

        TerrainType chosen = null;
        bool haveTerrainChoice = false;
        if (!(_selectedSecondTool >= 0 && _selectedSecondTool < _secondToolIds.Length))
        {
            if (_selectedTerrainIndex >= 0 && _selectedTerrainIndex < _terrainAsset.Types.Length)
            {
                chosen = _terrainAsset.Types[_selectedTerrainIndex];
                haveTerrainChoice = (chosen != null);
            }
        }

        for (int r = minR; r <= maxR; r++)
        {
            for (int c = minC; c <= maxC; c++)
            {
                var p = _grid.GetGridPoint(r, c);
                if (p != null)
                {
                    Undo.RecordObject(p, "MapGrid Edit");
                    if (_selectedSecondTool >= 0 && _selectedSecondTool < _secondToolIds.Length)
                    {
                        string selId = _secondToolIds[_selectedSecondTool];
                        bool singleCell = (minR == maxR && minC == maxC);
                        ApplySecondToolToPoint(p, selId, singleCell);
                    }
                    else if (haveTerrainChoice && chosen != null)
                    {
                        p.SetTerrainTypeId(chosen.Id);
                    }
                    EditorUtility.SetDirty(p);
                }
            }
        }

        EditorUtility.SetDirty(_grid);
        _grid.SaveFeatureLayer();
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
        SceneView.RepaintAll();
    }

    private void ApplySecondToolToPoint(MapGridPoint p, string selId, bool singleCell)
    {
        if (p == null || string.IsNullOrEmpty(selId))
            return;

        if (string.Equals(selId, "cursor", StringComparison.OrdinalIgnoreCase))
        {
            if (!singleCell)
                return;
            if (!string.IsNullOrEmpty(p.FeatureTypeId))
            {
                int idx = Array.IndexOf(_secondToolIds, p.FeatureTypeId);
                if (idx >= 0)
                    _selectedSecondTool = idx;
                _selectedSecondToolName = string.Empty;
                _selectedFeaturePoint = p;
                _selectedTerrainIndex = -1;
            }
            else
                _selectedFeaturePoint = null;
            Repaint();
            return;
        }

        if (!singleCell)
        {
            p.ApplyFeature(selId, _selectedSecondToolName ?? string.Empty, false);
            EditorUtility.SetDirty(p);
            return;
        }

        if (p.FeatureTypeId != selId)
        {
            p.ApplyFeature(selId, _selectedSecondToolName ?? string.Empty, false);
            _selectedFeaturePoint = p;
        }
        else
        {
            _selectedFeaturePoint = (_selectedFeaturePoint == p) ? null : p;
            Repaint();
        }

        EditorUtility.SetDirty(p);
    }

    private void DrawFeatureDetails(string toolId)
    {
        GUIStyle safeBold =
            _guiStyleBoldWrap ?? new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
        GUIStyle safeWrap = _guiStyleWrap ?? new GUIStyle(EditorStyles.label) { wordWrap = true };
        safeWrap.normal.textColor = EditorStyles.label.normal.textColor;
        safeBold.normal.textColor = EditorStyles.boldLabel.normal.textColor;

        CapturePropertySnapshot(toolId);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginVertical(GUILayout.Width(240));

        if (toolId == "eraser")
        {
            DrawWrappedLabel(new GUIContent("Feature: Eraser"), safeBold, 240f);
            DrawWrappedLabel(
                new GUIContent("Eraser clears the feature layer on painted tiles."),
                safeWrap,
                240f
            );
            _selectedSecondToolName = string.Empty;
        }
        else if (toolId == "cursor")
        {
            DrawWrappedLabel(new GUIContent("Feature: Cursor"), safeBold, 240f);
            DrawWrappedLabel(
                new GUIContent(
                    "Cursor selects existing features without changing or creating them."
                ),
                safeWrap,
                240f
            );
            _selectedSecondToolName = string.Empty;
        }
        else
        {
            DrawFeatureEditor(toolId, safeBold, safeWrap);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();
    }

    private void CapturePropertySnapshot(string toolId)
    {
        if (Event.current?.type != EventType.Layout)
            return;

        _detailSnapshotProps = null;
        _detailSnapshotPropTypes = null;

        MapGridPoint hp =
            (_selectedFeaturePoint?.FeatureTypeId == toolId) ? _selectedFeaturePoint : null;
        if (hp == null)
            return;

        _detailSnapshotProps = new List<KeyValuePair<string, string>>();
        _detailSnapshotPropTypes = new List<string>();

        AddPropsToSnapshot(hp.GetAllStringFeatureProperties(), "string");
        AddPropsToSnapshot(hp.GetAllBoolFeatureProperties(), "bool");
        AddPropsToSnapshot(hp.GetAllIntFeatureProperties(), "int");
        AddPropsToSnapshot(hp.GetAllFloatFeatureProperties(), "float");
    }

    private void AddPropsToSnapshot<T>(List<T> props, string type)
    {
        if (props == null)
            return;
        foreach (var p in props)
        {
            if (p == null)
                continue;
            var t = p.GetType();
            var kField = t.GetField("key") ?? t.GetField("Key");
            var vField = t.GetField("value") ?? t.GetField("Value");
            if (kField != null && vField != null)
            {
                _detailSnapshotProps.Add(
                    new KeyValuePair<string, string>(
                        kField.GetValue(p)?.ToString() ?? string.Empty,
                        vField.GetValue(p)?.ToString() ?? string.Empty
                    )
                );
                _detailSnapshotPropTypes.Add(type);
            }
        }
    }

    private void DrawFeatureEditor(string toolId, GUIStyle safeBold, GUIStyle safeWrap)
    {
        string friendly = toolId;
        int idx = Array.IndexOf(_secondToolIds, toolId);
        if (idx >= 0 && idx < _secondToolNames.Length)
            friendly = _secondToolNames[idx];

        GUIStyle titleStyle = new GUIStyle(safeBold);
        if (_selectedFeaturePoint?.FeatureTypeId == toolId)
            titleStyle.normal.textColor = Color.magenta;

        DrawWrappedLabel(new GUIContent($"Feature: {friendly}"), titleStyle, 240f);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
        _selectedSecondToolName = EditorGUILayout.TextField(
            _selectedSecondToolName,
            GUILayout.ExpandWidth(true)
        );
        EditorGUILayout.EndHorizontal();

        if (_selectedFeaturePoint != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply Defaults", GUILayout.Width(120)))
            {
                // Apply defaults from Resources/GameSettings for this feature id
                _selectedFeaturePoint.ApplyDefaultsForFeature(toolId);
                EditorUtility.SetDirty(_selectedFeaturePoint);
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
        }

        if (
            _selectedFeaturePoint != null
            && _detailSnapshotProps != null
            && _detailSnapshotPropTypes != null
        )
        {
            GUILayout.Space(6);
            DrawWrappedLabel(new GUIContent("Properties"), safeBold, 240f);
            DrawPropertyList();
        }
        else
        {
            GUILayout.Space(6);
            DrawWrappedLabel(
                new GUIContent(
                    "Click a tile with this feature to select it and edit its properties."
                ),
                safeWrap,
                240f
            );
        }
    }

    private void DrawPropertyList()
    {
        var removeKeys = new List<string>();
        var editOldKeys = new List<string>();
        var editNewKeys = new List<string>();
        var editNewVals = new List<string>();
        var editNewTypes = new List<string>();

        for (int i = 0; i < _detailSnapshotProps.Count; i++)
        {
            var p = _detailSnapshotProps[i];
            var t = _detailSnapshotPropTypes.Count > i ? _detailSnapshotPropTypes[i] : "string";

            EditorGUILayout.BeginHorizontal();
            string newKey = EditorGUILayout.TextField(p.Key, GUILayout.Width(120));
            string newValStr = DrawPropertyField(p.Value, t);

            // Small remove button (single char) so the value field can expand
            if (GUILayout.Button("-", GUILayout.Width(24)))
                removeKeys.Add(p.Key);
            EditorGUILayout.EndHorizontal();

            if (newKey != p.Key || newValStr != p.Value)
            {
                editOldKeys.Add(p.Key);
                editNewKeys.Add(newKey ?? string.Empty);
                editNewVals.Add(newValStr ?? string.Empty);
                editNewTypes.Add(t);
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add Property", GUILayout.Width(120)))
            ShowAddPropertyMenu();
        EditorGUILayout.EndHorizontal();

        if (removeKeys.Count > 0 || editOldKeys.Count > 0)
            SchedulePropertyUpdate(removeKeys, editOldKeys, editNewKeys, editNewVals, editNewTypes);
    }

    private string DrawPropertyField(string value, string type)
    {
        switch (type)
        {
            case "bool":
                bool.TryParse(value, out bool curB);
                return EditorGUILayout.Toggle(curB, GUILayout.ExpandWidth(true)).ToString();
            case "int":
                int.TryParse(value, out int curI);
                return EditorGUILayout.IntField(curI, GUILayout.ExpandWidth(true)).ToString();
            case "float":
                float.TryParse(value, out float curF);
                return EditorGUILayout.FloatField(curF, GUILayout.ExpandWidth(true)).ToString();
            default:
                return EditorGUILayout.TextField(
                    value ?? string.Empty,
                    GUILayout.ExpandWidth(true)
                );
        }
    }

    private void ShowAddPropertyMenu()
    {
        var menu = new GenericMenu();
        var point = _selectedFeaturePoint;
        var grid = _grid;
        menu.AddItem(
            new GUIContent("String"),
            false,
            () => NewPropertyPrompt.ShowFor(this, point, grid, "string")
        );
        menu.AddItem(
            new GUIContent("Bool"),
            false,
            () => NewPropertyPrompt.ShowFor(this, point, grid, "bool")
        );
        menu.AddItem(
            new GUIContent("Int"),
            false,
            () => NewPropertyPrompt.ShowFor(this, point, grid, "int")
        );
        menu.AddItem(
            new GUIContent("Float"),
            false,
            () => NewPropertyPrompt.ShowFor(this, point, grid, "float")
        );
        menu.ShowAsContext();
    }

    private void SchedulePropertyUpdate(
        List<string> removeKeys,
        List<string> editOldKeys,
        List<string> editNewKeys,
        List<string> editNewVals,
        List<string> editNewTypes
    )
    {
        var capturedPoint = _selectedFeaturePoint;
        var capturedGrid = _grid;
        EditorApplication.delayCall += () =>
        {
            if (capturedPoint == null)
                return;
            Undo.RecordObject(capturedPoint, "Edit Feature Properties");

            foreach (var rk in removeKeys)
                capturedPoint.ClearFeatureProperty(rk);

            for (int i = 0; i < editOldKeys.Count; i++)
            {
                capturedPoint.ClearFeatureProperty(editOldKeys[i]);
                if (!string.IsNullOrEmpty(editNewKeys[i]))
                {
                    SetTypedProperty(
                        capturedPoint,
                        editNewKeys[i],
                        editNewVals[i],
                        editNewTypes.Count > i ? editNewTypes[i] : "string"
                    );
                }
            }

            EditorUtility.SetDirty(capturedPoint);
            capturedGrid?.SaveFeatureLayer();
            MarkDirty();
        };
    }

    private void SetTypedProperty(MapGridPoint point, string key, string val, string type)
    {
        switch (type)
        {
            case "bool":
                point.SetBoolFeatureProperty(key, bool.TryParse(val, out var bv) ? bv : false);
                break;
            case "int":
                point.SetIntFeatureProperty(key, int.TryParse(val, out var iv) ? iv : 0);
                break;
            case "float":
                point.SetFloatFeatureProperty(key, float.TryParse(val, out var fv) ? fv : 0f);
                break;
            default:
                point.SetStringFeatureProperty(key, val ?? string.Empty);
                break;
        }
    }

    private void DrawWrappedLabel(GUIContent content, GUIStyle style, float width)
    {
        if (content == null)
            return;
        GUIStyle s = style ?? new GUIStyle(EditorStyles.label) { wordWrap = true };
        s.normal.textColor = EditorStyles.label.normal.textColor;
        float h = s.CalcHeight(content, width);
        Rect r = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width));
        EditorGUI.LabelField(r, content, s);
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(_grid);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
        SceneView.RepaintAll();
    }

    private class NewPropertyPrompt : EditorWindow
    {
        private MapGridPoint _point;
        private MapGrid _grid;
        private string _propType = "string";
        private string _key = string.Empty;

        public static void ShowFor(
            MapGridEditorWindow owner,
            MapGridPoint point,
            MapGrid grid,
            string propType
        )
        {
            if (point == null)
                return;
            var win = CreateInstance<NewPropertyPrompt>();
            win._point = point;
            win._grid = grid;
            win._propType = propType ?? "string";
            win._key = string.Empty;
            win.titleContent = new GUIContent("New Property");
            win.position = new Rect(100, 100, 360, 110);
            win.ShowModalUtility();
        }

        private void OnGUI()
        {
            GUILayout.Label($"Create new {_propType} property", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Key:", GUILayout.Width(40));
            _key = EditorGUILayout.TextField(_key, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
                return;
            }
            if (GUILayout.Button("Create", GUILayout.Width(100)))
            {
                if (string.IsNullOrEmpty(_key))
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Key",
                        "Please enter a key for the property.",
                        "OK"
                    );
                    return;
                }
                ApplyCreation();
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyCreation()
        {
            if (_point == null)
                return;
            Undo.RecordObject(_point, "Add Property");

            switch ((_propType ?? "").ToLowerInvariant())
            {
                case "bool":
                    _point.SetBoolFeatureProperty(_key, false);
                    break;
                case "int":
                    _point.SetIntFeatureProperty(_key, 0);
                    break;
                case "float":
                    _point.SetFloatFeatureProperty(_key, 0f);
                    break;
                default:
                    _point.SetStringFeatureProperty(_key, string.Empty);
                    break;
            }

            EditorUtility.SetDirty(_point);
            _grid?.SaveFeatureLayer();
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
            SceneView.RepaintAll();
        }
    }
}
#endif
