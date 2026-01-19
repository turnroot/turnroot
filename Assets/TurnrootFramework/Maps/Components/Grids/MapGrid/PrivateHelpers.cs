using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Combat;
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

            TerrainLevelModel?.SetActive(!UseHeightMeshAsTerrainModel);

            if (_single3dHeightMesh != null)
            {
                _single3dHeightMesh.SetActive(UseHeightMeshAsTerrainModel);
            }
            else if (UseHeightMeshAsTerrainModel)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    "MapGrid: Neither a 3D height mesh nor a terrain level model is assigned."
                );
#endif
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

        /// <summary>
        /// Convert a list of properties to PropertyRecords, returning null if the source is empty.
        /// This avoids allocating empty lists which saves memory and serialization overhead.
        /// </summary>
        private static List<PropertyRecord<TOut>> ConvertPropertiesIfAny<TIn, TOut>(
            List<TIn> source,
            System.Func<TIn, PropertyRecord<TOut>> converter
        )
        {
            if (source == null || source.Count == 0)
            {
                return null;
            }

            var result = new List<PropertyRecord<TOut>>(source.Count);
            foreach (var item in source)
            {
                result.Add(converter(item));
            }
            return result;
        }

        public void LoadFeatureLayer()
        {
            if (_features == null || _features.Count == 0)
            {
                return;
            }

            foreach (var rec in _features)
            {
                var mgp = GetGridPoint(rec.row, rec.col);
                if (mgp == null)
                {
                    continue;
                }

                mgp.SetFeatureTypeId(rec.typeId);
                mgp.FeatureName = rec.name ?? string.Empty;
                mgp.ApplyDefaultsForFeature(rec.typeId);
                ApplyPropertyList(rec.boolProperties, mgp.SetBoolFeatureProperty);
                ApplyPropertyList(rec.eventProperties, mgp.SetEventFeatureProperty);
                ApplyPropertyList(rec.floatProperties, mgp.SetFloatFeatureProperty);
                ApplyPropertyList(rec.unitProperties, mgp.SetUnitFeatureProperty);
                ApplyPropertyList(rec.objectItemProperties, mgp.SetObjectItemFeatureProperty);
            }
        }

        private void ApplyPropertyList<T>(
            List<PropertyRecord<T>> properties,
            System.Action<string, T> setter
        )
        {
            if (properties == null)
            {
                return;
            }

            foreach (var pr in properties)
            {
                if (!string.IsNullOrEmpty(pr.key))
                {
                    setter(pr.key, pr.value);
                }
            }
        }

        public void EnsureGridPoints()
        {
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
            }
            else if (needsCreate)
            {
                CreateChildrenPoints();
            }
            else if (_gridPoints?.Count == 0 && transform.childCount > 0)
            {
                RebuildGridDictionary();
            }

            RepositionGridPoints();
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

        private IOrderedEnumerable<KeyValuePair<Vector2Int, TValue>> OrderGridPoints<TValue>(
            IEnumerable<KeyValuePair<Vector2Int, TValue>> points
        ) =>
            // Use consistent ordering without flipping
            points.OrderBy(kv => kv.Key.x).ThenBy(kv => kv.Key.y);

        /// <summary>
        /// Builds the terrain position lookup dictionary for O(1) access to raycast points
        /// </summary>
        private void BuildTerrainPositionLookup()
        {
            if (
                _single3dHeightMeshRaycastPoints == null
                || _single3dHeightMeshRaycastIndices == null
                || _single3dHeightMeshRaycastPoints.Length
                    != _single3dHeightMeshRaycastIndices.Length
            )
            {
                _terrainPositionLookup?.Clear();
                return;
            }

            _terrainPositionLookup = new Dictionary<Vector2Int, Vector3>(
                _single3dHeightMeshRaycastIndices.Length
            );
            for (int i = 0; i < _single3dHeightMeshRaycastIndices.Length; i++)
            {
                var key = _single3dHeightMeshRaycastIndices[i];
                // In case of duplicate keys, last one wins (matches previous linear search semantics)
                _terrainPositionLookup[key] = _single3dHeightMeshRaycastPoints[i];
            }
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

            if (
                _showRaycastGizmos
                && _single3dHeightMeshRaycastPoints != null
                && _single3dHeightMeshRaycastPoints.Length > 0
            )
            {
                float s = Mathf.Max(0.2f, GridScale * 0.4f);
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
                    if (!_showRaycastCoordinates)
                    {
                        continue;
                    }

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
            // Draw a rectangle for the traversable area
            if (TraversableAreaCorners != null && TraversableAreaCorners.Length == 4)
            {
                Vector3 c1 = getPos(TraversableAreaCorners[0].x, TraversableAreaCorners[0].y);
                Vector3 c2 = getPos(TraversableAreaCorners[1].x, TraversableAreaCorners[1].y);
                Vector3 c3 = getPos(TraversableAreaCorners[2].x, TraversableAreaCorners[2].y);
                Vector3 c4 = getPos(TraversableAreaCorners[3].x, TraversableAreaCorners[3].y);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(c1, c2);
                Gizmos.DrawLine(c2, c4);
                Gizmos.DrawLine(c4, c3);
                Gizmos.DrawLine(c3, c1);
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
