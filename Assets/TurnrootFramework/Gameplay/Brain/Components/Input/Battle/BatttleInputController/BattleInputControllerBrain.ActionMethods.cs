using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Action Methods

        public void ConfirmTileSelection()
        {
            if (CursorPosition == null || !ValidateTileSelection(CursorPosition))
            {
                return;
            }

            switch (_playerTurnFlow.GetCurrentState())
            {
                case PlayerTurnStates.ChoosingDestination:
                    HandleDestinationSelection(CursorPosition);
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    HandleTargetSelection();
                    break;
            }
        }

        private void HandleDestinationSelection(MapGridPoint destinationPoint)
        {
            if (destinationPoint == null)
            {
                return;
            }

            _pendingDestination = destinationPoint;
            _playerTurnFlow.SelectTargetOrDestination(PlayerTurnStates.DestinationSelected);
        }

        private bool IsDestinationSameAsUnitPosition(MapGridPoint destinationPoint)
        {
            var unit = BattleContext.Unit.UnitInstance;
            if (unit == null)
            {
                return false;
            }

            var unitPoint = unit.UnitPositionToMapGridPoint(
                unit.MapGridPosition,
                Brain.battleBrain.BattleObject.Context.MapGrid
            );

            return unitPoint != null && unitPoint.Equals(destinationPoint);
        }

        private void HandleTargetSelection()
        {
            if (!Brain.cursorBrain.IsCursorOnUnit(out var targetUnit))
            {
                return;
            }

            if (ValidateTargetSelection(targetUnit))
            {
                _playerTurnFlow.SelectTargetOrDestination(
                    PlayerTurnStates.AttackActionChosenTargetSelected
                );
            }
        }

        public void ChangeSelectedUnit(CharacterInstance unit)
        {
            if (!BattleContext.IsPlayerControlledUnit(unit))
            {
                "unit is not player-controlled".LogWarning();
                return;
            }

            if (BattleContext.Unit.UnitInstance == unit)
            {
                return;
            }

            BattleContext.Unit.UnitInstance = unit;
            if (BattleContext.Flags?.ActiveUnitFlags == null)
            {
                BattleContext.Flags.ActiveUnitFlags = new UnitFlag();
            }

            BattleContext.Flags.ActiveUnitFlags.Unit = unit;
            Brain.PublishPlayerControlledUnitActivated(unit);
            ComputeValidTiles(unit);

            // Update adjacency and targets in range for the newly selected unit
            BattleContext.UpdateAdjacentUnits();
            BattleContext.UpdateTargetsInRange();

            HighlightValidTilesForSelectedUnit();
        }

        private void HighlightValidTilesForSelectedUnit()
        {
            var movePositions = GetValidMoveCoordinates();
            var attackPositions = GetValidAttackCoordinates();

            _tileHighlighter.ClearAll();
            _tileHighlighter.HighlightTiles(movePositions, TileHighlighter.HighlightType.Move);
            _tileHighlighter.HighlightTiles(attackPositions, TileHighlighter.HighlightType.Attack);

            Brain.cursorBrain.ClearAllowedPositions();
            Brain.cursorBrain.SetAllowedPositions(movePositions);
        }

        public void RequestUndo() => Brain.PublishPlayerUndoAction();

        #endregion
    }
}
