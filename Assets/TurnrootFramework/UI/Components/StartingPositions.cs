using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Manages the UI for selecting and placing units at their starting positions on the battle map.
    /// </summary>
    public partial class StartingPositions : MonoBehaviour
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
        private bool _gridPointsEnsured = false;

        public OperationResult Initialize(BattlePreparationObject battlePrep)
        {
            _mapGrid = battlePrep.MapGrid;

            if (_replaced)
            {
                return OperationResult.Successful();
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
                // If we have a Brain available, proactively initialize placements so the UI doesn't wait indefinitely
                if (_prepObject?.Brain != null)
                {
                    TurnrootLogger.Log(
                        "StartingPositions: Detected waiting for placements - invoking InitializePlacements on prep object",
                        TurnrootLogger.LogLevel.Info
                    );
                    var res = _prepObject.InitializePlacements();
                    if (!res.Success)
                    {
                        TurnrootLogger.Log(
                            $"StartingPositions: InitializePlacements returned failure: {res.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                    // If placements are now available, proceed to spawn models immediately
                    if (_prepObject?.placements != null && _prepObject.placements.Count > 0)
                    {
                        SpawnAllUnitModels();
                        SubscribeToEvents();
                        return OperationResult.Successful();
                    }
                }

                SubscribeToEvents();
                return OperationResult.Successful();
            }

            SpawnAllUnitModels();
            return OperationResult.Successful();
        }

        private void SetupProjectors(List<Vector2Int> positions)
        {
            if (!_gridPointsEnsured)
            {
                _mapGrid?.EnsureGridPoints();
                _gridPointsEnsured = true;
            }

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
                var anyBrain = FindFirstObjectByType<Brain>();
                if (anyBrain != null)
                {
                    anyBrain.OnBattlePrepObjectInitialized += HandleBrainPrepInitialized;
                }
                return;
            }

            _prepObject.Brain.OnPlacementsInitialized += HandlePlacementsInitialized;
            _prepObject.Brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;
        }

        private void UnsubscribeFromEvents()
        {
            var anyBrain = FindFirstObjectByType<Brain>();
            if (anyBrain != null)
            {
                anyBrain.OnBattlePrepObjectInitialized -= HandleBrainPrepInitialized;
            }

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
            UIFadeHelpers.ShowWithFade(Selected);
            UIFadeHelpers.HideWithFade(Swap);
            UIFadeHelpers.HideWithFade(SwapGraphic);
        }

        public void SetSwap(Vector2Int coords)
        {
            var worldPos = _mapGrid.GetTerrainAdjustedWorldPosition(coords);
            Swap.transform.position = worldPos + Vector3.up * (_mapGrid.GridScale / 2f);
            UIFadeHelpers.ShowWithFade(Swap);
        }

        public void Clears()
        {
            UIFadeHelpers.HideWithFade(Selected);
            UIFadeHelpers.HideWithFade(Swap);
            UIFadeHelpers.HideWithFade(SwapGraphic);
            UIFadeHelpers.HideWithFade(SelectedUnit.gameObject);
            UIFadeHelpers.HideWithFade(SwapUnit.gameObject);
            SelectedUnit.ClearData();
            SwapUnit.ClearData();
        }

        public void SetSelectedUnit(string name, string className, Sprite portrait)
        {
            SelectedUnit.SetData(name, className, portrait);
            UIFadeHelpers.ShowWithFade(SelectedUnit.gameObject);
        }

        public void SetSwapUnit(string name, string className, Sprite portrait)
        {
            SwapUnit.SetData(name, className, portrait);
            UIFadeHelpers.ShowWithFade(SwapUnit.gameObject);
            UIFadeHelpers.ShowWithFade(SwapGraphic);
        }

        public void ClearSelectedUnit() => SelectedUnit.ClearData();

        public void ClearSwapUnit()
        {
            SwapUnit.ClearData();
            UIFadeHelpers.HideWithFade(SwapUnit.gameObject);
            UIFadeHelpers.HideWithFade(SwapGraphic);
        }

        public void ClearSwapPreview()
        {
            UIFadeHelpers.HideWithFade(Swap);
            SwapUnit.ClearData();
            UIFadeHelpers.HideWithFade(SwapUnit.gameObject);
            UIFadeHelpers.HideWithFade(SwapGraphic);
        }

        private void SpawnAllUnitModels()
        {
            SpawnAllUnitModels_Impl();

            if (!_gridPointsEnsured)
            {
                _mapGrid?.EnsureGridPoints();
                _gridPointsEnsured = true;
            }

            CleanupOrphanedModels();
            DespawnExistingModels();

            TurnrootLogger.Log(
                $"SpawnAllUnitModels: spawn points={_prepObject.PlayerTeamSpawnPoints?.Count ?? 0}, placements={_prepObject.placements?.Count ?? 0}"
            );

            // Check for duplicate spawn points
            if (
                _prepObject.PlayerTeamSpawnPoints != null
                && _prepObject.PlayerTeamSpawnPoints.Count
                    != _prepObject.PlayerTeamSpawnPoints.Distinct().Count()
            )
            {
                TurnrootLogger.Log(
                    "SpawnAllUnitModels: Duplicate PlayerTeamSpawnPoints detected",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            foreach (var placement in _prepObject.placements)
            {
                var unit = placement.Value;
                var pos = placement.Key;

                // Only spawn if the position is a valid player spawn point
                if (
                    _prepObject.PlayerTeamSpawnPoints == null
                    || !_prepObject.PlayerTeamSpawnPoints.Contains(pos)
                )
                {
                    TurnrootLogger.Log(
                        $"SpawnAllUnitModels: Skipping spawn for {unit?.CharacterTemplate?.DisplayName ?? "<null>"} at {pos} - not a valid player spawn point",
                        TurnrootLogger.LogLevel.Warning
                    );
                    continue;
                }

                var spawnResult = _prepObject.Brain.unitAppearanceBrain.SpawnUnitAtPosition(
                    unit: placement.Value,
                    position: placement.Key,
                    prebattle: true
                );
                if (!spawnResult.Success)
                {
                    TurnrootLogger.Log(
                        $"SpawnAllUnitModels: Failed to spawn at {placement.Key}: {spawnResult.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                    continue;
                }

                // Track the spawned model in our local dictionary
                var model = _prepObject.Brain.unitAppearanceBrain.GetModelForUnit(unit.Id);
                if (model != null)
                {
                    _unitModels[placement.Key] = model;
                    TurnrootLogger.Log(
                        $"SpawnAllUnitModels: Model spawned for {unit?.CharacterTemplate?.DisplayName} at {placement.Key}"
                    );
                }
                else
                {
                    TurnrootLogger.Log(
                        $"SpawnAllUnitModels: Model spawned but not found for {unit?.CharacterTemplate?.DisplayName} at {placement.Key}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
        }

        private void CleanupOrphanedModels() => CleanupOrphanedModels_Impl();

        private void DespawnExistingModels() => DespawnExistingModels_Impl();

        private void DestroyModel(GameObject model)
        {
            model.SetActive(false);

            Destroy(model);
        }

        // Thin wrappers that forward to the extracted implementations in the EventHandlers partial
        private void HandlePlacementsInitialized() => HandlePlacementsInitialized_Impl();

        private void HandleUnitSelectionChanged(CharacterInstance unit, bool selected) =>
            HandleUnitSelectionChanged_Impl(unit, selected);

        private void HandleBrainPrepInitialized(BattlePreparationObject prep) =>
            HandleBrainPrepInitialized_Impl(prep);

        private void RemoveModelMapping(GameObject model)
        {
            var keys = _unitModels.Where(kvp => kvp.Value == model).Select(kvp => kvp.Key).ToList();
            foreach (var key in keys)
            {
                _unitModels.Remove(key);
            }
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
                    _prepObject.Brain.unitAppearanceBrain.DespawnUnitAtPosition(pos);
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

            return OperationResult.Successful();
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

            var validation = OperationResultGuards.RequireNotNull(model, nameof(model));
            if (!validation.Success)
            {
                return validation;
            }

            _unitModels.Remove(from);
            _unitModels[to] = model;

            UpdateModelPosition(model, to);
            PublishMoveEvent(model, from, to);

            return OperationResult.Successful();
        }

        private void OnDestroy()
        {
            DespawnAllModels();
            UnsubscribeFromEvents();
        }
    }
}
