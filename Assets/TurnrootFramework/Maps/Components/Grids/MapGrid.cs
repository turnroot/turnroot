using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor.EditorTools;
using UnityEngine;

public class MapGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [HorizontalLine(color: EColor.Green)]
    [SerializeField]
    private float _gridScale = 1f;

    [SerializeField]
    private Vector3 _gridOffset = Vector3.zero;

    [SerializeField]
    private int _gridWidth = 10;

    [SerializeField]
    private int _gridHeight = 10;

    [SerializeField]
    private string _mapName = string.Empty;
    public string MapName
    {
        get => _mapName;
        set => _mapName = value;
    }

    [SerializeField, ReadOnly]
    private Dictionary<Vector2Int, GameObject> _gridPoints = new();

    [SerializeField, ReadOnly]
    [Tooltip(
        "Serialized feature layer records (second layer) for editor features such as chests, doors, etc."
    )]
    private List<FeatureRecord> _features = new();

    [Header("3D Map Height Connection")]
    [HorizontalLine(color: EColor.Blue)]
    [SerializeField]
    private GameObject _single3dHeightMesh;

    [SerializeField]
    private Vector3[] _single3dHeightMeshRaycastPoints;
    private Color[] _single3dHeightMeshRaycastColors;

    [SerializeField]
    [Tooltip(
        "Row/Column indices (grid) matching each computed raycast point — useful for manual adjustments"
    )]
    private Vector2Int[] _single3dHeightMeshRaycastIndices;

    [SerializeField]
    [Tooltip("Show gizmo spheres for computed raycast points in the Scene view")]
    private bool _showRaycastGizmos = true;

    [SerializeField]
    [Tooltip("Flip the X ordering used when mapping raycast points to grid indices")]
    private bool _flipRaycastX = false;

    [SerializeField]
    [Tooltip("Flip the Y ordering used when mapping raycast points to grid indices")]
    private bool _flipRaycastY = false;

    [SerializeField]
    [Tooltip(
        "Layer mask used when raycasting to the 3D map. Use this to limit raycasts to the map's layer(s)."
    )]
    private LayerMask _raycastLayerMask = ~0;

    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public float GridScale => _gridScale;
    public Vector3 GridOffset => _gridOffset;

    /* -------------------------- Buttons -------------------------- */
    [Button("Create Grid Points")]
    public void CreateChildrenPoints()
    {
        if (_gridPoints.Count > 0)
            ClearGrid();

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                var point = new GameObject($"Point_R{x}_C{y}");
                var gridPoint = point.AddComponent<MapGridPoint>();
                gridPoint.Initialize(x, y);
                SetDefaultTerrainType(gridPoint);

                point.transform.parent = transform;
                point.transform.localPosition =
                    new Vector3(x * _gridScale, 0, y * _gridScale) + _gridOffset;
                _gridPoints[new Vector2Int(x, y)] = point;
            }
        }

        LoadFeatureLayer();
    }

    [Button("Add Row")]
    public void AddRow()
    {
        SaveFeatureLayer();
        _gridHeight++;
        int newRow = _gridHeight - 1;

        for (int col = 0; col < _gridWidth; col++)
        {
            if (GetGridPoint(col, newRow) != null)
                continue;
            CreateGridPoint(col, newRow);
        }

        LoadFeatureLayer();
        MarkDirty();
    }

    [Button("Add Column")]
    public void AddColumn()
    {
        SaveFeatureLayer();
        _gridWidth++;
        int newCol = _gridWidth - 1;

        for (int row = 0; row < _gridHeight; row++)
        {
            if (GetGridPoint(newCol, row) != null)
                continue;
            CreateGridPoint(newCol, row);
        }

        LoadFeatureLayer();
        MarkDirty();
    }

    [Button("Remove Row")]
    [Tooltip("Removes the last row from the grid. This doesn't remove the existing data.")]
    public void RemoveRow()
    {
        if (_gridHeight <= 1)
            return;
        SaveFeatureLayer();
        RemoveGridLine(_gridHeight - 1, true);
        _gridHeight--;
        LoadFeatureLayer();
        MarkDirty();
    }

    [Button("Remove Column")]
    public void RemoveColumn()
    {
        if (_gridWidth <= 1)
            return;
        SaveFeatureLayer();
        RemoveGridLine(_gridWidth - 1, false);
        _gridWidth--;
        LoadFeatureLayer();
        MarkDirty();
    }

    [Button("Connect to 3D Map Height")]
    public void ConnectTo3DMapObject()
    {
        if (_single3dHeightMesh == null)
            return;
        EnsureGridPoints();

        var colliders = _single3dHeightMesh.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
            return;

        var connector = new MapGridHeightConnector();
        var points = connector.RaycastPointsDownTo3DMap(
            _single3dHeightMesh,
            _gridPoints,
            _raycastLayerMask,
            _flipRaycastX,
            _flipRaycastY
        );

        if (points == null || points.Length == 0)
            return;

        _single3dHeightMeshRaycastPoints = points;
        RebuildRaycastColors();
        MarkDirty();
    }

    private void CreateGridPoint(int row, int col)
    {
        var point = new GameObject($"Point_R{row}_C{col}");
        var gridPoint = point.AddComponent<MapGridPoint>();
        gridPoint.Initialize(row, col);
        SetDefaultTerrainType(gridPoint);

        point.transform.parent = transform;
        point.transform.localPosition =
            new Vector3(row * _gridScale, 0, col * _gridScale) + _gridOffset;
        _gridPoints[new Vector2Int(row, col)] = point;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(point);
        UnityEditor.EditorUtility.SetDirty(gridPoint);
#endif
    }

    private void RemoveGridLine(int index, bool isRow)
    {
        int outerLimit = isRow ? _gridWidth : _gridHeight;

        for (int i = 0; i < outerLimit; i++)
        {
            var key = isRow ? new Vector2Int(i, index) : new Vector2Int(index, i);
            var mgp = GetGridPoint(key.x, key.y);
            if (mgp == null)
                continue;

            _gridPoints.Remove(key);
            DestroyImmediate(mgp.gameObject);
        }
    }

    private void SetDefaultTerrainType(MapGridPoint gridPoint)
    {
        var terrainAsset = TerrainTypes.LoadDefault();
        if (terrainAsset?.Types == null)
            return;

        var voidType = terrainAsset.Types.FirstOrDefault(t =>
            t != null && t.Name.Equals("Void", System.StringComparison.OrdinalIgnoreCase)
        );

        if (voidType != null)
            gridPoint.SetTerrainTypeId(voidType.Id);
        else if (terrainAsset.Types.Length > 0 && terrainAsset.Types[0] != null)
            gridPoint.SetTerrainTypeId(terrainAsset.Types[0].Id);
    }

    public void ClearGrid()
    {
        foreach (var point in _gridPoints.Values.Where(p => p != null))
            DestroyImmediate(point);
        _gridPoints.Clear();
    }

    public void RebuildGridDictionary()
    {
        var newDict = new Dictionary<Vector2Int, GameObject>();
        foreach (Transform child in transform)
        {
            if (child == null)
                continue;
            var mgp = child.GetComponent<MapGridPoint>();
            if (mgp != null)
                newDict[new Vector2Int(mgp.Row, mgp.Col)] = child.gameObject;
        }
        _gridPoints = newDict;
        LoadFeatureLayer();
    }

    public void SaveFeatureLayer()
    {
        _features.Clear();
        foreach (var kv in _gridPoints)
        {
            var mgp = kv.Value?.GetComponent<MapGridPoint>();
            if (mgp == null || string.IsNullOrEmpty(mgp.FeatureTypeId))
                continue;

            _features.Add(
                new FeatureRecord
                {
                    row = kv.Key.x,
                    col = kv.Key.y,
                    typeId = mgp.FeatureTypeId,
                    name = mgp.FeatureName,
                    stringProperties = mgp.GetAllStringFeatureProperties()
                        ?.Select(p => new PropertyRecord<string> { key = p.key, value = p.value })
                        .ToList(),
                    boolProperties = mgp.GetAllBoolFeatureProperties()
                        ?.Select(p => new PropertyRecord<bool> { key = p.key, value = p.value })
                        .ToList(),
                    intProperties = mgp.GetAllIntFeatureProperties()
                        ?.Select(p => new PropertyRecord<int> { key = p.key, value = p.value })
                        .ToList(),
                    floatProperties = mgp.GetAllFloatFeatureProperties()
                        ?.Select(p => new PropertyRecord<float> { key = p.key, value = p.value })
                        .ToList(),
                }
            );
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public void LoadFeatureLayer()
    {
        if (_features == null || _features.Count == 0)
            return;

        foreach (var rec in _features)
        {
            var mgp = GetGridPoint(rec.row, rec.col);
            if (mgp == null)
                continue;

            mgp.SetFeatureTypeId(rec.typeId);
            mgp.FeatureName = rec.name ?? string.Empty;
            mgp.ApplyDefaultsForFeature(rec.typeId);

            ApplyPropertyList(rec.stringProperties, mgp.SetStringFeatureProperty);
            ApplyPropertyList(rec.boolProperties, mgp.SetBoolFeatureProperty);
            ApplyPropertyList(rec.intProperties, mgp.SetIntFeatureProperty);
            ApplyPropertyList(rec.floatProperties, mgp.SetFloatFeatureProperty);
        }
    }

    private void ApplyPropertyList<T>(
        List<PropertyRecord<T>> properties,
        System.Action<string, T> setter
    )
    {
        if (properties == null)
            return;
        foreach (var pr in properties.Where(pr => !string.IsNullOrEmpty(pr.key)))
            setter(pr.key, pr.value);
    }

    public void EnsureGridPoints()
    {
        int expectedCount = _gridWidth * _gridHeight;
        int actualCount = transform
            .Cast<Transform>()
            .Count(child => child != null && child.GetComponent<MapGridPoint>() != null);

        if (
            actualCount != expectedCount
            || _gridPoints == null
            || _gridPoints.Count != expectedCount
        )
        {
            if (actualCount > 0)
                RebuildGridDictionary();
            else
                CreateChildrenPoints();
        }
        else if (_gridPoints.Count == 0 && transform.childCount > 0)
            RebuildGridDictionary();
        else if (_gridPoints.Count == 0 && transform.childCount == 0)
            CreateChildrenPoints();

        RepositionGridPoints();
    }

    private void RepositionGridPoints()
    {
        if (_gridPoints == null || _gridPoints.Count == 0)
            return;

        foreach (var kv in _gridPoints)
        {
            if (kv.Value == null)
                continue;
            kv.Value.transform.localPosition =
                new Vector3(kv.Key.x * _gridScale, 0, kv.Key.y * _gridScale) + _gridOffset;
        }
    }

    public MapGridPoint GetGridPoint(int row, int col)
    {
        return _gridPoints.TryGetValue(new Vector2Int(row, col), out var point)
            ? point.GetComponent<MapGridPoint>()
            : null;
    }

    private void RebuildRaycastColors()
    {
        if (
            _single3dHeightMeshRaycastPoints == null
            || _single3dHeightMeshRaycastPoints.Length == 0
        )
            return;

        var colors = new Color[_single3dHeightMeshRaycastPoints.Length];
        var indices = new Vector2Int[_single3dHeightMeshRaycastPoints.Length];

        var orderedFinal = OrderGridPoints(_gridPoints.AsEnumerable());
        int ci = 0;

        foreach (var kv in orderedFinal)
        {
            if (ci >= colors.Length)
                break;
            var mgp = kv.Value?.GetComponent<MapGridPoint>();
            var tt = mgp?.SelectedTerrainType;
            colors[ci] = tt != null ? tt.EditorColor : Color.yellow;
            indices[ci] = kv.Key;
            ci++;
        }

        for (; ci < colors.Length; ci++)
            colors[ci] = Color.yellow;

        _single3dHeightMeshRaycastColors = colors;
        _single3dHeightMeshRaycastIndices = indices;
    }

    private IOrderedEnumerable<KeyValuePair<Vector2Int, GameObject>> OrderGridPoints(
        IEnumerable<KeyValuePair<Vector2Int, GameObject>> points
    )
    {
        var orderedByX = _flipRaycastX
            ? points.OrderByDescending(kv => kv.Key.x)
            : points.OrderBy(kv => kv.Key.x);
        return _flipRaycastY
            ? orderedByX.ThenByDescending(kv => kv.Key.y)
            : orderedByX.ThenBy(kv => kv.Key.y);
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (_gridPoints == null || _gridPoints.Count == 0)
        {
            if (transform.childCount > 0)
                RebuildGridDictionary();
        }

        RepositionGridPoints();

        if (_features != null && _features.Count > 0)
            LoadFeatureLayer();

        if (_single3dHeightMeshRaycastPoints != null && _single3dHeightMeshRaycastPoints.Length > 0)
            RebuildRaycastColors();

        UnityEditor.EditorUtility.SetDirty(this);
    }

    void OnDrawGizmos()
    {
        Vector3 getPos(int x, int y) =>
            transform.position + new Vector3(x * _gridScale, 0, y * _gridScale) + _gridOffset;

        Vector3 topLeft = getPos(0, 0);
        Vector3 topRight = getPos(_gridWidth - 1, 0);
        Vector3 bottomLeft = getPos(0, _gridHeight - 1);
        Vector3 bottomRight = getPos(_gridWidth - 1, _gridHeight - 1);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        var corners = new[] { topLeft, topRight, bottomLeft, bottomRight };
        foreach (var corner in corners)
            Gizmos.DrawSphere(corner, 1f);

        if (
            _showRaycastGizmos
            && _single3dHeightMeshRaycastPoints != null
            && _single3dHeightMeshRaycastPoints.Length > 0
        )
        {
            float s = Mathf.Max(0.2f, _gridScale * 0.4f);
            for (int i = 0; i < _single3dHeightMeshRaycastPoints.Length; i++)
            {
                var p = _single3dHeightMeshRaycastPoints[i];
                var c =
                    (
                        _single3dHeightMeshRaycastColors != null
                        && i < _single3dHeightMeshRaycastColors.Length
                    )
                        ? _single3dHeightMeshRaycastColors[i]
                        : Color.yellow;
                Gizmos.color = c;
                Gizmos.DrawSphere(p, s);
            }
        }
    }
#endif
}

[System.Serializable]
public struct PropertyRecord<T>
{
    public string key;
    public T value;
}

[System.Serializable]
public class FeatureRecord
{
    public int row;
    public int col;
    public string typeId;
    public string name;

    public List<PropertyRecord<string>> stringProperties = new();
    public List<PropertyRecord<bool>> boolProperties = new();
    public List<PropertyRecord<int>> intProperties = new();
    public List<PropertyRecord<float>> floatProperties = new();
}
