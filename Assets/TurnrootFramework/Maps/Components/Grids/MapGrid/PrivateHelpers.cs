using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Combat;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGrid : MonoBehaviour
    {
        private void Awake()
        {
            // Rebuild dictionaries from existing children at runtime
            if (_gridPoints == null || _gridPoints.Count == 0)
            {
                if (transform.childCount > 0)
                {
                    RebuildGridDictionary();
                }
                // _single3dHeightMesh.SetActive(false);
            }

            // Ensure cache is built
            EnsureCachedGridPoints();

            // Rebuild raycast colors for gizmos in play mode
            if (
                _single3dHeightMeshRaycastPoints != null
                && _single3dHeightMeshRaycastPoints.Length > 0
            )
            {
                RebuildRaycastColors();
            }

            if (UseHeightMeshAsTerrainModel)
            {
                if (_single3dHeightMesh != null)
                {
                    _single3dHeightMesh.SetActive(UseHeightMeshAsTerrainModel);
                    // _single3dHeightMesh.GetComponent<Renderer>().renderingLayerMask = (uint)GetRenderingLayerMask.Get("Receive Map Grid Decals").value;
                }
            }
            else
            {
                if (TerrainLevelModel != null)
                {
                    TerrainLevelModel.SetActive(!UseHeightMeshAsTerrainModel);
                    // TerrainLevelModel.GetComponent<Renderer>().renderingLayerMask = (uint)GetRenderingLayerMask.Get("Receive Map Grid Decals").value;
                }
                else
                {
                    TurnrootLogger.Log(
                        "MapGrid: No TerrainLevelModel assigned while UseHeightMeshAsTerrainModel is false.",
                        TurnrootLogger.LogLevel.Error
                    );
                }
            }
        }

        private void CreateGridPoint(int row, int col)
        {
            var point = new GameObject($"Point_R{row}_C{col}");
            var gridPoint = point.AddComponent<MapGridPoint>();
            gridPoint.Initialize(row, col);
            SetDefaultTerrainType(gridPoint);

            point.transform.parent = transform;
            point.transform.localPosition =
                new Vector3(row * GridScale, 0, col * GridScale) + _gridOffset;

            var key = new Vector2Int(row, col);
            _gridPoints[key] = point;
            _cachedGridPoints ??= new Dictionary<Vector2Int, MapGridPoint>();
            _cachedGridPoints[key] = gridPoint;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(point);
            UnityEditor.EditorUtility.SetDirty(gridPoint);

            // Keep editor color cache up-to-date for this new point
            var tt = gridPoint.GetCachedTerrainType();
            var _col = tt?.EditorColor ?? Color.yellow;
            _gridPointColorCache[new Vector2Int(row, col)] = _col;
#endif
        }

        private void RemoveGridLine(int index, bool isRow)
        {
            int outerLimit = isRow ? GridWidth : GridHeight;

            for (int i = 0; i < outerLimit; i++)
            {
                var key = isRow ? new Vector2Int(i, index) : new Vector2Int(index, i);
                var mgp = GetGridPoint(key.x, key.y);
                if (mgp == null)
                {
                    continue;
                }

                _gridPoints.Remove(key);
                DestroyImmediate(mgp.gameObject);
            }
        }

        private void SetDefaultTerrainType(MapGridPoint gridPoint)
        {
            var terrainAsset = TerrainTypes.LoadDefault();
            if (terrainAsset?.Types == null)
            {
                return;
            }

            var voidType = terrainAsset.Types.FirstOrDefault(t =>
                t != null && t.Name.Equals("Void", System.StringComparison.OrdinalIgnoreCase)
            );

            if (voidType != null)
            {
                gridPoint.SetTerrainTypeId(voidType.Id);
            }
            else if (terrainAsset.Types.Length > 0 && terrainAsset.Types[0] != null)
            {
                gridPoint.SetTerrainTypeId(terrainAsset.Types[0].Id);
            }
        }

        public void EnsureGridPoints()
        {
            TurnrootLogger.Log("MapGrid: Ensuring grid points are created and positioned.");
            int expectedCount = GridWidth * GridHeight;
            int actualCount = transform
                .Cast<Transform>()
                .Count(child => child != null && child.TryGetComponent<MapGridPoint>(out _));

            bool needsRebuild =
                actualCount > 0
                && (actualCount != expectedCount || _gridPoints?.Count != expectedCount);
            bool needsCreate = actualCount == 0;

            if (needsRebuild)
            {
                RebuildGridDictionary();
                TurnrootLogger.Log("MapGrid: Rebuilt grid dictionary from existing children.");
            }
            else if (needsCreate)
            {
                CreateChildrenPoints();
                TurnrootLogger.Log("MapGrid: Created missing grid points.");
            }
            else if (_gridPoints?.Count == 0 && transform.childCount > 0)
            {
                RebuildGridDictionary();
                TurnrootLogger.Log("MapGrid: Rebuilt grid dictionary from existing children.");
            }

            RepositionGridPoints();
            TurnrootLogger.Log("MapGrid: Grid points ensured.");
        }

        private void RepositionGridPoints()
        {
            if (_gridPoints == null || _gridPoints.Count == 0)
            {
                return;
            }

            foreach (var kv in _gridPoints)
            {
                if (kv.Value == null)
                {
                    continue;
                }

                kv.Value.transform.localPosition =
                    new Vector3(kv.Key.x * GridScale, 0, kv.Key.y * GridScale) + _gridOffset;
            }
        }

        /// <summary>
        /// Ensures the cached MapGridPoint dictionary is populated.
        /// Call this before iterating over grid points to avoid repeated GetComponent calls.
        /// </summary>
        private void EnsureCachedGridPoints()
        {
            if (_cachedGridPoints != null && _cachedGridPoints.Count == _gridPoints.Count)
            {
                return;
            }

            _cachedGridPoints = new Dictionary<Vector2Int, MapGridPoint>(_gridPoints.Count);
            foreach (var kv in _gridPoints)
            {
                if (kv.Value != null && kv.Value.TryGetComponent<MapGridPoint>(out var mgp))
                {
                    _cachedGridPoints[kv.Key] = mgp;
                }
            }
        }

        private void RebuildRaycastColors()
        {
            if (
                _single3dHeightMeshRaycastPoints == null
                || _single3dHeightMeshRaycastPoints.Length == 0
            )
            {
                return;
            }

            EnsureCachedGridPoints();

            var colors = new Color[_single3dHeightMeshRaycastPoints.Length];
            var indices = new Vector2Int[_single3dHeightMeshRaycastPoints.Length];

            var orderedFinal = OrderGridPoints(_cachedGridPoints);
            int ci = 0;

            foreach (var kv in orderedFinal)
            {
                if (ci >= colors.Length)
                {
                    break;
                }

                var mgp = kv.Value;
                var tt = mgp?.SelectedTerrainType;
                colors[ci] = tt != null ? tt.EditorColor : Color.yellow;
                indices[ci] = kv.Key;
                ci++;
            }

            for (; ci < colors.Length; ci++)
            {
                colors[ci] = Color.yellow;
            }

            _single3dHeightMeshRaycastColors = colors;
            _single3dHeightMeshRaycastIndices = indices;

            // Build lookup dictionary for O(1) terrain position access
            BuildTerrainPositionLookup();
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (
                !UnityEditor.EditorApplication.isCompiling
                && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && !UnityEditor.EditorApplication.isUpdating
            )
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        void OnDrawGizmos()
        {
            Vector3 getPos(int x, int y) =>
                transform.position + new Vector3(x * GridScale, 0, y * GridScale) + _gridOffset;

            Vector3 topLeft = getPos(0, 0);
            Vector3 topRight = getPos(GridWidth - 1, 0);
            Vector3 bottomLeft = getPos(0, GridHeight - 1);
            Vector3 bottomRight = getPos(GridWidth - 1, GridHeight - 1);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            var corners = new[] { topLeft, topRight, bottomLeft, bottomRight };
            foreach (var corner in corners)
            {
                Gizmos.DrawSphere(corner, 1f);
            }

            if (_showRaycastGizmos)
            {
                float s = 0.25f * GridScale;

                // Prefer explicit raycast points when available (from a connected 3D height mesh)
                if (
                    _single3dHeightMeshRaycastPoints != null
                    && _single3dHeightMeshRaycastPoints.Length > 0
                )
                {
                    for (int i = 0; i < _single3dHeightMeshRaycastPoints.Length; i++)
                    {
                        var p = _single3dHeightMeshRaycastPoints[i];
                        var c =
                            (
                                _single3dHeightMeshRaycastColors != null
                                && i < _single3dHeightMeshRaycastColors.Length
                            )
                                ? _single3dHeightMeshRaycastColors[i]
                                : Color.magenta;
                        c.a = 1f;
                        Gizmos.color = c;
                        Gizmos.DrawSphere(p, s * (_showRaycastCoordinates ? 0.5f : 1f));
                        // add a Handle Label with coordinates
                        if (_showRaycastCoordinates)
                        {
                            Gizmos.color = Color.white;
                            UnityEditor.Handles.Label(
                                p + (Vector3.up * s * 2f),
                                _single3dHeightMeshRaycastIndices != null
                                && i < _single3dHeightMeshRaycastIndices.Length
                                    ? $"({_single3dHeightMeshRaycastIndices[i].x}, {_single3dHeightMeshRaycastIndices[i].y})"
                                    : "(?, ?)"
                            );
                        }
                    }
                }
                else if (_gridPoints != null && _gridPoints.Count > 0)
                {
                    // Performance guard: avoid expensive per-object checks and labels when grid is large.
                    bool heavy = _gridPoints.Count > 200;
                    bool showLabels = _showRaycastCoordinates && !heavy;

                    // Ensure we have map point references available quickly
                    EnsureCachedGridPoints();

                    foreach (var kv in _gridPoints)
                    {
                        var go = kv.Value;
                        if (go == null)
                        {
                            continue;
                        }

                        var p = go.transform.position;
                        Color c = Color.yellow;

#if UNITY_EDITOR
                        // Prefer the editor color cache first (fast, populated by editor tools)
                        if (TryGetEditorPointColor(kv.Key, out var cachedColor))
                        {
                            c = cachedColor;
                        }
                        // Then prefer the MapGrid's cached MapGridPoint lookup (cheap)
                        else if (
                            _cachedGridPoints != null
                            && _cachedGridPoints.TryGetValue(kv.Key, out var mgp)
                        )
                        {
                            var tt = mgp?.GetCachedTerrainType();
                            c = tt != null ? tt.EditorColor : Color.yellow;
                        }
                        // As a last resort (small grids), try component lookup
                        else if (!heavy && go.TryGetComponent<MapGridPoint>(out var mgp2))
                        {
                            var tt = mgp2?.SelectedTerrainType;
                            c = tt != null ? tt.EditorColor : Color.yellow;
                        }
#else
                        if (
                            _cachedGridPoints != null
                            && _cachedGridPoints.TryGetValue(kv.Key, out var mgp)
                        )
                        {
                            var tt = mgp?.GetCachedTerrainType();
                            c = tt != null ? tt.EditorColor : Color.yellow;
                        }
                        else if (!heavy && go.TryGetComponent<MapGridPoint>(out var mgp2))
                        {
                            var tt = mgp2?.SelectedTerrainType;
                            c = tt != null ? tt.EditorColor : Color.yellow;
                        }
#endif
                        c.a = 1f;
                        Gizmos.color = c;
                        Gizmos.DrawSphere(p, s * (showLabels ? 0.5f : 1f));

                        if (showLabels)
                        {
                            Gizmos.color = Color.white;
                            UnityEditor.Handles.Label(
                                p + (Vector3.up * s * 2f),
                                $"({kv.Key.x}, {kv.Key.y})"
                            );
                        }
                    }

                    if (heavy)
                    {
                        // Small visual indicator when full labeling is suppressed for performance.
                        Gizmos.color = Color.gray;
                        Gizmos.DrawSphere(
                            transform.position + (Vector3.up * 0.1f),
                            0.02f * GridScale
                        );
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (UseHeightMeshAsTerrainModel)
            {
                TerrainLevelModel = _single3dHeightMesh;
            }

            // get the BattleGameObject in the parent (includes self)
            battleGameObject = GetComponentInParent<BattleGameObject>();
            if (PlayerTeamSpawnPoints.Count > battleGameObject.MaxPlayerTeamUnits)
            {
                Debug.LogWarning(
                    $"MapGrid: Trimming PlayerTeamSpawnPoints to max allowed units ({battleGameObject.MaxPlayerTeamUnits})"
                );
                PlayerTeamSpawnPoints = PlayerTeamSpawnPoints
                    .Take(battleGameObject.MaxPlayerTeamUnits)
                    .ToList();
            }

            if (_gridPoints == null || _gridPoints.Count == 0)
            {
                if (transform.childCount > 0)
                {
                    RebuildGridDictionary();
                }
            }

            RepositionGridPoints();

            if (_features != null && _features.Count > 0)
            {
                LoadFeatureLayer();
            }

            if (
                _single3dHeightMeshRaycastPoints != null
                && _single3dHeightMeshRaycastPoints.Length > 0
            )
            {
                RebuildRaycastColors();
            }

#if UNITY_EDITOR
            // Keep editor color cache in sync when validating in the Inspector
            RebuildEditorPointColorCache();
#endif

            if (
                !UnityEditor.EditorApplication.isCompiling
                && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && !UnityEditor.EditorApplication.isUpdating
            )
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
    }
}
