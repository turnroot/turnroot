using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGridPoint : MonoBehaviour
    {
        // Cached parent grid reference to avoid repeated GetComponentInParent calls
        private MapGrid _cachedParentGrid;
        private bool _parentGridCached;

        // Cached terrain type to avoid repeated asset lookups during pathfinding
        private TerrainType _cachedTerrainType;
        private bool _terrainTypeCached;

        /// <summary>
        /// Gets the parent MapGrid, using a cached reference for performance.
        /// Call InvalidateParentCache() if the hierarchy changes.
        /// </summary>
        public MapGrid ParentGrid
        {
            get
            {
                if (!_parentGridCached)
                {
                    _cachedParentGrid = GetComponentInParent<MapGrid>();
                    _parentGridCached = true;
                }
                return _cachedParentGrid;
            }
        }

        /// <summary>
        /// Invalidate the cached parent grid reference. Call this if the
        /// MapGridPoint is reparented to a different grid.
        /// </summary>
        public void InvalidateParentCache()
        {
            _parentGridCached = false;
            _cachedParentGrid = null;
        }

        /// <summary>
        /// Invalidate the cached terrain type. Call this when the terrain type ID changes.
        /// </summary>
        public void InvalidateTerrainTypeCache()
        {
            _terrainTypeCached = false;
            _cachedTerrainType = null;
        }

        /// <summary>
        /// Gets the terrain type for this grid point. Does NOT use caching.
        /// For performance-critical code, use GetCachedTerrainType() instead.
        /// </summary>
        public TerrainType SelectedTerrainType
        {
            get
            {
                var asset = TerrainTypes.LoadDefault();
                if (asset == null)
                {
                    return null;
                }

                var terrainType = asset.GetTypeById(TerrainTypeId);
                return terrainType ?? (asset.Types?.Length > 0 ? asset.Types[0] : null);
            }
        }

        /// <summary>
        /// Gets the terrain type using cached lookup. Much faster for repeated calls
        /// during pathfinding. Cache is invalidated when SetTerrainTypeId is called.
        /// </summary>
        public TerrainType GetCachedTerrainType()
        {
            if (_terrainTypeCached)
            {
                return _cachedTerrainType;
            }

            var asset = TerrainTypes.LoadDefault();
            _cachedTerrainType =
                asset == null
                    ? null
                    : asset.GetTypeById(TerrainTypeId)
                        ?? (asset.Types?.Length > 0 ? asset.Types[0] : null);
            _terrainTypeCached = true;
            return _cachedTerrainType;
        }

        public void SetTerrainTypeId(string id)
        {
            _terrainTypeId = id ?? string.Empty;
            InvalidateTerrainTypeCache();
            ParentGrid?.IncrementStateVersion();
        }
    }
}
