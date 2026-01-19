using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using Turnroot.Maps.Components.Grids;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Gameplay.Maps
{
    public class MapGridPoint : MonoBehaviour
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

        [SerializeField]
        private int _row;

        [SerializeField]
        private int _col;

        [SerializeField]
        [Tooltip("Gizmo sphere radius (world units)")]
        private float _gizmoRadius = 0.35f;

        [SerializeField]
        [Tooltip("Terrain type")]
        private string _terrainTypeId = string.Empty;

        [SerializeField]
        [Tooltip("Feature type")]
        private string _featureTypeId = string.Empty;

        [SerializeField]
        [Tooltip("Feature display name (optional).")]
        private string _featureName = string.Empty;

        /* ---------------------------- Grid Point Properties ---------------------------- */
        [Header("Grid Point Properties")]
        [SerializeField]
        private List<MapGridPropertyBase.EventProperty> _pointEventProperties = new();

        [SerializeField]
        private List<MapGridPropertyBase.UnitProperty> _pointUnitProperties = new();

        [SerializeField]
        private List<MapGridPropertyBase.ObjectItemProperty> _pointObjectItemProperties = new();

        [SerializeField]
        private List<MapGridPropertyBase.BoolProperty> _pointBoolProperties = new();

        [SerializeField]
        private List<MapGridPropertyBase.FloatProperty> _pointFloatProperties = new();
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

        /* ---------------------------- Feature Properties ---------------------------- */
        [Header("Feature Properties")]
        [SerializeField]
        private List<MapGridPropertyBase.EventProperty> _featureEventProperties = new();

        [SerializeField]
        private List<MapGridPropertyBase.UnitProperty> _featureUnitProperties = new();

        [SerializeField]
        private List<MapGridPropertyBase.ObjectItemProperty> _featureObjectItemProperties = new();

        [SerializeField]
        private List<MapGridPropertyBase.BoolProperty> _featureBoolProperties = new();

        [SerializeField]
        private List<MapGridPropertyBase.FloatProperty> _featureFloatProperties = new();

        public int Row => _row;
        public int Col => _col;
        public string TerrainTypeId => _terrainTypeId;
        public string FeatureTypeId => _featureTypeId;
        public string FeatureName
        {
            get => _featureName;
            set => _featureName = value ?? string.Empty;
        }
        public MapGridPointFeature.FeatureType FeatureType
        {
            get => MapGridPointFeature.TypeFromId(_featureTypeId);
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

                var terrainType = asset.GetTypeById(_terrainTypeId);
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
                    : asset.GetTypeById(_terrainTypeId)
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

            if (singleClickToggle && _featureTypeId == selId)
            {
                ClearFeature();
                return;
            }

            _featureTypeId = selId;
            _featureName = name ?? string.Empty;
            ParentGrid?.IncrementStateVersion();

            // When a feature is applied, automatically apply any configured defaults
            // so newly created features get their expected starting properties.
            try
            {
                ApplyDefaultsForFeature(selId);
            }
            catch
            {
                // Defensive: don't throw in editor UI if defaults aren't available
            }

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
            _featureUnitProperties.Clear();
            _featureObjectItemProperties.Clear();
            _featureEventProperties.Clear();
            if (
                !UnityEditor.EditorApplication.isCompiling
                && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && !UnityEditor.EditorApplication.isUpdating
            )
            {
                _featureFloatProperties.Clear();
            }
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

        public void ClearFeatureProperty(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            _featureUnitProperties.RemoveAll(p => p.key == key);
            _featureObjectItemProperties.RemoveAll(p => p.key == key);
            _featureBoolProperties.RemoveAll(p => p.key == key);
            _featureEventProperties.RemoveAll(p => p.key == key);
            _featureFloatProperties.RemoveAll(p => p.key == key);
        }

        private void SetProperty<T>(List<T> list, string key, object value)
            where T : MapGridPropertyBase.IProperty, new()
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var existing = list.Find(p => p.key == key);
            if (existing != null)
            {
                existing.SetValue(value);
            }
            else
            {
                var newProp = new T { key = key };
                newProp.SetValue(value);
                list.Add(newProp);
            }
        }

        private TValue GetProperty<T, TValue>(
            List<T> list,
            string key,
            TValue defaultValue = default
        )
            where T : MapGridPropertyBase.IProperty
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            var prop = list.Find(p => p.key == key);
            return prop != null ? (TValue)prop.GetValue() : defaultValue;
        }

        private T? GetNullableProperty<T, TProp>(List<TProp> list, string key)
            where T : struct
            where TProp : MapGridPropertyBase.IProperty
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var prop = list.Find(p => p.key == key);
            return prop != null ? (T?)prop.GetValue() : null;
        }

        /* ---------------------------- Feature Property Accessors ---------------------------- */

        public void SetUnitFeatureProperty(string key, CharacterInstance value) =>
            SetProperty(_featureUnitProperties, key, value);

        public CharacterInstance GetUnitFeatureProperty(string key) =>
            GetProperty<MapGridPropertyBase.UnitProperty, CharacterInstance>(
                _featureUnitProperties,
                key
            );

        public List<MapGridPropertyBase.UnitProperty> GetAllUnitFeatureProperties() =>
            new(_featureUnitProperties);

        public void SetUnitPointProperty(string key, CharacterInstance value) =>
            SetProperty(_pointUnitProperties, key, value);

        public CharacterInstance GetUnitPointProperty(string key)
        {
            return GetProperty<MapGridPropertyBase.UnitProperty, CharacterInstance>(
                _pointUnitProperties,
                key
            );
        }

        public List<MapGridPropertyBase.UnitProperty> GetAllUnitPointProperties() =>
            new(_pointUnitProperties);

        public void SetObjectItemFeatureProperty(string key, ObjectItemInstance value) =>
            SetProperty(_featureObjectItemProperties, key, value);

        public ObjectItemInstance GetObjectItemFeatureProperty(string key) =>
            GetProperty<MapGridPropertyBase.ObjectItemProperty, ObjectItemInstance>(
                _featureObjectItemProperties,
                key
            );

        public List<MapGridPropertyBase.ObjectItemProperty> GetAllObjectItemFeatureProperties() =>
            new(_featureObjectItemProperties);

        public void SetObjectItemPointProperty(string key, ObjectItemInstance value) =>
            SetProperty(_pointObjectItemProperties, key, value);

        public ObjectItemInstance GetObjectItemPointProperty(string key) =>
            GetProperty<MapGridPropertyBase.ObjectItemProperty, ObjectItemInstance>(
                _pointObjectItemProperties,
                key
            );

        public List<MapGridPropertyBase.ObjectItemProperty> GetAllObjectItemPointProperties() =>
            new(_pointObjectItemProperties);

        // Bool properties
        public void SetBoolFeatureProperty(string key, bool value) =>
            SetProperty(_featureBoolProperties, key, value);

        public bool? GetBoolFeatureProperty(string key) =>
            GetNullableProperty<bool, MapGridPropertyBase.BoolProperty>(
                _featureBoolProperties,
                key
            );

        public List<MapGridPropertyBase.BoolProperty> GetAllBoolFeatureProperties() =>
            new(_featureBoolProperties);

        public void SetBoolPointProperty(string key, bool value) =>
            SetProperty(_pointBoolProperties, key, value);

        public bool? GetBoolPointProperty(string key) =>
            GetNullableProperty<bool, MapGridPropertyBase.BoolProperty>(_pointBoolProperties, key);

        public List<MapGridPropertyBase.BoolProperty> GetAllBoolPointProperties() =>
            new(_pointBoolProperties);

        public void SetFloatFeatureProperty(string key, float value) =>
            SetProperty(_featureFloatProperties, key, value);

        public float? GetFloatFeatureProperty(string key) =>
            GetNullableProperty<float, MapGridPropertyBase.FloatProperty>(
                _featureFloatProperties,
                key
            );

        public List<MapGridPropertyBase.FloatProperty> GetAllFloatFeatureProperties() =>
            new(_featureFloatProperties);

        public void SetFloatPointProperty(string key, float value) =>
            SetProperty(_pointFloatProperties, key, value);

        public float? GetFloatPointProperty(string key) =>
            GetNullableProperty<float, MapGridPropertyBase.FloatProperty>(
                _pointFloatProperties,
                key
            );

        public List<MapGridPropertyBase.FloatProperty> GetAllFloatPointProperties() =>
            new(_pointFloatProperties);

        public void SetEventFeatureProperty(string key, UnityEvent value) =>
            SetProperty(_featureEventProperties, key, value);

        public UnityEvent GetEventFeatureProperty(string key)
        {
            return GetProperty<MapGridPropertyBase.EventProperty, UnityEvent>(
                _featureEventProperties,
                key
            );
        }

        public List<MapGridPropertyBase.EventProperty> GetAllEventFeatureProperties() =>
            new(_featureEventProperties);

        public void SetEventPointProperty(string key, UnityEvent value) =>
            SetProperty(_pointEventProperties, key, value);

        public UnityEvent GetEventPointProperty(string key)
        {
            return GetProperty<MapGridPropertyBase.EventProperty, UnityEvent>(
                _pointEventProperties,
                key
            );
        }

        public List<MapGridPropertyBase.EventProperty> GetAllEventPointProperties() =>
            new(_pointEventProperties);

        public void ApplyDefaultsForFeature(string featureId)
        {
            if (string.IsNullOrEmpty(featureId))
            {
                return;
            }

            // Avoid calling Resources.LoadAll during OnValidate to prevent SendMessage errors
#if UNITY_EDITOR
            if (
                UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                || UnityEditor.EditorApplication.isCompiling
                || UnityEditor.EditorApplication.isUpdating
            )
            {
                return;
            }
#endif

            MapGridFeatureProperties[] allDefaults = null;

            try
            {
                allDefaults = Resources.LoadAll<MapGridFeatureProperties>("GameSettings");
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"MapGridPoint: Failed to load feature properties: {ex.Message}");
#endif
                return;
            }

            if (allDefaults == null || allDefaults.Length == 0)
            {
                return;
            }

            var defaultProps = FindFeatureProperties(allDefaults, featureId);
            if (defaultProps == null)
            {
                return;
            }

            ApplyDefaults(
                defaultProps.unitProperties,
                GetUnitFeatureProperty,
                SetUnitFeatureProperty
            );
            ApplyDefaults(
                defaultProps.objectItemProperties,
                GetObjectItemFeatureProperty,
                SetObjectItemFeatureProperty
            );
            ApplyDefaults(
                defaultProps.boolProperties,
                k => GetBoolFeatureProperty(k),
                (k, v) =>
                {
                    if (v.HasValue)
                    {
                        SetBoolFeatureProperty(k, v.Value);
                    }
                }
            );
            ApplyDefaults(
                defaultProps.eventProperties,
                GetEventFeatureProperty,
                SetEventFeatureProperty
            );
            ApplyDefaults(
                defaultProps.floatProperties,
                k => GetFloatFeatureProperty(k),
                (k, v) =>
                {
                    if (v.HasValue)
                    {
                        SetFloatFeatureProperty(k, v.Value);
                    }
                }
            );
        }

        private void ApplyDefaults<TProp, TValue>(
            List<TProp> defaults,
            Func<string, TValue> getter,
            Action<string, TValue> setter
        )
            where TProp : MapGridPropertyBase.IProperty
        {
            if (defaults == null)
            {
                return;
            }

            foreach (var prop in defaults)
            {
                if (string.IsNullOrEmpty(prop.key))
                {
                    continue;
                }

                var existing = getter(prop.key);
                // For reference types, check null. For nullable value types, the getter
                // should return null/default when no value exists.
                if (!EqualityComparer<TValue>.Default.Equals(existing, default))
                {
                    continue;
                }

                var value = prop.GetValue();
                if (value is TValue typedValue)
                {
                    setter(prop.key, typedValue);
                }
            }
        }

        private MapGridFeatureProperties FindFeatureProperties(
            MapGridFeatureProperties[] allDefaults,
            string featureId
        )
        {
            foreach (var props in allDefaults)
            {
                if (props == null)
                {
                    continue;
                }

                if (
                    props.featureId == featureId
                    || string.Equals(props.name, featureId, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return props;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the movement cost for this terrain type based on movement mode.
        /// Uses cached terrain type lookup for performance.
        /// </summary>
        public float GetTerrainTypeCost(
            bool isWalking = true,
            bool isFlying = false,
            bool isRiding = false,
            bool isMagic = false,
            bool isArmored = false
        )
        {
            var terrainType = GetCachedTerrainType();
            return terrainType == null ? 1f
                : isWalking ? terrainType.CostWalk
                : isFlying ? terrainType.CostFly
                : isRiding ? terrainType.CostRide
                : isMagic ? terrainType.CostMagic
                : isArmored ? terrainType.CostArmor
                : 1f;
        }

        public Vector2 Coordinates() => new(_row, _col);

        public Vector2Int CoordinatesInt => new(_row, _col);

        /// <summary>
        /// Get neighboring grid points. Allocates a new dictionary each call.
        /// For performance-critical code (pathfinding), use GetNeighborsNonAlloc instead.
        /// </summary>
        public Dictionary<string, MapGridPoint> GetNeighbors(bool cardinal = false)
        {
            var neighbors = new Dictionary<string, MapGridPoint>();
            var grid = ParentGrid;
            if (grid == null)
            {
                return neighbors;
            }

            var dirs = cardinal ? CardinalDirections : Directions;
            foreach (var (name, dRow, dCol) in dirs)
            {
                var neighbor = grid.GetGridPoint(_row + dRow, _col + dCol);
                if (neighbor != null)
                {
                    neighbors[name] = neighbor;
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Get neighboring grid points without allocation. Fills the provided dictionary.
        /// The dictionary is cleared before filling. Returns the count of neighbors found.
        /// Use this in performance-critical paths like pathfinding.
        /// </summary>
        /// <param name="neighbors">Dictionary to fill with neighbors. Will be cleared first.</param>
        /// <returns>Number of neighbors found.</returns>
        public int GetNeighborsNonAlloc(
            Dictionary<string, MapGridPoint> neighbors,
            bool cardinal = true
        )
        {
            neighbors.Clear();
            var grid = ParentGrid;
            if (grid == null)
            {
                return 0;
            }

            var dirs = cardinal ? CardinalDirections : Directions;
            int count = 0;
            foreach (var (name, dRow, dCol) in dirs)
            {
                var neighbor = grid.GetGridPoint(_row + dRow, _col + dCol);
                if (neighbor != null)
                {
                    neighbors[name] = neighbor;
                    count++;
                }
            }

            return count;
        }
    }
}
