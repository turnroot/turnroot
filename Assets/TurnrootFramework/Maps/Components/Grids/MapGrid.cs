using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Represents a tactical map grid with terrain, spawn points, and 3D height mesh integration.
    /// </summary>
    public partial class MapGrid : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Appearance"), HorizontalLine(color: EColor.Orange)]
        [InfoBox("If enabled, the height mesh will be used as the visual terrain also")]
        public bool UseHeightMeshAsTerrainModel = false;

        [
            InfoBox("The main terrain level model for this map"),
            HideIf(nameof(UseHeightMeshAsTerrainModel))
        ]
        public GameObject TerrainLevelModel;

        [InfoBox(
            "Models hidden in top-down view (roofs, trees, etc). Visible during combat animations"
        )]
        public GameObject[] HideOnTopDownLayerModels;

        [InfoBox(
            "Models that are only visible in top-down view (e.g. tree bases). Hidden during combat animations"
        )]

        public GameObject[] ShowOnTopDownLayerModels;

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

        [SerializeField]
        private Vector3 _gridOffset = Vector3.zero;

        [field: SerializeField]
        public string MapName { get; set; } = string.Empty;

        [SerializeField, ReadOnly]
        private Dictionary<Vector2Int, GameObject> _gridPoints = new();
        private Dictionary<Vector2Int, MapGridPoint> _cachedGridPoints;

        [
            SerializeField,
            ReadOnly,
            Tooltip(
                "Serialized feature layer records for editor features such as chests, doors, etc."
            )
        ]
        private List<FeatureRecord> _features = new();

        [Header("3D Map Height Connection"), HorizontalLine(color: EColor.Blue)]
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

        [
            SerializeField,
            Tooltip("Show gizmo spheres for computed raycast points in the Scene view")
        ]
        private bool _showRaycastGizmos = true;

        [SerializeField, Tooltip("Show coordinate labels for raycast points in the Scene view")]
        private bool _showRaycastCoordinates = true;

        [SerializeField, Tooltip("Layer mask used when raycasting to the 3D map.")]
        private LayerMask _raycastLayerMask = ~0;

        [field:
            SerializeField,
            InfoBox(
                "Changing width/height requires Create New Points. Or use Add Row/Column buttons."
            )
        ]
        public int GridWidth { get; private set; } = 10;

        [field: SerializeField]
        public int GridHeight { get; private set; } = 10;

        [Header("Grid Settings"), HorizontalLine(color: EColor.Green)]
        [SerializeField, InfoBox("Grid scale adjusts cell size. Scale 1 = ~8x8 feet per cell.")]
        private float _gridScale = 1f;
        #endregion

        #region Properties
        public Sprite FullMapImage => _fullMapImage;
        public Sprite StandardMapImage => _standardMapImage;
        public Sprite UnexploredMapImage => _unexploredMapImage;
        public float GridScale => _gridScale * 2.5f; // Multiply by 2.5 to match real-world scale
        public Vector3 GridOffset => _gridOffset;
        public LayerMask RaycastLayerMask => _raycastLayerMask;
        public int StateVersion { get; private set; }
        public event Action OnStateVersionChanged;
        #endregion

        #region Grid Creation & Management
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
                if (GetGridPoint(col, newRow) == null)
                {
                    CreateGridPoint(col, newRow);
                }
            }

            LoadFeatureLayer();
            RebuildEditorCacheAndMarkDirty();
        }

        [Button("Add Column")]
        public void AddColumn()
        {
            SaveFeatureLayer();
            GridWidth++;
            int newCol = GridWidth - 1;

            for (int row = 0; row < GridHeight; row++)
            {
                if (GetGridPoint(newCol, row) == null)
                {
                    CreateGridPoint(newCol, row);
                }
            }

            LoadFeatureLayer();
            RebuildEditorCacheAndMarkDirty();
        }

        [Button("Remove Row")]
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
            RebuildEditorCacheAndMarkDirty();
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
            RebuildEditorCacheAndMarkDirty();
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
                if (child != null && child.TryGetComponent<MapGridPoint>(out var mgp))
                {
                    var key = new Vector2Int(mgp.Row, mgp.Col);
                    newDict[key] = child.gameObject;
                    newCache[key] = mgp;
                }
            }

            _gridPoints = newDict;
            _cachedGridPoints = newCache;

            foreach (var mgp in _cachedGridPoints.Values)
            {
                mgp?.Initialize(mgp.Row, mgp.Col);
            }

            LoadFeatureLayer();
        }
        #endregion

        #region 3D Height Connection

        [Button("Connect to 3D Map Height")]
        public OperationResult ConnectTo3DMapObject()
        {
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
            _single3dHeightMesh?.SetActive(false);
            _single3dHeightMesh = null;

            _single3dHeightMeshRaycastPoints = null;
            _single3dHeightMeshRaycastIndices = null;
            _single3dHeightMeshRaycastColors = null;
            UseHeightMeshAsTerrainModel = false;

            RebuildRaycastColors();
            BuildTerrainPositionLookup();
            MarkDirty();
        }
        #endregion

        #region Map Rendering
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
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        public void SetEditorPointColor(Vector2Int cell, Color color) =>
            _gridPointColorCache[cell] = color;

        public void RebuildEditorPointColorCache()
        {
            _gridPointColorCache.Clear();
            EnsureCachedGridPoints();

            foreach (
                var (key, mgp) in _cachedGridPoints ?? new Dictionary<Vector2Int, MapGridPoint>()
            )
            {
                Color c = mgp?.GetCachedTerrainType()?.EditorColor ?? Color.yellow;
                _gridPointColorCache[key] = c;
            }
        }

        public bool TryGetEditorPointColor(Vector2Int cell, out Color color) =>
            _gridPointColorCache.TryGetValue(cell, out color);

        public void ClearEditorPointColorCache() => _gridPointColorCache.Clear();
#endif

        private void RebuildEditorCacheAndMarkDirty()
        {
#if UNITY_EDITOR
            RebuildEditorPointColorCache();
#endif
            MarkDirty();
        }
        #endregion
    }

    [Serializable]
    public class FeatureRecord
    {
        public int row,
            col;
        public string typeId,
            name;
        public bool locked;
        public ObjectItem unlockItem;
        public ObjectItem commonItem;
        public ObjectItem rareItem;
        public List<Vector2Int> warpDestinations = new();
        public int activeWarpIndex;

        public int breakableHealth;
        public float healingPercentPerTurn;
        public int rangedRange;
        public int rangedDamage;
        public float rangedHit;
        public bool rangedAllowsRiding;
        public bool rangedAllowsFlying;
        public bool rangedMagicOnly;
        public bool shelterNoFly;
        public bool shelterNoRide;
        public bool shelterNoInfantry;
    }
}
