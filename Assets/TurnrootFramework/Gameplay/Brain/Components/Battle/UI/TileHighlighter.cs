using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Turnroot.Gameplay.Brain.Components.Battle
{
    /// <summary>
    /// Manages tile highlighting using URP Decal Projectors.
    /// Each highlight type uses a unique material instance with UV atlas offsets
    /// to display different colors from a single texture atlas.
    /// </summary>
    public class TileHighlighter : MonoBehaviour
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

        [Header("Advanced")]
        [SerializeField]
        private float _atlasPadding = 0.001f;

        [SerializeField]
        private float _projectionDepth = 2f;

        #endregion

        #region Private Fields

        private Vector4 _moveRangeUVParams;
        private Vector4 _attackRangeUVParams;
        private Vector4 _healRangeUVParams;
        private Vector4 _dangerZoneUVParams;
        private Dictionary<Vector2Int, DecalProjector> _decalCache = new();
        private HashSet<Vector2Int> _activeMoveTiles = new();
        private HashSet<Vector2Int> _activeAttackTiles = new();
        private HashSet<Vector2Int> _activeHealTiles = new();
        private HashSet<Vector2Int> _activeDangerTiles = new();

        private MapGrid _mapGrid;

        private const string UV_OFFSET_PROPERTY = "_BaseMap_ST";

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
        }

        private void OnDestroy() => UnsubscribeFromBrainEvents();

        #endregion

        #region Initialization

        private void CalculateUVParameters()
        {
            float tileU = (1f / _atlasGridSize.x) - (_atlasPadding * 2f);
            float tileV = (1f / _atlasGridSize.y) - (_atlasPadding * 2f);

            _moveRangeUVParams = CalculateUVParams(
                _moveRangeAtlasPos.x,
                _moveRangeAtlasPos.y,
                tileU,
                tileV
            );
            _attackRangeUVParams = CalculateUVParams(
                _attackRangeAtlasPos.x,
                _attackRangeAtlasPos.y,
                tileU,
                tileV
            );
            _healRangeUVParams = CalculateUVParams(
                _healRangeAtlasPos.x,
                _healRangeAtlasPos.y,
                tileU,
                tileV
            );
            _dangerZoneUVParams = CalculateUVParams(
                _dangerZoneAtlasPos.x,
                _dangerZoneAtlasPos.y,
                tileU,
                tileV
            );
        }

        private Vector4 CalculateUVParams(int atlasX, int atlasY, float tileU, float tileV)
        {
            float offsetU = (atlasX / _atlasGridSize.x) + _atlasPadding;
            float offsetV = (atlasY / _atlasGridSize.y) + _atlasPadding;
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
                    var worldPos =
                        _mapGrid.GetTerrainAdjustedWorldPosition(pos) + (Vector3.up * 10f);

                    var decalObj = Instantiate(
                        _decalProjectorPrefab,
                        worldPos,
                        Quaternion.Euler(90, 0, 0)
                    );
                    decalObj.name = $"TileHighlight_{x}_{y}";
                    decalObj.transform.SetParent(transform);

                    var decal = decalObj.GetComponent<DecalProjector>();
                    if (decal != null)
                    {
                        decal.size = new Vector3(
                            _mapGrid.GridScale,
                            _mapGrid.GridScale,
                            _projectionDepth
                        );
                        _decalCache[pos] = decal;
                    }
                    else
                    {
                        return OperationResult.Failure(
                            "Decal projector prefab is missing DecalProjector component"
                        );
                    }

                    decalObj.SetActive(false);
                }
            }

            return OperationResult.Successful();
        }

        #endregion

        #region Public API

        public enum HighlightType
        {
            Move,
            Attack,
            Heal,
            Danger,
            PathPreview,
        }

        public void HighlightTiles(IEnumerable<Vector2Int> tiles, HighlightType highlightType)
        {
            switch (highlightType)
            {
                case HighlightType.Move:
                    ClearMoveTiles();
                    BatchHighlightTiles(tiles, _moveRangeUVParams, _activeMoveTiles);
                    break;
                case HighlightType.Attack:
                    ClearAttackTiles();
                    BatchHighlightTiles(tiles, _attackRangeUVParams, _activeAttackTiles);
                    break;
                case HighlightType.Heal:
                    ClearHealTiles();
                    BatchHighlightTiles(tiles, _healRangeUVParams, _activeHealTiles);
                    break;
                case HighlightType.Danger:
                    ClearDangerTiles();
                    BatchHighlightTiles(tiles, _dangerZoneUVParams, _activeDangerTiles);
                    break;
            }
        }

        public void ClearMoveTiles() => BatchClearTiles(_activeMoveTiles);

        public void ClearAttackTiles() => BatchClearTiles(_activeAttackTiles);

        public void ClearHealTiles() => BatchClearTiles(_activeHealTiles);

        public void ClearDangerTiles() => BatchClearTiles(_activeDangerTiles);

        public void ClearAll()
        {
            ClearMoveTiles();
            ClearAttackTiles();
            ClearHealTiles();
            ClearDangerTiles();
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
                    SetDecalActive(decal, true);
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
                    SetDecalActive(decal, false);
                }
            }
            activeSet.Clear();
        }

        private void SetDecalActive(DecalProjector decal, bool active)
        {
            if (decal.gameObject.activeSelf != active)
            {
                decal.gameObject.SetActive(active);
            }
        }

        private OperationResult ApplyUVToDecal(DecalProjector decal, Vector4 uvParams)
        {
            if (decal == null)
            {
                return OperationResult.Failure("ApplyUVToDecal: decal is null");
            }

            try
            {
                // Map uvParams: x=tileU, y=tileV, z=offsetU, w=offsetV
                decal.uvScale = new Vector2(uvParams.x, uvParams.y);
                decal.uvBias = new Vector2(uvParams.z, uvParams.w);

                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                // Fatal: if DecalProjector API isn't available or fails, surface the error so the caller knows
                return OperationResult.Failure(
                    $"ApplyUVToDecal: Failed to set uvScale/uvBias on {decal.gameObject.name}: {ex.Message}"
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
