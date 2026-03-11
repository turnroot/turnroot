using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGrid : MonoBehaviour
    {
        #region Caching & Raycast

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

        #endregion
    }
}
