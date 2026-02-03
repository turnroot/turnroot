using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Turnroot.Gameplay.Brain.Components.Battle
{
    /// <summary>
    /// Manages tile highlighting using URP Decal Projectors.
    /// </summary>
    public partial class TileHighlighter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Decal Setup")]
        [SerializeField]
        private GameObject _decalProjectorPrefab;

        [SerializeField]
        private Material _baseMaterial;

        [Header("UV Atlas Configuration")]
        [SerializeField]
        private Vector2 _atlasGridSize = new(4, 4);

        [SerializeField]
        private Vector2Int _moveRangeAtlasPos = new(0, 0);

        [SerializeField]
        private Vector2Int _attackRangeAtlasPos = new(1, 0);

        [SerializeField]
        private Vector2Int _healRangeAtlasPos = new(2, 0);

        [SerializeField]
        private Vector2Int _dangerZoneAtlasPos = new(0, 1);

        [SerializeField]
        private Vector2Int _pathStart = new(0, 1);

        [SerializeField]
        private Vector2Int _pathStraight = new(0, 1);

        [SerializeField]
        private Vector2Int _pathCorner = new(0, 1);

        [SerializeField]
        private Vector2Int _pathEnd = new(0, 1);

        [Header("Advanced")]
        [SerializeField]
        private float _atlasPadding = 0.001f;

        [SerializeField]
        private float _projectionDepth = 2f;

        #endregion

        #region Private Fields

        private const int MAX_PATH_LENGTH = 15;

        private Vector4 _moveRangeUVParams;
        private Vector4 _attackRangeUVParams;
        private Vector4 _healRangeUVParams;
        private Vector4 _dangerZoneUVParams;
        private Vector4 _pathStartUVParams;
        private Vector4 _pathStraightUVParams;
        private Vector4 _pathCornerUVParams;
        private Vector4 _pathEndUVParams;

        private Dictionary<Vector2Int, DecalProjector> _decalCache = new();
        private HashSet<Vector2Int> _activeMoveTiles = new();
        private HashSet<Vector2Int> _activeAttackTiles = new();
        private HashSet<Vector2Int> _activeHealTiles = new();
        private HashSet<Vector2Int> _activeDangerTiles = new();

        private DecalProjector[] _pathDecalPool;
        private int _activePathDecalCount = 0;

        private MapGrid _mapGrid;
        private Brain _brain;

        #endregion

        #region Unity Lifecycle

        public void Initialize(Brain brain, MapGrid mapGrid)
        {
            _brain = brain;
            _mapGrid = mapGrid;
            SubscribeToBrainEvents();
            CalculateUVParameters();
            EnsureBaseMaterial();
            PrewarmDecalCache();
            InitializePathDecalPool();
        }

        private void OnDestroy() => UnsubscribeFromBrainEvents();

        #endregion

        #region Initialization

        private void CalculateUVParameters()
        {
            float tileU = (1f / _atlasGridSize.x) - (_atlasPadding * 2f);
            float tileV = (1f / _atlasGridSize.y) - (_atlasPadding * 2f);

            _moveRangeUVParams = CalculateUVParams(_moveRangeAtlasPos, tileU, tileV);
            _attackRangeUVParams = CalculateUVParams(_attackRangeAtlasPos, tileU, tileV);
            _healRangeUVParams = CalculateUVParams(_healRangeAtlasPos, tileU, tileV);
            _dangerZoneUVParams = CalculateUVParams(_dangerZoneAtlasPos, tileU, tileV);
            _pathStartUVParams = CalculateUVParams(_pathStart, tileU, tileV);
            _pathStraightUVParams = CalculateUVParams(_pathStraight, tileU, tileV);
            _pathCornerUVParams = CalculateUVParams(_pathCorner, tileU, tileV);
            _pathEndUVParams = CalculateUVParams(_pathEnd, tileU, tileV);
        }

        private Vector4 CalculateUVParams(Vector2Int atlasPos, float tileU, float tileV)
        {
            float offsetU = (atlasPos.x / _atlasGridSize.x) + _atlasPadding;
            float offsetV = (atlasPos.y / _atlasGridSize.y) + _atlasPadding;
            return new Vector4(tileU, tileV, offsetU, offsetV);
        }

        private OperationResult EnsureBaseMaterial() =>
            _baseMaterial == null
                ? OperationResult.Failure("Base material is not assigned")
                : OperationResult.Successful();

        private OperationResult PrewarmDecalCache()
        {
            if (_decalProjectorPrefab == null)
            {
                return OperationResult.Failure("Decal projector prefab is not assigned");
            }

            for (int x = 0; x < _mapGrid.GridWidth; x++)
            {
                for (int y = 0; y < _mapGrid.GridHeight; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var decal = CreateDecal($"TileHighlight_{x}_{y}", pos);
                    if (decal == null)
                    {
                        return OperationResult.Failure("Failed to create decal projector");
                    }

                    _decalCache[pos] = decal;
                    decal.gameObject.SetActive(false);
                }
            }

            return OperationResult.Successful();
        }

        private OperationResult InitializePathDecalPool()
        {
            if (_decalProjectorPrefab == null)
            {
                return OperationResult.Failure("Decal projector prefab is not assigned");
            }

            _pathDecalPool = new DecalProjector[MAX_PATH_LENGTH];

            for (int i = 0; i < MAX_PATH_LENGTH; i++)
            {
                var decal = CreateDecal($"PathDecal_{i}", Vector2Int.zero);
                if (decal == null)
                {
                    return OperationResult.Failure("Failed to create path decal projector");
                }

                _pathDecalPool[i] = decal;
                decal.gameObject.SetActive(false);
            }

            return OperationResult.Successful();
        }

        private DecalProjector CreateDecal(string name, Vector2Int gridPos)
        {
            var worldPos =
                gridPos == Vector2Int.zero
                    ? Vector3.up * 10f
                    : _mapGrid.GetTerrainAdjustedWorldPosition(gridPos) + (Vector3.up * 10f);

            var decalObj = Instantiate(_decalProjectorPrefab, worldPos, Quaternion.Euler(90, 0, 0));
            decalObj.name = name;
            decalObj.transform.SetParent(transform);

            var decal = decalObj.GetComponent<DecalProjector>();
            if (decal != null)
            {
                decal.size = new Vector3(_mapGrid.GridScale, _mapGrid.GridScale, _projectionDepth);
            }

            return decal;
        }

        #endregion

        #region Private Operations

        private void BatchHighlightTiles(
            IEnumerable<Vector2Int> tiles,
            Vector4 uvParams,
            HashSet<Vector2Int> activeSet
        )
        {
            foreach (var tile in tiles)
            {
                if (_decalCache.TryGetValue(tile, out var decal))
                {
                    ApplyUVToDecal(decal, uvParams);
                    decal.gameObject.SetActive(true);
                    activeSet.Add(tile);
                }
            }
        }

        private void BatchClearTiles(HashSet<Vector2Int> activeSet)
        {
            foreach (var tile in activeSet)
            {
                if (_decalCache.TryGetValue(tile, out var decal))
                {
                    decal.gameObject.SetActive(false);
                }
            }
            activeSet.Clear();
        }

        private void RenderPathDecal(
            int index,
            Vector2Int gridPos,
            Vector4 uvParams,
            float rotation
        )
        {
            var decal = _pathDecalPool[index];
            if (decal == null)
            {
                return;
            }

            decal.transform.position =
                _mapGrid.GetTerrainAdjustedWorldPosition(gridPos) + (Vector3.up * 10f);
            ApplyUVToDecal(decal, uvParams);
            decal.transform.localEulerAngles = new Vector3(90f, rotation, 0f);
            decal.gameObject.SetActive(true);
        }

        private OperationResult ApplyUVToDecal(DecalProjector decal, Vector4 uvParams)
        {
            if (decal == null)
            {
                return OperationResult.Failure("ApplyUVToDecal: decal is null");
            }

            try
            {
                decal.uvScale = new Vector2(uvParams.x, uvParams.y);
                decal.uvBias = new Vector2(uvParams.z, uvParams.w);
                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure(
                    $"ApplyUVToDecal: Failed to set UV on {decal.gameObject.name}: {ex.Message}"
                );
            }
        }

        #endregion


        #region Brain Events

        private void SubscribeToBrainEvents()
        {
            _brain.OnBattleMapReady += HandleBattleMapReady;
            _brain.OnBattleCompleted += HandleBattleCompleted;
        }

        private void UnsubscribeFromBrainEvents()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnBattleMapReady -= HandleBattleMapReady;
            _brain.OnBattleCompleted -= HandleBattleCompleted;
        }

        private void HandleBattleMapReady(MapGrid mapGrid) => Initialize(_brain, mapGrid);

        private void HandleBattleCompleted(Combat.BattleExitType exitType) => ClearAll();

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                CalculateUVParameters();
            }
        }
#endif
    }
}
