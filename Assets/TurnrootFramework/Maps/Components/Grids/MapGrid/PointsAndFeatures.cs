using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGrid : MonoBehaviour
    {
        #region Grid Point Access
        public MapGridPoint GetGridPoint(int row, int col)
        {
            var key = new Vector2Int(row, col);

            if (_cachedGridPoints?.TryGetValue(key, out var cached) == true && cached != null)
            {
                return cached;
            }

            if (
                _gridPoints.TryGetValue(key, out var point)
                && point != null
                && point.TryGetComponent<MapGridPoint>(out var mgp)
            )
            {
                _cachedGridPoints ??= new Dictionary<Vector2Int, MapGridPoint>();
                _cachedGridPoints[key] = mgp;
                return mgp;
            }

            return null;
        }

        public List<MapGridPoint> GetAllGridPoints()
        {
            EnsureCachedGridPoints();

            var points = new List<MapGridPoint>(_cachedGridPoints.Count);
            foreach (var mgp in _cachedGridPoints.Values)
            {
                if (mgp != null)
                {
                    points.Add(mgp);
                }
            }
            return points;
        }

        public List<MapGridPoint> GetAllGridPointsByFeatureType(
            MapGridPointFeature.FeatureType featureType
        )
        {
            EnsureCachedGridPoints();

            var points = new List<MapGridPoint>();
            foreach (var mgp in _cachedGridPoints.Values)
            {
                if (mgp?.FeatureType == featureType)
                {
                    points.Add(mgp);
                }
            }
            return points.Count > 0 ? points : null;
        }

        public Vector3 GetMapGridPointWorldLocation(MapGridPoint gridPoint)
        {
            var key = new Vector2Int(gridPoint.Row, gridPoint.Col);
            return _gridPoints.TryGetValue(key, out var point) && point != null
                ? point.transform.position
                : Vector3.zero;
        }

        public Vector3 GetTerrainAdjustedWorldPosition(Vector2Int gridCoordinates)
        {
            if (_terrainPositionLookup?.TryGetValue(gridCoordinates, out var terrainPos) == true)
            {
                return terrainPos;
            }

            var gridPoint = GetGridPoint(gridCoordinates.x, gridCoordinates.y);
            return gridPoint != null ? GetMapGridPointWorldLocation(gridPoint) : Vector3.zero;
        }

        public int GetManhattanDistance(MapGridPoint a, MapGridPoint b) =>
            a == null || b == null ? -1 : Mathf.Abs(a.Row - b.Row) + Mathf.Abs(a.Col - b.Col);
        #endregion

        #region Feature Layer Management
        public void SaveFeatureLayer()
        {
            EnsureCachedGridPoints();
            _features.Clear();

            foreach (var (key, mgp) in _cachedGridPoints)
            {
                if (mgp == null || string.IsNullOrEmpty(mgp.FeatureTypeId))
                {
                    continue;
                }

                _features.Add(
                    new FeatureRecord
                    {
                        row = key.x,
                        col = key.y,
                        typeId = mgp.FeatureTypeId,
                        name = mgp.FeatureName,
                        boolProperties = ConvertPropertiesIfAny(
                            mgp.GetAllBoolFeatureProperties(),
                            p => new PropertyRecord<bool> { key = p.key, value = p.value }
                        ),
                        eventProperties = ConvertPropertiesIfAny(
                            mgp.GetAllEventFeatureProperties(),
                            p => new PropertyRecord<UnityEvent> { key = p.key, value = p.value }
                        ),
                        floatProperties = ConvertPropertiesIfAny(
                            mgp.GetAllFloatFeatureProperties(),
                            p => new PropertyRecord<float> { key = p.key, value = p.value }
                        ),
                        unitProperties = ConvertPropertiesIfAny(
                            mgp.GetAllUnitFeatureProperties(),
                            p => new PropertyRecord<CharacterInstance>
                            {
                                key = p.key,
                                value = p.value,
                            }
                        ),
                        objectItemProperties = ConvertPropertiesIfAny(
                            mgp.GetAllObjectItemFeatureProperties(),
                            p => new PropertyRecord<ObjectItemInstance>
                            {
                                key = p.key,
                                value = p.value,
                            }
                        ),
                    }
                );
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private IOrderedEnumerable<KeyValuePair<Vector2Int, TValue>> OrderGridPoints<TValue>(
            IEnumerable<KeyValuePair<Vector2Int, TValue>> points
        ) => points.OrderBy(kv => kv.Key.x).ThenBy(kv => kv.Key.y);

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
                _terrainPositionLookup[key] = _single3dHeightMeshRaycastPoints[i];
            }
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
        #endregion
    }
}
