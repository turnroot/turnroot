using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Maps;
using Turnroot.UI.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public enum PlacementState
    {
        NonePlaced,
        DefaultPlaced,
        PlayerPlaced,
        PlayerConfirmed,
    }

    [RequireComponent(typeof(EnvironmentalConditions))]
    public class BattlePreparationObject : MonoBehaviour
    {
        public Brain.Brain Brain { get; private set; }

        public MapGrid MapGrid { get; private set; }

        [HideInInspector]
        public EnvironmentalConditions EnvironmentalConditions { get; private set; }

        [HideInInspector]
        public int MaxPlayerTeamUnits;

        [field: SerializeField, HideInInspector]
        public List<CharacterData> RequiredPlayerUnits { get; private set; } = new();

        [HideInInspector]
        public List<Vector2Int> PlayerTeamSpawnPoints;

        [HideInInspector]
        public StartingPositions StartingPositionsComponent;

        public OperationResult Initialize(Brain.Brain brain)
        {
            Brain = brain;
            EnvironmentalConditions = GetComponentInChildren<EnvironmentalConditions>(true);
            MapGrid = GetComponentInChildren<MapGrid>(true);
            PlayerTeamSpawnPoints = MapGrid.PlayerTeamSpawnPoints;

            // Copy MaxPlayerTeamUnits and RequiredPlayerUnits from a BattleGameObject when available.
            if (brain?.battleBrain?.BattleObject != null)
            {
                MaxPlayerTeamUnits = brain.battleBrain.BattleObject.MaxPlayerTeamUnits;
                RequiredPlayerUnits =
                    brain.battleBrain.BattleObject.RequiredPlayerUnits ?? new List<CharacterData>();
            }
            else
            {
                var parentBattleObject = GetComponentInParent<BattleGameObject>();
                if (parentBattleObject != null)
                {
                    MaxPlayerTeamUnits = parentBattleObject.MaxPlayerTeamUnits;
                    RequiredPlayerUnits =
                        parentBattleObject.RequiredPlayerUnits ?? new List<CharacterData>();
                }
            }

            // Keep placement view in sync with gamewide selection. When selection changes we
            // will reinitialize placements, but we avoid overwriting user edits.
            if (brain != null)
            {
                brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;
                brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
                brain.OnPositioningModeEntered += HandlePositioningModeEntered;
            }

            // Set the map grid for Camera Brain
            var cameraBrain = brain?.cameraBrain;
            cameraBrain.SetMapGrid(MapGrid);

            if (EnvironmentalConditions == null)
            {
                return OperationResult.Failure("EnvironmentalConditions not found");
            }

            // Notify the Brain that this BattlePreparationObject has been initialized.
            Brain?.PublishBattlePrepObjectInitialized(this);

            return OperationResult.Successful();
        }

        /* --------------------------- Starting Positions --------------------------- */
        [HideInInspector]
        public Dictionary<Vector2Int, CharacterInstance> placements;

        private bool _isInitializingPlacements = false;
        private bool _needsReinitialize = false;

        public OperationResult InitializePlacements()
        {
            // If we're already initializing, mark that we need another pass and return quickly.
            // This avoids re-entrant calls from PreBattle selection helper which publishes
            // UnitSelectionChanged for each unit and can cause multiple partial runs.
            if (_isInitializingPlacements)
            {
                _needsReinitialize = true;
                return OperationResult.Successful();
            }

            _isInitializingPlacements = true;
            try
            {
                // Use gamewide selection as the single source of truth for which units are selected.
                var gw = Brain?.gamewideContextBrain;
                var selectedUnits = gw?.GetSelectedForBattlePlayerTeamUnits();

                // If no runtime selections are present, attempt to compute default selections from roster/templates.
                if (selectedUnits == null || selectedUnits.Count == 0)
                {
                    var persistent =
                        gw?.GamewidePersistentPlayerRoster
                        ?? gw?.CreateOrRecallGamewidePersistentPlayerRoster();
                    var runtimeInstance =
                        persistent != null ? gw.GetOrCreatePlayerTeamRoster(persistent) : null;
                    var selectedTemplates =
                        PreBattleSelectionHelper.EnsureDefaultPreBattleSelections(
                            Brain,
                            persistent,
                            runtimeInstance,
                            MaxPlayerTeamUnits,
                            RequiredPlayerUnits
                        );

                    if (selectedTemplates != null && selectedTemplates.Count > 0)
                    {
                        // Build selected units from templates by finding runtime instances.
                        var tempList = new List<CharacterInstance>();
                        var placementsArr =
                            runtimeInstance != null
                                ? runtimeInstance.GetPlacements()
                                : persistent?.characters ?? new Characters.Roster.UnitPlacement[0];
                        foreach (var p in placementsArr)
                        {
                            if (p == null || p.CharacterData == null)
                            {
                                continue;
                            }

                            if (selectedTemplates.Contains(p.CharacterData))
                            {
                                var inst =
                                    runtimeInstance != null
                                        ? runtimeInstance.GetInstanceFor(p.CharacterData)
                                        : null;
                                inst ??= gw?.FindInstanceByTemplate(p.CharacterData);
                                if (inst != null)
                                {
                                    tempList.Add(inst);
                                }
                            }
                        }

                        selectedUnits = tempList;
                    }
                }

                if (selectedUnits == null || selectedUnits.Count == 0)
                {
                    return OperationResult.Failure("No units available for positioning");
                }

                placements = new Dictionary<Vector2Int, CharacterInstance>();

                // Place units at spawn points based on their roster order
                for (int i = 0; i < selectedUnits.Count && i < PlayerTeamSpawnPoints.Count; i++)
                {
                    if (i >= MaxPlayerTeamUnits)
                    {
                        break;
                    }

                    var spawnPos = PlayerTeamSpawnPoints[i];
                    var unit = selectedUnits[i];

                    placements[spawnPos] = unit;
                }

                CurrentPlacementState = PlacementState.DefaultPlaced;
                Brain?.PublishPlacementsInitialized();
            }
            finally
            {
                _isInitializingPlacements = false;
            }

            // If another InitializePlacements was requested during our run, run it once more now
            if (_needsReinitialize)
            {
                _needsReinitialize = false;
                return InitializePlacements();
            }

            return OperationResult.Successful();
        }

        [HideInInspector]
        public PlacementState CurrentPlacementState = PlacementState.NonePlaced;

        [HideInInspector]
        public Vector2Int? selectedPosition;

        [HideInInspector]
        public Vector2Int? potentialSwapPosition;

        [HideInInspector]
        public CharacterInstance selectedUnit;

        [HideInInspector]
        public CharacterInstance potentialSwapUnit;

        [HideInInspector]
        public bool CanSwap => selectedUnit != null && potentialSwapUnit != null;

        public OperationResult PlaceUnit(Vector2Int pos, CharacterInstance unit)
        {
            if (!PlayerTeamSpawnPoints.Contains(pos))
            {
                return OperationResult.Failure("Cannot place unit: invalid position");
            }
            else
            {
                placements[pos] = unit;
                return OperationResult.Successful();
            }
        }

        public OperationResult SelectPosition(Vector2Int pos)
        {
            if (!PlayerTeamSpawnPoints.Contains(pos))
            {
                return OperationResult.Failure("Invalid position");
            }

            if (!placements.ContainsKey(pos))
            {
                return OperationResult.Failure("Cannot select empty position");
            }

            selectedPosition = pos;
            selectedUnit = placements[pos];
            TurnrootLogger.Log(
                $"Selected unit: {selectedUnit.CharacterTemplate.DisplayName} at {pos}"
            );

            // Update visuals: position the selected projector and show unit data immediately
            StartingPositionsComponent?.SetSelected(pos);

            if (StartingPositionsComponent != null && selectedUnit != null)
            {
                var name = selectedUnit.CharacterTemplate?.DisplayName ?? "";
                var currentClassInstance = selectedUnit.GetCurrentClass();
                var className =
                    currentClassInstance?.ClassData?.GetClassName()
                    ?? selectedUnit.CharacterTemplate?.StartingClass?.Identity?.ClassName
                    ?? "";
                var portrait =
                    selectedUnit.CharacterTemplate?.DefaultPortrait?.RuntimeSprite ?? null;

                StartingPositionsComponent.SetSelectedUnit(name, className, portrait);
            }

            return OperationResult.Successful();
        }

        public OperationResult ClearSelection()
        {
            selectedPosition = null;
            potentialSwapPosition = null;
            selectedUnit = null;
            potentialSwapUnit = null;
            StartingPositionsComponent.Clears();
            return OperationResult.Successful();
        }

        public OperationResult ExecutePositionAction()
        {
            // Called when second Confirm happens
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

            // Determine if target is occupied
            bool targetOccupied = placements.ContainsKey(potentialSwapPosition.Value);

            if (targetOccupied)
            {
                // Swap
                StartingPositionsComponent.SetSwap(potentialSwapPosition.Value);
                (placements[selectedPosition.Value], placements[potentialSwapPosition.Value]) = (
                    placements[potentialSwapPosition.Value],
                    placements[selectedPosition.Value]
                );
                StartingPositionsComponent.SwapModels(
                    selectedPosition.Value,
                    potentialSwapPosition.Value
                );
            }
            else
            {
                // Move
                StartingPositionsComponent.SetSelected(potentialSwapPosition.Value);
                placements[potentialSwapPosition.Value] = placements[selectedPosition.Value];
                placements.Remove(selectedPosition.Value);
                StartingPositionsComponent.MoveModel(
                    selectedPosition.Value,
                    potentialSwapPosition.Value
                );
            }

            ClearSelection();
            CurrentPlacementState = PlacementState.PlayerPlaced; // Mark as modified

            return OperationResult.Successful();
        }

        /// <summary>
        /// Preview a potential swap/move to <paramref name="pos"/>. This updates
        /// swap projector and swap unit UI immediately without committing the action.
        /// If the target tile is empty, swap unit data is cleared. If the cursor
        /// is on the selected unit, the swap preview is cleared.
        /// </summary>
        public OperationResult PreviewPotentialSwap(Vector2Int pos)
        {
            if (selectedPosition == null)
            {
                return OperationResult.Failure("No selected unit to preview against");
            }

            // Invalid positions (not a player spawn point) should clear preview
            if (PlayerTeamSpawnPoints == null || !PlayerTeamSpawnPoints.Contains(pos))
            {
                potentialSwapPosition = null;
                potentialSwapUnit = null;
                StartingPositionsComponent?.ClearSwapPreview();
                return OperationResult.Failure("Invalid position");
            }

            // If cursor is on the same tile as the selected unit, clear swap preview
            if (pos == selectedPosition.Value)
            {
                potentialSwapPosition = null;
                potentialSwapUnit = null;
                StartingPositionsComponent?.SetSelected(selectedPosition.Value);
                StartingPositionsComponent?.ClearSwapPreview();
                return OperationResult.Successful();
            }

            potentialSwapPosition = pos;

            // Show swap projector at the target
            StartingPositionsComponent?.SetSwap(pos);

            if (placements.ContainsKey(pos))
            {
                var unit = placements[pos];
                potentialSwapUnit = unit;

                // Prepare display data
                var name = unit?.CharacterTemplate?.DisplayName ?? "";
                var className =
                    unit?.CurrentClassTemplate?.Identity?.ClassName
                    ?? unit?.CharacterTemplate?.StartingClass?.Identity?.ClassName
                    ?? "";
                var portrait = unit?.CharacterTemplate?.DefaultPortrait?.RuntimeSprite ?? null;

                StartingPositionsComponent?.SetSwapUnit(name, className, portrait);
            }
            else
            {
                potentialSwapUnit = null;
                StartingPositionsComponent?.ClearSwapUnit();
            }

            return OperationResult.Successful();
        }

        private void HandleUnitSelectionChanged(CharacterInstance unit, bool selected)
        {
            // Recompute placements when selection changes
            if (CurrentPlacementState is PlacementState.NonePlaced or PlacementState.DefaultPlaced)
            {
                InitializePlacements();
            }
        }

        private void HandlePositioningModeEntered()
        {
            // Ensure there is a runtime player roster instance so selection queries work.
            var gw = Brain?.gamewideContextBrain;
            if (gw != null)
            {
                // Ensure the persistent player roster asset is present and has a runtime instance.
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

        private void OnDestroy()
        {
            if (Brain != null)
            {
                Brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                Brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
            }
        }
    }
}
