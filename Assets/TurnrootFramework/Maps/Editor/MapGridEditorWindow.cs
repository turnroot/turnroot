#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine.Events;

public class MapGridEditorWindow : EditorWindow
{
    // Core references
    private MapGrid _grid;
    private TerrainTypes _terrainAsset;

    // State
    private int _selectedTerrainIndex = 0;
    private Vector2 _scroll = Vector2.zero;
    private Vector2 _rightPanelScroll = Vector2.zero;
    private float _zoom = 1f;
    private bool _isDragging = false,
        _spacePan = false,
        _isPanning = false;
    private Vector2Int _dragStart,
        _dragEnd,
        _hoveredCell = new(-1, -1);
    private Mode _mode = Mode.Paint;

    // Dimension tracking
    private int _lastKnownWidth = 0,
        _lastKnownHeight = 0;

    // Feature editing
    private MapGridPoint _selectedFeaturePoint = null;
    private int _selectedSecondTool = -1;
    private string _selectedSecondToolName = string.Empty;

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
    private const int RIGHT_PANEL_WIDTH = 320;
    private const string ICON_PATH = "EditorSettings/MapGridEditorIcons/";

    // Cached resources
    private readonly Dictionary<string, Texture2D> _iconCache = new();
    private MapGridEditorSettings _editorSettings;
    private string _editorSettingsPath = string.Empty;
    private DateTime _editorSettingsLastWriteTimeUtc = DateTime.MinValue;
    private GUIStyle _guiStyleButton,
        _guiStyleWrap,
        _guiStyleBoldWrap;
    private Dictionary<KeyCode, string> _featureHotkeys = null;
    private Dictionary<KeyCode, int> _terrainHotkeys = null;
    private Dictionary<KeyCode, string> _shiftHotkeys = null;
    private Dictionary<string, bool> _sectionFoldouts = null;

    // Tool definitions
    private readonly ToolSet _featureTools = new ToolSet(
        new[]
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
        },
        new[]
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
        }
    );

    private enum Mode
    {
        Paint = 0,
        TestMovement = 1,
    }

    private class ToolSet
    {
        public string[] Ids { get; }
        public string[] Names { get; }

        public ToolSet(string[] ids, string[] names)
        {
            Ids = ids;
            Names = names;
        }
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

        InitializeHotkeys();
    }

    private void InitializeHotkeys()
    {
        _featureHotkeys = new Dictionary<KeyCode, string>
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

        _shiftHotkeys = new Dictionary<KeyCode, string>
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

        _terrainHotkeys = new Dictionary<KeyCode, int>
        {
            { KeyCode.Tilde, 0 },
            { KeyCode.BackQuote, 0 },
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
            { KeyCode.A, 14 },
        };

        // Section foldouts
        _sectionFoldouts = new Dictionary<string, bool>();
    }

    private void OnInspectorUpdate() => ReloadEditorSettingsIfChanged();

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
        DateTime writeTime =
            !string.IsNullOrEmpty(path) && File.Exists(path)
                ? File.GetLastWriteTimeUtc(path)
                : DateTime.MinValue;

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

        _guiStyleWrap = new GUIStyle(EditorStyles.label) { wordWrap = true, stretchWidth = true };
        _guiStyleBoldWrap = new GUIStyle(EditorStyles.boldLabel)
        {
            wordWrap = true,
            stretchWidth = true,
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

        HandleModeSwitch();
        DrawMainLayout();
        DrawStatusBar();
    }

    private void HandleModeSwitch()
    {
        var prevMode = _mode;
        _mode = (Mode)
            GUILayout.Toolbar((int)_mode, new[] { "Paint Terrain Types", "Test Movement" });

        if (prevMode != _mode)
        {
            _selectedSecondTool = -1;
            _selectedSecondToolName = string.Empty;
            SetSelectedFeaturePoint(null);
            _selectedTerrainIndex = -1;
            Repaint();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _grid = (MapGrid)EditorGUILayout.ObjectField(_grid, typeof(MapGrid), true);

        if (_grid != null)
        {
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

        if (GUILayout.Button("Help", GUILayout.Width(50)))
        {
            HelpWindow.ShowHelp();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;
        if (e?.type != EventType.KeyDown || EditorGUIUtility.editingTextField)
        {
            if (e?.type == EventType.KeyUp && e.keyCode == KeyCode.Space)
            {
                _spacePan = false;
                e.Use();
            }
            return;
        }

        if (e.keyCode == KeyCode.P)
        {
            _mode = Mode.Paint;
            _selectedSecondTool = -1;
            _selectedSecondToolName = string.Empty;
            e.Use();
            return;
        }

        if (
            _mode == Mode.Paint
            && (HandleShiftHotkey(e) || HandleFeatureHotkey(e) || HandleTerrainHotkey(e))
        )
            return;

        HandleZoomHotkeys(e);

        if (e.keyCode == KeyCode.Space)
        {
            _spacePan = true;
            e.Use();
        }
    }

    private bool HandleShiftHotkey(Event e)
    {
        if (!e.shift)
            return false;

        if (
            _shiftHotkeys?.TryGetValue(e.keyCode, out var featId) == true
            || (e.character == '`' || e.character == '~')
                && _shiftHotkeys?.TryGetValue(KeyCode.Tilde, out featId) == true
        )
        {
            _selectedSecondTool = Array.IndexOf(_featureTools.Ids, featId);
            _mode = Mode.Paint;
            e.Use();
            return true;
        }
        return false;
    }

    private bool HandleFeatureHotkey(Event e)
    {
        if (_featureHotkeys?.TryGetValue(e.keyCode, out var featId) == true)
        {
            _selectedSecondTool = Array.IndexOf(_featureTools.Ids, featId);
            _mode = Mode.Paint;
            e.Use();
            return true;
        }
        return false;
    }

    private bool HandleTerrainHotkey(Event e)
    {
        if (_terrainHotkeys?.TryGetValue(e.keyCode, out var terrainIdx) == true)
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
                return true;
            }
        }
        return false;
    }

    private void HandleZoomHotkeys(Event e)
    {
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
            DrawToolsPalette(_featureTools);
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

        float leftAreaW = Mathf.Max(200f, position.width - 120f - RIGHT_PANEL_WIDTH);
        Rect area = GUILayoutUtility.GetRect(leftAreaW, position.height - 120 - 24);
        DrawGridArea(area);
        EditorGUILayout.EndVertical();

        // Right panel
        if (_mode != Mode.TestMovement)
            DrawRightPanel();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(RIGHT_PANEL_WIDTH));

        _rightPanelScroll = EditorGUILayout.BeginScrollView(_rightPanelScroll);

        if (_selectedFeaturePoint != null)
        {
            DrawGridPointProperties();

            GUILayout.Space(10);

            if (!string.IsNullOrEmpty(_selectedFeaturePoint.FeatureTypeId))
            {
                DrawFeatureProperties();
            }
            else
            {
                EditorGUILayout.LabelField("Feature Properties", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "No feature on this tile. Select a feature tool and click to add one.",
                    MessageType.Info
                );
            }
        }
        else
        {
            EditorGUILayout.LabelField("Grid Point Properties", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select a grid point to edit properties. Use the Cursor tool (shortcut: C) to select tiles.",
                MessageType.Info
            );
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawGridPointProperties()
    {
        DrawAccentHeader("Grid Point Properties");
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var point = _selectedFeaturePoint;
        var serializedPoint = new SerializedObject(point);
        serializedPoint.Update();
        serializedPoint.Update();

        var startingUnitProp = serializedPoint.FindProperty("_startingUnit");
        if (startingUnitProp != null)
        {
            var templateProp = startingUnitProp.FindPropertyRelative("_characterTemplate");
            if (templateProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(templateProp, new GUIContent("Starting Unit"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedPoint.ApplyModifiedProperties();
                    MarkDirty();
                }
            }
            else
            {
                var current = point.GetStartingUnit();
                var currentTemplate = current?.CharacterTemplate;
                EditorGUI.BeginChangeCheck();
                var chosen = (Turnroot.Characters.CharacterData)
                    EditorGUILayout.ObjectField(
                        "Starting Unit",
                        currentTemplate,
                        typeof(Turnroot.Characters.CharacterData),
                        false
                    );
                if (EditorGUI.EndChangeCheck())
                {
                    if (chosen == null)
                        point.SetStartingUnit(null);
                    else
                        point.SetStartingUnit(new Turnroot.Characters.CharacterInstance(chosen));
                    SafeSetDirty(point);
                    MarkDirty();
                }
            }
        }

        GUILayout.Space(5);

        var friendlyProp = serializedPoint.FindProperty("_friendlyEntersEvent");
        if (friendlyProp != null)
        {
            EditorGUILayout.LabelField("Friendly Enters Event", _guiStyleBoldWrap);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(friendlyProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                serializedPoint.ApplyModifiedProperties();
                MarkDirty();
            }
        }

        var enemyProp = serializedPoint.FindProperty("_enemyEntersEvent");
        if (enemyProp != null)
        {
            EditorGUILayout.LabelField("Enemy Enters Event", _guiStyleBoldWrap);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(enemyProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                serializedPoint.ApplyModifiedProperties();
                MarkDirty();
            }
        }

        GUILayout.Space(5);

        GUILayout.Space(5);

        // Point-level property sections (serialized) - show any custom properties added to the tile
        DrawSerializedPropertySection(
            "String Properties",
            point,
            "_pointStringProperties",
            () => ShowAddPropertyMenu(point, "string", false)
        );

        DrawSerializedPropertySection(
            "Bool Properties",
            point,
            "_pointBoolProperties",
            () => ShowAddPropertyMenu(point, "bool", false)
        );

        DrawSerializedPropertySection(
            "Int Properties",
            point,
            "_pointIntProperties",
            () => ShowAddPropertyMenu(point, "int", false)
        );

        DrawSerializedPropertySection(
            "Float Properties",
            point,
            "_pointFloatProperties",
            () => ShowAddPropertyMenu(point, "float", false)
        );

        DrawSerializedPropertySection(
            "Unit Properties",
            point,
            "_pointUnitProperties",
            () => ShowAddPropertyMenu(point, "unit", false)
        );

        DrawSerializedPropertySection(
            "ObjectItem Properties",
            point,
            "_pointObjectItemProperties",
            () => ShowAddPropertyMenu(point, "objectitem", false)
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawFeatureProperties()
    {
        var point = _selectedFeaturePoint;
        string toolId = point.FeatureTypeId;
        DrawAccentHeader("Feature Properties");
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        string friendlyName = GetFriendlyName(toolId);
        EditorGUILayout.LabelField($"Type: {friendlyName} ({toolId})", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        string newName = EditorGUILayout.TextField("Feature Name", point.FeatureName);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(point, "Change Feature Name");
            point.FeatureName = newName;
            MarkDirty();
        }

        if (GUILayout.Button("Apply Defaults", GUILayout.Width(120)))
        {
            Undo.RecordObject(point, "Apply Defaults");
            point.ApplyDefaultsForFeature(toolId);
            MarkDirty();
        }

        GUILayout.Space(10);

        // String Properties
        DrawEditablePropertySection(
            "String Properties",
            point.GetAllStringFeatureProperties(),
            (key) => point.GetStringFeatureProperty(key),
            (key, val) =>
            {
                Undo.RecordObject(point, "Edit String Property");
                point.SetStringFeatureProperty(key, val);
                MarkDirty();
            },
            (key) =>
            {
                Undo.RecordObject(point, "Remove String Property");
                point.ClearFeatureProperty(key);
                MarkDirty();
            },
            () => ShowAddPropertyMenu(point, "string")
        );

        // Bool Properties
        DrawEditablePropertySection(
            "Bool Properties",
            point.GetAllBoolFeatureProperties(),
            (key) => point.GetBoolFeatureProperty(key) ?? false,
            (key, val) =>
            {
                Undo.RecordObject(point, "Edit Bool Property");
                point.SetBoolFeatureProperty(key, val);
                MarkDirty();
            },
            (key) =>
            {
                Undo.RecordObject(point, "Remove Bool Property");
                point.ClearFeatureProperty(key);
                MarkDirty();
            },
            () => ShowAddPropertyMenu(point, "bool")
        );

        // Int Properties
        DrawEditablePropertySection(
            "Int Properties",
            point.GetAllIntFeatureProperties(),
            (key) => point.GetIntFeatureProperty(key) ?? 0,
            (key, val) =>
            {
                Undo.RecordObject(point, "Edit Int Property");
                point.SetIntFeatureProperty(key, val);
                MarkDirty();
            },
            (key) =>
            {
                Undo.RecordObject(point, "Remove Int Property");
                point.ClearFeatureProperty(key);
                MarkDirty();
            },
            () => ShowAddPropertyMenu(point, "int")
        );

        // Float Properties
        DrawEditablePropertySection(
            "Float Properties",
            point.GetAllFloatFeatureProperties(),
            (key) => point.GetFloatFeatureProperty(key) ?? 0f,
            (key, val) =>
            {
                Undo.RecordObject(point, "Edit Float Property");
                point.SetFloatFeatureProperty(key, val);
                MarkDirty();
            },
            (key) =>
            {
                Undo.RecordObject(point, "Remove Float Property");
                point.ClearFeatureProperty(key);
                MarkDirty();
            },
            () => ShowAddPropertyMenu(point, "float")
        );

        // Unit Properties - using SerializedProperty
        DrawSerializedPropertySection(
            "Unit Properties",
            point,
            "_featureUnitProperties",
            () => ShowAddPropertyMenu(point, "unit")
        );

        // ObjectItem Properties - using SerializedProperty
        DrawSerializedPropertySection(
            "ObjectItem Properties",
            point,
            "_featureObjectItemProperties",
            () => ShowAddPropertyMenu(point, "objectitem")
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawSerializedPropertySection(
        string sectionTitle,
        MapGridPoint point,
        string propertyName,
        Action onAddNew
    )
    {
        // Draw header with accent color and foldout
        bool expanded = GetSectionFoldout(sectionTitle, true);
        DrawSectionHeader(sectionTitle, expanded, onAddNew);
        if (!expanded)
            return;
        var serializedPoint = new SerializedObject(point);
        var propertiesArray = serializedPoint.FindProperty(propertyName);

        bool hasVisible = false;
        if (propertiesArray != null && propertiesArray.arraySize > 0)
        {
            for (int i = 0; i < propertiesArray.arraySize; i++)
            {
                var el = propertiesArray.GetArrayElementAtIndex(i);
                var keyProp = el.FindPropertyRelative("key");
                if (keyProp == null)
                    continue;

                hasVisible = true;
                break;
            }
        }

        if (propertiesArray == null || !hasVisible)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                onAddNew();
            }
            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+", GUILayout.Width(20)))
            onAddNew();
        EditorGUILayout.EndHorizontal();

        var toRemove = new List<int>();

        for (int i = 0; i < propertiesArray.arraySize; i++)
        {
            var element = propertiesArray.GetArrayElementAtIndex(i);
            var keyProp = element.FindPropertyRelative("key");
            var valueProp = element.FindPropertyRelative("value");

            if (keyProp == null || valueProp == null)
                continue;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(_editorSettings?.propertyIndent ?? 12);
            EditorGUILayout.LabelField(keyProp.stringValue, _guiStyleWrap, GUILayout.Width(120));

            // Special handling for Unit and ObjectItem properties. Both are complex serializable
            // types (CharacterInstance, ObjectItemInstance) so we present an asset selector for the
            // underlying template (CharacterData/ObjectItem). We then wrap the selection in the
            // instance type and assign via the typed API so it behaves correctly in the serialized lists.
            if (propertyName.IndexOf("Unit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var current = point.GetUnitFeatureProperty(keyProp.stringValue);
                var currentTemplate = current?.CharacterTemplate;
                EditorGUI.BeginChangeCheck();
                var chosen = (Turnroot.Characters.CharacterData)
                    EditorGUILayout.ObjectField(
                        currentTemplate,
                        typeof(Turnroot.Characters.CharacterData),
                        false
                    );
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(point, "Edit Unit Property");
                    if (chosen == null)
                        point.SetUnitFeatureProperty(keyProp.stringValue, null);
                    else
                        point.SetUnitFeatureProperty(
                            keyProp.stringValue,
                            new Turnroot.Characters.CharacterInstance(chosen)
                        );
                    SafeSetDirty(point);
                    MarkDirty();
                }
            }
            else if (propertyName.IndexOf("ObjectItem", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var current = point.GetObjectItemFeatureProperty(keyProp.stringValue);
                var currentTemplate = current?.Template;
                EditorGUI.BeginChangeCheck();
                var chosen = (Turnroot.Gameplay.Objects.ObjectItem)
                    EditorGUILayout.ObjectField(
                        currentTemplate,
                        typeof(Turnroot.Gameplay.Objects.ObjectItem),
                        false
                    );
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(point, "Edit ObjectItem Property");
                    if (chosen == null)
                        point.SetObjectItemFeatureProperty(keyProp.stringValue, null);
                    else
                        point.SetObjectItemFeatureProperty(
                            keyProp.stringValue,
                            new Turnroot.Gameplay.Objects.ObjectItemInstance(chosen)
                        );
                    SafeSetDirty(point);
                    MarkDirty();
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(valueProp, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedPoint.ApplyModifiedProperties();
                    MarkDirty();
                }
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                toRemove.Add(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        // Remove marked properties
        for (int i = toRemove.Count - 1; i >= 0; i--)
        {
            Undo.RecordObject(point, "Remove Property");
            var element = propertiesArray.GetArrayElementAtIndex(toRemove[i]);
            var keyProp = element.FindPropertyRelative("key");
            if (keyProp != null)
            {
                point.ClearFeatureProperty(keyProp.stringValue);
            }
            MarkDirty();
        }
        serializedPoint.ApplyModifiedProperties();
    }

    private void DrawEditablePropertySection<T, TProp>(
        string sectionTitle,
        List<TProp> properties,
        Func<string, T> getter,
        Action<string, T> setter,
        Action<string> remover,
        Action onAddNew
    )
        where TProp : MapGridPropertyBase.IProperty
    {
        bool expanded = GetSectionFoldout(sectionTitle, true);
        DrawSectionHeader(sectionTitle, expanded, onAddNew);
        if (!expanded)
            return;

        if (properties == null || properties.Count == 0)
        {
            if (GUILayout.Button("+", GUILayout.Width(20)))
                onAddNew();
            return;
        }

        var toRemove = new List<string>();

        foreach (var prop in properties)
        {
            string key = prop.key;
            T currentValue = getter(key);

            EditorGUILayout.BeginHorizontal();
            // Indent property keys (children) so they visually live under the section header
            GUILayout.Space(_editorSettings?.propertyIndent ?? 12);
            EditorGUILayout.LabelField(key, _guiStyleWrap, GUILayout.Width(120));

            EditorGUI.BeginChangeCheck();
            T newValue = DrawPropertyField(currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                setter(key, newValue);
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                toRemove.Add(key);
            }
            EditorGUILayout.EndHorizontal();
        }

        foreach (var key in toRemove)
        {
            remover(key);
        }
    }

    private T DrawPropertyField<T>(T value)
    {
        if (value is string strVal)
            return (T)(object)EditorGUILayout.TextField(strVal);
        if (value is bool boolVal)
            return (T)(object)EditorGUILayout.Toggle(boolVal);
        if (value is int intVal)
            return (T)(object)EditorGUILayout.IntField(intVal);
        if (value is float floatVal)
            return (T)(object)EditorGUILayout.FloatField(floatVal);

        EditorGUILayout.LabelField(value?.ToString() ?? "null");
        return value;
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

    private bool GetSectionFoldout(string key, bool defaultValue)
    {
        if (_sectionFoldouts == null)
            _sectionFoldouts = new Dictionary<string, bool>();
        if (!_sectionFoldouts.TryGetValue(key, out var v))
        {
            _sectionFoldouts[key] = defaultValue;
            return defaultValue;
        }
        return v;
    }

    private void DrawSectionHeader(string title, bool expanded, Action onAddNew)
    {
        Color bg = GetHeaderAccentColor(title);
        Rect rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, bg);

        // compute text color from background luminance
        float lum = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
        Color textCol = lum < 0.5f ? Color.white : Color.black;

        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.normal.textColor = textCol;

        bool newExpanded = EditorGUI.Foldout(
            new Rect(rect.x + 8, rect.y + 3, rect.width - 36, rect.height - 6),
            expanded,
            title,
            true,
            headerStyle
        );

        Rect buttonRect = new Rect(rect.x + rect.width - 28, rect.y + 2, 24, rect.height - 4);
        if (GUI.Button(buttonRect, "+"))
            onAddNew?.Invoke();

        _sectionFoldouts[title] = newExpanded;
    }

    private Color GetHeaderAccentColor(string sectionTitle)
    {
        if (_editorSettings == null)
            return new Color(0.0f, 0.35f, 0.8f, 0.18f);

        string title = sectionTitle ?? string.Empty;
        if (title.IndexOf("String", StringComparison.OrdinalIgnoreCase) >= 0)
            return _editorSettings.headerAccentStringColor;
        if (title.IndexOf("Bool", StringComparison.OrdinalIgnoreCase) >= 0)
            return _editorSettings.headerAccentBoolColor;
        if (title.IndexOf("Int", StringComparison.OrdinalIgnoreCase) >= 0)
            return _editorSettings.headerAccentIntColor;
        if (title.IndexOf("Float", StringComparison.OrdinalIgnoreCase) >= 0)
            return _editorSettings.headerAccentFloatColor;
        if (title.IndexOf("Unit", StringComparison.OrdinalIgnoreCase) >= 0)
            return _editorSettings.headerAccentUnitColor;
        if (
            title.IndexOf("ObjectItem", StringComparison.OrdinalIgnoreCase) >= 0
            || title.IndexOf("Object", StringComparison.OrdinalIgnoreCase) >= 0
        )
            return _editorSettings.headerAccentObjectItemColor;
        if (title.IndexOf("Event", StringComparison.OrdinalIgnoreCase) >= 0)
            return _editorSettings.headerAccentEventColor;

        return _editorSettings.headerAccentColor;
    }

    private void DrawAccentHeader(string title)
    {
        Color bg = _editorSettings?.headerAccentColor ?? new Color(0.0f, 0.35f, 0.8f, 0.18f);
        Rect rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, bg);

        float lum = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
        Color textCol = lum < 0.5f ? Color.white : Color.black;
        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.normal.textColor = textCol;

        EditorGUI.LabelField(
            new Rect(rect.x + 8, rect.y + 3, rect.width, rect.height),
            title,
            headerStyle
        );
    }

    private void DrawZoomControls()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Zoom:", GUILayout.Width(40));
        _zoom = EditorGUILayout.Slider(_zoom, 0.25f, 3f, GUILayout.Width(150));

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
            $"Ctrl +/- : Zoom | Space + Drag : Pan | {leftAction} | Left click + drag: Paint Area | [`, 1-0, -, =, Q, A] : Terrain | Shift+[`, 1-0, -, =] : Feature";
        string hoverText =
            _hoveredCell.x >= 0 ? $"Row {_hoveredCell.x}, Col {_hoveredCell.y}" : "(none)";

        GUILayout.Label(controls, GUILayout.ExpandWidth(true), GUILayout.Height(20));
        GUILayout.FlexibleSpace();
        GUILayout.Label(hoverText, GUILayout.ExpandWidth(false), GUILayout.MinWidth(100));
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
                SetSelectedFeaturePoint(null);
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

    private void DrawToolsPalette(ToolSet tools)
    {
        GUILayout.Label("Tools", EditorStyles.boldLabel);

        for (int i = 0; i < tools.Ids.Length; i++)
        {
            string id = tools.Ids[i];
            string label = tools.Names[i];
            Texture2D icon = GetToolIcon(id);
            GUIContent content =
                icon != null ? new GUIContent(icon, label) : new GUIContent(label.Substring(0, 1));

            bool isSelected = _selectedSecondTool == i;
            bool newState = GUILayout.Toggle(isSelected, content, _guiStyleButton);

            if (newState && !isSelected)
            {
                _selectedSecondTool = i;
                _selectedSecondToolName = string.Empty;
                SetSelectedFeaturePoint(null);
                _selectedTerrainIndex = -1;
            }
            else if (!newState && isSelected)
            {
                _selectedSecondTool = -1;
                SetSelectedFeaturePoint(null);
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

    private Texture2D GetIcon(string cacheKey, string[] paths)
    {
        if (_iconCache.TryGetValue(cacheKey, out var cached))
            return cached;

        Texture2D tex = null;
        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;

            tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                break;

            var spr = Resources.Load<Sprite>(path);
            if (spr?.texture != null)
            {
                tex = spr.texture;
                break;
            }
        }

        _iconCache[cacheKey] = tex;
        return tex;
    }

    private Texture2D GetTerrainIcon(TerrainType t)
    {
        if (t == null)
            return null;

        string cacheKey = "terrain_" + (!string.IsNullOrEmpty(t.Id) ? t.Id : t.Name);

        var variants = new List<string>
        {
            t.Id,
            t.Name,
            t.Name?.Replace(" ", ""),
            t.Id?.ToLower(),
            t.Name?.Replace(" ", "").ToLower(),
        };

        return GetIcon(
            cacheKey,
            variants.Where(v => !string.IsNullOrEmpty(v)).Select(v => ICON_PATH + v).ToArray()
        );
    }

    private Texture2D GetToolIcon(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        string cacheKey = "feature_" + id;

        var variants = new List<string>();

        var toolSet = _featureTools;
        int idx = Array.IndexOf(toolSet.Ids, id);
        string friendlyName = (idx >= 0 && idx < toolSet.Names.Length) ? toolSet.Names[idx] : null;

        if (!string.IsNullOrEmpty(friendlyName))
        {
            variants.Add(friendlyName);
            variants.Add(friendlyName.Replace(" ", ""));
        }

        variants.Add(id);
        variants.Add(id.Replace(" ", "").ToLower());
        variants.Add(id.ToLower());

        if (id.Length > 0)
            variants.Add(char.ToUpper(id[0]) + id.Substring(1));

        return GetIcon(
            cacheKey,
            variants.Where(v => !string.IsNullOrEmpty(v)).Select(v => ICON_PATH + v).ToArray()
        );
    }

    private void DrawGridArea(Rect area)
    {
        float cellSize = _baseCellSize * _zoom;
        int width = _grid.GridWidth,
            height = _grid.GridHeight;

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
            _hoveredCell = ClampCell(MouseToCell(localMouse, cellSize), width, height);
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

                DrawCellOverlays(cellRect, point, fill, cellSize);
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

    private void DrawCellOverlays(Rect cellRect, MapGridPoint point, Color fill, float cellSize)
    {
        if (point == null)
            return;

        // Paint selection overlay
        if ((_mode == Mode.Paint) && _isDragging)
        {
            int minR = Mathf.Min(_dragStart.x, _dragEnd.x),
                maxR = Mathf.Max(_dragStart.x, _dragEnd.x);
            int minC = Mathf.Min(_dragStart.y, _dragEnd.y),
                maxC = Mathf.Max(_dragStart.y, _dragEnd.y);
            if (point.Row >= minR && point.Row <= maxR && point.Col >= minC && point.Col <= maxC)
                EditorGUI.DrawRect(cellRect, new Color(0, 0, 0, 0.25f));
        }

        // Test movement overlay
        if (_mode == Mode.TestMovement && _testMovementResults?.ContainsKey(point) == true)
            EditorGUI.DrawRect(cellRect, new Color(.8f, 0f, 0.8f, .65f));

        // Feature overlay
        string featureId = point.FeatureTypeId;
        bool hasFeature = !string.IsNullOrEmpty(featureId);

        if (hasFeature)
        {
            float luminance = 0.299f * fill.r + 0.587f * fill.g + 0.114f * fill.b;
            bool isSelectedb = point == _selectedFeaturePoint;
            Color tint = luminance < 0.5f ? Color.white : Color.black;

            string id = featureId;
            Texture2D icon =
                _editorSettings?.featureDisplay == FeatureDisplay.Icon ? GetToolIcon(id) : null;

            if (icon != null)
            {
                DrawCellIcon(cellRect, icon, tint, cellSize);
            }
            else
            {
                string letter = MapGridPointFeature.GetFeatureLetter(id);
                if (!string.IsNullOrEmpty(letter))
                {
                    DrawCellText(cellRect, letter, tint, cellSize);
                }
                else
                {
                    // Fallback: show first letter of id
                    string fallback = id.Length > 0 ? id.Substring(0, 1).ToUpper() : "?";
                    DrawCellText(cellRect, fallback, tint, cellSize);
                }
            }
        }

        // Selection border
        bool isSelected = point == _selectedFeaturePoint;
        if (isSelected)
        {
            // If has feature, use magenta. If no feature but has modified properties, use lighter color
            Color borderCol;
            if (hasFeature)
                borderCol =
                    _editorSettings?.selectedFeatureBorderColor
                    ?? _editorSettings?.selectedTileBorderColor
                    ?? Color.black;
            else
                borderCol =
                    _editorSettings != null
                        ? _editorSettings.selectedTileBorderColor
                        : new Color(0.1f, 0.7f, 0.95f, 1f);
            DrawSelectionBorder(cellRect, cellSize, borderCol);
        }
        // Modified properties indicator (no feature, not selected, but has custom properties)
        else if (!hasFeature && HasModifiedProperties(point))
        {
            var lightCol =
                _editorSettings?.modifiedPropertyBorderColor ?? new Color(1f, 0.75f, 1f, 0.6f);
            DrawSelectionBorder(cellRect, cellSize, lightCol);
        }
    }

    private bool HasModifiedProperties(MapGridPoint point)
    {
        if (point == null)
            return false;

        // Show the modified overlay only when a map tile's built-in default
        // point properties are changed. These are the starting unit and the
        // two tile-enter events. This avoids marking tiles modified when
        // only custom feature properties were added.
        var startingUnit = point.GetStartingUnit();
        // A CharacterInstance object may exist as a wrapper but doesn't mean a
        // template was assigned. Only treat the tile as modified if the
        // starting unit has a non-null template asset.
        if (startingUnit != null && startingUnit.CharacterTemplate != null)
            return true;

        var friendlyEvent = point.GetFriendlyEntersEvent();
        if (friendlyEvent != null && friendlyEvent.GetPersistentEventCount() > 0)
            return true;

        var enemyEvent = point.GetEnemyEntersEvent();
        if (enemyEvent != null && enemyEvent.GetPersistentEventCount() > 0)
            return true;

        return false;
    }

    private void DrawCellText(Rect cellRect, string text, Color tint, float cellSize)
    {
        var txtStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(10, Mathf.FloorToInt(cellSize * 0.8f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = tint },
        };
        EditorGUI.LabelField(cellRect, text, txtStyle);
    }

    private void DrawCellIcon(Rect cellRect, Texture2D icon, Color tint, float cellSize)
    {
        float pad = Mathf.Max(1f, cellSize * 0.08f);
        Rect iconRect = new(
            cellRect.x + pad,
            cellRect.y + pad,
            cellRect.width - pad * 2f,
            cellRect.height - pad * 2f
        );
        Color prev = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = prev;
    }

    private void DrawSelectionBorder(Rect cellRect, float cellSize, Color borderCol)
    {
        float t = Mathf.Max(2f, Mathf.Round(cellSize * 0.08f));
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

    private void DrawCellBorders(Rect cellRect)
    {
        float t = _editorSettings?.gridThickness ?? 1f;
        Color col = _editorSettings?.gridColor ?? Color.black;
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
            HandlePaintMode(e, localMouse, cellSize, width, height, inside);
        else if (_mode == Mode.TestMovement)
            HandleTestMovementMode(e, localMouse, cellSize, width, height, inside);
    }

    private void HandlePaintMode(
        Event e,
        Vector2 localMouse,
        float cellSize,
        int width,
        int height,
        bool inside
    )
    {
        if (e.type == EventType.MouseDown && e.button == 1 && inside)
        {
            HandleRightClick(localMouse, cellSize, width, height);
            e.Use();
        }
        else if (e.type == EventType.MouseDown && e.button == 0 && inside)
        {
            GUI.FocusControl(null);
            _dragStart = ClampCell(MouseToCell(localMouse, cellSize), width, height);
            _dragEnd = _dragStart;
            _isDragging = true;
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

    private void HandleTestMovementMode(
        Event e,
        Vector2 localMouse,
        float cellSize,
        int width,
        int height,
        bool inside
    )
    {
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (!inside)
            {
                e.Use();
                return;
            }

            var cell = ClampCell(MouseToCell(localMouse, cellSize), width, height);
            var clicked = _grid.GetGridPoint(cell.x, cell.y);
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

    private void HandleRightClick(Vector2 localMouse, float cellSize, int width, int height)
    {
        Vector2Int cellClamped = ClampCell(MouseToCell(localMouse, cellSize), width, height);
        var clickedPoint = _grid.GetGridPoint(cellClamped.x, cellClamped.y);

        if (clickedPoint != null)
        {
            if (!string.IsNullOrEmpty(clickedPoint.FeatureTypeId))
            {
                string fid = clickedPoint.FeatureTypeId;
                int idx = Array.IndexOf(_featureTools.Ids, fid);

                if (idx >= 0)
                {
                    _mode = Mode.Paint;
                    _selectedSecondTool = idx;
                }

                _selectedSecondToolName = string.Empty;
                _selectedTerrainIndex = -1;
            }

            SetSelectedFeaturePoint(clickedPoint);
            Repaint();
        }
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

        var currentTools = _featureTools;
        bool isToolSelected =
            _selectedSecondTool >= 0 && _selectedSecondTool < currentTools.Ids.Length;

        TerrainType chosenTerrain = null;
        if (
            !isToolSelected
            && _selectedTerrainIndex >= 0
            && _selectedTerrainIndex < _terrainAsset.Types.Length
        )
            chosenTerrain = _terrainAsset.Types[_selectedTerrainIndex];

        for (int r = minR; r <= maxR; r++)
        {
            for (int c = minC; c <= maxC; c++)
            {
                var p = _grid.GetGridPoint(r, c);
                if (p != null)
                {
                    Undo.RecordObject(p, "MapGrid Edit");

                    if (isToolSelected)
                    {
                        string toolId = currentTools.Ids[_selectedSecondTool];
                        bool singleCell = (minR == maxR && minC == maxC);
                        ApplyToolToPoint(p, toolId, singleCell);
                    }
                    else if (chosenTerrain != null)
                    {
                        p.SetTerrainTypeId(chosenTerrain.Id);
                    }

                    SafeSetDirty(p);
                }
            }
        }

        SafeSetDirty(_grid);
        _grid.SaveFeatureLayer();
        MarkDirty();
        SceneView.RepaintAll();
    }

    private void ApplyToolToPoint(MapGridPoint p, string toolId, bool singleCell)
    {
        if (p == null || string.IsNullOrEmpty(toolId))
            return;

        // Handle cursor tool
        if (string.Equals(toolId, "cursor", StringComparison.OrdinalIgnoreCase))
        {
            if (!singleCell)
                return;

            // Select any grid point, whether it has a feature or not
            SetSelectedFeaturePoint(p);

            // If it has a feature, select that feature tool
            if (!string.IsNullOrEmpty(p.FeatureTypeId))
            {
                int idx = Array.IndexOf(_featureTools.Ids, p.FeatureTypeId);
                _selectedSecondTool = idx >= 0 ? idx : Array.IndexOf(_featureTools.Ids, "cursor");
            }

            _selectedSecondToolName = string.Empty;
            _selectedTerrainIndex = -1;
            Repaint();
            return;
        }

        // Handle eraser tool
        if (string.Equals(toolId, "eraser", StringComparison.OrdinalIgnoreCase))
        {
            p.ClearFeature();
            SafeSetDirty(p);
            return;
        }

        // Multi-cell paint: always apply tool
        if (!singleCell)
        {
            p.ClearFeature();
            p.ApplyFeature(toolId, _selectedSecondToolName ?? string.Empty, false);

            SafeSetDirty(p);
            return;
        }

        // Single cell: toggle or select
        string currentId = p.FeatureTypeId;

        if (currentId != toolId)
        {
            p.ClearFeature();
            p.ApplyFeature(toolId, _selectedSecondToolName ?? string.Empty, false);

            SetSelectedFeaturePoint(p);
        }
        else
        {
            SetSelectedFeaturePoint(_selectedFeaturePoint == p ? null : p);
            Repaint();
        }

        SafeSetDirty(p);
    }

    private string GetFriendlyName(string toolId)
    {
        int idx = Array.IndexOf(_featureTools.Ids, toolId);
        if (idx >= 0 && idx < _featureTools.Names.Length)
            return _featureTools.Names[idx];

        return toolId;
    }

    private void SetSelectedFeaturePoint(MapGridPoint p)
    {
        _selectedFeaturePoint = p;
        Repaint();
    }

    private void ShowAddPropertyMenu(MapGridPoint point, string propType, bool forFeature = true)
    {
        if (point == null)
            return;

        NewPropertyPrompt.ShowFor(this, point, _grid, propType, forFeature);
    }

    private void MarkDirty()
    {
        // Avoid marking the scene dirty during compile/domain reload or while
        // entering/exiting Play Mode. These events can re-run editor UI code and
        // should not automatically flag the scene as modified.
        if (!EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SafeSetDirty(_grid);
            if (
                !EditorApplication.isCompiling
                && !EditorApplication.isPlayingOrWillChangePlaymode
                && !EditorApplication.isUpdating
            )
            {
                EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                );
            }
            SceneView.RepaintAll();
        }
    }

    // Avoid marking scene objects dirty during editor update/import windows.
    // If the object is part of a scene (no asset path) and the editor is
    // currently updating, skip SetDirty to prevent the scene flag and save prompt.
    private void SafeSetDirty(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        try
        {
            if (EditorApplication.isUpdating)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    return; // don't mark scene objects during import/update
            }
        }
        catch { }

        EditorUtility.SetDirty(obj);
    }

    private class NewPropertyPrompt : EditorWindow
    {
        private MapGridPoint _point;
        private MapGrid _grid;
        private string _propType = "string";
        private bool _forFeature = true;
        private string _key = string.Empty;

        public static void ShowFor(
            MapGridEditorWindow owner,
            MapGridPoint point,
            MapGrid grid,
            string propType,
            bool forFeature = true
        )
        {
            if (point == null)
                return;
            var win = CreateInstance<NewPropertyPrompt>();
            win._point = point;
            win._grid = grid;
            win._propType = propType ?? "string";
            win._forFeature = forFeature;
            win._forFeature = forFeature;
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
                    if (_forFeature)
                        _point.SetBoolFeatureProperty(_key, false);
                    else
                        _point.SetBoolPointProperty(_key, false);
                    break;
                case "int":
                    if (_forFeature)
                        _point.SetIntFeatureProperty(_key, 0);
                    else
                        _point.SetIntPointProperty(_key, 0);
                    break;
                case "float":
                    if (_forFeature)
                        _point.SetFloatFeatureProperty(_key, 0f);
                    else
                        _point.SetFloatPointProperty(_key, 0f);
                    break;
                case "unit":
                    if (_forFeature)
                        _point.SetUnitFeatureProperty(_key, null);
                    else
                        _point.SetUnitPointProperty(_key, null);
                    break;
                case "objectitem":
                    if (_forFeature)
                        _point.SetObjectItemFeatureProperty(_key, null);
                    else
                        _point.SetObjectItemPointProperty(_key, null);
                    break;
                default:
                    if (_forFeature)
                        _point.SetStringFeatureProperty(_key, string.Empty);
                    else
                        _point.SetStringPointProperty(_key, string.Empty);
                    break;
            }
            // Avoid marking scene objects during non-interactive editor update/import.
            if (!EditorApplication.isUpdating)
                EditorUtility.SetDirty(_point);
            if (_forFeature)
                _grid?.SaveFeatureLayer();
            if (!EditorApplication.isUpdating)
            {
                EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                );
            }
            SceneView.RepaintAll();
        }
    }

    private class HelpWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        public static void ShowHelp()
        {
            var window = GetWindow<HelpWindow>(true, "Map Grid Editor Help", true);
            window.minSize = new Vector2(500, 400);
            window.maxSize = new Vector2(600, 600);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Map Grid Editor Help", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Keyboard Shortcuts", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Terrain Hotkeys:\n"
                    + "`, 1-0, -, =, Q, A - Select terrain type\n\n"
                    + "Feature Hotkeys:\n"
                    + "T - Treasure, D - Door, W - Warp, H - Healing\n"
                    + "R - Ranged, M - Mechanism, C - Control, B - Breakable\n"
                    + "S - Shelter, V - Village, F - Fortress, U - Underground\n"
                    + "E - Eraser\n\n"
                    + "Shift + Number - Quick feature selection\n"
                    + "P - Switch to Paint mode\n"
                    + "Ctrl/Cmd + Plus/Minus - Zoom in/out\n"
                    + "Space + Drag - Pan the grid",
                MessageType.Info
            );

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Mouse Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Left Click - Paint terrain or place feature\n"
                    + "Left Click + Drag - Paint area\n"
                    + "Right Click - Select existing feature for editing\n"
                    + "Space + Left Drag - Pan the view",
                MessageType.Info
            );

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Workflow Tips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Select a MapGrid object in the toolbar\n"
                    + "2. Choose a terrain type or feature tool from the left palette\n"
                    + "3. Click or drag on the grid to paint\n"
                    + "4. Right-click features to edit their properties in the right panel\n"
                    + "5. Use the Test Movement mode to preview pathfinding",
                MessageType.Info
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Property Editing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Starting Unit: choose which character should start on this tile when a level begins (optional).\n"
                    + "Enter Events: attach actions that happen when a friendly or enemy character steps onto this tile.\n"
                    + "Use the + button to add custom named properties to a tile, and the - button to remove them; these properties can hold numbers, text, or true/false flags that your gameplay logic can read.\n"
                    + "To set a default configuration for an entire feature type (for example, every door), make a 'Feature Defaults' asset in Game Settings and use the Apply Defaults button in the feature panel.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Customization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Appearance: change the editor colors and accents by editing the 'Map Grid Editor Settings' in Resources/EditorSettings.\n"
                    + "You can change the border shown around the currently selected object/tile and the color used to mark tiles with edited defaults.\n"
                    + "Header colors for each property group (string, bool, int, float, unit, object) are also configurable so you can visually organize sections.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Close", GUILayout.Height(30)))
            {
                Close();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
