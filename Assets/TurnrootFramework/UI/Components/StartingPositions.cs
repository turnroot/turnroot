using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public class StartingPositions : MonoBehaviour
    {
        public List<GameObject> TileProjectors;
        public GameObject Selected;
        public GameObject Swap;
        public UnitCellDataOnly SelectedUnit;
        public GameObject SwapGraphic;
        public UnitCellDataOnly SwapUnit;

        private MapGrid _mapGrid;
        private Dictionary<Vector2Int, GameObject> _unitModels = new();
        private BattlePreparationObject _prepObject;
        private bool _replaced = false;

        public OperationResult Initialize(BattlePreparationObject battlePrep)
        {
            _mapGrid = battlePrep.MapGrid;

            if (_replaced)
            {
                return OperationResult.SuccessResult();
            }

            var spawnPoints = _mapGrid.PlayerTeamSpawnPoints;
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                return OperationResult.Failure("Improper starting positions");
            }

            SetupProjectors(spawnPoints);
            HideAllVisuals();

            _prepObject = battlePrep;
            ReplaceOtherInstances();

            if (ShouldWaitForPlacements())
            {
                SubscribeToEvents();
                return OperationResult.SuccessResult();
            }

            SpawnAllUnitModels();
            return OperationResult.SuccessResult();
        }

        private void SetupProjectors(List<Vector2Int> positions)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                SetProjectorPosition(i, positions[i]);
            }

            for (int i = positions.Count; i < TileProjectors.Count; i++)
            {
                TileProjectors[i].SetActive(false);
            }
        }

        private void SetProjectorPosition(int index, Vector2Int coords)
        {
            var worldPos = _mapGrid.GetTerrainAdjustedWorldPosition(coords);
            TileProjectors[index].transform.position =
                worldPos + Vector3.up * (_mapGrid.GridScale / 2f);
        }

        private void HideAllVisuals()
        {
            Selected.SetActive(false);
            Swap.SetActive(false);
            SwapGraphic.SetActive(false);
            SelectedUnit.gameObject.SetActive(false);
            SwapUnit.gameObject.SetActive(false);
        }

        private bool ShouldWaitForPlacements() =>
            _prepObject.placements == null || _prepObject.placements.Count == 0;

        private void SubscribeToEvents()
        {
            if (_prepObject.Brain == null)
            {
                return;
            }

            _prepObject.Brain.OnPlacementsInitialized += HandlePlacementsInitialized;
            _prepObject.Brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (_prepObject?.Brain == null)
            {
                return;
            }

            _prepObject.Brain.OnPlacementsInitialized -= HandlePlacementsInitialized;
            _prepObject.Brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
        }

        private void ReplaceOtherInstances()
        {
            var others = FindObjectsByType<StartingPositions>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            foreach (var other in others)
            {
                if (other != null && other != this)
                {
                    other.ReplaceBy(this);
                }
            }
        }

        public void SetSelected(Vector2Int coords)
        {
            var worldPos = _mapGrid.GetTerrainAdjustedWorldPosition(coords);
            Selected.transform.position = worldPos + Vector3.up * (_mapGrid.GridScale / 2f);
            ShowWithFade(Selected);
            HideWithFade(Swap);
            HideWithFade(SwapGraphic);
        }

        public void SetSwap(Vector2Int coords)
        {
            var worldPos = _mapGrid.GetTerrainAdjustedWorldPosition(coords);
            Swap.transform.position = worldPos + Vector3.up * (_mapGrid.GridScale / 2f);
            ShowWithFade(Swap);
        }

        public void Clears()
        {
            HideWithFade(Selected);
            HideWithFade(Swap);
            HideWithFade(SwapGraphic);
            HideWithFade(SelectedUnit.gameObject);
            HideWithFade(SwapUnit.gameObject);
            SelectedUnit.ClearData();
            SwapUnit.ClearData();
        }

        public void SetSelectedUnit(string name, string className, Sprite portrait)
        {
            SelectedUnit.SetData(name, className, portrait);
            ShowWithFade(SelectedUnit.gameObject);
        }

        public void SetSwapUnit(string name, string className, Sprite portrait)
        {
            SwapUnit.SetData(name, className, portrait);
            ShowWithFade(SwapUnit.gameObject);
            ShowWithFade(SwapGraphic);
        }

        public void ClearSelectedUnit() => SelectedUnit.ClearData();

        public void ClearSwapUnit()
        {
            SwapUnit.ClearData();
            HideWithFade(SwapUnit.gameObject);
            HideWithFade(SwapGraphic);
        }

        public void ClearSwapPreview()
        {
            HideWithFade(Swap);
            SwapUnit.ClearData();
            HideWithFade(SwapUnit.gameObject);
            HideWithFade(SwapGraphic);
        }

        private OperationResult ShowWithFade(GameObject go)
        {
            if (go == null)
            {
                return OperationResult.Failure("ShowWithFade: GameObject is null.");
            }

            go.SetActive(true);
            var fade = UIFadeCache.Get(go);
            if (fade != null)
            {
                fade.Show();
            }
            return OperationResult.SuccessResult();
        }

        private OperationResult HideWithFade(GameObject go)
        {
            if (go == null || !go.activeInHierarchy)
            {
                return OperationResult.Failure("HideWithFade: GameObject is null or not active.");
            }

            var fade = UIFadeCache.Get(go);
            if (fade != null)
            {
                fade.Hide();
            }
            else
            {
                go.SetActive(false);
            }
            return OperationResult.SuccessResult();
        }

        private void SpawnAllUnitModels()
        {
            if (_replaced || _prepObject?.placements == null)
            {
                return;
            }

            CleanupOrphanedModels();
            DespawnExistingModels();

            foreach (var placement in _prepObject.placements)
            {
                _prepObject.Brain.unitAppearanceBrain.SpawnUnitModelOnGrid(
                    placement.Key,
                    placement.Value,
                    _unitModels,
                    prebattle: true
                );
            }
        }

        private void CleanupOrphanedModels()
        {
            var validIds = new HashSet<string>(
                _prepObject
                    .placements.Values.Where(p => p != null && !string.IsNullOrEmpty(p.Id))
                    .Select(p => p.Id)
            );

            var ownerships = FindObjectsByType<UnitModelOwnership>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            foreach (var own in ownerships)
            {
                if (own == null || string.IsNullOrEmpty(own.UnitId))
                {
                    continue;
                }

                if (validIds.Contains(own.UnitId))
                {
                    continue;
                }

                DestroyModel(own.gameObject);
                RemoveModelMapping(own.gameObject);
            }
        }

        private void DespawnExistingModels()
        {
            if (_unitModels.Count == 0 || _prepObject?.Brain == null)
            {
                return;
            }

            var positions = _unitModels.Keys.ToList();
            foreach (var pos in positions)
            {
                _prepObject.Brain.unitAppearanceBrain.DespawnUnitModelFromGrid(pos, _unitModels);
            }
        }

        private void DestroyModel(GameObject model)
        {
            try
            {
                model.SetActive(false);
            }
            catch { }
            Destroy(model);
        }

        private void RemoveModelMapping(GameObject model)
        {
            var keys = _unitModels.Where(kvp => kvp.Value == model).Select(kvp => kvp.Key).ToList();
            foreach (var key in keys)
            {
                _unitModels.Remove(key);
            }
        }

        private void HandlePlacementsInitialized()
        {
            if (_prepObject?.Brain == null)
            {
                return;
            }

            _prepObject.Brain.OnPlacementsInitialized -= HandlePlacementsInitialized;

            if (_prepObject.placements != null && _prepObject.placements.Count > 0)
            {
                SpawnAllUnitModels();
            }
        }

        private void HandleUnitSelectionChanged(CharacterInstance unit, bool selected)
        {
            if (_prepObject?.Brain == null)
            {
                return;
            }

            _prepObject.InitializePlacements();
            SpawnAllUnitModels();
        }

        public void ReplaceBy(StartingPositions newOwner)
        {
            _replaced = true;

            if (_prepObject != null)
            {
                _prepObject.StartingPositionsComponent = newOwner;
            }

            UnsubscribeFromEvents();
            DespawnAllModels();
        }

        public void DespawnAllModels()
        {
            if (_unitModels == null || _unitModels.Count == 0)
            {
                return;
            }

            var positions = _unitModels.Keys.ToList();
            foreach (var pos in positions)
            {
                if (_prepObject?.Brain != null)
                {
                    _prepObject.Brain.unitAppearanceBrain.DespawnUnitModelFromGrid(
                        pos,
                        _unitModels
                    );
                }
                else if (_unitModels.TryGetValue(pos, out var model) && model != null)
                {
                    DestroyModel(model);
                    _unitModels.Remove(pos);
                }
            }
        }

        public OperationResult SwapModels(Vector2Int posA, Vector2Int posB)
        {
            if (_unitModels == null)
            {
                return OperationResult.Failure("No unit models to swap");
            }

            if (
                !_unitModels.TryGetValue(posA, out var modelA)
                || !_unitModels.TryGetValue(posB, out var modelB)
            )
            {
                return OperationResult.Failure("One or both positions do not have unit models");
            }

            if (modelA == null || modelB == null)
            {
                return OperationResult.Failure("One or both unit models are null");
            }

            _unitModels[posA] = modelB;
            _unitModels[posB] = modelA;

            UpdateModelPosition(modelA, posB);
            UpdateModelPosition(modelB, posA);
            PublishSwapEvent(modelA, modelB, posA, posB);

            return OperationResult.SuccessResult();
        }

        public OperationResult MoveModel(Vector2Int from, Vector2Int to)
        {
            if (_unitModels == null)
            {
                return OperationResult.Failure("No unit models to move");
            }

            if (!_unitModels.TryGetValue(from, out var model))
            {
                return OperationResult.Failure("Source position does not have a unit model");
            }

            if (model == null)
            {
                return OperationResult.Failure("Unit model is null");
            }

            _unitModels.Remove(from);
            _unitModels[to] = model;

            UpdateModelPosition(model, to);
            PublishMoveEvent(model, from, to);

            return OperationResult.SuccessResult();
        }

        private void UpdateModelPosition(GameObject model, Vector2Int pos)
        {
            var worldPos = _mapGrid.GetTerrainAdjustedWorldPosition(pos);
            model.transform.position = worldPos + Vector3.up * (_mapGrid.GridScale / 2f);
        }

        private void PublishSwapEvent(
            GameObject modelA,
            GameObject modelB,
            Vector2Int posA,
            Vector2Int posB
        )
        {
            try
            {
                var idA = modelA.GetComponent<UnitModelOwnership>()?.UnitId;
                var idB = modelB.GetComponent<UnitModelOwnership>()?.UnitId;

                _prepObject.Brain?.Publish(
                    new Gameplay.Brain.Events.ModelSwappedEvent(
                        idA,
                        idB,
                        posA,
                        posB,
                        modelA,
                        modelB
                    )
                );
            }
            catch { }
        }

        private void PublishMoveEvent(GameObject model, Vector2Int from, Vector2Int to)
        {
            try
            {
                var owner = model.GetComponent<UnitModelOwnership>();
                var id = owner?.UnitId;
                CharacterInstance inst = null;

                if (!string.IsNullOrEmpty(id))
                {
                    var all = _prepObject.Brain?.gamewideContextBrain?.GetAllActiveInstances();
                    inst = all?.FirstOrDefault(u => u != null && u.Id == id);
                }

                _prepObject.Brain?.Publish(
                    new Gameplay.Brain.Events.ModelMovedEvent(inst, id, from, to, model)
                );
            }
            catch { }
        }

        private void OnDestroy()
        {
            DespawnAllModels();
            UnsubscribeFromEvents();
        }
    }
}
