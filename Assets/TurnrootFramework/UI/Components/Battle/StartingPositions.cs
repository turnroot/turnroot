using System.Collections.Generic;
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
                    "StartingPositions: Detected waiting for placements - invoking InitializePlacements on prep object".LogInfo();
                    var res = _prepObject.InitializePlacements();
                    if (!res.Success)
                    {
                        $"StartingPositions: InitializePlacements returned failure: {res.ErrorMessage}".LogWarning();
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
                _mapGrid.EnsureGridPoints();
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
                    // Ensure single subscription
                    anyBrain.OnBattlePrepObjectInitialized -= HandleBrainPrepInitialized;
                    anyBrain.OnBattlePrepObjectInitialized += HandleBrainPrepInitialized;
                }
                return;
            }

            // Make subscription idempotent to avoid duplicate handlers when UI is opened multiple times.
            _prepObject.Brain.OnPlacementsInitialized -= HandlePlacementsInitialized;
            _prepObject.Brain.OnPlacementsInitialized += HandlePlacementsInitialized;

            _prepObject.Brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
            _prepObject.Brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;
        }

        private void UnsubscribeFromEvents()
        {
            var anyBrain = FindFirstObjectByType<Brain>();
            if (anyBrain != null)
            {
                anyBrain.OnBattlePrepObjectInitialized -= HandleBrainPrepInitialized;
            }

            if (_prepObject.Brain == null)
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

            // Primary work (cleanup + spawn) is performed by SpawnAllUnitModels_Impl.

            $"SpawnAllUnitModels: spawn points={_prepObject.PlayerTeamSpawnPoints.Count}, placements={_prepObject.placements.Count}".LogInfo();
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
            // No-op: UnitAppearanceBrain now owns all model tracking
            // This method is kept for compatibility with CleanupOrphanedModels but does nothing
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
            if (_prepObject?.placements == null || _prepObject.placements.Count == 0)
            {
                return;
            }

            var gw = _prepObject.Brain?.gamewideContextBrain;
            if (gw == null)
            {
                return;
            }

            // Despawn all models by getting unit IDs from placements (source of truth)
            foreach (var placement in _prepObject.placements)
            {
                var inst = gw.FindInstanceByTemplate(placement.Value);
                if (inst != null && !string.IsNullOrEmpty(inst.Id))
                {
                    if (_prepObject.Brain != null)
                    {
                        _prepObject.Brain.unitAppearanceBrain.DespawnUnit(inst.Id);
                    }
                }
            }
        }

        public OperationResult SwapModels(Vector2Int posA, Vector2Int posB)
        {
            // Get unit IDs from placements (source of truth)
            if (
                !_prepObject.placements.TryGetValue(posA, out var dataA)
                || !_prepObject.placements.TryGetValue(posB, out var dataB)
            )
            {
                return OperationResult.Failure("One or both positions do not have placements");
            }

            // Find instances for these templates
            var gw = _prepObject.Brain?.gamewideContextBrain;
            if (gw == null)
            {
                return OperationResult.Failure("No gamewide context available");
            }

            var instA = gw.FindInstanceByTemplate(dataA);
            var instB = gw.FindInstanceByTemplate(dataB);

            if (instA == null || instB == null)
            {
                return OperationResult.Failure("Could not find instances for one or both units");
            }

            // Get models from UnitAppearanceBrain (single source of truth for models)
            var modelA = _prepObject.Brain.unitAppearanceBrain.GetModelForUnit(instA.Id);
            var modelB = _prepObject.Brain.unitAppearanceBrain.GetModelForUnit(instB.Id);

            if (modelA == null || modelB == null)
            {
                return OperationResult.Failure("One or both unit models are null");
            }

            // Move transforms to swapped positions
            UpdateModelPosition(modelA, posB);
            UpdateModelPosition(modelB, posA);

            // Publish event so UnitAppearanceBrain can update its tracking
            PublishSwapEvent(modelA, modelB, posA, posB);

            return OperationResult.Successful();
        }

        public OperationResult MoveModel(Vector2Int from, Vector2Int to)
        {
            // Get unit data from placements (source of truth)
            if (!_prepObject.placements.TryGetValue(from, out var data))
            {
                return OperationResult.Failure("Source position does not have a placement");
            }

            // Find instance for this template
            var gw = _prepObject.Brain?.gamewideContextBrain;
            if (gw == null)
            {
                return OperationResult.Failure("No gamewide context available");
            }

            var inst = gw.FindInstanceByTemplate(data);
            if (inst == null)
            {
                return OperationResult.Failure("Could not find instance for unit");
            }

            // Get model from UnitAppearanceBrain (single source of truth for models)
            var model = _prepObject.Brain.unitAppearanceBrain.GetModelForUnit(inst.Id);

            var validation = OperationResultGuards.RequireNotNull(model, nameof(model));
            if (!validation.Success)
            {
                return validation;
            }

            // Move transform to new position
            UpdateModelPosition(model, to);

            // Publish event so UnitAppearanceBrain can update its tracking
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
