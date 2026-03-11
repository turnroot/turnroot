using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
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
    }
}


