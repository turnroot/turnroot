using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGrid : MonoBehaviour
    {
        [Header("Appearance"), HorizontalLine(color: EColor.Orange)]
        [InfoBox("If enabled, the height mesh will be used as the visual terrain also")]
        public bool UseHeightMeshAsTerrainModel = false;

        [
            InfoBox("The main terrain level model for this map"),
            HideIf(nameof(UseHeightMeshAsTerrainModel))
        ]
        public GameObject TerrainLevelModel;

        [InfoBox(
            "Optional decorative layers (buildings, foliage). These cover the whole map. For smaller or dynamic effects like birds, water, etc, don't use this, add those objects directly to the scene."
        )]
        public GameObject[] AdditionalDecorativeModels;

        [Header("Player Team Spawn Points"), HorizontalLine(color: EColor.Yellow)]
        public List<Vector2Int> PlayerTeamSpawnPoints = new();

        [HideInInspector]
        public BattleGameObject battleGameObject;

        [Header("Rendered Map Images")]
        [SerializeField, ReadOnly]
        private Sprite _fullMapImage;

        [SerializeField, ReadOnly]
        private Sprite _standardMapImage;

        [SerializeField, ReadOnly]
        private Sprite _unexploredMapImage;

        // Public accessors
        public Sprite FullMapImage => _fullMapImage;
        public Sprite StandardMapImage => _standardMapImage;
        public Sprite UnexploredMapImage => _unexploredMapImage;

        [SerializeField]
        private Vector3 _gridOffset = Vector3.zero;

        [field: SerializeField]
        public string MapName { get; set; } = string.Empty;

        [SerializeField, ReadOnly]
        private Dictionary<Vector2Int, GameObject> _gridPoints = new();
        private Dictionary<Vector2Int, MapGridPoint> _cachedGridPoints;

        [SerializeField, ReadOnly]
        [Tooltip(
            "Serialized feature layer records (second layer) for editor features such as chests, doors, etc."
        )]
        private List<FeatureRecord> _features = new();

        [Header("3D Map Height Connection")]
        [HorizontalLine(color: EColor.Blue)]
        [SerializeField]
        private GameObject _single3dHeightMesh;

        [SerializeField, HideInInspector]
        private Vector3[] _single3dHeightMeshRaycastPoints;
        private Color[] _single3dHeightMeshRaycastColors;

        [SerializeField, HideInInspector]
        private Vector2Int[] _single3dHeightMeshRaycastIndices;

        private Dictionary<Vector2Int, Vector3> _terrainPositionLookup;

#if UNITY_EDITOR
        private Dictionary<Vector2Int, Color> _gridPointColorCache = new();
#endif

        [SerializeField]
        [Tooltip("Show gizmo spheres for computed raycast points in the Scene view")]
        private bool _showRaycastGizmos = true;

        [SerializeField]
        [Tooltip("Show coordinate labels for raycast points in the Scene view")]
        private bool _showRaycastCoordinates = true;

        [Button("Reset Mesh Scale")]
        private void ResetMeshScale()
        {
            if (_single3dHeightMesh == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "MapGrid: No 3D height mesh assigned. Please assign a mesh to reset."
                );
#endif
                return;
            }

            _single3dHeightMesh.transform.localScale = Vector3.one;

#if UNITY_EDITOR
            Debug.Log("MapGrid: Reset mesh scale to (1, 1, 1)");
#endif
        }

        [SerializeField]
        [Tooltip(
            "Layer mask used when raycasting to the 3D map. Use this to limit raycasts to the map's layer(s)."
        )]
        private LayerMask _raycastLayerMask = ~0;
        public LayerMask RaycastLayerMask => _raycastLayerMask;

        [field:
            SerializeField,
            InfoBox(
                "Changing the width/height will not create new points- you need to Create New Points after changing these values. Or, you can Add a Row or a Column, whch will add a single line of points."
            )
        ]
        public int GridWidth { get; private set; } = 10;

        [field: SerializeField]
        public int GridHeight { get; private set; } = 10;

        [Header("Grid Settings")]
        [HorizontalLine(color: EColor.Green)]
        [SerializeField]
        [field: InfoBox(
            "Grid scale adjusts the size of each grid cell. If your models are at real world scale, 1 gives you about 8x8 feet per cell."
        )]
        private float _gridScale = 1f;
        public float GridScale => _gridScale * 2.5f; // Multiply by 2.5 to match real-world scale better;

        // this makes each grid cell about 8x8 feet at scale 1, which gives a good amount of arm room and room for a paired unit
        public Vector3 GridOffset => _gridOffset;

        /* -------------------------- Buttons -------------------------- */
        [Button("Create Grid Points")]
        public void CreateChildrenPoints()
        {
            if (_gridPoints.Count > 0)
            {
                ClearGrid();
            }

            _cachedGridPoints = new Dictionary<Vector2Int, MapGridPoint>(GridWidth * GridHeight);

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    CreateGridPoint(x, y);
                }
            }

            LoadFeatureLayer();
#if UNITY_EDITOR
            // Ensure editor cache is populated after grid creation so OnDrawGizmos uses correct colors
            RebuildEditorPointColorCache();
#endif
        }

        [Button("Add Row")]
        public void AddRow()
        {
            SaveFeatureLayer();
            GridHeight++;
            int newRow = GridHeight - 1;

            for (int col = 0; col < GridWidth; col++)
            {
                if (GetGridPoint(col, newRow) != null)
                {
                    continue;
                }

                CreateGridPoint(col, newRow);
            }

            LoadFeatureLayer();
#if UNITY_EDITOR
            RebuildEditorPointColorCache();
#endif
            MarkDirty();
        }

        [Button("Add Column")]
        public void AddColumn()
        {
            SaveFeatureLayer();
            GridWidth++;
            int newCol = GridWidth - 1;

            for (int row = 0; row < GridHeight; row++)
            {
                if (GetGridPoint(newCol, row) != null)
                {
                    continue;
                }

                CreateGridPoint(newCol, row);
            }

            LoadFeatureLayer();
#if UNITY_EDITOR
            RebuildEditorPointColorCache();
#endif
            MarkDirty();
        }

        [Button("Remove Row")]
        [Tooltip("Removes the last row from the grid. This doesn't remove the existing data.")]
        public void RemoveRow()
        {
            if (GridHeight <= 1)
            {
                return;
            }

            SaveFeatureLayer();
            RemoveGridLine(GridHeight - 1, true);
            GridHeight--;
            LoadFeatureLayer();
#if UNITY_EDITOR
            RebuildEditorPointColorCache();
#endif
            MarkDirty();
        }

        [Button("Remove Column")]
        public void RemoveColumn()
        {
            if (GridWidth <= 1)
            {
                return;
            }

            SaveFeatureLayer();
            RemoveGridLine(GridWidth - 1, false);
            GridWidth--;
            LoadFeatureLayer();
#if UNITY_EDITOR
            RebuildEditorPointColorCache();
#endif
            MarkDirty();
        }

        [Button("Connect to 3D Map Height")]
        public OperationResult ConnectTo3DMapObject()
        {
            TurnrootLogger.Log("MapGrid: Connecting to 3D map object for height adjustment.");
            if (_single3dHeightMesh == null)
            {
                return OperationResult.Failure("No 3D height mesh assigned.");
            }

            EnsureGridPoints();

            var colliders = _single3dHeightMesh.GetComponentsInChildren<Collider>(true);
            if (colliders == null || colliders.Length == 0)
            {
                return OperationResult.Failure("No colliders found on the 3D height mesh object.");
            }

            var connector = new MapGridHeightConnector();
            var points = connector.RaycastPointsDownTo3DMap(
                _single3dHeightMesh,
                _gridPoints,
                _raycastLayerMask
            );

            if (points == null || points.Length == 0)
            {
                return OperationResult.Failure(
                    "Failed to compute raycast points on the 3D height mesh."
                );
            }

            _single3dHeightMeshRaycastPoints = points;
            RebuildRaycastColors();
            BuildTerrainPositionLookup();
            MarkDirty();
            return OperationResult.Successful();
        }

        [Button("Remove Height Connection")]
        public void RemoveHeightConnection()
        {
            TurnrootLogger.Log(
                "MapGrid: Removing 3D height mesh connection and clearing computed data."
            );

            if (_single3dHeightMesh != null)
            {
                _single3dHeightMesh.SetActive(false);
                _single3dHeightMesh = null;
            }

            _single3dHeightMeshRaycastPoints = null;
            _single3dHeightMeshRaycastIndices = null;
            _single3dHeightMeshRaycastColors = null;

            UseHeightMeshAsTerrainModel = false;
            RebuildRaycastColors();
            BuildTerrainPositionLookup();
            MarkDirty();
        }

        [Button("Render Map Images")]
        public void RenderMapImages()
        {
#if UNITY_EDITOR
            var renderer = new MapGridRenderer();
            renderer.RenderAndSaveMapImages(
                this,
                out _fullMapImage,
                out _standardMapImage,
                out _unexploredMapImage
            );
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public MapGridPoint GetGridPoint(int row, int col)
        {
            var key = new Vector2Int(row, col);

            // Try cached dictionary first (fast path)
            if (
                _cachedGridPoints != null
                && _cachedGridPoints.TryGetValue(key, out var cached)
                && cached != null
            )
            {
                return cached;
            }

            // Fallback to GetComponent if cache miss (rebuilds cache entry)
            if (_gridPoints.TryGetValue(key, out var point) && point != null)
            {
                if (point.TryGetComponent<MapGridPoint>(out var mgp))
                {
                    _cachedGridPoints ??= new Dictionary<Vector2Int, MapGridPoint>();
                    _cachedGridPoints[key] = mgp;
                }
                return mgp;
            }

            return null;
        }

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
            _cachedGridPoints?.Clear();
        }

        public void RebuildGridDictionary()
        {
            var newDict = new Dictionary<Vector2Int, GameObject>();
            var newCache = new Dictionary<Vector2Int, MapGridPoint>();

            foreach (Transform child in transform)
            {
                if (child == null)
                {
                    continue;
                }

                if (child.TryGetComponent<MapGridPoint>(out var mgp))
                {
                    var key = new Vector2Int(mgp.Row, mgp.Col);
                    newDict[key] = child.gameObject;
                    newCache[key] = mgp;
                }
            }
            _gridPoints = newDict;
            _cachedGridPoints = newCache;

            foreach (var kv in _cachedGridPoints)
            {
                var mgp = kv.Value;
                if (mgp == null)
                {
                    continue;
                }

                mgp.Initialize(mgp.Row, mgp.Col);
            }

            LoadFeatureLayer();
        }

        public void SaveFeatureLayer()
        {
            EnsureCachedGridPoints();

            _features.Clear();
            foreach (var kv in _cachedGridPoints)
            {
                var mgp = kv.Value;
                if (mgp == null || string.IsNullOrEmpty(mgp.FeatureTypeId))
                {
                    continue;
                }

                _features.Add(
                    new FeatureRecord
                    {
                        row = kv.Key.x,
                        col = kv.Key.y,
                        typeId = mgp.FeatureTypeId,
                        name = mgp.FeatureName,
                        // Only create property lists if there are properties to save
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

        public Vector3 GetMapGridPointWorldLocation(MapGridPoint gridPoint)
        {
            var key = new Vector2Int(gridPoint.Row, gridPoint.Col);
            return _gridPoints.TryGetValue(key, out var point) && point != null
                ? point.transform.position
                : Vector3.zero;
        }

        public Vector3 GetTerrainAdjustedWorldPosition(Vector2Int gridCoordinates)
        {
            if (
                _terrainPositionLookup != null
                && _terrainPositionLookup.TryGetValue(
                    gridCoordinates,
                    out var terrainAdjustedPosition
                )
            )
            {
                return terrainAdjustedPosition;
            }

            var gridPoint = GetGridPoint(gridCoordinates.x, gridCoordinates.y);
            return gridPoint != null ? GetMapGridPointWorldLocation(gridPoint) : Vector3.zero;
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
                if (mgp != null && mgp.FeatureType == featureType)
                {
                    points.Add(mgp);
                }
            }
            return points.Count == 0 ? null : points;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor API: set cached color for a specific grid cell. Used by editor tools to keep gizmo colors in sync.
        /// </summary>
        public void SetEditorPointColor(Vector2Int cell, Color color)
        {
            _gridPointColorCache[cell] = color;
        }

        /// <summary>
        /// Rebuilds the entire editor color cache from current grid points.
        /// </summary>
        public void RebuildEditorPointColorCache()
        {
            _gridPointColorCache.Clear();
            EnsureCachedGridPoints();
            foreach (var kv in _cachedGridPoints ?? new Dictionary<Vector2Int, MapGridPoint>())
            {
                var mgp = kv.Value;
                Color c = Color.yellow;
                if (mgp != null)
                {
                    var tt = mgp.GetCachedTerrainType();
                    c = tt?.EditorColor ?? Color.yellow;
                }
                _gridPointColorCache[kv.Key] = c;
            }
        }

        public bool TryGetEditorPointColor(Vector2Int cell, out Color color) =>
            _gridPointColorCache.TryGetValue(cell, out color);

        public void ClearEditorPointColorCache() => _gridPointColorCache.Clear();
#endif

        public int GetManhattanDistance(MapGridPoint a, MapGridPoint b) =>
            a == null || b == null ? -1 : Mathf.Abs(a.Row - b.Row) + Mathf.Abs(a.Col - b.Col);

        public int StateVersion { get; private set; }
        public event System.Action OnStateVersionChanged;

        public void IncrementStateVersion()
        {
            StateVersion++;
            OnStateVersionChanged?.Invoke();
        }

        public OperationResult SetOccupied(MapGridPoint point, CharacterInstance occupier)
        {
            EnsureCachedGridPoints();

            var key = new Vector2Int(point.Row, point.Col);
            if (_cachedGridPoints != null && _cachedGridPoints.TryGetValue(key, out var mgp))
            {
                mgp.CurrentInstance = occupier;
                IncrementStateVersion();
                return OperationResult.Successful();
            }
            return OperationResult.Failure(
                $"Set occupied for point ({point.Row}, {point.Col}) failed"
            );
        }

        public OperationResult RemoveOccupied(MapGridPoint point)
        {
            EnsureCachedGridPoints();

            var key = new Vector2Int(point.Row, point.Col);
            if (_cachedGridPoints != null && _cachedGridPoints.TryGetValue(key, out var mgp))
            {
                mgp.CurrentInstance = null;
                IncrementStateVersion();
                return OperationResult.Successful();
            }
            return OperationResult.Failure(
                $"Remove occupied for point ({point.Row}, {point.Col}) failed"
            );
        }

        public void GetAllOccupiedPoints()
        {
            EnsureCachedGridPoints();

            var occupiedPoints = new List<MapGridPoint>();
            var occupyingInstances = new List<CharacterInstance>();

            foreach (var mgp in _cachedGridPoints.Values)
            {
                if (mgp != null && mgp.IsOccupied && mgp.CurrentInstance != null)
                {
                    occupiedPoints.Add(mgp);
                    occupyingInstances.Add(mgp.CurrentInstance);
#if UNITY_EDITOR
                    Debug.Log(
                        $"Occupied Point: ({mgp.Row}, {mgp.Col}) by {mgp.CurrentInstance.Id}"
                    );
#endif
                }
            }
        }
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
        public List<PropertyRecord<bool>> boolProperties = new();
        public List<PropertyRecord<UnityEvent>> eventProperties = new();
        public List<PropertyRecord<float>> floatProperties = new();

        public List<PropertyRecord<CharacterInstance>> unitProperties = new();
        public List<PropertyRecord<ObjectItemInstance>> objectItemProperties = new();
    }
}
