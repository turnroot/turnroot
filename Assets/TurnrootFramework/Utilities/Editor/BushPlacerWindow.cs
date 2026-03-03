using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.Editor
{
    public enum BushPlacerMode
    {
        Bushes,
        Forest,
    }

    /// <summary>
    /// Tools -> Turnroot -> Bush Placer
    /// Places bush prefabs on every MapGridPoint whose terrain type name is "Bushes".
    /// </summary>
    public class BushPlacerWindow : EditorWindow
    {
        // ── Prefab list ──────────────────────────────────────────────────────────
        private readonly List<GameObject> _prefabs = new();

        private readonly List<GameObject> _forestTopDownPrefabs = new();
        private readonly List<GameObject> _forestCinematicPrefabs = new();

        private Vector2Int TreesPerForestTile = new Vector2Int(3, 5);

        private Vector2 ForestTreeSpacing = new Vector2(1f, 2f);

        private BushPlacerMode _mode = BushPlacerMode.Bushes;

        private Vector2 _prefabListScroll;
        private Vector2 _forestTopDownPrefabListScroll;
        private Vector2 _forestCinematicPrefabListScroll;

        // ── Settings ─────────────────────────────────────────────────────────────
        private float _scale = 5f;
        private float _scaleVariation = 1f;

        // ── Scene references ──────────────────────────────────────────────────────
        private Transform _bushesParent;

        private Transform _forestsTopDownParent;
        private Transform _forestsCinematicParent;
        private MapGrid _mapGrid;

        // ── Styles (lazy init) ────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private bool _stylesInitialised;

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Tools/Turnroot/Bush and Forest Placer")]
        public static void ShowWindow()
        {
            var win = GetWindow<BushPlacerWindow>("Bush/Forest Placer");
            win.minSize = new Vector2(360f, 520f);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void InitStyles()
        {
            if (_stylesInitialised)
            {
                return;
            }

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _stylesInitialised = true;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(6);

            _mode = (BushPlacerMode)EditorGUILayout.EnumPopup("Mode", _mode);

            EditorGUILayout.Space(6);

            // ── Scale (shared) ────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Scale Settings", _headerStyle);

            _scale = EditorGUILayout.Slider(
                new GUIContent("Scale", "Uniform scale applied to every placed instance."),
                _scale,
                1f,
                20f
            );

            _scaleVariation = EditorGUILayout.Slider(
                new GUIContent(
                    "Scale Variation",
                    "Each instance scale is offset by a random amount in [-variation, +variation]."
                ),
                _scaleVariation,
                0f,
                10f
            );

            EditorGUILayout.Space(6);

            // ── Map Grid (shared) ─────────────────────────────────────────────────
            _mapGrid = (MapGrid)
                EditorGUILayout.ObjectField(
                    new GUIContent("Map Grid", "The MapGrid whose points will be scanned."),
                    _mapGrid,
                    typeof(MapGrid),
                    allowSceneObjects: true
                );

            EditorGUILayout.Space(8);

            // ── Mode-specific ─────────────────────────────────────────────────────
            if (_mode == BushPlacerMode.Bushes)
            {
                DrawBushMode();
            }
            else
            {
                DrawForestMode();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void DrawBushMode()
        {
            EditorGUILayout.LabelField("Bush Prefabs", _headerStyle);
            EditorGUILayout.HelpBox(
                "Drag prefabs into the list. One will be chosen at random per Bushes grid point.",
                MessageType.None
            );
            DrawPrefabList(_prefabs, ref _prefabListScroll);

            EditorGUILayout.Space(6);

            _bushesParent = (Transform)
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Bushes Parent",
                        "Parent transform that spawned bushes will be childed to."
                    ),
                    _bushesParent,
                    typeof(Transform),
                    allowSceneObjects: true
                );

            EditorGUILayout.Space(10);

            bool canPlace = _prefabs.Count > 0 && _mapGrid != null;
            using (new EditorGUI.DisabledGroupScope(!canPlace))
            {
                if (GUILayout.Button("Place Bushes", GUILayout.Height(36)))
                {
                    PlaceBushes();
                }
            }
            if (!canPlace)
            {
                EditorGUILayout.HelpBox(
                    "Add at least one prefab and assign a Map Grid to enable placement.",
                    MessageType.Warning
                );
            }
        }

        private void DrawForestMode()
        {
            // ── Top-Down prefabs ──────────────────────────────────────────────
            EditorGUILayout.LabelField("Forests (Top-Down) Prefabs", _headerStyle);
            EditorGUILayout.HelpBox(
                "Prefabs for the top-down view layer. One is chosen at random per tree spawn.",
                MessageType.None
            );
            DrawPrefabList(_forestTopDownPrefabs, ref _forestTopDownPrefabListScroll);

            EditorGUILayout.Space(6);

            _forestsTopDownParent = (Transform)
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Forests (Top-Down) Parent",
                        "Parent transform for top-down forest instances."
                    ),
                    _forestsTopDownParent,
                    typeof(Transform),
                    allowSceneObjects: true
                );

            EditorGUILayout.Space(10);

            // ── Cinematic prefabs ─────────────────────────────────────────────
            EditorGUILayout.LabelField("Forests (Cinematic) Prefabs", _headerStyle);
            EditorGUILayout.HelpBox(
                "Prefabs for the cinematic/combat view layer. Placed at the exact same position, rotation, and scale as the top-down counterpart.",
                MessageType.None
            );
            DrawPrefabList(_forestCinematicPrefabs, ref _forestCinematicPrefabListScroll);

            EditorGUILayout.Space(6);

            _forestsCinematicParent = (Transform)
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Forests (Cinematic) Parent",
                        "Parent transform for cinematic forest instances."
                    ),
                    _forestsCinematicParent,
                    typeof(Transform),
                    allowSceneObjects: true
                );

            EditorGUILayout.Space(10);

            // ── Forest placement settings ─────────────────────────────────────
            EditorGUILayout.LabelField("Forest Settings", _headerStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                new GUIContent(
                    "Trees Per Tile",
                    "Min and max number of trees placed per Forest tile."
                ),
                GUILayout.Width(EditorGUIUtility.labelWidth)
            );
            int newMin = EditorGUILayout.IntField(TreesPerForestTile.x, GUILayout.Width(40));
            EditorGUILayout.LabelField("to", GUILayout.Width(20));
            int newMax = EditorGUILayout.IntField(TreesPerForestTile.y, GUILayout.Width(40));
            TreesPerForestTile = new Vector2Int(
                Mathf.Max(1, newMin),
                Mathf.Max(Mathf.Max(1, newMin), newMax)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                new GUIContent(
                    "Scatter Radius",
                    "Min and max XZ scatter radius (world units) from the tile centre."
                ),
                GUILayout.Width(EditorGUIUtility.labelWidth)
            );
            float newSpMin = EditorGUILayout.FloatField(ForestTreeSpacing.x, GUILayout.Width(40));
            EditorGUILayout.LabelField("to", GUILayout.Width(20));
            float newSpMax = EditorGUILayout.FloatField(ForestTreeSpacing.y, GUILayout.Width(40));
            ForestTreeSpacing = new Vector2(
                Mathf.Max(0f, newSpMin),
                Mathf.Max(Mathf.Max(0f, newSpMin), newSpMax)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            bool canPlace =
                (_forestTopDownPrefabs.Count > 0 || _forestCinematicPrefabs.Count > 0)
                && _mapGrid != null;
            using (new EditorGUI.DisabledGroupScope(!canPlace))
            {
                if (GUILayout.Button("Place Forest", GUILayout.Height(36)))
                {
                    PlaceForest();
                }
            }
            if (!canPlace)
            {
                EditorGUILayout.HelpBox(
                    "Add at least one prefab in either list and assign a Map Grid to enable placement.",
                    MessageType.Warning
                );
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void DrawPrefabList(List<GameObject> prefabs, ref Vector2 scroll)
        {
            float listHeight = Mathf.Clamp(prefabs.Count * 22f + 8f, 44f, 160f);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(listHeight));

            if (prefabs.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "(no prefabs added)",
                    EditorStyles.centeredGreyMiniLabel
                );
            }

            int removeAt = -1;
            for (int i = 0; i < prefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                prefabs[i] = (GameObject)
                    EditorGUILayout.ObjectField(
                        prefabs[i],
                        typeof(GameObject),
                        allowSceneObjects: false
                    );
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    removeAt = i;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0)
            {
                prefabs.RemoveAt(removeAt);
            }

            EditorGUILayout.EndScrollView();

            HandleDragDrop(prefabs);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Add Slot"))
                {
                    prefabs.Add(null);
                }
                using (new EditorGUI.DisabledGroupScope(prefabs.Count == 0))
                {
                    if (GUILayout.Button("Clear All"))
                    {
                        prefabs.Clear();
                    }
                }
            }
        }

        private void HandleDragDrop(List<GameObject> prefabs)
        {
            var dropRect = GUILayoutUtility.GetLastRect();
            var evt = Event.current;
            if (evt == null)
            {
                return;
            }

            if (
                (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                && dropRect.Contains(evt.mousePosition)
            )
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject go)
                        {
                            prefabs.Add(go);
                        }
                    }
                    evt.Use();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void PlaceForest()
        {
            _forestTopDownPrefabs.RemoveAll(p => p == null);
            _forestCinematicPrefabs.RemoveAll(p => p == null);

            if (_forestTopDownPrefabs.Count == 0 && _forestCinematicPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Bush/Forest Placer",
                    "No valid forest prefabs in either list.",
                    "OK"
                );
                return;
            }

            int width = _mapGrid.GridWidth;
            int height = _mapGrid.GridHeight;
            int placed = 0;

            Undo.SetCurrentGroupName("Place Forest");
            int undoGroup = Undo.GetCurrentGroup();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    MapGridPoint point = _mapGrid.GetGridPoint(x, y);
                    if (point == null)
                        continue;

                    TerrainType terrain = point.GetCachedTerrainType();
                    if (terrain == null)
                        continue;

                    string terrainName = terrain.Name.Replace(" ", "").ToLowerInvariant();
                    if (terrainName != "forest" && terrainName != "forests")
                        continue;

                    Vector3 tilePos = _mapGrid.GetTerrainAdjustedWorldPosition(
                        new Vector2Int(x, y)
                    );

                    int treeCount = Random.Range(TreesPerForestTile.x, TreesPerForestTile.y + 1);

                    for (int t = 0; t < treeCount; t++)
                    {
                        // Compute shared transform values once so both instances are identical
                        float radius = Random.Range(ForestTreeSpacing.x, ForestTreeSpacing.y);
                        float angle = Random.Range(0f, Mathf.PI * 2f);
                        Vector3 offset = new Vector3(
                            Mathf.Cos(angle) * radius,
                            0f,
                            Mathf.Sin(angle) * radius
                        );
                        Vector3 spawnPos = tilePos + offset;
                        Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        float variation = Random.Range(-_scaleVariation, _scaleVariation);
                        float finalScale = Mathf.Max(0.01f, _scale + variation);

                        // ── Top-Down instance ────────────────────────────────
                        if (_forestTopDownPrefabs.Count > 0)
                        {
                            GameObject tdPrefab = _forestTopDownPrefabs[
                                Random.Range(0, _forestTopDownPrefabs.Count)
                            ];
                            if (tdPrefab != null)
                            {
                                GameObject tdInstance = (GameObject)
                                    PrefabUtility.InstantiatePrefab(tdPrefab);
                                if (tdInstance == null)
                                    tdInstance = Instantiate(tdPrefab);

                                tdInstance.transform.position = spawnPos;
                                tdInstance.transform.rotation = spawnRot;
                                tdInstance.transform.localScale = Vector3.one * finalScale;

                                if (_forestsTopDownParent != null)
                                    tdInstance.transform.SetParent(
                                        _forestsTopDownParent,
                                        worldPositionStays: true
                                    );

                                Undo.RegisterCreatedObjectUndo(tdInstance, "Place Tree (Top-Down)");
                                placed++;
                            }
                        }

                        // ── Cinematic instance ───────────────────────────────
                        if (_forestCinematicPrefabs.Count > 0)
                        {
                            GameObject cinPrefab = _forestCinematicPrefabs[
                                Random.Range(0, _forestCinematicPrefabs.Count)
                            ];
                            if (cinPrefab != null)
                            {
                                GameObject cinInstance = (GameObject)
                                    PrefabUtility.InstantiatePrefab(cinPrefab);
                                if (cinInstance == null)
                                    cinInstance = Instantiate(cinPrefab);

                                cinInstance.transform.position = spawnPos;
                                cinInstance.transform.rotation = spawnRot;
                                cinInstance.transform.localScale = Vector3.one * finalScale;

                                if (_forestsCinematicParent != null)
                                    cinInstance.transform.SetParent(
                                        _forestsCinematicParent,
                                        worldPositionStays: true
                                    );

                                Undo.RegisterCreatedObjectUndo(
                                    cinInstance,
                                    "Place Tree (Cinematic)"
                                );
                                placed++;
                            }
                        }
                    }
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                $"[Bush/Forest Placer] Placed {placed} tree instance(s) on '{_mapGrid.name}'."
            );
            EditorUtility.DisplayDialog(
                "Bush/Forest Placer",
                $"Done! Placed {placed} tree instance(s).\nAll placements can be undone with Ctrl+Z.",
                "OK"
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void PlaceBushes()
        {
            // Remove nulls from prefab list
            _prefabs.RemoveAll(p => p == null);
            if (_prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("Bush Placer", "No valid prefabs in the list.", "OK");
                return;
            }

            int width = _mapGrid.GridWidth;
            int height = _mapGrid.GridHeight;
            int placed = 0;

            Undo.SetCurrentGroupName("Place Bushes");
            int undoGroup = Undo.GetCurrentGroup();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    MapGridPoint point = _mapGrid.GetGridPoint(x, y);
                    if (point == null)
                    {
                        continue;
                    }

                    TerrainType terrain = point.GetCachedTerrainType();
                    if (terrain == null)
                    {
                        continue;
                    }

                    // Case-insensitive match; strips spaces so "Bush" or "Bushes" or "bush" all work
                    string terrainName = terrain.Name.Replace(" ", "").ToLowerInvariant();
                    if (terrainName != "bushes" && terrainName != "bush")
                    {
                        continue;
                    }

                    // Pick a random prefab
                    GameObject prefab = _prefabs[Random.Range(0, _prefabs.Count)];
                    if (prefab == null)
                    {
                        continue;
                    }

                    // Instantiate
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    if (instance == null)
                    {
                        instance = Instantiate(prefab);
                    }

                    // Position at terrain-height-adjusted world position
                    instance.transform.position = _mapGrid.GetTerrainAdjustedWorldPosition(
                        new Vector2Int(x, y)
                    );

                    // Random Y rotation
                    instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    // Scale with variation
                    float variation = Random.Range(-_scaleVariation, _scaleVariation);
                    float finalScale = Mathf.Max(0.01f, _scale + variation);
                    instance.transform.localScale = Vector3.one * finalScale;

                    // Parent
                    if (_bushesParent != null)
                    {
                        instance.transform.SetParent(_bushesParent, worldPositionStays: true);
                    }

                    Undo.RegisterCreatedObjectUndo(instance, "Place Bush");
                    placed++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"[Bush Placer] Placed {placed} bush instance(s) on '{_mapGrid.name}'.");
            EditorUtility.DisplayDialog(
                "Bush Placer",
                $"Done! Placed {placed} bush instance(s).\nAll placements can be undone with Ctrl+Z.",
                "OK"
            );
        }
    }
}
