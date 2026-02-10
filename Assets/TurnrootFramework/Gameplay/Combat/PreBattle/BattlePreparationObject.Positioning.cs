using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        public OperationResult PlaceUnit(Vector2Int pos, CharacterInstance unit)
        {
            if (!PlayerTeamSpawnPoints.Contains(pos))
            {
                return OperationResult.Failure("Cannot place unit: invalid position");
            }
            else
            {
                placements[pos] = unit;
                // Keep runtime roster updated (no immediate persistence)
                SyncPlacementsToRuntimeRoster(persist: false);
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

            // Persist final player changes so starting positions are saved to Long Term Memory
            SyncPlacementsToRuntimeRoster(persist: true);

            return OperationResult.Successful();
        }

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
            // Ignore selection change events that occur while we're running InitializePlacements
            // to avoid an infinite re-initialization loop when InitializePlacements publishes
            // selection changes as part of its work.
            if (_isInitializingPlacements)
            {
                return;
            }

            // Recompute placements when selection changes (only when not currently initializing)
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
    }
}
