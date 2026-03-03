using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    /// <summary>
    /// Unified placement management - handles BOTH logical placement data AND visual model tracking.
    /// Single source of truth for all position and model operations.
    /// </summary>
    public partial class BattlePreparationObject
    {
        #region Model Tracking State

        // Model tracking - maps position to the spawned GameObject
        private Dictionary<Vector2Int, GameObject> _positionToModel = new();

        // Reverse lookup - maps position to unit ID for quick lookups
        private Dictionary<Vector2Int, string> _positionToUnitId = new();

        // Forward lookup - maps unit ID to position for reverse queries
        private Dictionary<string, Vector2Int> _unitIdToPosition = new();

        // Default placements before user customization (for reset functionality)
        private Dictionary<Vector2Int, CharacterData> _defaultPlacements = new();

        #endregion

        #region Model Queries

        /// <summary>
        /// Get the model GameObject at a specific position.
        /// </summary>
        public GameObject GetModelAtPosition(Vector2Int position)
        {
            return _positionToModel.TryGetValue(position, out var model) ? model : null;
        }

        /// <summary>
        /// Get the model GameObject for a specific unit ID.
        /// </summary>
        public GameObject GetModelForUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return null;
            }

            if (_unitIdToPosition.TryGetValue(unitId, out var position))
            {
                var model = GetModelAtPosition(position);
                $"[MODEL TRACKING] GetModelForUnit({unitId}): found position={position}, model={model?.name ?? "null"}".LogInfo();
                return model;
            }

            $"[MODEL TRACKING] GetModelForUnit({unitId}): NOT FOUND in _unitIdToPosition".LogInfo();
            return null;
        }

        /// <summary>
        /// Get the unit ID at a specific position.
        /// </summary>
        public string GetUnitIdAtPosition(Vector2Int position)
        {
            return _positionToUnitId.TryGetValue(position, out var unitId) ? unitId : null;
        }

        /// <summary>
        /// Get the position where a unit ID is located.
        /// </summary>
        public Vector2Int? GetPositionForUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return null;
            }

            return _unitIdToPosition.TryGetValue(unitId, out var position)
                ? position
                : (Vector2Int?)null;
        }

        /// <summary>
        /// Check if a model exists at a position.
        /// </summary>
        public bool HasModelAtPosition(Vector2Int position)
        {
            return _positionToModel.ContainsKey(position) && _positionToModel[position] != null;
        }

        /// <summary>
        /// Get all spawned models with their positions.
        /// </summary>
        public IEnumerable<(Vector2Int position, GameObject model, string unitId)> GetAllModels()
        {
            foreach (var kvp in _positionToModel)
            {
                if (kvp.Value != null)
                {
                    var unitId = GetUnitIdAtPosition(kvp.Key);
                    yield return (kvp.Key, kvp.Value, unitId);
                }
            }
        }

        #endregion

        #region Model Registration (Low-Level)

        /// <summary>
        /// Register a model at a position. Called by UnitAppearanceBrain after spawning.
        /// </summary>
        public OperationResult RegisterModel(Vector2Int position, GameObject model, string unitId)
        {
            var validation = OperationResultGuards.RequireNotNull(model, nameof(model));
            if (!validation.Success)
            {
                return validation;
            }

            if (string.IsNullOrEmpty(unitId))
            {
                return OperationResult.Failure("Unit ID cannot be null or empty");
            }

            $"[MODEL TRACKING] RegisterModel called: position={position}, unitId={unitId}, model={model.name}".LogInfo();

            // Clear any existing model at this position
            if (_positionToModel.ContainsKey(position))
            {
                var existingUnitId = GetUnitIdAtPosition(position);
                $"RegisterModel: Replacing existing model at {position} (was unitId={existingUnitId})".LogWarning();
                UnregisterModelAtPosition(position);
            }

            // Clear any existing model for this unit ID
            if (_unitIdToPosition.ContainsKey(unitId))
            {
                var oldPos = _unitIdToPosition[unitId];
                $"RegisterModel: Unit {unitId} moving from {oldPos} to {position}".LogInfo();
                UnregisterModelAtPosition(oldPos);
            }

            _positionToModel[position] = model;
            _positionToUnitId[position] = unitId;
            _unitIdToPosition[unitId] = position;

            $"[MODEL TRACKING] After registration: _unitIdToPosition[{unitId}]={position}, _positionToUnitId[{position}]={unitId}".LogInfo();

            return OperationResult.Successful();
        }

        /// <summary>
        /// Unregister model at a position. Called before despawning.
        /// </summary>
        public OperationResult UnregisterModelAtPosition(Vector2Int position)
        {
            if (!_positionToModel.ContainsKey(position))
            {
                return OperationResult.Successful(); // Already unregistered
            }

            var unitId = GetUnitIdAtPosition(position);
            if (!string.IsNullOrEmpty(unitId))
            {
                _unitIdToPosition.Remove(unitId);
            }

            _positionToModel.Remove(position);
            _positionToUnitId.Remove(position);

            return OperationResult.Successful();
        }

        /// <summary>
        /// Unregister model by unit ID. Called before despawning.
        /// </summary>
        public OperationResult UnregisterModelForUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return OperationResult.Failure("Unit ID cannot be null or empty");
            }

            if (!_unitIdToPosition.TryGetValue(unitId, out var position))
            {
                return OperationResult.Successful(); // Already unregistered
            }

            return UnregisterModelAtPosition(position);
        }

        /// <summary>
        /// Clear all model tracking. Called when transitioning away from pre-battle.
        /// </summary>
        public void ClearAllModelTracking()
        {
            _positionToModel.Clear();
            _positionToUnitId.Clear();
            _unitIdToPosition.Clear();
        }

        /// <summary>
        /// Update model position when a unit moves. Updates all tracking dictionaries.
        /// </summary>
        public OperationResult UpdateModelPosition(Vector2Int oldPosition, Vector2Int newPosition)
        {
            if (!_positionToModel.TryGetValue(oldPosition, out var model))
            {
                return OperationResult.Failure($"No model found at {oldPosition}");
            }

            var unitId = GetUnitIdAtPosition(oldPosition);
            if (string.IsNullOrEmpty(unitId))
            {
                return OperationResult.Failure($"No unit ID tracked at {oldPosition}");
            }

            // Unregister from old position
            UnregisterModelAtPosition(oldPosition);

            // Register at new position
            return RegisterModel(newPosition, model, unitId);
        }

        /// <summary>
        /// Swap models between two positions. Updates all tracking dictionaries.
        /// </summary>
        public OperationResult SwapModelPositions(Vector2Int posA, Vector2Int posB)
        {
            var modelA = GetModelAtPosition(posA);
            var modelB = GetModelAtPosition(posB);
            var unitIdA = GetUnitIdAtPosition(posA);
            var unitIdB = GetUnitIdAtPosition(posB);

            if (modelA == null || modelB == null)
            {
                return OperationResult.Failure("Both positions must have models to swap");
            }

            if (string.IsNullOrEmpty(unitIdA) || string.IsNullOrEmpty(unitIdB))
            {
                return OperationResult.Failure("Both positions must have tracked unit IDs");
            }

            // Swap the registrations
            UnregisterModelAtPosition(posA);
            UnregisterModelAtPosition(posB);

            RegisterModel(posA, modelB, unitIdB);
            RegisterModel(posB, modelA, unitIdA);

            return OperationResult.Successful();
        }

        #endregion

        #region High-Level Placement Operations

        /// <summary>
        /// Place a unit at a position.
        /// </summary>
        public OperationResult PlaceUnit(Vector2Int pos, CharacterInstance unit)
        {
            if (!IsPlayerSpawnPoint(pos))
            {
                return OperationResult.Failure("Cannot place unit: invalid position");
            }

            var data = unit?.CharacterTemplate;
            if (data == null)
            {
                return OperationResult.Failure("Cannot place unit: CharacterData missing");
            }

            placements[pos] = data;
            Brain?.PublishPlacementsSyncRequested(
                persist: false,
                forceApplyPlacementsOnLoad: false
            );
            return OperationResult.Successful();
        }

        /// <summary>
        /// Swap two positions - updates BOTH logical placements AND model tracking.
        /// </summary>
        private void ApplySwap(Vector2Int from, Vector2Int to)
        {
            // Capture data references BEFORE the swap
            placements.TryGetValue(from, out var dataFrom);
            placements.TryGetValue(to, out var dateTo);

            // Update UI
            StartingPositionsComponent.SetSwap(to);

            // Swap logical placements
            (placements[from], placements[to]) = (placements[to], placements[from]);

            // Update model tracking
            var modelA = GetModelAtPosition(from);
            var modelB = GetModelAtPosition(to);
            var unitIdA = GetUnitIdAtPosition(from);
            var unitIdB = GetUnitIdAtPosition(to);

            if (
                modelA != null
                && modelB != null
                && !string.IsNullOrEmpty(unitIdA)
                && !string.IsNullOrEmpty(unitIdB)
            )
            {
                // Swap the registrations
                UnregisterModelAtPosition(from);
                UnregisterModelAtPosition(to);
                RegisterModel(from, modelB, unitIdB);
                RegisterModel(to, modelA, unitIdA);
            }

            // Update visual models (transforms)
            StartingPositionsComponent.SwapModels(from, to);

            // Update instance positions
            var gw = Brain?.gamewideContextBrain;
            if (gw != null)
            {
                var instFrom = dataFrom != null ? gw.FindInstanceByTemplate(dataFrom) : null;
                var instTo = dateTo != null ? gw.FindInstanceByTemplate(dateTo) : null;
                if (instFrom != null)
                {
                    instFrom.MapGridPosition = to;
                }

                if (instTo != null)
                {
                    instTo.MapGridPosition = from;
                }
            }
        }

        /// <summary>
        /// Move a unit from one position to another - updates BOTH logical placements AND model tracking.
        /// </summary>
        private void ApplyMove(Vector2Int from, Vector2Int to)
        {
            // Capture data reference BEFORE the move
            placements.TryGetValue(from, out var dataFrom);

            // Update UI
            StartingPositionsComponent.SetSelected(to);

            // Move logical placement
            placements[to] = placements[from];
            placements.Remove(from);

            // Update model tracking
            var model = GetModelAtPosition(from);
            var unitId = GetUnitIdAtPosition(from);

            if (model != null && !string.IsNullOrEmpty(unitId))
            {
                UnregisterModelAtPosition(from);
                RegisterModel(to, model, unitId);
            }

            // Update visual model (transform)
            StartingPositionsComponent.MoveModel(from, to);

            // Update instance position
            var inst =
                dataFrom != null
                    ? Brain?.gamewideContextBrain?.FindInstanceByTemplate(dataFrom)
                    : null;
            if (inst != null)
            {
                inst.MapGridPosition = to;
            }
        }

        #endregion

        #region UI Interaction and Selection

        public OperationResult SelectPosition(Vector2Int pos)
        {
            if (!IsPlayerSpawnPoint(pos))
            {
                return OperationResult.Failure("Invalid position");
            }

            if (!TryGetPlacement(pos, out var data))
            {
                return OperationResult.Failure("Cannot select empty position");
            }

            selectedPosition = pos;
            selectedUnit = Brain?.gamewideContextBrain?.FindInstanceByTemplate(data);

            UpdateSelectedVisual(pos, selectedUnit);

            return OperationResult.Successful();
        }

        public OperationResult ClearSelection()
        {
            selectedPosition = null;
            potentialSwapPosition = null;
            selectedUnit = null;
            potentialSwapUnit = null;
            StartingPositionsComponent?.Clears();
            return OperationResult.Successful();
        }

        public OperationResult ExecutePositionAction()
        {
            if (
                !ValidationHelper.ValidateNotNull(
                    "BattlePreparationObject.ExecutePositionAction",
                    (selectedPosition, nameof(selectedPosition)),
                    (potentialSwapPosition, nameof(potentialSwapPosition))
                )
            )
            {
                return OperationResult.Failure("Invalid action state");
            }

            if (TryGetPlacement(potentialSwapPosition.Value, out var _))
            {
                ApplySwap(selectedPosition.Value, potentialSwapPosition.Value);
            }
            else
            {
                ApplyMove(selectedPosition.Value, potentialSwapPosition.Value);
            }

            ClearSelection();
            CurrentPlacementState = PlacementState.PlayerPlaced;

            Brain?.PublishPlacementsSyncRequested(persist: true, forceApplyPlacementsOnLoad: true);

            return OperationResult.Successful();
        }

        public OperationResult PreviewPotentialSwap(Vector2Int pos)
        {
            if (selectedPosition == null)
            {
                return OperationResult.Failure("No selected unit to preview against");
            }

            if (!IsPlayerSpawnPoint(pos))
            {
                potentialSwapPosition = null;
                potentialSwapUnit = null;
                StartingPositionsComponent?.ClearSwapPreview();
                return OperationResult.Failure("Invalid position");
            }

            if (pos == selectedPosition.Value)
            {
                potentialSwapPosition = null;
                potentialSwapUnit = null;
                var sp = StartingPositionsComponent;
                sp?.SetSelected(selectedPosition.Value);
                sp?.ClearSwapPreview();
                return OperationResult.Successful();
            }

            potentialSwapPosition = pos;
            StartingPositionsComponent?.SetSwap(pos);

            if (TryGetPlacement(pos, out var data))
            {
                potentialSwapUnit = Brain?.gamewideContextBrain?.FindInstanceByTemplate(data);
                UpdateSwapPreview(pos, potentialSwapUnit);
            }
            else
            {
                potentialSwapUnit = null;
                StartingPositionsComponent?.ClearSwapPreview();
            }

            return OperationResult.Successful();
        }

        private void UpdateSelectedVisual(Vector2Int pos, CharacterInstance unit)
        {
            var sp = StartingPositionsComponent;
            sp?.SetSelected(pos);
            if (sp == null)
            {
                return;
            }

            if (unit != null)
            {
                var (name, className, portrait) = BuildUnitDisplayData(unit);
                sp.SetSelectedUnit(name, className, portrait);
                return;
            }

            if (TryGetPlacement(pos, out var data) && data != null)
            {
                var name = data.DisplayName ?? "";
                var className = data.StartingClass?.Identity.ClassName ?? "n/a";
                var portrait = data.DefaultPortrait?.RuntimeSprite;
                sp.SetSelectedUnit(name, className, portrait);
            }
        }

        private void UpdateSwapPreview(Vector2Int pos, CharacterInstance unit)
        {
            var sp = StartingPositionsComponent;
            sp?.SetSwap(pos);
            if (sp == null)
            {
                return;
            }

            if (unit != null)
            {
                var (name, className, portrait) = BuildUnitDisplayData(unit);
                sp.SetSwapUnit(name, className, portrait);
                return;
            }

            if (TryGetPlacement(pos, out var data) && data != null)
            {
                var name = data.DisplayName ?? "";
                var className = data.StartingClass?.Identity.ClassName ?? "n/a";
                var portrait = data.DefaultPortrait?.RuntimeSprite;
                sp.SetSwapUnit(name, className, portrait);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleUnitSelectionChanged(CharacterInstance unit, bool selected)
        {
            if (CurrentPlacementState is PlacementState.NonePlaced or PlacementState.DefaultPlaced)
            {
                InitializePlacements();
            }
        }

        private void HandlePositioningModeEntered()
        {
            var gw = Brain?.gamewideContextBrain;
            if (gw != null)
            {
                var persistent =
                    gw.GamewidePersistentPlayerRoster
                    ?? gw.CreateOrRecallGamewidePersistentPlayerRoster();
                if (persistent != null)
                {
                    var rosterInstance = gw.GetOrCreatePlayerTeamRoster(persistent);
                    PreBattleSelectionHelper.EnsureDefaultPreBattleSelections(
                        Brain,
                        persistent,
                        rosterInstance,
                        MaxPlayerTeamUnits,
                        RequiredPlayerUnits
                    );
                }

                var cameraBrain = Brain?.cameraBrain;
                var cameraChildren = GetComponentsInChildren<Camera>();
                foreach (var cam in cameraChildren)
                {
                    if (cam != null && cam.CompareTag("BattleMapCamera"))
                    {
                        cameraBrain.SetBattleMapCamera(cam);
                        break;
                    }
                }
                cameraBrain.MoveCameraToPosition(PlayerTeamSpawnPoints.FirstOrDefault());
                InitializePlacements();
            }
        }

        #endregion

        #region Default Placements

        public void StoreDefaultPlacements()
        {
            _defaultPlacements = new Dictionary<Vector2Int, CharacterData>(placements);
        }

        public OperationResult ResetToDefaultPlacements()
        {
            if (_defaultPlacements == null || _defaultPlacements.Count == 0)
            {
                return OperationResult.Failure("No default placements to restore");
            }

            placements = new Dictionary<Vector2Int, CharacterData>(_defaultPlacements);
            CurrentPlacementState = PlacementState.DefaultPlaced;

            StartingPositionsComponent?.DespawnAllModels();
            ClearAllModelTracking();

            Brain?.PublishPlacementsInitialized();

            return OperationResult.Successful();
        }

        #endregion

        #region Helpers

        private (string name, string className, Sprite portrait) BuildUnitDisplayData(
            CharacterInstance unit
        )
        {
            if (unit == null)
            {
                return ("", "n/a", null);
            }

            var name = unit.CharacterTemplate?.DisplayName ?? "";
            var curClass = unit.GetCurrentClass();
            var className = curClass?.ClassData.Identity.ClassName;
            if (string.IsNullOrEmpty(className))
            {
                className = unit.CharacterTemplate?.StartingClass?.Identity.ClassName ?? "n/a";
            }

            var portrait = unit.CharacterTemplate?.DefaultPortrait?.RuntimeSprite;
            return (name, className, portrait);
        }

        private bool IsPlayerSpawnPoint(Vector2Int pos) =>
            PlayerTeamSpawnPoints != null && PlayerTeamSpawnPoints.Contains(pos);

        private bool TryGetPlacement(Vector2Int pos, out CharacterData data)
        {
            data = null;
            return placements != null && placements.TryGetValue(pos, out data);
        }

        #endregion
    }
}
