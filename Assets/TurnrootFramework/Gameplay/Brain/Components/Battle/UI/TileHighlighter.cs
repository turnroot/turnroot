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
        private Vector2 _atlasGridSize = new Vector2(4, 4);

        [SerializeField]
        private Vector2Int _moveRangeAtlasPos = new Vector2Int(0, 0);

        [SerializeField]
        private Vector2Int _attackRangeAtlasPos = new Vector2Int(1, 0);

        [SerializeField]
        private Vector2Int _healRangeAtlasPos = new Vector2Int(2, 0);

        [SerializeField]
        private Vector2Int _dangerZoneAtlasPos = new Vector2Int(0, 1);

        [SerializeField]
        private Vector2Int _pathPreviewAtlasPos = new Vector2Int(1, 1);

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
        private Vector4 _pathPreviewUVParams;

        private Material _moveRangeMaterial;
        private Material _attackRangeMaterial;
        private Material _healRangeMaterial;
        private Material _dangerZoneMaterial;
        private Material _pathPreviewMaterial;

        private Dictionary<Vector2Int, DecalProjector> _decalCache = new();
        private HashSet<Vector2Int> _activeMoveTiles = new();
        private HashSet<Vector2Int> _activeAttackTiles = new();
        private HashSet<Vector2Int> _activeHealTiles = new();
        private HashSet<Vector2Int> _activeDangerTiles = new();
        private HashSet<Vector2Int> _activePathTiles = new();

        private MapGrid _mapGrid;

        private const string UV_OFFSET_PROPERTY = "_BaseMap_ST";

        private Brain _brain;

        #endregion

        #region Unity Lifecycle

        public void Initialize(Brain brain)
        {
            _brain = brain;
            SubscribeToBrainEvents();
            CalculateUVParameters();
            CreateMaterials();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBrainEvents();
            CleanupMaterials();
        }

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
            _pathPreviewUVParams = CalculateUVParams(
                _pathPreviewAtlasPos.x,
                _pathPreviewAtlasPos.y,
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

        private OperationResult CreateMaterials()
        {
            if (_baseMaterial == null)
            {
                return OperationResult.Failure("Base material is not assigned");
            }

            _moveRangeMaterial = CreateMaterialWithUV(_moveRangeUVParams);
            _attackRangeMaterial = CreateMaterialWithUV(_attackRangeUVParams);
            _healRangeMaterial = CreateMaterialWithUV(_healRangeUVParams);
            _dangerZoneMaterial = CreateMaterialWithUV(_dangerZoneUVParams);
            _pathPreviewMaterial = CreateMaterialWithUV(_pathPreviewUVParams);
            return OperationResult.SuccessResult();
        }

        private Material CreateMaterialWithUV(Vector4 uvParams)
        {
            Material mat = new Material(_baseMaterial);
            mat.SetVector(UV_OFFSET_PROPERTY, uvParams);
            return mat;
        }

        private void CleanupMaterials()
        {
            if (_moveRangeMaterial != null)
            {
                Destroy(_moveRangeMaterial);
            }

            if (_attackRangeMaterial != null)
            {
                Destroy(_attackRangeMaterial);
            }

            if (_healRangeMaterial != null)
            {
                Destroy(_healRangeMaterial);
            }

            if (_dangerZoneMaterial != null)
            {
                Destroy(_dangerZoneMaterial);
            }

            if (_pathPreviewMaterial != null)
            {
                Destroy(_pathPreviewMaterial);
            }
        }

        public void Initialize(MapGrid mapGrid)
        {
            _mapGrid = mapGrid;
            PrewarmDecalCache();
        }

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
                    var worldPos = _mapGrid.GetTerrainAdjustedWorldPosition(pos);

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

            return OperationResult.SuccessResult();
        }

        #endregion

        #region Public API

        public void HighlightMoveTiles(IEnumerable<Vector2Int> tiles)
        {
            ClearMoveTiles();
            BatchHighlightTiles(tiles, _moveRangeMaterial, _activeMoveTiles);
        }

        public void HighlightAttackTiles(IEnumerable<Vector2Int> tiles)
        {
            ClearAttackTiles();
            BatchHighlightTiles(tiles, _attackRangeMaterial, _activeAttackTiles);
        }

        public void HighlightHealTiles(IEnumerable<Vector2Int> tiles)
        {
            ClearHealTiles();
            BatchHighlightTiles(tiles, _healRangeMaterial, _activeHealTiles);
        }

        public void HighlightDangerTiles(IEnumerable<Vector2Int> tiles)
        {
            ClearDangerTiles();
            BatchHighlightTiles(tiles, _dangerZoneMaterial, _activeDangerTiles);
        }

        public void HighlightPathPreview(IEnumerable<Vector2Int> tiles)
        {
            ClearPathPreview();
            BatchHighlightTiles(tiles, _pathPreviewMaterial, _activePathTiles);
        }

        public void ClearMoveTiles() => BatchClearTiles(_activeMoveTiles);

        public void ClearAttackTiles() => BatchClearTiles(_activeAttackTiles);

        public void ClearHealTiles() => BatchClearTiles(_activeHealTiles);

        public void ClearDangerTiles() => BatchClearTiles(_activeDangerTiles);

        public void ClearPathPreview() => BatchClearTiles(_activePathTiles);

        public void ClearAll()
        {
            ClearMoveTiles();
            ClearAttackTiles();
            ClearHealTiles();
            ClearDangerTiles();
            ClearPathPreview();
        }

        #endregion

        #region Private Operations

        private void BatchHighlightTiles(
            IEnumerable<Vector2Int> tiles,
            Material material,
            HashSet<Vector2Int> activeSet
        )
        {
            foreach (var tile in tiles)
            {
                if (_decalCache.TryGetValue(tile, out var decal))
                {
                    decal.material = material;
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

        #endregion

        #region Brain Events

        private void SubscribeToBrainEvents()
        {
            _brain.OnBattleMapReady += HandleBattleMapReady;
            _brain.OnBattleCompleted += HandleBattleCompleted;
        }

        private void UnsubscribeFromBrainEvents()
        {
            _brain.OnBattleMapReady -= HandleBattleMapReady;
            _brain.OnBattleCompleted -= HandleBattleCompleted;
        }

        private void HandleBattleMapReady(MapGrid mapGrid) => Initialize(mapGrid);

        private void HandleBattleCompleted(Combat.BattleExitType exitType) => ClearAll();

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                CalculateUVParameters();
                CreateMaterials();
            }
        }
#endif
    }
}
