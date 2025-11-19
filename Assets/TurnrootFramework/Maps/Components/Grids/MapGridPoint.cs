using System;
using System.Collections.Generic;
using Turnroot.Maps.Components.Grids;
using UnityEngine;

public class MapGridPoint : MonoBehaviour
{
    [SerializeField]
    private SpawnPoint _spawnPoint = new();
    public SpawnPoint SpawnPoint
    {
        get => _spawnPoint;
        set => _spawnPoint = value ?? new SpawnPoint();
    }

    void OnValidate() => SpawnPoint ??= new SpawnPoint();

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

    [SerializeField]
    private List<MapGridPointFeatureProperties.StringProperty> _stringProperties = new();

    [SerializeField]
    private List<MapGridPointFeatureProperties.ObjectProperty> _objectProperties = new();

    [SerializeField]
    private List<MapGridPointFeatureProperties.BoolProperty> _boolProperties = new();

    [SerializeField]
    private List<MapGridPointFeatureProperties.IntProperty> _intProperties = new();

    [SerializeField]
    private List<MapGridPointFeatureProperties.FloatProperty> _floatProperties = new();

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

    public TerrainType SelectedTerrainType
    {
        get
        {
            var asset = TerrainTypes.LoadDefault();
            if (asset == null)
                return null;
            var terrainType = asset.GetTypeById(_terrainTypeId);
            return terrainType ?? (asset.Types?.Length > 0 ? asset.Types[0] : null);
        }
    }

    public void Initialize(int row, int col)
    {
        _row = row;
        _col = col;
    }

    public void SetTerrainTypeId(string id) => _terrainTypeId = id ?? string.Empty;

    public void SetFeatureTypeId(string id) => _featureTypeId = id ?? string.Empty;

    public void ApplyFeature(string selId, string name, bool singleClickToggle)
    {
        if (string.IsNullOrEmpty(selId))
            return;

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
        ApplyDefaultsForFeature(selId);
    }

    public void ClearFeature()
    {
        _featureTypeId = string.Empty;
        _featureName = string.Empty;
        _stringProperties.Clear();
        _objectProperties.Clear();
        _boolProperties.Clear();
        _intProperties.Clear();
        _floatProperties.Clear();
    }

    public void ClearFeatureProperty(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;
        _stringProperties.RemoveAll(p => p.key == key);
        _objectProperties.RemoveAll(p => p.key == key);
        _boolProperties.RemoveAll(p => p.key == key);
        _intProperties.RemoveAll(p => p.key == key);
        _floatProperties.RemoveAll(p => p.key == key);
    }

    // Generic property accessor pattern
    private void SetProperty<T>(List<T> list, string key, object value)
        where T : MapGridPointFeatureProperties.IProperty, new()
    {
        if (string.IsNullOrEmpty(key))
            return;

        var existing = list.Find(p => p.key == key);
        if (existing != null)
            existing.SetValue(value);
        else
        {
            var newProp = new T { key = key };
            newProp.SetValue(value);
            list.Add(newProp);
        }
    }

    private TValue GetProperty<T, TValue>(List<T> list, string key, TValue defaultValue = default)
        where T : MapGridPointFeatureProperties.IProperty
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;
        var prop = list.Find(p => p.key == key);
        return prop != null ? (TValue)prop.GetValue() : defaultValue;
    }

    private T? GetNullableProperty<T, TProp>(List<TProp> list, string key)
        where T : struct
        where TProp : MapGridPointFeatureProperties.IProperty
    {
        if (string.IsNullOrEmpty(key))
            return null;
        var prop = list.Find(p => p.key == key);
        return prop != null ? (T?)prop.GetValue() : null;
    }

    // String properties
    public void SetStringFeatureProperty(string key, string value) =>
        SetProperty(_stringProperties, key, value ?? string.Empty);

    public string GetStringFeatureProperty(string key) =>
        GetProperty<MapGridPointFeatureProperties.StringProperty, string>(_stringProperties, key);

    public List<MapGridPointFeatureProperties.StringProperty> GetAllStringFeatureProperties() =>
        new(_stringProperties);

    // Object properties
    public void SetObjectFeatureProperty(string key, UnityEngine.Object value) =>
        SetProperty(_objectProperties, key, value);

    public UnityEngine.Object GetObjectFeatureProperty(string key) =>
        GetProperty<MapGridPointFeatureProperties.ObjectProperty, UnityEngine.Object>(
            _objectProperties,
            key
        );

    public List<MapGridPointFeatureProperties.ObjectProperty> GetAllObjectFeatureProperties() =>
        new(_objectProperties);

    // Bool properties
    public void SetBoolFeatureProperty(string key, bool value) =>
        SetProperty(_boolProperties, key, value);

    public bool? GetBoolFeatureProperty(string key) =>
        GetNullableProperty<bool, MapGridPointFeatureProperties.BoolProperty>(_boolProperties, key);

    public List<MapGridPointFeatureProperties.BoolProperty> GetAllBoolFeatureProperties() =>
        new(_boolProperties);

    // Int properties
    public void SetIntFeatureProperty(string key, int value) =>
        SetProperty(_intProperties, key, value);

    public int? GetIntFeatureProperty(string key) =>
        GetNullableProperty<int, MapGridPointFeatureProperties.IntProperty>(_intProperties, key);

    public List<MapGridPointFeatureProperties.IntProperty> GetAllIntFeatureProperties() =>
        new(_intProperties);

    // Float properties
    public void SetFloatFeatureProperty(string key, float value) =>
        SetProperty(_floatProperties, key, value);

    public float? GetFloatFeatureProperty(string key) =>
        GetNullableProperty<float, MapGridPointFeatureProperties.FloatProperty>(
            _floatProperties,
            key
        );

    public List<MapGridPointFeatureProperties.FloatProperty> GetAllFloatFeatureProperties() =>
        new(_floatProperties);

    public void ApplyDefaultsForFeature(string featureId)
    {
        if (string.IsNullOrEmpty(featureId))
            return;

        var allDefaults = Resources.LoadAll<MapGridPointFeatureProperties>("GameSettings");
        if (allDefaults == null || allDefaults.Length == 0)
            return;

        var defaultProps = FindFeatureProperties(allDefaults, featureId);
        if (defaultProps == null)
            return;

        ApplyDefaultStringProperties(defaultProps.stringProperties);
        ApplyDefaultObjectProperties(defaultProps.objectProperties);
        ApplyDefaultBoolProperties(defaultProps.boolProperties);
        ApplyDefaultIntProperties(defaultProps.intProperties);
        ApplyDefaultFloatProperties(defaultProps.floatProperties);
    }

    private MapGridPointFeatureProperties FindFeatureProperties(
        MapGridPointFeatureProperties[] allDefaults,
        string featureId
    )
    {
        foreach (var props in allDefaults)
        {
            if (props == null)
                continue;
            if (
                props.featureId == featureId
                || string.Equals(props.name, featureId, StringComparison.OrdinalIgnoreCase)
            )
                return props;
        }
        return null;
    }

    private void ApplyDefaultStringProperties(
        List<MapGridPointFeatureProperties.StringProperty> defaults
    )
    {
        if (defaults == null)
            return;
        foreach (var prop in defaults)
        {
            if (string.IsNullOrEmpty(prop.key) || GetStringFeatureProperty(prop.key) != null)
                continue;
            SetStringFeatureProperty(prop.key, prop.value);
        }
    }

    private void ApplyDefaultObjectProperties(
        List<MapGridPointFeatureProperties.ObjectProperty> defaults
    )
    {
        if (defaults == null)
            return;
        foreach (var prop in defaults)
        {
            if (string.IsNullOrEmpty(prop.key) || GetObjectFeatureProperty(prop.key) != null)
                continue;
            SetObjectFeatureProperty(prop.key, prop.value);
        }
    }

    private void ApplyDefaultBoolProperties(
        List<MapGridPointFeatureProperties.BoolProperty> defaults
    )
    {
        if (defaults == null)
            return;
        foreach (var prop in defaults)
        {
            if (string.IsNullOrEmpty(prop.key) || GetBoolFeatureProperty(prop.key).HasValue)
                continue;
            SetBoolFeatureProperty(prop.key, prop.value);
        }
    }

    private void ApplyDefaultIntProperties(List<MapGridPointFeatureProperties.IntProperty> defaults)
    {
        if (defaults == null)
            return;
        foreach (var prop in defaults)
        {
            if (string.IsNullOrEmpty(prop.key) || GetIntFeatureProperty(prop.key).HasValue)
                continue;
            SetIntFeatureProperty(prop.key, prop.value);
        }
    }

    private void ApplyDefaultFloatProperties(
        List<MapGridPointFeatureProperties.FloatProperty> defaults
    )
    {
        if (defaults == null)
            return;
        foreach (var prop in defaults)
        {
            if (string.IsNullOrEmpty(prop.key) || GetFloatFeatureProperty(prop.key).HasValue)
                continue;
            SetFloatFeatureProperty(prop.key, prop.value);
        }
    }

    public float GetTerrainTypeCost(
        bool isWalking = true,
        bool isFlying = false,
        bool isRiding = false,
        bool isMagic = false,
        bool isArmored = false
    )
    {
        var terrainType = SelectedTerrainType;
        if (terrainType == null)
            return 1f;

        if (isWalking)
            return terrainType.CostWalk;
        if (isFlying)
            return terrainType.CostFly;
        if (isRiding)
            return terrainType.CostRide;
        if (isMagic)
            return terrainType.CostMagic;
        if (isArmored)
            return terrainType.CostArmor;

        return 1f;
    }

    public Vector2 Coordinates() => new(_row, _col);

    public Dictionary<string, MapGridPoint> GetNeighbors(bool cardinal = false)
    {
        var neighbors = new Dictionary<string, MapGridPoint>();
        var grid = GetComponentInParent<MapGrid>();
        if (grid == null)
            return neighbors;

        var dirs = cardinal ? CardinalDirections : Directions;
        foreach (var (name, dRow, dCol) in dirs)
        {
            var neighbor = grid.GetGridPoint(_row + dRow, _col + dCol);
            if (neighbor != null)
                neighbors[name] = neighbor;
        }

        return neighbors;
    }
}
