#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Turnroot.Gameplay.Brain;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.SceneFlows.Editor
{
    /// <summary>
    /// Visual graph editor for SceneFlowGraph assets.
    /// Allows designers to create and visualize scene flow networks.
    /// </summary>
    public class SceneFlowGraphEditorWindow : EditorWindow
    {
        private SceneFlowGraph _graph;
        private Vector2 _panOffset = Vector2.zero;
        private float _zoom = 1f;
        private const float MIN_ZOOM = 0.5f;
        private const float MAX_ZOOM = 2f;

        // Node dragging
        private SceneNode _draggedNode;
        private Vector2 _dragStartPos;
        private bool _isDragging;

        // Selection
        private SceneNode _selectedNode;
        private SceneTransition _selectedTransition;
        private HashSet<SceneNode> _selectedNodes = new HashSet<SceneNode>();

        // UI state
        private Vector2 _sidebarScroll;
        private const int SIDEBAR_WIDTH = 320;
        private const int NODE_WIDTH = 180;
        private const int NODE_HEIGHT = 60;

        // Transition creation
        private SceneNode _transitionStartNode;
        private bool _transitionIsDrag;
        private SceneNode _potentialTransitionDrag;
        private Vector2 _potentialTransitionDragStart;
        private const float DRAG_TRANSITION_THRESHOLD = 8f;
        private bool _clickedEmptySpace;

        // Styles
        private GUIStyle _nodeStyle;
        private GUIStyle _nodeSelectedStyle;
        private GUIStyle _hubNodeStyle;
        private GUIStyle _hubNodeSelectedStyle;
        private GUIStyle _battleNodeStyle;
        private GUIStyle _battleNodeSelectedStyle;
        private GUIStyle _labelStyle;

        // Derived cached styles — allocated once in InitializeStyles, not per-frame
        private GUIStyle _smallLabelStyle;
        private GUIStyle _dateLabelStyle;
        private GUIStyle _badgeLabelStyle;
        private GUIStyle _chapterBadgeLabelStyle;
        private GUIStyle _nodeIdStyle;
        private bool _stylesInitialized;
        private const int STATUS_BAR_HEIGHT = 22;

        // Settings
        private SceneFlowGraphEditorSettings _settings;
        private int _lastSettingsHash;

        // Cached brain state options — built once per session, cleared on domain reload
        private string[] _cachedBrainStateIds;
        private string[] _cachedBrainStateOptions;

        // Cached singleton graph
        private static SceneFlowGraph _cachedSingletonGraph;
        private static bool _hasSearchedForGraph;

        [MenuItem("Window/Turnroot/Editors/Scene Flow Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneFlowGraphEditorWindow>("Scene Flow Editor");
            window.minSize = new Vector2(800, 600);
        }

        public static void OpenGraph(SceneFlowGraph graph)
        {
            var window = GetWindow<SceneFlowGraphEditorWindow>("Scene Flow Editor");
            window._graph = graph;
            window.Repaint();
        }

        private void OnEnable()
        {
            _stylesInitialized = false;
            _cachedBrainStateIds = null;
            _cachedBrainStateOptions = null;
            LoadSettings();
            _lastSettingsHash = GetSettingsHash();
            LoadOrFindGraph();
        }

        private void LoadOrFindGraph()
        {
            // If graph is already set, keep it
            if (_graph != null)
            {
                return;
            }

            // Use cached singleton if available
            if (_cachedSingletonGraph != null)
            {
                _graph = _cachedSingletonGraph;
                return;
            }

            // Only search once per session
            if (_hasSearchedForGraph)
            {
                return;
            }

            _hasSearchedForGraph = true;

            // Search for SceneFlowGraph assets (enforce singleton)
            string[] guids = AssetDatabase.FindAssets("t:SceneFlowGraph");

            if (guids.Length > 1)
            {
                Debug.LogWarning(
                    $"[Scene Flow Editor] Found {guids.Length} SceneFlowGraph assets. Only one should exist. Using the first one found."
                );
            }

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _cachedSingletonGraph = AssetDatabase.LoadAssetAtPath<SceneFlowGraph>(path);
                _graph = _cachedSingletonGraph;

                if (guids.Length > 1)
                {
                    Debug.Log($"[Scene Flow Editor] Loaded: {path}");
                }
            }
        }

        private void LoadSettings()
        {
            // Try to load from editor prefs first
            string settingsGuid = EditorPrefs.GetString("SceneFlowGraphEditor_SettingsGUID", "");
            if (!string.IsNullOrEmpty(settingsGuid))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(settingsGuid);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    _settings = AssetDatabase.LoadAssetAtPath<SceneFlowGraphEditorSettings>(
                        assetPath
                    );
                }
            }

            // If not found in prefs, search for any settings asset in the project
            if (_settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:SceneFlowGraphEditorSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _settings = AssetDatabase.LoadAssetAtPath<SceneFlowGraphEditorSettings>(path);
                }
            }

            // Fall back to default instance if still not found
            if (_settings == null)
            {
                _settings = SceneFlowGraphEditorSettings.Instance;
            }
        }

        private int GetSettingsHash()
        {
            if (_settings == null)
                return 0;
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _settings.nodeColor.GetHashCode();
                hash = hash * 31 + _settings.nodeSelectedColor.GetHashCode();
                hash = hash * 31 + _settings.hubNodeColor.GetHashCode();
                hash = hash * 31 + _settings.hubNodeSelectedColor.GetHashCode();
                hash = hash * 31 + _settings.nodeFontSize.GetHashCode();
                hash = hash * 31 + _settings.transitionColor.GetHashCode();
                hash = hash * 31 + _settings.transitionSelectedColor.GetHashCode();
                hash = hash * 31 + _settings.transitionBidirectionalColor.GetHashCode();
                hash = hash * 31 + _settings.transitionConditionalColor.GetHashCode();
                hash = hash * 31 + _settings.transitionCreationColor.GetHashCode();
                hash = hash * 31 + _settings.transitionWidth.GetHashCode();
                hash = hash * 31 + _settings.arrowSize.GetHashCode();
                hash = hash * 31 + _settings.arrowNodeOffset.GetHashCode();
                hash = hash * 31 + _settings.gridMajorColor.GetHashCode();
                hash = hash * 31 + _settings.gridMinorColor.GetHashCode();
                hash = hash * 31 + _settings.backgroundColor.GetHashCode();
                hash = hash * 31 + _settings.chapterBadgeColor.GetHashCode();
                return hash;
            }
        }

        private void Update()
        {
            // Check if settings have been modified and force style refresh
            if (_settings != null)
            {
                int currentHash = GetSettingsHash();
                if (currentHash != _lastSettingsHash)
                {
                    _lastSettingsHash = currentHash;
                    _stylesInitialized = false;
                    Repaint();
                }
            }

            // Toggle wantsMouseMove so transition-creation preview updates on every mouse move
            // rather than relying on the Update poll interval.
            bool needsMouseTracking =
                _transitionStartNode != null || _potentialTransitionDrag != null;
            if (wantsMouseMove != needsMouseTracking)
            {
                wantsMouseMove = needsMouseTracking;
            }
            if (needsMouseTracking)
            {
                Repaint();
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            // Ensure settings are loaded
            if (_settings == null)
            {
                LoadSettings();
            }

            _nodeStyle = new GUIStyle("box")
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = _settings.nodeFontSize,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 8, 8),
                wordWrap = true,
            };
            _nodeStyle.normal.background = MakeTexture(2, 2, _settings.nodeColor);
            _nodeStyle.normal.textColor = Color.white;

            _nodeSelectedStyle = new GUIStyle(_nodeStyle);
            _nodeSelectedStyle.normal.background = MakeTexture(2, 2, _settings.nodeSelectedColor);

            _hubNodeStyle = new GUIStyle(_nodeStyle);
            _hubNodeStyle.normal.background = MakeTexture(2, 2, _settings.hubNodeColor);

            _hubNodeSelectedStyle = new GUIStyle(_hubNodeStyle);
            _hubNodeSelectedStyle.normal.background = MakeTexture(
                2,
                2,
                _settings.hubNodeSelectedColor
            );

            _battleNodeStyle = new GUIStyle(_nodeStyle);
            _battleNodeStyle.normal.background = MakeTexture(
                2,
                2,
                new Color(0.45f, 0.1f, 0.1f, 1f)
            );

            _battleNodeSelectedStyle = new GUIStyle(_battleNodeStyle);
            _battleNodeSelectedStyle.normal.background = MakeTexture(
                2,
                2,
                new Color(0.75f, 0.2f, 0.2f, 1f)
            );

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _settings.nodeFontSize,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Normal,
            };
            _labelStyle.normal.textColor = Color.white;

            _smallLabelStyle = new GUIStyle(_labelStyle) { fontSize = 10 };

            _dateLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
            };

            _badgeLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
            };

            _chapterBadgeLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

            int idFontSize = Mathf.Max(8, _settings.nodeFontSize - 2);
            _nodeIdStyle = new GUIStyle(_labelStyle) { fontSize = idFontSize };
            _nodeIdStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            _stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            InitializeStyles();
            DrawToolbar();

            if (_graph == null)
            {
                DrawNoGraphSelected();
                return;
            }

            var graphRect = new Rect(
                0,
                20,
                position.width - SIDEBAR_WIDTH,
                position.height - 20 - STATUS_BAR_HEIGHT
            );
            var sidebarRect = new Rect(
                position.width - SIDEBAR_WIDTH,
                20,
                SIDEBAR_WIDTH,
                position.height - 20 - STATUS_BAR_HEIGHT
            );
            var statusBarRect = new Rect(
                0,
                position.height - STATUS_BAR_HEIGHT,
                position.width,
                STATUS_BAR_HEIGHT
            );

            DrawGraph(graphRect);
            DrawSidebar(sidebarRect);
            DrawStatusBar(statusBarRect);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Graph selection
            var newGraph = (SceneFlowGraph)
                EditorGUILayout.ObjectField(
                    _graph,
                    typeof(SceneFlowGraph),
                    false,
                    GUILayout.Width(250)
                );
            if (newGraph != _graph)
            {
                _graph = newGraph;
                _selectedNode = null;
                _selectedTransition = null;
                _transitionStartNode = null;
                _transitionIsDrag = false;
                _potentialTransitionDrag = null;
                _selectedNodes.Clear();
                _isDragging = false;
                _draggedNode = null;
            }

            GUILayout.FlexibleSpace();

            // Tools
            if (_graph != null)
            {
                if (GUILayout.Button("Add Scene", EditorStyles.toolbarButton))
                {
                    AddNewScene();
                }

                if (GUILayout.Button("Center View", EditorStyles.toolbarButton))
                {
                    CenterView();
                }
            }

            // Settings
            var newSettings = (SceneFlowGraphEditorSettings)
                EditorGUILayout.ObjectField(
                    _settings,
                    typeof(SceneFlowGraphEditorSettings),
                    false,
                    GUILayout.Width(200)
                );
            if (newSettings != _settings && newSettings != null)
            {
                _settings = newSettings;
                _lastSettingsHash = GetSettingsHash();
                _stylesInitialized = false;

                // Save the selection to editor prefs
                string assetPath = AssetDatabase.GetAssetPath(_settings);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    EditorPrefs.SetString("SceneFlowGraphEditor_SettingsGUID", guid);
                }

                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoGraphSelected()
        {
            GUILayout.BeginArea(new Rect(0, 20, position.width, position.height - 20));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.Label("No Scene Flow Graph Selected", EditorStyles.boldLabel);
            GUILayout.Space(10);
            if (GUILayout.Button("Create New Scene Flow Graph", GUILayout.Height(30)))
            {
                CreateNewGraph();
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        private void DrawGraph(Rect graphRect)
        {
            // Background
            EditorGUI.DrawRect(graphRect, _settings.backgroundColor);

            // Handle input
            HandleGraphInput(graphRect);

            // Begin zoomed and panned area
            GUILayout.BeginArea(graphRect);

            // Draw grid BEFORE applying matrix (in screen space)
            Matrix4x4 oldMatrix = GUI.matrix;
            DrawGrid(graphRect);

            // Now apply matrix for nodes and transitions
            GUI.matrix = Matrix4x4.TRS(_panOffset, Quaternion.identity, Vector3.one * _zoom);

            // Draw transitions first (so they appear behind nodes)
            DrawAllTransitions(graphRect);

            // Draw nodes
            DrawAllNodes(graphRect);

            GUI.matrix = oldMatrix;
            GUILayout.EndArea();

            // Draw connection line if creating transition
            if (_transitionStartNode != null)
            {
                var startPos = GetNodeCenter(_transitionStartNode);
                var nodeScreenPos = TransformToScreenSpace(startPos, graphRect);
                DrawConnectionLine(
                    nodeScreenPos,
                    Event.current.mousePosition,
                    _settings.transitionCreationColor
                );
            }

            // Cancel drag-based transition if mouse released over empty space
            // (nodes consume the event via e.Use() when completing over them)
            if (
                _transitionIsDrag
                && _transitionStartNode != null
                && Event.current.type == EventType.MouseUp
                && Event.current.button == 0
            )
            {
                _transitionStartNode = null;
                _transitionIsDrag = false;
                Repaint();
            }

            // Cancel potential shift+drag if released over empty space
            if (
                _potentialTransitionDrag != null
                && Event.current.type == EventType.MouseUp
                && Event.current.button == 0
            )
            {
                _potentialTransitionDrag = null;
                Repaint();
            }

            // Handle deselection on empty space click
            if (_clickedEmptySpace && Event.current.type == EventType.MouseDown)
            {
                _selectedNode = null;
                _selectedTransition = null;
                _selectedNodes.Clear();
                _transitionStartNode = null;
                _transitionIsDrag = false;
                _potentialTransitionDrag = null;
                Repaint();
            }
            _clickedEmptySpace = false;
        }

        private void HandleGraphInput(Rect graphRect)
        {
            var e = Event.current;

            // Pan with middle mouse or Alt+drag
            if (
                (e.type == EventType.MouseDrag && e.button == 2)
                || (e.type == EventType.MouseDrag && e.alt)
            )
            {
                _panOffset += e.delta;
                e.Use();
                Repaint();
            }

            // Zoom with scroll wheel
            if (e.type == EventType.ScrollWheel && graphRect.Contains(e.mousePosition))
            {
                float zoomDelta = -e.delta.y * 0.05f;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, MIN_ZOOM, MAX_ZOOM);
                e.Use();
                Repaint();
            }

            // Cancel transition creation on right click or Escape
            if (
                (e.type == EventType.MouseDown && e.button == 1)
                || (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            )
            {
                if (_transitionStartNode != null || _potentialTransitionDrag != null)
                {
                    _transitionStartNode = null;
                    _transitionIsDrag = false;
                    _potentialTransitionDrag = null;
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseDown && e.button == 1)
                {
                    // Right-click on empty space - deselect
                    _selectedNode = null;
                    _selectedTransition = null;
                    _selectedNodes.Clear();
                    e.Use();
                    Repaint();
                }
            }

            // Left-click on empty graph space - deselect and cancel transition
            if (
                e.type == EventType.MouseDown
                && e.button == 0
                && graphRect.Contains(e.mousePosition)
            )
            {
                // This will be overridden if a node or transition is clicked
                // We set a flag and check it at the end of the frame
                _clickedEmptySpace = true;
            }

            // Delete selected with Delete key
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                if (_selectedNodes.Count > 0)
                {
                    DeleteMultipleNodes(_selectedNodes);
                    e.Use();
                    Repaint();
                }
                else if (_selectedNode != null)
                {
                    DeleteNode(_selectedNode);
                    e.Use();
                    Repaint();
                }
                else if (_selectedTransition != null)
                {
                    DeleteTransition(_selectedTransition);
                    e.Use();
                    Repaint();
                }
            }
        }

        private void DrawAllNodes(Rect graphRect)
        {
            if (_graph.scenes == null)
            {
                return;
            }

            foreach (var node in _graph.scenes)
            {
                DrawNode(node, graphRect);
            }
        }

        private void DrawNode(SceneNode node, Rect graphRect)
        {
            var rect = GetNodeRect(node);
            var e = Event.current;

            // Choose style
            GUIStyle style;
            if (_selectedNode == node || _selectedNodes.Contains(node))
            {
                style =
                    node.isBattle ? _battleNodeSelectedStyle
                    : node.isHub ? _hubNodeSelectedStyle
                    : _nodeSelectedStyle;
            }
            else
            {
                style =
                    node.isBattle ? _battleNodeStyle
                    : node.isHub ? _hubNodeStyle
                    : _nodeStyle;
            }

            // Draw date label above node when this scene advances the game date
            if (node.TimePasses)
            {
                string dateLabel;
                if (node.IncrementDate)
                {
                    int days = node.IncrementDays;
                    dateLabel = days >= 0 ? $"+{days}d" : $"{days}d";
                }
                else
                {
                    string monthAbbr = node.MonthForThisScene.ToString().Substring(0, 3);
                    dateLabel = $"{monthAbbr} {node.DayForThisScene}";
                    if (node.HasYear)
                        dateLabel += $", Y{node.YearForThisScene}";
                    else
                        dateLabel += "*"; // year auto-advances at runtime
                }
                var dateLabelRect = new Rect(rect.x, rect.y - 18, rect.width, 16);
                EditorGUI.DrawRect(
                    new Rect(
                        dateLabelRect.x - 1,
                        dateLabelRect.y - 1,
                        dateLabelRect.width + 2,
                        dateLabelRect.height + 2
                    ),
                    new Color(0.1f, 0.1f, 0.3f, 0.85f)
                );
                GUI.Label(dateLabelRect, dateLabel, _dateLabelStyle);
            }

            // Draw node box
            GUI.Box(rect, "", style);

            // Draw label
            var labelRect = new Rect(rect.x, rect.y + 5, rect.width, 20);
            GUI.Label(labelRect, node.displayName, _labelStyle);

            // Draw ID (smaller)
            var idRect = new Rect(rect.x, rect.y + 25, rect.width, 15);
            GUI.Label(idRect, node.id, _nodeIdStyle);

            // Hub indicator (top-left)
            if (node.isHub)
            {
                var hubRect = new Rect(rect.x + 5, rect.y + 5, 15, 15);
                EditorGUI.DrawRect(hubRect, new Color(1f, 0.8f, 0f, 0.5f));
                GUI.Label(hubRect, "H", _badgeLabelStyle);
            }

            // Battle indicator (bottom-left)
            if (node.isBattle)
            {
                var battleRect = new Rect(rect.x + 5, rect.y + rect.height - 20, 15, 15);
                EditorGUI.DrawRect(battleRect, new Color(0.9f, 0.2f, 0.2f, 0.75f));
                GUI.Label(battleRect, "B", _badgeLabelStyle);
            }

            // Chapter number badge (top left corner)
            if (node.SpecificChapter)
            {
                var chapterRect = new Rect(rect.x + 5, rect.y + 5, 25, 15);
                EditorGUI.DrawRect(chapterRect, _settings.chapterBadgeColor);
                GUI.Label(chapterRect, $"Ch{node.ChapterNumber}", _chapterBadgeLabelStyle);
            }

            // Starting scene indicator (top-right)
            if (_graph.startingScene == node)
            {
                var startRect = new Rect(rect.x + rect.width - 20, rect.y + 5, 15, 15);
                EditorGUI.DrawRect(startRect, new Color(0f, 1f, 0f, 0.5f));
                GUI.Label(startRect, "▶", _smallLabelStyle);
            }

            // Handle node interactions
            // Inside BeginArea with GUI.matrix, mouse IS transformed by the matrix
            // Mouse is in transformed space, rect is in graph space, so no transform needed
            var mousePos = e.mousePosition;

            if (rect.Contains(mousePos))
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    _clickedEmptySpace = false; // Clicked on a node, not empty space

                    if (_transitionStartNode != null && !_transitionIsDrag)
                    {
                        // Complete click-based transition creation
                        if (_transitionStartNode != node)
                        {
                            CreateTransition(_transitionStartNode, node);
                        }
                        _transitionStartNode = null;
                        _transitionIsDrag = false;
                    }
                    else if (e.shift && _transitionStartNode == null)
                    {
                        // Shift+mousedown: track for potential drag-to-connect or click multi-select
                        _potentialTransitionDrag = node;
                        _potentialTransitionDragStart = mousePos;
                    }
                    else if (e.control || e.command)
                    {
                        // Ctrl/Cmd: multi-select
                        if (_selectedNodes.Contains(node))
                        {
                            _selectedNodes.Remove(node);
                            if (_selectedNode == node)
                            {
                                _selectedNode = null;
                            }
                        }
                        else
                        {
                            _selectedNodes.Add(node);
                            _selectedNode = node;
                        }
                        _selectedTransition = null;
                    }
                    else if (_transitionStartNode == null)
                    {
                        // Normal single selection and start dragging
                        _selectedNodes.Clear();
                        _selectedNode = node;
                        _selectedTransition = null;
                        _draggedNode = node;
                        _dragStartPos = e.mousePosition;
                        _isDragging = true;
                    }
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseDown && e.button == 1)
                {
                    // Right-click context menu
                    ShowNodeContextMenu(node);
                    e.Use();
                }
            }

            // Handle dragging
            if (_isDragging && _draggedNode == node && e.type == EventType.MouseDrag)
            {
                node.editorPosition =
                    e.mousePosition - new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
                EditorUtility.SetDirty(_graph);
                e.Use();
                Repaint();
            }

            // Handle shift+drag to initiate transition creation from this node
            if (_potentialTransitionDrag == node && e.type == EventType.MouseDrag && e.button == 0)
            {
                if (
                    Vector2.Distance(e.mousePosition, _potentialTransitionDragStart)
                    > DRAG_TRANSITION_THRESHOLD
                )
                {
                    _transitionStartNode = node;
                    _transitionIsDrag = true;
                    _potentialTransitionDrag = null;
                    Repaint();
                }
                e.Use();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                // Complete drag-based transition creation when released over a target node
                if (
                    _transitionIsDrag
                    && _transitionStartNode != null
                    && _transitionStartNode != node
                    && rect.Contains(mousePos)
                )
                {
                    CreateTransition(_transitionStartNode, node);
                    _transitionStartNode = null;
                    _transitionIsDrag = false;
                    e.Use();
                    Repaint();
                }
                // Shift+click (drag threshold not exceeded) → multi-select
                else if (_potentialTransitionDrag == node && rect.Contains(mousePos))
                {
                    _potentialTransitionDrag = null;
                    if (_selectedNodes.Contains(node))
                    {
                        _selectedNodes.Remove(node);
                        if (_selectedNode == node)
                        {
                            _selectedNode = null;
                        }
                    }
                    else
                    {
                        _selectedNodes.Add(node);
                        _selectedNode = node;
                    }
                    _selectedTransition = null;
                    e.Use();
                    Repaint();
                }

                if (_isDragging && _draggedNode == node)
                {
                    // Save after dragging is complete
                    AssetDatabase.SaveAssetIfDirty(_graph);
                }
                _isDragging = false;
                _draggedNode = null;
            }
        }

        private void DrawAllTransitions(Rect graphRect)
        {
            if (_graph.transitions == null)
            {
                return;
            }

            foreach (var transition in _graph.transitions)
            {
                DrawTransition(transition, graphRect);
            }
        }

        private void DrawTransition(SceneTransition transition, Rect graphRect)
        {
            var fromNode = _graph.GetScene(transition.fromSceneId);
            var toNode = _graph.GetScene(transition.toSceneId);

            if (fromNode == null || toNode == null)
            {
                return;
            }

            var fromPos = GetNodeCenter(fromNode);
            var toPos = GetNodeCenter(toNode);
            var fromRect = GetNodeRect(fromNode);
            var toRect = GetNodeRect(toNode);

            // Check if this is a cross-chapter transition
            bool isCrossChapter =
                fromNode.SpecificChapter
                && toNode.SpecificChapter
                && fromNode.ChapterNumber != toNode.ChapterNumber;

            // Color based on selection and conditions
            Color lineColor = _settings.transitionColor;
            if (_selectedTransition == transition)
            {
                lineColor = _settings.transitionSelectedColor;
            }
            else if (isCrossChapter)
            {
                lineColor = _settings.chapterTransitionColor;
            }
            else if (transition.isBidirectional)
            {
                lineColor = _settings.transitionBidirectionalColor;
            }
            else if (transition.conditions != null && transition.conditions.Count > 0)
            {
                lineColor = _settings.transitionConditionalColor;
            }

            // Calculate line endpoints so they stop at the edge of the node rects
            var adjustedFrom = GetPointOnRectEdge(
                fromRect,
                fromPos,
                toPos,
                _settings.arrowNodeOffset
            );
            var adjustedTo = GetPointOnRectEdge(toRect, toPos, fromPos, _settings.arrowNodeOffset);

            // Draw arrow
            DrawArrow(adjustedFrom, adjustedTo, lineColor, transition.isBidirectional);

            // Determine label text
            string labelText = transition.label;
            if (isCrossChapter)
            {
                labelText =
                    $"Ch{fromNode.ChapterNumber} → Ch{toNode.ChapterNumber}: {transition.label}";
            }

            // Draw label at midpoint
            var midPoint = (fromPos + toPos) / 2f;
            var labelRect = new Rect(midPoint.x - 50, midPoint.y - 10, 100, 20);

            // Background for label
            var bgRect = new Rect(
                labelRect.x - 2,
                labelRect.y - 2,
                labelRect.width + 4,
                labelRect.height + 4
            );
            EditorGUI.DrawRect(bgRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));

            GUI.Label(labelRect, labelText, _smallLabelStyle);

            // Check if clicking on transition - check label area or line proximity
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var mousePos = e.mousePosition;

                // Check if clicked on label background
                if (bgRect.Contains(mousePos))
                {
                    _clickedEmptySpace = false;
                    _selectedTransition = transition;
                    _selectedNode = null;
                    e.Use();
                    Repaint();
                }
                // Also check proximity to the line itself
                else if (IsPointNearLine(mousePos, fromPos, toPos, 15f / _zoom))
                {
                    _clickedEmptySpace = false;
                    _selectedTransition = transition;
                    _selectedNode = null;
                    e.Use();
                    Repaint();
                }
            }
        }

        private void DrawGrid(Rect graphRect)
        {
            const float gridSpacing = 50f;
            const float thickLineInterval = 5; // Every 5th line is thicker

            Handles.BeginGUI();

            // Calculate visible graph space range
            // Graph position = (screen position - panOffset) / zoom
            float graphStartX = (0 - _panOffset.x) / _zoom;
            float graphEndX = (graphRect.width - _panOffset.x) / _zoom;
            float graphStartY = (0 - _panOffset.y) / _zoom;
            float graphEndY = (graphRect.height - _panOffset.y) / _zoom;

            // Snap to grid boundaries
            float firstGridX = Mathf.Ceil(graphStartX / gridSpacing) * gridSpacing;
            float firstGridY = Mathf.Ceil(graphStartY / gridSpacing) * gridSpacing;

            // Draw vertical lines (at graph X positions, spanning visible Y)
            int lineCount = (int)(firstGridX / gridSpacing);
            for (float graphX = firstGridX; graphX <= graphEndX; graphX += gridSpacing)
            {
                // Convert graph position to screen position
                float screenX = graphX * _zoom + _panOffset.x;

                bool isThickLine = lineCount % thickLineInterval == 0;
                Handles.color = isThickLine ? _settings.gridMajorColor : _settings.gridMinorColor;

                // Draw in screen coordinates (not affected by GUI.matrix since we draw before setting it)
                Handles.DrawLine(new Vector2(screenX, 0), new Vector2(screenX, graphRect.height));
                lineCount++;
            }

            // Draw horizontal lines (at graph Y positions, spanning visible X)
            lineCount = (int)(firstGridY / gridSpacing);
            for (float graphY = firstGridY; graphY <= graphEndY; graphY += gridSpacing)
            {
                // Convert graph position to screen position
                float screenY = graphY * _zoom + _panOffset.y;

                bool isThickLine = lineCount % thickLineInterval == 0;
                Handles.color = isThickLine ? _settings.gridMajorColor : _settings.gridMinorColor;

                // Draw in screen coordinates
                Handles.DrawLine(new Vector2(0, screenY), new Vector2(graphRect.width, screenY));
                lineCount++;
            }

            Handles.EndGUI();
        }

        private void DrawArrow(Vector2 from, Vector2 to, Color color, bool bidirectional)
        {
            Handles.BeginGUI();
            Handles.color = color;

            Vector2 direction = (to - from).normalized;

            // Draw main line with thickness
            Handles.DrawAAPolyLine(_settings.transitionWidth, from, to);

            // Calculate arrowhead dimensions based on settings
            float arrowLength = _settings.arrowSize;
            float arrowWidth = _settings.arrowSize * 0.5f;

            // Draw arrowhead
            if (!bidirectional)
            {
                Vector2 arrowTip = to;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                Vector2 arrowLeft = arrowTip - direction * arrowLength + perpendicular * arrowWidth;
                Vector2 arrowRight =
                    arrowTip - direction * arrowLength - perpendicular * arrowWidth;

                Handles.DrawAAPolyLine(_settings.transitionWidth, arrowTip, arrowLeft);
                Handles.DrawAAPolyLine(_settings.transitionWidth, arrowTip, arrowRight);
            }
            else
            {
                // Draw arrows on both ends for bidirectional
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);

                // Arrow at 'to' end
                Vector2 arrowTip1 = to;
                Handles.DrawAAPolyLine(
                    _settings.transitionWidth,
                    arrowTip1,
                    arrowTip1 - direction * arrowLength + perpendicular * arrowWidth
                );
                Handles.DrawAAPolyLine(
                    _settings.transitionWidth,
                    arrowTip1,
                    arrowTip1 - direction * arrowLength - perpendicular * arrowWidth
                );

                // Arrow at 'from' end
                Vector2 arrowTip2 = from;
                Handles.DrawAAPolyLine(
                    _settings.transitionWidth,
                    arrowTip2,
                    arrowTip2 + direction * arrowLength + perpendicular * arrowWidth
                );
                Handles.DrawAAPolyLine(
                    _settings.transitionWidth,
                    arrowTip2,
                    arrowTip2 + direction * arrowLength - perpendicular * arrowWidth
                );
            }

            Handles.EndGUI();
        }

        private void DrawConnectionLine(Vector2 from, Vector2 to, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(_settings.transitionWidth, from, to);
            Handles.EndGUI();
        }

        private void DrawSidebar(Rect sidebarRect)
        {
            GUILayout.BeginArea(sidebarRect);
            EditorGUILayout.BeginVertical("box");

            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

            if (_selectedNodes.Count == 2)
            {
                DrawMultiNodeInspector();
            }
            else if (_selectedNode != null)
            {
                DrawNodeInspector();
            }
            else if (_selectedTransition != null)
            {
                DrawTransitionInspector();
            }
            else
            {
                DrawGraphInspector();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawGraphInspector()
        {
            EditorGUILayout.LabelField("Scene Flow Graph", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_graph == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Scenes: {_graph.scenes?.Count ?? 0}");
            EditorGUILayout.LabelField($"Transitions: {_graph.transitions?.Count ?? 0}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Starting Scene", EditorStyles.boldLabel);

            if (_graph.scenes != null && _graph.scenes.Count > 0)
            {
                EditorGUI.BeginChangeCheck();
                var sceneNames = _graph.scenes.ConvertAll(s => s.displayName).ToArray();
                var currentIndex = _graph.scenes.IndexOf(_graph.startingScene);
                var newIndex = EditorGUILayout.Popup("Starting Scene", currentIndex, sceneNames);
                if (EditorGUI.EndChangeCheck() && newIndex >= 0)
                {
                    Undo.RecordObject(_graph, "Change Starting Scene");
                    _graph.startingScene = _graph.scenes[newIndex];
                    EditorUtility.SetDirty(_graph);
                    AssetDatabase.SaveAssetIfDirty(_graph);
                }
            }
            else
            {
                EditorGUILayout.LabelField("(No scenes in graph)");
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Click a node to select it.\nCtrl+Click or Shift+Click to multi-select nodes.\nShift+Drag from one node to another to create a transition.\nRight-click a node for more options.\nDelete key removes the selected node/transition.",
                MessageType.Info
            );
        }

        private void DrawMultiNodeInspector()
        {
            EditorGUILayout.LabelField("Multiple Nodes Selected", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_selectedNodes.Count == 2)
            {
                var nodesList = new List<SceneNode>(_selectedNodes);
                var node1 = nodesList[0];
                var node2 = nodesList[1];

                EditorGUILayout.LabelField($"Node 1: {node1.displayName}");
                EditorGUILayout.LabelField($"Node 2: {node2.displayName}");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                if (
                    GUILayout.Button(
                        $"Create Transition: {node1.displayName} → {node2.displayName}",
                        GUILayout.Height(30)
                    )
                )
                {
                    CreateTransition(node1, node2);
                }

                if (
                    GUILayout.Button(
                        $"Create Transition: {node2.displayName} → {node1.displayName}",
                        GUILayout.Height(30)
                    )
                )
                {
                    CreateTransition(node2, node1);
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Create Bidirectional Transition", GUILayout.Height(30)))
                {
                    CreateBidirectionalTransition(node1, node2);
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Clear Selection"))
                {
                    _selectedNodes.Clear();
                    _selectedNode = null;
                    Repaint();
                }
            }
            else
            {
                EditorGUILayout.LabelField($"{_selectedNodes.Count} nodes selected");
                EditorGUILayout.HelpBox(
                    "Select exactly 2 nodes to create transitions between them.",
                    MessageType.Info
                );

                if (GUILayout.Button("Clear Selection"))
                {
                    _selectedNodes.Clear();
                    _selectedNode = null;
                    Repaint();
                }
            }
        }

        private void DrawNodeInspector()
        {
            EditorGUILayout.LabelField("Scene Node", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            // Scene Asset field with auto-population
            var newSceneAsset = (UnityEditor.SceneAsset)
                EditorGUILayout.ObjectField(
                    "Scene",
                    _selectedNode.sceneAsset,
                    typeof(UnityEditor.SceneAsset),
                    false
                );

            if (newSceneAsset != _selectedNode.sceneAsset)
            {
                _selectedNode.sceneAsset = newSceneAsset;
                if (newSceneAsset != null)
                {
                    // Auto-populate from scene asset
                    string assetPath = AssetDatabase.GetAssetPath(newSceneAsset);
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                    _selectedNode.sceneName = sceneName;
                    _selectedNode.displayName = sceneName;

                    // Auto-generate ID if empty
                    if (string.IsNullOrEmpty(_selectedNode.id))
                    {
                        _selectedNode.id = sceneName.ToLower().Replace(" ", "_");
                    }
                }
            }

            _selectedNode.displayName = EditorGUILayout.TextField(
                "Display Name",
                _selectedNode.displayName
            );

            // Track ID changes and update all referencing transitions
            string oldNodeId = _selectedNode.id;
            _selectedNode.id = EditorGUILayout.TextField("ID", _selectedNode.id);
            if (_selectedNode.id != oldNodeId && !string.IsNullOrEmpty(_selectedNode.id))
            {
                foreach (var t in _graph.transitions)
                {
                    if (t.fromSceneId == oldNodeId)
                        t.fromSceneId = _selectedNode.id;
                    if (t.toSceneId == oldNodeId)
                        t.toSceneId = _selectedNode.id;
                }
                if (_graph.StartingSceneId == oldNodeId)
                    _graph.SetStartingSceneById(_selectedNode.id);
            }

            EditorGUILayout.Space();
            _selectedNode.isHub = EditorGUILayout.Toggle("Is Hub Scene", _selectedNode.isHub);
            _selectedNode.persistWhenLeaving = EditorGUILayout.Toggle(
                "Persist When Leaving",
                _selectedNode.persistWhenLeaving
            );
            _selectedNode.isBattle = EditorGUILayout.Toggle(
                "Is Battle Scene",
                _selectedNode.isBattle
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notes");
            _selectedNode.notes = EditorGUILayout.TextArea(
                _selectedNode.notes,
                GUILayout.Height(60)
            );

            EditorGUILayout.Space();
            _selectedNode.TimePasses = EditorGUILayout.Toggle(
                "Time Passes",
                _selectedNode.TimePasses
            );
            if (_selectedNode.TimePasses)
            {
                EditorGUI.indentLevel++;

                _selectedNode.IncrementDate = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Increment Mode",
                        "When enabled, entering this scene advances the date by a fixed number of days from wherever it currently is. Use this for scenes that should not have a fixed calendar date."
                    ),
                    _selectedNode.IncrementDate
                );

                if (_selectedNode.IncrementDate)
                {
                    _selectedNode.IncrementDays = EditorGUILayout.IntField(
                        new GUIContent("Days to Add", "How many days to advance the game date."),
                        _selectedNode.IncrementDays
                    );
                }
                else
                {
                    _selectedNode.MonthForThisScene = (Turnroot.Utilities.Month)
                        EditorGUILayout.EnumPopup("Month", _selectedNode.MonthForThisScene);
                    _selectedNode.DayForThisScene = EditorGUILayout.IntSlider(
                        "Day",
                        _selectedNode.DayForThisScene,
                        1,
                        31
                    );
                    _selectedNode.HasYear = EditorGUILayout.Toggle(
                        "Has Year?",
                        _selectedNode.HasYear
                    );
                    if (_selectedNode.HasYear)
                    {
                        _selectedNode.YearForThisScene = EditorGUILayout.IntField(
                            "Year",
                            _selectedNode.YearForThisScene
                        );
                    }
                }

                EditorGUI.indentLevel--;
            }

            // Hub scenes cannot have SpecificChapter (they persist across chapters)
            if (!_selectedNode.isHub)
            {
                EditorGUILayout.Space();
                _selectedNode.SpecificChapter = EditorGUILayout.Toggle(
                    "Specific Chapter",
                    _selectedNode.SpecificChapter
                );
                if (_selectedNode.SpecificChapter)
                {
                    EditorGUI.indentLevel++;
                    _selectedNode.ChapterName = EditorGUILayout.TextField(
                        "Chapter Name",
                        _selectedNode.ChapterName
                    );
                    _selectedNode.ChapterNumber = EditorGUILayout.IntField(
                        "Chapter Number",
                        _selectedNode.ChapterNumber
                    );
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                // Ensure hub scenes never have SpecificChapter enabled
                if (_selectedNode.SpecificChapter)
                {
                    _selectedNode.SpecificChapter = false;
                }
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Hub scenes cannot belong to a specific chapter as they persist across the story.",
                    MessageType.Info
                );
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssetIfDirty(_graph);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Set as Starting Scene"))
            {
                Undo.RecordObject(_graph, "Set Starting Scene");
                _graph.startingScene = _selectedNode;
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssetIfDirty(_graph);
                Repaint();
            }

            if (GUILayout.Button("Create Transition From This"))
            {
                _transitionStartNode = _selectedNode;
                _transitionIsDrag = false;
            }

            if (GUILayout.Button("Delete Node", GUILayout.Height(30)))
            {
                DeleteNode(_selectedNode);
            }
        }

        private static readonly string[] _conditionKeyOptions = GetSceneFlowConditionKeys();

        private static string[] GetSceneFlowConditionKeys()
        {
            var type = typeof(Turnroot.Utilities.SceneFlows.SceneFlowConditionKeys);
            var fields = type.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
            );
            var keys = fields
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .OrderBy(k => k)
                .ToList();
            keys.Insert(0, "<Custom>");
            return keys.ToArray();
        }

        private void DrawTransitionInspector()
        {
            EditorGUILayout.LabelField("Scene Transition", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var fromNode = _graph.GetScene(_selectedTransition.fromSceneId);
            var toNode = _graph.GetScene(_selectedTransition.toSceneId);

            EditorGUILayout.LabelField($"From: {fromNode?.displayName ?? "Unknown"}");
            EditorGUILayout.LabelField($"To: {toNode?.displayName ?? "Unknown"}");

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            _selectedTransition.label = EditorGUILayout.TextField(
                "Label",
                _selectedTransition.label
            );

            // Brain State dropdown(s)
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Brain State", EditorStyles.boldLabel);

            if (_cachedBrainStateIds == null)
            {
                _cachedBrainStateIds = BrainStateNames.GetAllStateIds();
                _cachedBrainStateOptions = new string[_cachedBrainStateIds.Length + 1];
                _cachedBrainStateOptions[0] = "(Keep Current State)";
                System.Array.Copy(
                    _cachedBrainStateIds,
                    0,
                    _cachedBrainStateOptions,
                    1,
                    _cachedBrainStateIds.Length
                );
            }
            var allStates = _cachedBrainStateIds;
            var stateOptions = _cachedBrainStateOptions;

            // Forward direction state
            int currentStateIndex = 0;
            if (!string.IsNullOrEmpty(_selectedTransition.targetBrainState))
            {
                currentStateIndex = System.Array.IndexOf(
                    allStates,
                    _selectedTransition.targetBrainState
                );
                if (currentStateIndex >= 0)
                {
                    currentStateIndex++; // Offset by 1 because of the "Keep Current State" option
                }
                else
                {
                    currentStateIndex = 0; // Invalid state, default to "Keep Current State"
                }
            }

            string forwardLabel = _selectedTransition.isBidirectional
                ? $"Forward ({fromNode?.displayName ?? "?"} → {toNode?.displayName ?? "?"})"
                : "Target State";

            int newStateIndex = EditorGUILayout.Popup(
                forwardLabel,
                currentStateIndex,
                stateOptions
            );
            if (newStateIndex == 0)
            {
                _selectedTransition.targetBrainState = string.Empty;
            }
            else
            {
                _selectedTransition.targetBrainState = allStates[newStateIndex - 1];
            }

            // Reverse direction state (only for bidirectional)
            if (_selectedTransition.isBidirectional)
            {
                int currentReverseStateIndex = 0;
                if (!string.IsNullOrEmpty(_selectedTransition.targetBrainStateReverse))
                {
                    currentReverseStateIndex = System.Array.IndexOf(
                        allStates,
                        _selectedTransition.targetBrainStateReverse
                    );
                    if (currentReverseStateIndex >= 0)
                    {
                        currentReverseStateIndex++;
                    }
                    else
                    {
                        currentReverseStateIndex = 0;
                    }
                }

                string reverseLabel =
                    $"Reverse ({toNode?.displayName ?? "?"} → {fromNode?.displayName ?? "?"})";

                int newReverseStateIndex = EditorGUILayout.Popup(
                    reverseLabel,
                    currentReverseStateIndex,
                    stateOptions
                );
                if (newReverseStateIndex == 0)
                {
                    _selectedTransition.targetBrainStateReverse = string.Empty;
                }
                else
                {
                    _selectedTransition.targetBrainStateReverse = allStates[
                        newReverseStateIndex - 1
                    ];
                }
            }

            EditorGUILayout.Space();
            _selectedTransition.isBidirectional = EditorGUILayout.Toggle(
                "Bidirectional",
                _selectedTransition.isBidirectional
            );
            _selectedTransition.unloadPreviousScene = EditorGUILayout.Toggle(
                "Unload Previous Scene",
                _selectedTransition.unloadPreviousScene
            );
            _selectedTransition.isReturnTransition = EditorGUILayout.Toggle(
                "Is Return Transition",
                _selectedTransition.isReturnTransition
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notes");
            _selectedTransition.notes = EditorGUILayout.TextArea(
                _selectedTransition.notes,
                GUILayout.Height(60)
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

            if (_selectedTransition.conditions == null)
            {
                _selectedTransition.conditions = new List<SceneCondition>();
            }

            // Display and edit each condition
            for (int i = 0; i < _selectedTransition.conditions.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                var condition = _selectedTransition.conditions[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Condition {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    _selectedTransition.conditions.RemoveAt(i);
                    EditorUtility.SetDirty(_graph);
                    AssetDatabase.SaveAssetIfDirty(_graph);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                condition.conditionType = (SceneConditionType)
                    EditorGUILayout.EnumPopup("Type", condition.conditionType);

                if (condition.conditionType != SceneConditionType.Always)
                {
                    // Use a dropdown of known flag keys, but allow custom input as well.
                    if (
                        condition.conditionType == SceneConditionType.BrainStateBool
                        || condition.conditionType == SceneConditionType.CustomFlag
                    )
                    {
                        int selectedIndex = 0;
                        if (!string.IsNullOrEmpty(condition.conditionKey))
                        {
                            int found = System.Array.IndexOf(
                                _conditionKeyOptions,
                                condition.conditionKey
                            );
                            if (found >= 0)
                            {
                                selectedIndex = found;
                            }
                        }

                        int newIndex = EditorGUILayout.Popup(
                            "Key",
                            selectedIndex,
                            _conditionKeyOptions
                        );
                        if (newIndex != selectedIndex)
                        {
                            condition.conditionKey =
                                newIndex == 0 ? string.Empty : _conditionKeyOptions[newIndex];
                        }

                        if (string.IsNullOrEmpty(condition.conditionKey))
                        {
                            condition.conditionKey = EditorGUILayout.TextField(
                                "Custom Key",
                                condition.conditionKey
                            );
                        }
                    }
                    else
                    {
                        condition.conditionKey = EditorGUILayout.TextField(
                            "Key",
                            condition.conditionKey
                        );
                    }

                    switch (condition.conditionType)
                    {
                        case SceneConditionType.BrainStateBool:
                        case SceneConditionType.CustomFlag:
                            condition.expectedBoolValue = EditorGUILayout.Toggle(
                                "Expected Value",
                                condition.expectedBoolValue
                            );
                            break;

                        case SceneConditionType.BrainStateInt:
                            condition.comparisonOperator = (ComparisonOperator)
                                EditorGUILayout.EnumPopup("Operator", condition.comparisonOperator);
                            condition.expectedIntValue = EditorGUILayout.IntField(
                                "Value",
                                condition.expectedIntValue
                            );
                            break;

                        case SceneConditionType.BrainStateString:
                            condition.expectedStringValue = EditorGUILayout.TextField(
                                "Expected Value",
                                condition.expectedStringValue
                            );
                            break;
                    }
                }

                EditorGUILayout.LabelField("Preview:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(condition.ToString(), EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Add Condition"))
            {
                _selectedTransition.conditions.Add(
                    new SceneCondition { conditionType = SceneConditionType.Always }
                );
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssetIfDirty(_graph);
            }

            if (_selectedTransition.conditions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No conditions means this transition is always available.",
                    MessageType.Info
                );
            }

            // Reverse conditions (for bidirectional transitions)
            if (_selectedTransition.isBidirectional)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Reverse Conditions", EditorStyles.boldLabel);

                if (_selectedTransition.reverseConditions == null)
                {
                    _selectedTransition.reverseConditions = new List<SceneCondition>();
                }

                for (int i = 0; i < _selectedTransition.reverseConditions.Count; i++)
                {
                    EditorGUILayout.BeginVertical("box");
                    var condition = _selectedTransition.reverseConditions[i];

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        $"Reverse Condition {i + 1}",
                        EditorStyles.boldLabel
                    );
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        _selectedTransition.reverseConditions.RemoveAt(i);
                        EditorUtility.SetDirty(_graph);
                        AssetDatabase.SaveAssetIfDirty(_graph);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    condition.conditionType = (SceneConditionType)
                        EditorGUILayout.EnumPopup("Type", condition.conditionType);

                    if (condition.conditionType != SceneConditionType.Always)
                    {
                        // Use the same key dropdown logic as forward conditions
                        if (
                            condition.conditionType == SceneConditionType.BrainStateBool
                            || condition.conditionType == SceneConditionType.CustomFlag
                        )
                        {
                            int selectedIndex = 0;
                            if (!string.IsNullOrEmpty(condition.conditionKey))
                            {
                                int found = System.Array.IndexOf(
                                    _conditionKeyOptions,
                                    condition.conditionKey
                                );
                                if (found >= 0)
                                {
                                    selectedIndex = found;
                                }
                            }

                            int newIndex = EditorGUILayout.Popup(
                                "Key",
                                selectedIndex,
                                _conditionKeyOptions
                            );
                            if (newIndex != selectedIndex)
                            {
                                condition.conditionKey =
                                    newIndex == 0 ? string.Empty : _conditionKeyOptions[newIndex];
                            }

                            if (string.IsNullOrEmpty(condition.conditionKey))
                            {
                                condition.conditionKey = EditorGUILayout.TextField(
                                    "Custom Key",
                                    condition.conditionKey
                                );
                            }
                        }
                        else
                        {
                            condition.conditionKey = EditorGUILayout.TextField(
                                "Key",
                                condition.conditionKey
                            );
                        }

                        switch (condition.conditionType)
                        {
                            case SceneConditionType.BrainStateBool:
                            case SceneConditionType.CustomFlag:
                                condition.expectedBoolValue = EditorGUILayout.Toggle(
                                    "Expected Value",
                                    condition.expectedBoolValue
                                );
                                break;

                            case SceneConditionType.BrainStateInt:
                                condition.comparisonOperator = (ComparisonOperator)
                                    EditorGUILayout.EnumPopup(
                                        "Operator",
                                        condition.comparisonOperator
                                    );
                                condition.expectedIntValue = EditorGUILayout.IntField(
                                    "Value",
                                    condition.expectedIntValue
                                );
                                break;

                            case SceneConditionType.BrainStateString:
                                condition.expectedStringValue = EditorGUILayout.TextField(
                                    "Expected Value",
                                    condition.expectedStringValue
                                );
                                break;
                        }
                    }

                    EditorGUILayout.LabelField("Preview:", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(
                        condition.ToString(),
                        EditorStyles.wordWrappedMiniLabel
                    );

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }

                if (GUILayout.Button("Add Reverse Condition"))
                {
                    _selectedTransition.reverseConditions.Add(
                        new SceneCondition { conditionType = SceneConditionType.Always }
                    );
                    EditorUtility.SetDirty(_graph);
                    AssetDatabase.SaveAssetIfDirty(_graph);
                }

                if (_selectedTransition.reverseConditions.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No reverse conditions means the reverse direction is always available.",
                        MessageType.Info
                    );
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssetIfDirty(_graph);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Transition", GUILayout.Height(30)))
            {
                DeleteTransition(_selectedTransition);
            }
        }

        private Rect GetNodeRect(SceneNode node)
        {
            return new Rect(node.editorPosition.x, node.editorPosition.y, NODE_WIDTH, NODE_HEIGHT);
        }

        private Vector2 GetNodeCenter(SceneNode node)
        {
            return node.editorPosition + new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
        }

        private Vector2 GetPointOnRectEdge(Rect rect, Vector2 center, Vector2 target, float padding)
        {
            // Find the intersection point between a ray from the center toward the target and the rect edge.
            // This keeps connection lines from overlapping the node boxes.
            Vector2 direction = (target - center).normalized;
            Vector2 hitPoint = center;
            float bestT = float.PositiveInfinity;

            // Check vertical sides (left/right)
            if (Mathf.Abs(direction.x) > 0.0001f)
            {
                float tx =
                    (direction.x > 0 ? rect.xMax - center.x : rect.xMin - center.x) / direction.x;
                if (tx > 0)
                {
                    Vector2 p = center + direction * tx;
                    if (p.y >= rect.yMin && p.y <= rect.yMax && tx < bestT)
                    {
                        bestT = tx;
                        hitPoint = p;
                    }
                }
            }

            // Check horizontal sides (top/bottom)
            if (Mathf.Abs(direction.y) > 0.0001f)
            {
                float ty =
                    (direction.y > 0 ? rect.yMax - center.y : rect.yMin - center.y) / direction.y;
                if (ty > 0)
                {
                    Vector2 p = center + direction * ty;
                    if (p.x >= rect.xMin && p.x <= rect.xMax && ty < bestT)
                    {
                        bestT = ty;
                        hitPoint = p;
                    }
                }
            }

            // Apply padding so the line doesn't touch the node border
            return hitPoint + direction * padding;
        }

        private Vector2 TransformToScreenSpace(Vector2 graphPos, Rect graphRect)
        {
            return graphPos * _zoom + _panOffset + graphRect.position;
        }

        private bool IsPointNearLine(
            Vector2 point,
            Vector2 lineStart,
            Vector2 lineEnd,
            float threshold
        )
        {
            // Calculate distance from point to line segment
            Vector2 line = lineEnd - lineStart;
            float lineLength = line.magnitude;
            if (lineLength < 0.001f)
            {
                return false;
            }

            Vector2 lineDir = line / lineLength;
            Vector2 toPoint = point - lineStart;
            float projection = Vector2.Dot(toPoint, lineDir);

            // Clamp projection to line segment
            projection = Mathf.Clamp(projection, 0f, lineLength);
            Vector2 closestPoint = lineStart + lineDir * projection;

            return Vector2.Distance(point, closestPoint) < threshold;
        }

        private void ShowNodeContextMenu(SceneNode node)
        {
            var menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Set as Starting Scene"),
                false,
                () =>
                {
                    Undo.RecordObject(_graph, "Set Starting Scene");
                    _graph.startingScene = node;
                    EditorUtility.SetDirty(_graph);
                    AssetDatabase.SaveAssetIfDirty(_graph);
                    Repaint();
                }
            );
            menu.AddItem(
                new GUIContent("Create Transition From This"),
                false,
                () =>
                {
                    _transitionStartNode = node;
                    _transitionIsDrag = false;
                    Repaint();
                }
            );
            menu.AddSeparator("");
            menu.AddItem(
                new GUIContent("Toggle Hub"),
                false,
                () =>
                {
                    Undo.RecordObject(_graph, "Toggle Hub");
                    node.isHub = !node.isHub;
                    EditorUtility.SetDirty(_graph);
                    AssetDatabase.SaveAssetIfDirty(_graph);
                    Repaint();
                }
            );
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(node));
            menu.ShowAsContext();
        }

        private void AddNewScene()
        {
            Undo.RecordObject(_graph, "Add Scene");

            var newNode = new SceneNode
            {
                id = $"scene_{_graph.scenes.Count}",
                displayName = "New Scene",
                sceneName = "NewScene",
                editorPosition = -_panOffset / _zoom + new Vector2(100, 100),
            };

            _graph.AddScene(newNode);
            _selectedNode = newNode;
            _selectedTransition = null;
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssetIfDirty(_graph);
            Repaint();
        }

        private void CreateTransition(SceneNode from, SceneNode to)
        {
            Undo.RecordObject(_graph, "Create Transition");

            var newTransition = new SceneTransition
            {
                fromSceneId = from.id,
                toSceneId = to.id,
                label = "Continue",
                conditions = new List<SceneCondition>(),
            };

            _graph.AddTransition(newTransition);
            _selectedTransition = newTransition;
            _selectedNode = null;
            _selectedNodes.Clear();
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssetIfDirty(_graph);
            Repaint();
        }

        private void CreateBidirectionalTransition(SceneNode node1, SceneNode node2)
        {
            Undo.RecordObject(_graph, "Create Bidirectional Transition");

            var newTransition = new SceneTransition
            {
                fromSceneId = node1.id,
                toSceneId = node2.id,
                label = "Continue",
                isBidirectional = true,
                conditions = new List<SceneCondition>(),
            };

            _graph.AddTransition(newTransition);
            _selectedTransition = newTransition;
            _selectedNode = null;
            _selectedNodes.Clear();
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssetIfDirty(_graph);
            Repaint();
        }

        private void DeleteNode(SceneNode node)
        {
            if (
                EditorUtility.DisplayDialog(
                    "Delete Node",
                    $"Delete scene '{node.displayName}'? This will also remove all transitions involving this scene.",
                    "Delete",
                    "Cancel"
                )
            )
            {
                Undo.RecordObject(_graph, "Delete Node");
                _graph.RemoveScene(node.id);
                _selectedNode = null;
                _selectedNodes.Clear();
                _selectedTransition = null;
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssetIfDirty(_graph);
                Repaint();
            }
        }

        private void DeleteMultipleNodes(HashSet<SceneNode> nodes)
        {
            if (
                EditorUtility.DisplayDialog(
                    "Delete Nodes",
                    $"Delete {nodes.Count} scenes? This will also remove all transitions involving these scenes.",
                    "Delete",
                    "Cancel"
                )
            )
            {
                Undo.RecordObject(_graph, "Delete Multiple Nodes");
                foreach (var node in nodes.ToArray())
                {
                    _graph.RemoveScene(node.id);
                }
                _selectedNode = null;
                _selectedNodes.Clear();
                _selectedTransition = null;
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssetIfDirty(_graph);
                Repaint();
            }
        }

        private void DeleteTransition(SceneTransition transition)
        {
            if (
                EditorUtility.DisplayDialog(
                    "Delete Transition",
                    "Delete this transition?",
                    "Delete",
                    "Cancel"
                )
            )
            {
                Undo.RecordObject(_graph, "Delete Transition");
                _graph.RemoveTransition(transition);
                _selectedTransition = null;
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssetIfDirty(_graph);
                Repaint();
            }
        }

        private void CenterView()
        {
            if (_graph.scenes == null || _graph.scenes.Count == 0)
            {
                return;
            }

            // Calculate center of all nodes
            Vector2 center = Vector2.zero;
            foreach (var node in _graph.scenes)
            {
                center += GetNodeCenter(node);
            }
            center /= _graph.scenes.Count;

            // Center the view on that point
            _panOffset =
                -center * _zoom
                + new Vector2(position.width / 2 - SIDEBAR_WIDTH / 2, position.height / 2);
            Repaint();
        }

        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Scene Flow Graph",
                "NewSceneFlowGraph",
                "asset",
                "Choose a location for the new Scene Flow Graph"
            );

            if (!string.IsNullOrEmpty(path))
            {
                var newGraph = CreateInstance<SceneFlowGraph>();
                newGraph.scenes = new List<SceneNode>();
                newGraph.transitions = new List<SceneTransition>();

                AssetDatabase.CreateAsset(newGraph, path);
                AssetDatabase.SaveAssets();

                _graph = newGraph;
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newGraph;
            }
        }

        private void DrawStatusBar(Rect rect)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var settings = Turnroot.GameSettings.GameplayGeneralSettings.Instance;
            if (settings != null)
            {
                var startDate = settings.StartingGameDate;
                var monthName = ((Turnroot.Utilities.Month)(startDate.month - 1)).ToString();
                GUILayout.Label(
                    $"Game Start: {monthName} {startDate.day}, Year {startDate.year}",
                    EditorStyles.toolbarButton
                );
            }
            else
            {
                GUILayout.Label(
                    "Game Start Date: (GameplayGeneralSettings not found)",
                    EditorStyles.toolbarButton
                );
            }

            GUILayout.FlexibleSpace();

            if (_transitionStartNode != null)
            {
                GUILayout.Label(
                    $"Creating transition from '{_transitionStartNode.displayName}' — click or drag to target node  |  Esc to cancel",
                    EditorStyles.toolbarButton
                );
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
#endif
