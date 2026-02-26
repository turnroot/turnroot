using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                        locked = mgp.FeatureLocked,
                        unlockItem = mgp.UnlockItem,
                        commonItem = mgp.FeatureCommonItem,
                        rareItem = mgp.FeatureRareItem,
                        warpDestinations = new List<Vector2Int>(mgp.WarpDestinations),
                        activeWarpIndex = mgp.ActiveWarpIndex,
                        breakableHealth = mgp.BreakableHealth,
                        healingPercentPerTurn = mgp.HealingPercentPerTurn,
                        rangedRange = mgp.RangedRange,
                        rangedDamage = mgp.RangedDamage,
                        rangedHit = mgp.RangedHit,
                        rangedAllowsRiding = mgp.RangedAllowsRiding,
                        rangedAllowsFlying = mgp.RangedAllowsFlying,
                        rangedMagicOnly = mgp.RangedMagicOnly,
                        shelterNoFly = mgp.ShelterNoFly,
                        shelterNoRide = mgp.ShelterNoRide,
                        shelterNoInfantry = mgp.ShelterNoInfantry,
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
                mgp.FeatureLocked = rec.locked;
                mgp.UnlockItem = rec.unlockItem;
                mgp.FeatureCommonItem = rec.commonItem;
                mgp.FeatureRareItem = rec.rareItem;
                mgp.WarpDestinations.Clear();
                if (rec.warpDestinations != null)
                {
                    mgp.WarpDestinations.AddRange(rec.warpDestinations);
                }
                mgp.ActiveWarpIndex = rec.activeWarpIndex;
                mgp.BreakableHealth = rec.breakableHealth;
                mgp.HealingPercentPerTurn = rec.healingPercentPerTurn;
                mgp.RangedRange = rec.rangedRange;
                mgp.RangedDamage = rec.rangedDamage;
                mgp.RangedHit = rec.rangedHit;
                mgp.RangedAllowsRiding = rec.rangedAllowsRiding;
                mgp.RangedAllowsFlying = rec.rangedAllowsFlying;
                mgp.RangedMagicOnly = rec.rangedMagicOnly;
                mgp.ShelterNoFly = rec.shelterNoFly;
                mgp.ShelterNoRide = rec.shelterNoRide;
                mgp.ShelterNoInfantry = rec.shelterNoInfantry;
            }
        }

        #endregion
    }
}
