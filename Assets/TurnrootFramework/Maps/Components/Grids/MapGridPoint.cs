using System;
using System.Collections.Generic;
using UnityEngine;

public class MapGridPoint : MonoBehaviour
{
    // Initialize row/col after AddComponent (MonoBehaviours can't use constructors)
    public void Initialize(int row, int col)
    {
        _row = row;
        _col = col;
    }

    // Allow editor tools to change terrain id
    public void SetTerrainTypeId(string id)
    {
        _terrainTypeId = id ?? string.Empty;
    }

    // Expose the stored terrain id for editor tooling
    public string TerrainTypeId => _terrainTypeId;

    private readonly (string name, int dRow, int dCol)[] directions = new (
        string name,
        int dRow,
        int dCol
    )[]
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

    private readonly (string name, int dRow, int dCol)[] cardinalDirections = new (
        string name,
        int dRow,
        int dCol
    )[]
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

    public int Row => _row;
    public int Col => _col;

    public MapGridPoint() { }

    public MapGridPoint(int row, int col)
    {
        _row = row;
        _col = col;
    }

    [SerializeField]
    [Tooltip("Gizmo sphere radius (world units)")]
    private float _gizmoRadius = 0.35f;

    [SerializeField]
    [Tooltip(
        "ID of the terrain type from the TerrainTypes asset. If empty, the editor/runtime will try to find the default asset and allow selection."
    )]
    private string _terrainTypeId = string.Empty;

    // Optional editor feature layer (second layer) - stores a feature id and an optional name.
    [SerializeField]
    [Tooltip("ID of an editor feature (chest, door, warp, etc). Empty = none.")]
    private string _featureTypeId = string.Empty;

    [SerializeField]
    [Tooltip("Optional name or label for the feature. Editable from the Map Grid Editor.")]
    private string _featureName = string.Empty;

    // Per-instance typed feature property overrides
    [SerializeField]
    private List<MapGridPointFeatureProperties.StringProperty> _stringProperties =
        new List<MapGridPointFeatureProperties.StringProperty>();

    [SerializeField]
    private List<MapGridPointFeatureProperties.BoolProperty> _boolProperties =
        new List<MapGridPointFeatureProperties.BoolProperty>();

    [SerializeField]
    private List<MapGridPointFeatureProperties.IntProperty> _intProperties =
        new List<MapGridPointFeatureProperties.IntProperty>();

    [SerializeField]
    private List<MapGridPointFeatureProperties.FloatProperty> _floatProperties =
        new List<MapGridPointFeatureProperties.FloatProperty>();

    // Expose feature fields for editor tooling
    public string FeatureTypeId => _featureTypeId;
    public MapGridPointFeature.FeatureType FeatureType
    {
        get => MapGridPointFeature.TypeFromId(_featureTypeId);
        set => _featureTypeId = MapGridPointFeature.IdFromType(value) ?? string.Empty;
    }
    public string FeatureName
    {
        get => _featureName;
        set => _featureName = value ?? string.Empty;
    }

    // Editor helpers
    public void SetFeatureTypeId(string id)
    {
        _featureTypeId = id ?? string.Empty;
    }

    public void ApplyFeature(string selId, string name, bool singleClickToggle)
    {
        if (string.IsNullOrEmpty(selId))
            return;

        if (selId == "eraser")
        {
            ClearFeature();
            return;
        }

        if (singleClickToggle && !string.IsNullOrEmpty(_featureTypeId) && _featureTypeId == selId)
        {
            ClearFeature();
            return;
        }

        _featureTypeId = selId;
        _featureName = name ?? string.Empty;

        // When a feature is applied, populate instance properties from the shared defaults
        // (scriptable objects under Resources/GameSettings/*/Map). Do not overwrite any
        // existing per-instance property values.
        ApplyDefaultsForFeature(selId);
    }

    public void ClearFeature()
    {
        _featureTypeId = string.Empty;
        _featureName = string.Empty;
        _stringProperties.Clear();
        _boolProperties.Clear();
        _intProperties.Clear();
        _floatProperties.Clear();
    }

    // Feature property helpers (typed key/value pairs persisted on the MapGridPoint)

    public void ClearFeatureProperty(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;
        _stringProperties.RemoveAll(p => p.key == key);
        _boolProperties.RemoveAll(p => p.key == key);
        _intProperties.RemoveAll(p => p.key == key);
        _floatProperties.RemoveAll(p => p.key == key);
    }

    // Typed accessors - string
    public void SetStringFeatureProperty(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            return;
        var existing = _stringProperties.Find(p => p.key == key);
        if (existing != null)
        {
            existing.value = value ?? string.Empty;
            return;
        }
        _stringProperties.Add(
            new MapGridPointFeatureProperties.StringProperty
            {
                key = key,
                value = value ?? string.Empty,
            }
        );
    }

    public string GetStringFeatureProperty(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        var p = _stringProperties.Find(x => x.key == key);
        return p != null ? p.value : null;
    }

    public List<MapGridPointFeatureProperties.StringProperty> GetAllStringFeatureProperties()
    {
        return new List<MapGridPointFeatureProperties.StringProperty>(_stringProperties);
    }

    public void SetBoolFeatureProperty(string key, bool value)
    {
        if (string.IsNullOrEmpty(key))
            return;
        var existing = _boolProperties.Find(p => p.key == key);
        if (existing != null)
        {
            existing.value = value;
            return;
        }
        _boolProperties.Add(
            new MapGridPointFeatureProperties.BoolProperty { key = key, value = value }
        );
    }

    public bool? GetBoolFeatureProperty(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        var p = _boolProperties.Find(x => x.key == key);
        return p != null ? (bool?)p.value : null;
    }

    public List<MapGridPointFeatureProperties.BoolProperty> GetAllBoolFeatureProperties()
    {
        return new List<MapGridPointFeatureProperties.BoolProperty>(_boolProperties);
    }

    public void SetIntFeatureProperty(string key, int value)
    {
        if (string.IsNullOrEmpty(key))
            return;
        var existing = _intProperties.Find(p => p.key == key);
        if (existing != null)
        {
            existing.value = value;
            return;
        }
        _intProperties.Add(
            new MapGridPointFeatureProperties.IntProperty { key = key, value = value }
        );
    }

    public int? GetIntFeatureProperty(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        var p = _intProperties.Find(x => x.key == key);
        return p != null ? (int?)p.value : null;
    }

    public List<MapGridPointFeatureProperties.IntProperty> GetAllIntFeatureProperties()
    {
        return new List<MapGridPointFeatureProperties.IntProperty>(_intProperties);
    }

    public void SetFloatFeatureProperty(string key, float value)
    {
        if (string.IsNullOrEmpty(key))
            return;
        var existing = _floatProperties.Find(p => p.key == key);
        if (existing != null)
        {
            existing.value = value;
            return;
        }
        _floatProperties.Add(
            new MapGridPointFeatureProperties.FloatProperty { key = key, value = value }
        );
    }

    public float? GetFloatFeatureProperty(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        var p = _floatProperties.Find(x => x.key == key);
        return p != null ? (float?)p.value : null;
    }

    public List<MapGridPointFeatureProperties.FloatProperty> GetAllFloatFeatureProperties()
    {
        return new List<MapGridPointFeatureProperties.FloatProperty>(_floatProperties);
    }

    // Populate per-instance properties from the shared defaults (ScriptableObject)
    public void ApplyDefaultsForFeature(string featureId)
    {
        if (string.IsNullOrEmpty(featureId))
            return;

        // Load all defaults under GameSettings (covers GameSettings/*/Map)
        var all = Resources.LoadAll<MapGridPointFeatureProperties>("GameSettings");
        if (all == null || all.Length == 0)
            return;

        MapGridPointFeatureProperties found = null;
        foreach (var a in all)
        {
            if (a == null)
                continue;
            if (!string.IsNullOrEmpty(a.featureId) && a.featureId == featureId)
            {
                found = a;
                break;
            }
            if (string.Equals(a.name, featureId, StringComparison.OrdinalIgnoreCase))
            {
                found = a;
                break;
            }
        }

        if (found == null)
            return;

        // Apply string defaults
        if (found.stringProperties != null)
        {
            foreach (var sp in found.stringProperties)
            {
                if (string.IsNullOrEmpty(sp.key))
                    continue;
                var existing = GetStringFeatureProperty(sp.key);
                if (existing == null)
                    SetStringFeatureProperty(sp.key, sp.value ?? string.Empty);
            }
        }

        // Bool defaults
        if (found.boolProperties != null)
        {
            foreach (var bp in found.boolProperties)
            {
                if (string.IsNullOrEmpty(bp.key))
                    continue;
                var existing = GetBoolFeatureProperty(bp.key);
                if (existing == null)
                    SetBoolFeatureProperty(bp.key, bp.value);
            }
        }

        // Int defaults
        if (found.intProperties != null)
        {
            foreach (var ip in found.intProperties)
            {
                if (string.IsNullOrEmpty(ip.key))
                    continue;
                var existing = GetIntFeatureProperty(ip.key);
                if (existing == null)
                    SetIntFeatureProperty(ip.key, ip.value);
            }
        }

        // Float defaults
        if (found.floatProperties != null)
        {
            foreach (var fp in found.floatProperties)
            {
                if (string.IsNullOrEmpty(fp.key))
                    continue;
                var existing = GetFloatFeatureProperty(fp.key);
                if (existing == null)
                    SetFloatFeatureProperty(fp.key, fp.value);
            }
        }
    }

    // Feature letter mapping moved to MapGridPointFeature.GetFeatureLetter(string)

    public TerrainType SelectedTerrainType
    {
        get
        {
            var asset = TerrainTypes.LoadDefault();
            if (asset == null)
                return null;
            var t = asset.GetTypeById(_terrainTypeId);
            if (t != null)
                return t;
            // fallback to first type if available
            if (asset.Types != null && asset.Types.Length > 0)
                return asset.Types[0];
            return null;
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
        var tt = SelectedTerrainType;
        if (tt == null)
            return 1; // default cost
        if (isWalking)
            return tt.CostWalk;
        else if (isFlying)
            return tt.CostFly;
        else if (isRiding)
            return tt.CostRide;
        else if (isMagic)
            return tt.CostMagic;
        else if (isArmored)
            return tt.CostArmor;
        return 1; // default cost
    }

    public Vector2 Coordinates()
    {
        return new Vector2(_row, _col);
    }

    public Dictionary<string, MapGridPoint> GetNeighbors(bool cardinal = false)
    {
        var neighbors = new Dictionary<string, MapGridPoint>();
        var grid = GetComponentInParent<MapGrid>();

        var dirs = cardinal ? cardinalDirections : directions;
        foreach (var (name, dRow, dCol) in dirs)
        {
            int newRow = _row + dRow;
            int newCol = _col + dCol;
            var neighbor = grid.GetGridPoint(newRow, newCol);
            if (neighbor != null)
            {
                neighbors[name] = neighbor;
            }
        }

        return neighbors;
    }
}
