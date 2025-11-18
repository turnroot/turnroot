using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class MapGrid : MonoBehaviour
{
    [SerializeField]
    private float _gridScale = 1f;

    [SerializeField]
    private Vector3 _gridOffset = Vector3.zero;

    [SerializeField]
    private int _gridWidth = 10;

    [SerializeField]
    private int _gridHeight = 10;

    // Optional user-provided map name shown in the editor toolbar
    [SerializeField]
    private string _mapName = string.Empty;

    public string MapName
    {
        get => _mapName;
        set => _mapName = value;
    }

    [SerializeField]
    private Dictionary<Vector2Int, GameObject> _gridPoints = new();

    [System.Serializable]
    private class FeatureRecord
    {
        public int row;
        public int col;
        public string typeId;
        public string name;

        [System.Serializable]
        public class PropertyString
        {
            public string key;
            public string value;
        }

        [System.Serializable]
        public class PropertyBool
        {
            public string key;
            public bool value;
        }

        [System.Serializable]
        public class PropertyInt
        {
            public string key;
            public int value;
        }

        [System.Serializable]
        public class PropertyFloat
        {
            public string key;
            public float value;
        }

        // Typed properties attached to the feature
        public List<PropertyString> stringProperties = new List<PropertyString>();
        public List<PropertyBool> boolProperties = new List<PropertyBool>();
        public List<PropertyInt> intProperties = new List<PropertyInt>();
        public List<PropertyFloat> floatProperties = new List<PropertyFloat>();
    }

    [SerializeField]
    [Tooltip(
        "Serialized feature layer records (second layer) for editor features such as chests, doors, etc."
    )]
    private List<FeatureRecord> _featureRecords = new();

    [Button("Create Grid Points")]
    public void CreateChildrenPoints()
    {
        if (_gridPoints.Count > 0)
        {
            ClearGrid();
        }
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                var point = new GameObject($"Point_R{x}_C{y}");
                var gridPoint = point.AddComponent<MapGridPoint>();
                gridPoint.Initialize(x, y);

                // Set default terrain to "Void" if it exists
                SetDefaultTerrainType(gridPoint);

                point.transform.parent = transform;
                point.transform.localPosition =
                    new Vector3(x * _gridScale, 0, y * _gridScale) + _gridOffset;
                _gridPoints[new Vector2Int(x, y)] = point;
            }
        }

        // After creating points, restore any serialized feature data
        LoadFeatureLayer();
    }

    private void SetDefaultTerrainType(MapGridPoint gridPoint)
    {
        var terrainAsset = TerrainTypes.LoadDefault();
        if (terrainAsset?.Types != null)
        {
            // Look for "Void" terrain type
            foreach (var terrainType in terrainAsset.Types)
            {
                if (
                    terrainType != null
                    && terrainType.Name.Equals("Void", System.StringComparison.OrdinalIgnoreCase)
                )
                {
                    gridPoint.SetTerrainTypeId(terrainType.Id);
                    return;
                }
            }

            if (terrainAsset.Types.Length > 0 && terrainAsset.Types[0] != null)
            {
                gridPoint.SetTerrainTypeId(terrainAsset.Types[0].Id);
            }
        }
    }

    [Button("Add Row")]
    public void AddRow()
    {
        SaveFeatureLayer();

        _gridHeight++;

        // Create new points for the new row (last row)
        int newRow = _gridHeight - 1;
        for (int col = 0; col < _gridWidth; col++)
        {
            // Check if point already exists at this location (shouldn't, but just in case)
            var existingPoint = GetGridPoint(col, newRow);
            if (existingPoint != null)
                continue;

            var point = new GameObject($"Point_R{col}_C{newRow}");

            var gridPoint = point.AddComponent<MapGridPoint>();
            gridPoint.Initialize(col, newRow);
            SetDefaultTerrainType(gridPoint);

            point.transform.parent = transform;
            point.transform.localPosition =
                new Vector3(col * _gridScale, 0, newRow * _gridScale) + _gridOffset;
            _gridPoints[new Vector2Int(col, newRow)] = point;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(point);
            UnityEditor.EditorUtility.SetDirty(gridPoint);
#endif
        }

        // Restore features to existing points
        LoadFeatureLayer();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    [Button("Add Column")]
    public void AddColumn()
    {
        // Save current state
        SaveFeatureLayer();

        // Increment width
        _gridWidth++;

        // Create new points for the new column (rightmost column)
        int newCol = _gridWidth - 1;
        for (int row = 0; row < _gridHeight; row++)
        {
            // Check if point already exists at this location (shouldn't, but just in case)
            var existingPoint = GetGridPoint(newCol, row);
            if (existingPoint != null)
                continue;

            var point = new GameObject($"Point_R{newCol}_C{row}");

            var gridPoint = point.AddComponent<MapGridPoint>();
            gridPoint.Initialize(newCol, row);
            SetDefaultTerrainType(gridPoint);

            point.transform.parent = transform;
            point.transform.localPosition =
                new Vector3(newCol * _gridScale, 0, row * _gridScale) + _gridOffset;
            _gridPoints[new Vector2Int(newCol, row)] = point;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(point);
            UnityEditor.EditorUtility.SetDirty(gridPoint);
#endif
        }

        // Restore features to existing points
        LoadFeatureLayer();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    [Button("Remove Row")]
    public void RemoveRow()
    {
        if (_gridHeight <= 1)
        {
            Debug.LogWarning("Cannot remove row: grid must have at least one row.");
            return;
        }

        // Persist current state before removing
        SaveFeatureLayer();

        int removeRow = _gridHeight - 1;
        for (int col = 0; col < _gridWidth; col++)
        {
            var mgp = GetGridPoint(col, removeRow);
            if (mgp == null)
                continue;
            var go = mgp.gameObject;
            // Remove from internal dictionary and destroy
            _gridPoints.Remove(new Vector2Int(col, removeRow));
            DestroyImmediate(go);
        }

        _gridHeight--;

        // Restore feature layer onto remaining points
        LoadFeatureLayer();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    [Button("Remove Column")]
    public void RemoveColumn()
    {
        if (_gridWidth <= 1)
        {
            Debug.LogWarning("Cannot remove column: grid must have at least one column.");
            return;
        }

        // Persist current state before removing
        SaveFeatureLayer();

        int removeCol = _gridWidth - 1;
        for (int row = 0; row < _gridHeight; row++)
        {
            var mgp = GetGridPoint(removeCol, row);
            if (mgp == null)
                continue;
            var go = mgp.gameObject;
            _gridPoints.Remove(new Vector2Int(removeCol, row));
            DestroyImmediate(go);
        }

        _gridWidth--;

        // Restore feature layer onto remaining points
        LoadFeatureLayer();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    [SerializeField]
    [FormerlySerializedAs("_3dMapObject")]
    [Tooltip("Single 3D height mesh (scene instance)")]
    private GameObject _single3dHeightMesh;

    [SerializeField]
    [FormerlySerializedAs("_3dMapObjectRaycastPoints")]
    private Vector3[] _single3dHeightMeshRaycastPoints;

    [SerializeField]
    [Tooltip(
        "Layer mask used when raycasting to the 3D map. Use this to limit raycasts to the map's layer(s)."
    )]
    private LayerMask _raycastLayerMask = ~0;

    public void ClearGrid()
    {
        foreach (var point in _gridPoints.Values)
        {
            if (point != null)
            {
                DestroyImmediate(point);
            }
        }
        _gridPoints.Clear();
    }

    // Rebuild the internal lookup dictionary from existing child GameObjects.
    // Useful because serialized dictionaries may not persist between editor sessions.
    public void RebuildGridDictionary()
    {
        var newDict = new Dictionary<Vector2Int, GameObject>();
        foreach (Transform child in transform)
        {
            if (child == null)
                continue;
            var mgp = child.GetComponent<MapGridPoint>();
            if (mgp != null)
            {
                var key = new Vector2Int(mgp.Row, mgp.Col);
                newDict[key] = child.gameObject;
            }
        }
        _gridPoints = newDict;

        // Restore feature layer onto the rebuilt points
        LoadFeatureLayer();
    }

    [Button("Connect to 3D Map Height")]
    public void ConnectTo3DMapObject()
    {
        if (_single3dHeightMesh == null)
        {
            Debug.LogWarning("No 3D Map Object assigned for connection.");
            return;
        }

        // Ensure internal grid point index is populated before attempting raycasts
        EnsureGridPoints();
        Debug.Log($"ConnectTo3DMapObject: gridPoints={_gridPoints?.Count ?? 0}");
#if UNITY_EDITOR
        // If the user accidentally assigned a prefab asset from the Project window, raycasts won't hit it.
        // Prefer that the user assigns the instance placed in the Scene (Hierarchy).
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(_single3dHeightMesh))
        {
            Debug.LogWarning(
                "Assigned 3D Map Object appears to be a prefab asset. Assign the instance from the Scene (Hierarchy), not the asset from the Project window."
            );
            // continue — still attempt to probe colliders on the object, but warn first
        }
#endif

        // Check that the target (or its children) has colliders so raycasts can hit
        var colliders = _single3dHeightMesh.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
        {
            Debug.LogWarning(
                "Selected 3D Map Object (and children) have no Colliders — raycasts will not hit. Add Colliders to the mesh or children or assign the scene instance."
            );
        }
        else
        {
            int active = 0;
            foreach (var c in colliders)
            {
                if (c != null && c.enabled && c.gameObject.activeInHierarchy)
                    active++;
            }
            Debug.Log(
                $"ConnectTo3DMapObject: found {colliders.Length} colliders ({active} active) on '{_single3dHeightMesh.name}'"
            );
        }

        var connector = new MapGridHeightConnector();
        var points = connector.RaycastPointsDownTo3DMap(
            _single3dHeightMesh,
            _gridPoints,
            _raycastLayerMask,
            true
        );
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning(
                "Raycast returned no points. Ensure your 3D map has colliders and is in the Scene."
            );
            return;
        }

        _single3dHeightMeshRaycastPoints = points;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif
        Debug.Log(
            $"ConnectTo3DMapObject: computed {_single3dHeightMeshRaycastPoints.Length} raycast points for {_single3dHeightMesh.name}"
        );
    }

    // Persist current MapGridPoint features into the serialized record list
    public void SaveFeatureLayer()
    {
        _featureRecords.Clear();
        foreach (var kv in _gridPoints)
        {
            var key = kv.Key;
            var go = kv.Value;
            if (go == null)
                continue;
            var mgp = go.GetComponent<MapGridPoint>();
            if (mgp == null)
                continue;
            if (string.IsNullOrEmpty(mgp.FeatureTypeId))
                continue;
            var rec = new FeatureRecord
            {
                row = key.x,
                col = key.y,
                typeId = mgp.FeatureTypeId,
                name = mgp.FeatureName,
            };

            // Copy typed feature properties from the MapGridPoint
            var sprops = mgp.GetAllStringFeatureProperties();
            if (sprops != null && sprops.Count > 0)
            {
                foreach (var p in sprops)
                {
                    rec.stringProperties.Add(
                        new FeatureRecord.PropertyString { key = p.key, value = p.value }
                    );
                }
            }

            var bprops = mgp.GetAllBoolFeatureProperties();
            if (bprops != null && bprops.Count > 0)
            {
                foreach (var p in bprops)
                {
                    rec.boolProperties.Add(
                        new FeatureRecord.PropertyBool { key = p.key, value = p.value }
                    );
                }
            }

            var iprops = mgp.GetAllIntFeatureProperties();
            if (iprops != null && iprops.Count > 0)
            {
                foreach (var p in iprops)
                {
                    rec.intProperties.Add(
                        new FeatureRecord.PropertyInt { key = p.key, value = p.value }
                    );
                }
            }

            var fprops = mgp.GetAllFloatFeatureProperties();
            if (fprops != null && fprops.Count > 0)
            {
                foreach (var p in fprops)
                {
                    rec.floatProperties.Add(
                        new FeatureRecord.PropertyFloat { key = p.key, value = p.value }
                    );
                }
            }
            _featureRecords.Add(rec);
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // Apply serialized feature records back onto MapGridPoint children
    public void LoadFeatureLayer()
    {
        if (_featureRecords == null || _featureRecords.Count == 0)
            return;
        foreach (var rec in _featureRecords)
        {
            var mgp = GetGridPoint(rec.row, rec.col);
            if (mgp != null)
            {
                mgp.SetFeatureTypeId(rec.typeId);
                mgp.FeatureName = rec.name ?? string.Empty;
                // Ensure defaults from ScriptableObject are applied for this feature
                // so the editor shows default values until the user overrides them.
                mgp.ApplyDefaultsForFeature(rec.typeId);
                // Restore any serialized typed properties
                if (rec.stringProperties != null && rec.stringProperties.Count > 0)
                {
                    foreach (var pr in rec.stringProperties)
                    {
                        if (pr != null && !string.IsNullOrEmpty(pr.key))
                            mgp.SetStringFeatureProperty(pr.key, pr.value ?? string.Empty);
                    }
                }
                if (rec.boolProperties != null && rec.boolProperties.Count > 0)
                {
                    foreach (var pr in rec.boolProperties)
                    {
                        if (pr != null && !string.IsNullOrEmpty(pr.key))
                            mgp.SetBoolFeatureProperty(pr.key, pr.value);
                    }
                }
                if (rec.intProperties != null && rec.intProperties.Count > 0)
                {
                    foreach (var pr in rec.intProperties)
                    {
                        if (pr != null && !string.IsNullOrEmpty(pr.key))
                            mgp.SetIntFeatureProperty(pr.key, pr.value);
                    }
                }
                if (rec.floatProperties != null && rec.floatProperties.Count > 0)
                {
                    foreach (var pr in rec.floatProperties)
                    {
                        if (pr != null && !string.IsNullOrEmpty(pr.key))
                            mgp.SetFloatFeatureProperty(pr.key, pr.value);
                    }
                }
            }
        }
    }

    // Ensure grid points exist and the internal index is populated. If there are no
    // child points, create them. If there are children but the index is empty, rebuild it.
    public void EnsureGridPoints()
    {
        // First check if dimensions match child count
        int expectedCount = _gridWidth * _gridHeight;
        int actualCount = 0;
        foreach (Transform child in transform)
        {
            if (child != null && child.GetComponent<MapGridPoint>() != null)
                actualCount++;
        }

        // If counts don't match, rebuild dictionary
        if (
            actualCount != expectedCount
            || _gridPoints == null
            || _gridPoints.Count != expectedCount
        )
        {
            if (actualCount > 0)
            {
                RebuildGridDictionary();
            }
            else
            {
                CreateChildrenPoints();
            }
        }
        else if (_gridPoints == null || _gridPoints.Count == 0)
        {
            if (transform.childCount == 0)
            {
                CreateChildrenPoints();
            }
            else
            {
                RebuildGridDictionary();
            }
        }
        // Ensure children positions reflect the current scale/offset
        RepositionGridPoints();
    }

    // Reposition existing child MapGridPoints to match current grid scale/offset.
    private void RepositionGridPoints()
    {
        if (_gridPoints == null || _gridPoints.Count == 0)
            return;

        foreach (var kv in _gridPoints)
        {
            var key = kv.Key;
            var go = kv.Value;
            if (go == null)
                continue;
            go.transform.localPosition =
                new Vector3(key.x * _gridScale, 0, key.y * _gridScale) + _gridOffset;
        }
    }
#if UNITY_EDITOR
    // Called in the editor when serialized values change. Keep child positions in sync
    // with the `Grid Scale` and `Grid Offset` inspector fields.
    private void OnValidate()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // Rebuild dictionary if needed, then reposition children to reflect current scale/offset
        if (_gridPoints == null || _gridPoints.Count == 0)
        {
            if (transform.childCount > 0)
                RebuildGridDictionary();
        }

        RepositionGridPoints();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    public MapGridPoint GetGridPoint(int row, int col)
    {
        if (_gridPoints.TryGetValue(new Vector2Int(row, col), out var point))
        {
            return point.GetComponent<MapGridPoint>();
        }
        return null;
    }

    // Public getters used by editor tools
    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public float GridScale => _gridScale;
    public Vector3 GridOffset => _gridOffset;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // draw a rectangle representing the grid bounds, with a sphere in the center of the four corner points
        // use the offset and scale to calculate the positions
        Vector3 topLeft = transform.position + new Vector3(0, 0, 0) + _gridOffset;
        Vector3 topRight =
            transform.position + new Vector3((_gridWidth - 1) * _gridScale, 0, 0) + _gridOffset;
        Vector3 bottomLeft =
            transform.position + new Vector3(0, 0, (_gridHeight - 1) * _gridScale) + _gridOffset;
        Vector3 bottomRight =
            transform.position
            + new Vector3((_gridWidth - 1) * _gridScale, 0, (_gridHeight - 1) * _gridScale)
            + _gridOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
        var corners = new Vector3[] { topLeft, topRight, bottomLeft, bottomRight };
        foreach (var corner in corners)
        {
            Gizmos.DrawSphere(corner, 1f);
        }

        // Draw computed raycast points (if available) for quick visual verification
        if (_single3dHeightMeshRaycastPoints != null && _single3dHeightMeshRaycastPoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            float s = Mathf.Max(0.05f, _gridScale * 0.2f);
            foreach (var p in _single3dHeightMeshRaycastPoints)
            {
                Gizmos.DrawSphere(p, s);
            }
        }
    }
#endif
}
