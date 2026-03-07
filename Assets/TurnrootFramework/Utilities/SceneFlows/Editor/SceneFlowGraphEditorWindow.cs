#if UNITY_EDITOR
using System.Collections.Generic;
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

        // UI state
        private Vector2 _sidebarScroll;
        private const int SIDEBAR_WIDTH = 320;
        private const int NODE_WIDTH = 180;
        private const int NODE_HEIGHT = 60;

        // Transition creation
        private SceneNode _transitionStartNode;
        private bool _clickedEmptySpace;

        // Styles
        private GUIStyle _nodeStyle;
        private GUIStyle _nodeSelectedStyle;
        private GUIStyle _hubNodeStyle;
        private GUIStyle _hubNodeSelectedStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized;

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
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _nodeStyle = new GUIStyle("box")
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 8, 8),
                wordWrap = true,
            };
            _nodeStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 1f));
            _nodeStyle.normal.textColor = Color.white;

            _nodeSelectedStyle = new GUIStyle(_nodeStyle);
            _nodeSelectedStyle.normal.background = MakeTexture(
                2,
                2,
                new Color(0.2f, 0.5f, 0.8f, 1f)
            );

            _hubNodeStyle = new GUIStyle(_nodeStyle);
            _hubNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.5f, 0.3f, 0.6f, 1f));

            _hubNodeSelectedStyle = new GUIStyle(_hubNodeStyle);
            _hubNodeSelectedStyle.normal.background = MakeTexture(
                2,
                2,
                new Color(0.6f, 0.4f, 0.8f, 1f)
            );

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Normal,
            };
            _labelStyle.normal.textColor = Color.white;

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

            var graphRect = new Rect(0, 20, position.width - SIDEBAR_WIDTH, position.height - 20);
            var sidebarRect = new Rect(
                position.width - SIDEBAR_WIDTH,
                20,
                SIDEBAR_WIDTH,
                position.height - 20
            );

            DrawGraph(graphRect);
            DrawSidebar(sidebarRect);
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

                if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton))
                {
                    AutoLayoutNodes();
                }
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
            EditorGUI.DrawRect(graphRect, new Color(0.2f, 0.2f, 0.2f, 1f));

            // Handle input
            HandleGraphInput(graphRect);

            // Begin zoomed and panned area
            var zoomedRect = new Rect(
                graphRect.x + _panOffset.x,
                graphRect.y + _panOffset.y,
                graphRect.width * _zoom,
                graphRect.height * _zoom
            );

            GUILayout.BeginArea(graphRect);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(_panOffset, Quaternion.identity, Vector3.one * _zoom);

            // Draw grid
            DrawGrid();

            // Draw transitions first (so they appear behind nodes)
            DrawAllTransitions();

            // Draw nodes
            DrawAllNodes();

            GUI.matrix = oldMatrix;
            GUILayout.EndArea();

            // Draw connection line if creating transition
            if (_transitionStartNode != null)
            {
                var startPos = GetNodeCenter(_transitionStartNode);
                var nodeScreenPos = TransformToScreenSpace(startPos, graphRect);
                DrawConnectionLine(nodeScreenPos, Event.current.mousePosition, Color.yellow);
            }

            // Handle deselection on empty space click
            if (_clickedEmptySpace && Event.current.type == EventType.MouseDown)
            {
                _selectedNode = null;
                _selectedTransition = null;
                if (_transitionStartNode != null)
                {
                    _transitionStartNode = null;
                }
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
                if (_transitionStartNode != null)
                {
                    _transitionStartNode = null;
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseDown && e.button == 1)
                {
                    // Right-click on empty space - deselect
                    _selectedNode = null;
                    _selectedTransition = null;
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
                if (_selectedNode != null)
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

        private void DrawAllNodes()
        {
            if (_graph.scenes == null)
            {
                return;
            }

            foreach (var node in _graph.scenes)
            {
                DrawNode(node);
            }
        }

        private void DrawNode(SceneNode node)
        {
            var rect = GetNodeRect(node);
            var e = Event.current;

            // Choose style
            GUIStyle style;
            if (_selectedNode == node)
            {
                style = node.isHub ? _hubNodeSelectedStyle : _nodeSelectedStyle;
            }
            else
            {
                style = node.isHub ? _hubNodeStyle : _nodeStyle;
            }

            // Draw node box
            GUI.Box(rect, "", style);

            // Draw label
            var labelRect = new Rect(rect.x, rect.y + 5, rect.width, 20);
            GUI.Label(labelRect, node.displayName, _labelStyle);

            // Draw scene name (smaller)
            var sceneNameRect = new Rect(rect.x, rect.y + 25, rect.width, 15);
            var sceneStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
            };
            GUI.Label(sceneNameRect, node.sceneName, sceneStyle);

            // Hub indicator
            if (node.isHub)
            {
                var hubRect = new Rect(rect.x + 5, rect.y + 5, 15, 15);
                EditorGUI.DrawRect(hubRect, new Color(1f, 0.8f, 0f, 0.5f));
                GUI.Label(
                    hubRect,
                    "H",
                    new GUIStyle(_labelStyle) { fontSize = 11, fontStyle = FontStyle.Bold }
                );
            }

            // Starting scene indicator
            if (_graph.startingScene == node)
            {
                var startRect = new Rect(rect.x + rect.width - 20, rect.y + 5, 15, 15);
                EditorGUI.DrawRect(startRect, new Color(0f, 1f, 0f, 0.5f));
                GUI.Label(startRect, "▶", new GUIStyle(_labelStyle) { fontSize = 10 });
            }

            // Handle node interactions
            // Transform mouse position to match the zoomed/panned graph space
            var mousePos = e.mousePosition / _zoom - _panOffset / _zoom;
            
            if (rect.Contains(mousePos))
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    _clickedEmptySpace = false; // Clicked on a node, not empty space

                    if (_transitionStartNode != null)
                    {
                        // Complete transition creation
                        if (_transitionStartNode != node)
                        {
                            CreateTransition(_transitionStartNode, node);
                        }
                        _transitionStartNode = null;
                    }
                    else
                    {
                        // Start dragging
                        _selectedNode = node;
                        _selectedTransition = null;
                        _draggedNode = node;
                        _dragStartPos = e.mousePosition / _zoom - _panOffset / _zoom;
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
                    e.mousePosition / _zoom
                    - _panOffset / _zoom
                    - new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
                EditorUtility.SetDirty(_graph);
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (_isDragging && _draggedNode == node)
                {
                    // Save after dragging is complete
                    AssetDatabase.SaveAssetIfDirty(_graph);
                }
                _isDragging = false;
                _draggedNode = null;
            }
        }

        private void DrawAllTransitions()
        {
            if (_graph.transitions == null)
            {
                return;
            }

            foreach (var transition in _graph.transitions)
            {
                DrawTransition(transition);
            }
        }

        private void DrawTransition(SceneTransition transition)
        {
            var fromNode = _graph.GetScene(transition.fromSceneId);
            var toNode = _graph.GetScene(transition.toSceneId);

            if (fromNode == null || toNode == null)
            {
                return;
            }

            var fromPos = GetNodeCenter(fromNode);
            var toPos = GetNodeCenter(toNode);

            // Color based on selection and conditions
            Color lineColor = Color.white;
            if (_selectedTransition == transition)
            {
                lineColor = Color.cyan;
            }
            else if (transition.isBidirectional)
            {
                lineColor = new Color(0.5f, 1f, 0.5f);
            }
            else if (transition.conditions != null && transition.conditions.Count > 0)
            {
                lineColor = new Color(1f, 0.8f, 0.3f);
            }

            // Draw arrow
            DrawArrow(fromPos, toPos, lineColor, transition.isBidirectional);

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

            var transLabelStyle = new GUIStyle(_labelStyle) { fontSize = 10 };
            GUI.Label(labelRect, transition.label, transLabelStyle);

            // Check if clicking on transition - check label area or line proximity
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var mousePos = e.mousePosition / _zoom - _panOffset / _zoom;

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
                else if (IsPointNearLine(mousePos, fromPos, toPos, 15f))
                {
                    _clickedEmptySpace = false;
                    _selectedTransition = transition;
                    _selectedNode = null;
                    e.Use();
                    Repaint();
                }
            }
        }

        private void DrawGrid()
        {
            const float gridSpacing = 50f;
            const float thickLineInterval = 5; // Every 5th line is thicker

            Handles.BeginGUI();

            // Calculate visible area
            float startX = (-_panOffset.x / _zoom) - 1000;
            float endX = (position.width - SIDEBAR_WIDTH - _panOffset.x) / _zoom + 1000;
            float startY = (-_panOffset.y / _zoom) - 1000;
            float endY = (position.height - _panOffset.y) / _zoom + 1000;

            // Snap to grid
            startX = Mathf.Floor(startX / gridSpacing) * gridSpacing;
            startY = Mathf.Floor(startY / gridSpacing) * gridSpacing;

            // Draw vertical lines
            int lineCount = 0;
            for (float x = startX; x < endX; x += gridSpacing)
            {
                bool isThickLine = lineCount % thickLineInterval == 0;
                Handles.color = isThickLine
                    ? new Color(1f, 1f, 1f, 0.15f)
                    : new Color(1f, 1f, 1f, 0.05f);
                Handles.DrawLine(new Vector2(x, startY), new Vector2(x, endY));
                lineCount++;
            }

            // Draw horizontal lines
            lineCount = 0;
            for (float y = startY; y < endY; y += gridSpacing)
            {
                bool isThickLine = lineCount % thickLineInterval == 0;
                Handles.color = isThickLine
                    ? new Color(1f, 1f, 1f, 0.15f)
                    : new Color(1f, 1f, 1f, 0.05f);
                Handles.DrawLine(new Vector2(startX, y), new Vector2(endX, y));
                lineCount++;
            }

            Handles.EndGUI();
        }

        private void DrawArrow(Vector2 from, Vector2 to, Color color, bool bidirectional)
        {
            Handles.BeginGUI();
            Handles.color = color;

            Vector2 direction = (to - from).normalized;
            float distance = Vector2.Distance(from, to);

            // Shorten line to not overlap nodes
            Vector2 adjustedFrom = from + direction * (NODE_WIDTH / 2);
            Vector2 adjustedTo = to - direction * (NODE_WIDTH / 2);

            // Draw main line
            Handles.DrawLine(adjustedFrom, adjustedTo);

            // Draw arrowhead
            if (!bidirectional)
            {
                Vector2 arrowTip = adjustedTo;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                Vector2 arrowLeft = arrowTip - direction * 10 + perpendicular * 5;
                Vector2 arrowRight = arrowTip - direction * 10 - perpendicular * 5;

                Handles.DrawLine(arrowTip, arrowLeft);
                Handles.DrawLine(arrowTip, arrowRight);
            }
            else
            {
                // Draw arrows on both ends for bidirectional
                // Arrow at 'to' end
                Vector2 arrowTip1 = adjustedTo;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                Handles.DrawLine(arrowTip1, arrowTip1 - direction * 10 + perpendicular * 5);
                Handles.DrawLine(arrowTip1, arrowTip1 - direction * 10 - perpendicular * 5);

                // Arrow at 'from' end
                Vector2 arrowTip2 = adjustedFrom;
                Handles.DrawLine(arrowTip2, arrowTip2 + direction * 10 + perpendicular * 5);
                Handles.DrawLine(arrowTip2, arrowTip2 + direction * 10 - perpendicular * 5);
            }

            Handles.EndGUI();
        }

        private void DrawConnectionLine(Vector2 from, Vector2 to, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawLine(from, to);
            Handles.EndGUI();
        }

        private void DrawSidebar(Rect sidebarRect)
        {
            GUILayout.BeginArea(sidebarRect);
            EditorGUILayout.BeginVertical("box");

            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

            if (_selectedNode != null)
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
                "Click a node to select it.\nRight-click a node to create a transition.\nDelete key removes selected node/transition.",
                MessageType.Info
            );
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

            _selectedNode.id = EditorGUILayout.TextField("ID", _selectedNode.id);

            EditorGUILayout.Space();
            _selectedNode.isHub = EditorGUILayout.Toggle("Is Hub Scene", _selectedNode.isHub);
            _selectedNode.persistWhenLeaving = EditorGUILayout.Toggle(
                "Persist When Leaving",
                _selectedNode.persistWhenLeaving
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notes");
            _selectedNode.notes = EditorGUILayout.TextArea(
                _selectedNode.notes,
                GUILayout.Height(60)
            );

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
            }

            if (GUILayout.Button("Delete Node", GUILayout.Height(30)))
            {
                DeleteNode(_selectedNode);
            }
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
            EditorGUILayout.HelpBox(
                "Use the Inspector window to edit conditions in detail.",
                MessageType.Info
            );

            if (_selectedTransition.conditions != null && _selectedTransition.conditions.Count > 0)
            {
                foreach (var condition in _selectedTransition.conditions)
                {
                    EditorGUILayout.LabelField($"• {condition}", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField(
                    "No conditions (always available)",
                    EditorStyles.miniLabel
                );
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

        private void AutoLayoutNodes()
        {
            if (_graph.scenes == null || _graph.scenes.Count == 0)
            {
                return;
            }

            Undo.RecordObject(_graph, "Auto Layout");

            // Simple circular layout
            int count = _graph.scenes.Count;
            float radius = 200f;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                _graph.scenes[i].editorPosition = pos;
            }

            EditorUtility.SetDirty(_graph);
            CenterView();
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
    }
}
#endif
