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
            // Keep runtime roster updated (no immediate persistence)
            Brain?.PublishPlacementsSyncRequested(
                persist: false,
                forceApplyPlacementsOnLoad: false
            );
            return OperationResult.Successful();
        }

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
            // Resolve instance for UI purposes if available
            selectedUnit = Brain?.gamewideContextBrain?.FindInstanceByTemplate(data);

            // Update visuals: position the selected projector and show unit data immediately
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

            if (TryGetPlacement(potentialSwapPosition.Value, out var _))
            {
                ApplySwap(selectedPosition.Value, potentialSwapPosition.Value);
            }
            else
            {
                ApplyMove(selectedPosition.Value, potentialSwapPosition.Value);
            }

            // Log placements for diagnostics
            ClearSelection();
            CurrentPlacementState = PlacementState.PlayerPlaced; // Mark as modified

            // Persist final player changes so starting positions are saved to Long Term Memory.
            // This is a user-initiated save so force the saved placements to be applied on subsequent load.

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

            // If cursor is on the same tile as the selected unit, clear swap preview and keep selected projector visible
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

        // Convenience helpers moved to partial implementations (BattlePreparationObject.Helpers.cs)

        // Helper: update selected projector visuals and unit data display
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

            // If there is no active instance, fall back to placement data for display.
            if (TryGetPlacement(pos, out var data) && data != null)
            {
                var name = data.DisplayName ?? "";
                var className = data.StartingClass?.Identity.ClassName ?? "n/a";
                var portrait = data.DefaultPortrait?.RuntimeSprite;
                sp.SetSelectedUnit(name, className, portrait);
            }
        }

        // Helper: update swap preview visuals and unit display
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
