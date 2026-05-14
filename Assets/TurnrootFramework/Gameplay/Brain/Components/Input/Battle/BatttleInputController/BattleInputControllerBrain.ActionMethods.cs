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
                _pendingTarget = targetUnit;
                _playerTurnFlow.SelectTargetOrDestination(
                    PlayerTurnStates.AttackActionChosenTargetSelected
                );
            }
        }

        private void ExecuteConfirmedAttack()
        {
            if (_pendingTarget == null)
            {
                "ExecuteConfirmedAttack: no pending target".LogWarning();
                return;
            }

            var attacker = BattleContext.Unit.UnitInstance;
            var defender = _pendingTarget;
            _pendingTarget = null;

            // Set context target so skill graphs (CombatStarts, PostCombat) see the defender
            BattleContext.Participants.Targets =
                new System.Collections.Generic.List<CharacterInstance> { defender };

            _playerTurnFlow.ExecuteConfirmedAction();
            BattleContext.ExecuteCombatExchange(attacker, defender);
            _playerTurnFlow.EndTurn();
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
