using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using Turnroot.Maps.Components.Grids;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Represents a single grid cell on the map with terrain type, spawn point data, and neighbor connectivity.
    /// </summary>
    public partial class MapGridPoint : MonoBehaviour
    {
        // Cached parent grid reference to avoid repeated GetComponentInParent calls
        private MapGrid _cachedParentGrid;
        private bool _parentGridCached;

        // Cached terrain type to avoid repeated asset lookups during pathfinding
        private TerrainType _cachedTerrainType;
        private bool _terrainTypeCached;

        [SerializeField]
        private SpawnPoint _spawnPoint = new();
        public SpawnPoint SpawnPoint
        {
            get => _spawnPoint;
            set => _spawnPoint = value ?? new SpawnPoint();
        }

        private void OnValidate()
        {
            SpawnPoint ??= new SpawnPoint();
            // Invalidate caches when inspector changes
            _terrainTypeCached = false;
        }

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

        /* ---------------------------- Grid point data ---------------------------- */
        private static readonly (string name, int dRow, int dCol)[] Directions = new[]
        {
            ("N", -1, 0),
            ("NE", -1, 1),
            ("E", 0, 1),
            ("SE", 1, 1),
            ("S", 1, 0),
            ("SW", 1, -1),
            ("W", 0, -1),
            ("NW", -1, -1),
        };

        private static readonly (string name, int dRow, int dCol)[] CardinalDirections = new[]
        {
            ("N", -1, 0),
            ("E", 0, 1),
            ("S", 1, 0),
            ("W", 0, -1),
        };

        private const float GizmoRadius = 0.35f;

        [SerializeField]
        [Tooltip("Feature display name (optional).")]
        private string _featureName = string.Empty;

        // Starting unit tracking (unrelated to the old property system)
        private CharacterInstance _startingUnit = null;

        [HideInInspector, NonSerialized]
        /// <summary>
        /// The character instance currently occupying this grid point.
        /// This should be set when a character enters the grid point and cleared when the character leaves.
        /// Used to track which character, if any, is present at this location.
        /// </summary>
        public CharacterInstance CurrentInstance;

        /// <summary>
        /// Indicates whether a character is currently occupying this grid point.
        /// Returns true if <see cref="CurrentInstance"/> is not null.
        /// </summary>
        public bool IsOccupied => CurrentInstance != null;

        // ---------------------------------------------------------------------
        // Feature-specific state
        // ---------------------------------------------------------------------

        [SerializeField]
        private bool _featureLocked = false;

        [SerializeField]
        private ObjectItem _unlockItem = null;

        [SerializeField]
        private ObjectItem _featureCommonItem = null;

        [SerializeField]
        private ObjectItem _featureRareItem = null;

        [SerializeField]
        private List<Vector2Int> _warpDestinations = new();

        [SerializeField]
        private int _activeWarpIndex = 0;

        // shelter targeting restrictions
        [SerializeField]
        private bool _shelterNoFly = false;

        [SerializeField]
        private bool _shelterNoRide = false;

        [SerializeField]
        private bool _shelterNoInfantry = false;

        // breakable-specific state
        [SerializeField]
        private int _breakableHealth = 0;

        // healing-specific state
        [SerializeField]
        private float _healingPercentPerTurn = 0f;

        // ranged-specific state
        [SerializeField]
        private int _rangedRange = 0;

        [SerializeField]
        private int _rangedDamage = 0;

        [SerializeField]
        private float _rangedHit = 0f;

        [SerializeField]
        private bool _rangedAllowsRiding = false;

        [SerializeField]
        private bool _rangedAllowsFlying = false;

        [SerializeField]
        private bool _rangedMagicOnly = false;

        /// <summary>
        /// Indicates whether a door feature placed on this point is locked.
        /// Only meaningful when <see cref="FeatureType"/> == <see cref="MapGridPointFeature.FeatureType.Door"/>.
        /// </summary>
        public bool FeatureLocked
        {
            get => _featureLocked;
            set
            {
                _featureLocked = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public ObjectItem UnlockItem
        {
            get => _unlockItem;
            set
            {
                _unlockItem = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool ShelterNoFly
        {
            get => _shelterNoFly;
            set
            {
                _shelterNoFly = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool ShelterNoRide
        {
            get => _shelterNoRide;
            set
            {
                _shelterNoRide = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool ShelterNoInfantry
        {
            get => _shelterNoInfantry;
            set
            {
                _shelterNoInfantry = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public int BreakableHealth
        {
            get => _breakableHealth;
            set
            {
                _breakableHealth = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public float HealingPercentPerTurn
        {
            get => _healingPercentPerTurn;
            set
            {
                _healingPercentPerTurn = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public int RangedRange
        {
            get => _rangedRange;
            set
            {
                _rangedRange = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public int RangedDamage
        {
            get => _rangedDamage;
            set
            {
                _rangedDamage = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public float RangedHit
        {
            get => _rangedHit;
            set
            {
                _rangedHit = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool RangedAllowsRiding
        {
            get => _rangedAllowsRiding;
            set
            {
                _rangedAllowsRiding = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool RangedAllowsFlying
        {
            get => _rangedAllowsFlying;
            set
            {
                _rangedAllowsFlying = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool RangedMagicOnly
        {
            get => _rangedMagicOnly;
            set
            {
                _rangedMagicOnly = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        /// <summary>
        /// For treasure/underground features, the common item reward.
        /// </summary>
        public ObjectItem FeatureCommonItem
        {
            get => _featureCommonItem;
            set
            {
                _featureCommonItem = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        /// <summary>
        /// For treasure/underground features, the rare item reward.
        /// </summary>
        public ObjectItem FeatureRareItem
        {
            get => _featureRareItem;
            set
            {
                _featureRareItem = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        /// <summary>
        /// Destination coordinates for warp features.
        /// </summary>
        public List<Vector2Int> WarpDestinations => _warpDestinations;

        /// <summary>
        /// Index into <see cref="WarpDestinations"/> identifying the active exit.
        /// </summary>
        public int ActiveWarpIndex
        {
            get => _activeWarpIndex;
            set
            {
                _activeWarpIndex = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        [SerializeField]
        private int _row;
        public int Row => _row;

        [SerializeField]
        private int _col;
        public int Col => _col;

        [SerializeField]
        [Tooltip("Terrain type")]
        private string _terrainTypeId = string.Empty;
        public string TerrainTypeId => _terrainTypeId;

        [SerializeField]
        [Tooltip("Feature type")]
        private string _featureTypeId = string.Empty;
        public string FeatureTypeId => _featureTypeId;
        public string FeatureName
        {
            get => _featureName;
            set => _featureName = value ?? string.Empty;
        }
        public MapGridPointFeature.FeatureType FeatureType
        {
            get => MapGridPointFeature.TypeFromId(FeatureTypeId);
            set => _featureTypeId = MapGridPointFeature.IdFromType(value) ?? string.Empty;
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

        public void Initialize(int row, int col)
        {
            _row = row;
            _col = col;
        }

        /* ---------------------------- Grid Point Property Accessors ---------------------------- */

        public CharacterInstance GetStartingUnit() => _startingUnit;

        public void SetStartingUnit(CharacterInstance unit) => _startingUnit = unit;

        public void SetTerrainTypeId(string id)
        {
            _terrainTypeId = id ?? string.Empty;
            InvalidateTerrainTypeCache();
            ParentGrid?.IncrementStateVersion();
        }

        public void SetFeatureTypeId(string id)
        {
            _featureTypeId = id ?? string.Empty;
            // if the feature type changes we clear any door-locked state, item rewards,
            // any warp destinations, and specialized values
            _featureLocked = false;
            _unlockItem = null;
            _featureCommonItem = null;
            _featureRareItem = null;
            _warpDestinations.Clear();
            _activeWarpIndex = 0;
            _breakableHealth = 0;
            _healingPercentPerTurn = 0f;
            _rangedRange = 0;
            _rangedDamage = 0;
            _rangedHit = 0f;
            _rangedAllowsRiding = false;
            _rangedAllowsFlying = false;
            _rangedMagicOnly = false;
            _shelterNoFly = false;
            _shelterNoRide = false;
            _shelterNoInfantry = false;
            ParentGrid?.IncrementStateVersion();
        }

        public void ApplyFeature(string selId, string name, bool singleClickToggle)
        {
            if (string.IsNullOrEmpty(selId))
            {
                return;
            }

            if (selId == "eraser")
            {
                ClearFeature();
                return;
            }

            if (singleClickToggle && FeatureTypeId == selId)
            {
                ClearFeature();
                return;
            }

            _featureTypeId = selId;
            _featureName = name ?? string.Empty;
            // clear per-feature state from previous feature
            _featureLocked = false;
            _unlockItem = null;
            _featureCommonItem = null;
            _featureRareItem = null;
            _warpDestinations.Clear();
            _activeWarpIndex = 0;
            _breakableHealth = 0;
            _healingPercentPerTurn = 0f;
            _rangedRange = 0;
            _rangedDamage = 0;
            _rangedHit = 0f;
            _rangedAllowsRiding = false;
            _rangedAllowsFlying = false;
            _rangedMagicOnly = false;
            _shelterNoFly = false;
            _shelterNoRide = false;
            _shelterNoInfantry = false;
            ParentGrid?.IncrementStateVersion();

#if UNITY_EDITOR
            // Avoid marking the scene dirty if this is triggered by compilation/domain reload
            // or while the editor is updating (asset reimport / progress UI) or entering/exiting
            // play mode. Only mark the scene dirty on explicit user-driven edits.
            if (
                !UnityEditor.EditorApplication.isCompiling
                && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && !UnityEditor.EditorApplication.isUpdating
            )
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(this.gameObject);
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        public void ClearFeature()
        {
            _featureTypeId = string.Empty;
            _featureName = string.Empty;
            _featureLocked = false;
            _featureCommonItem = null;
            _featureRareItem = null;
            _warpDestinations.Clear();
            _activeWarpIndex = 0;
            _breakableHealth = 0;
            _healingPercentPerTurn = 0f;
            _rangedRange = 0;
            _rangedDamage = 0;
            _rangedHit = 0f;
            _rangedAllowsRiding = false;
            _rangedAllowsFlying = false;
            _rangedMagicOnly = false;
            ParentGrid?.IncrementStateVersion();

#if UNITY_EDITOR
            if (
                !UnityEditor.EditorApplication.isCompiling
                && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && !UnityEditor.EditorApplication.isUpdating
            )
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(this.gameObject);
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }
    }
}
