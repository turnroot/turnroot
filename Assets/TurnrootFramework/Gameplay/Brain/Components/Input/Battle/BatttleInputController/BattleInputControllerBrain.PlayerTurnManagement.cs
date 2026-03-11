using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Player Turn Management

        private MapGridPoint _pendingDestination;

        private void HandlePlayerUnitActivated(CharacterInstance unit) => ComputeValidTiles(unit);

        private void HandlePlayerTurnStateChanged(PlayerTurnStates newState)
        {
            switch (newState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    HandleNoUnitSelectedState();
                    break;

                case PlayerTurnStates.UnitSelected:
                    HandleUnitSelectedState();
                    break;

                case PlayerTurnStates.ChoosingDestination:
                    HandleChoosingDestinationState();
                    break;

                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    HandleAttackActionChoosingTargetState();
                    break;

                case PlayerTurnStates.ChoosingAction:
                    HandleChoosingActionState();
                    break;

                case PlayerTurnStates.DestinationSelected:
                    HandleDestinationSelectedState();
                    break;

                case PlayerTurnStates.TurnEnded:
                    HandleTurnEndedState();
                    break;
            }
        }

        private void HandleNoUnitSelectedState()
        {
            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            Brain.cursorBrain.ClearAllowedPositions();
            _tileHighlighter.ClearAll();
        }

        private void HandleUnitSelectedState()
        {
            var movePositions = GetValidMoveCoordinates();
            var attackPositions = GetValidAttackCoordinates();

            _tileHighlighter.HighlightTiles(movePositions, TileHighlighter.HighlightType.Move);
            _tileHighlighter.HighlightTiles(attackPositions, TileHighlighter.HighlightType.Attack);
            Brain.cursorBrain.SetAllowedPositions(movePositions);
        }

        private void HandleChoosingDestinationState()
        {
            var movePositions = GetValidMoveCoordinates();
            _tileHighlighter.HighlightTiles(movePositions, TileHighlighter.HighlightType.Move);
            Brain.cursorBrain.SetAllowedPositions(movePositions);
        }

        private void HandleAttackActionChoosingTargetState()
        {
            var attackPositions = GetValidAttackCoordinates();
            _tileHighlighter.HighlightTiles(attackPositions, TileHighlighter.HighlightType.Attack);
            Brain.cursorBrain.SetAllowedPositions(attackPositions);
        }

        private void HandleChoosingActionState()
        {
            // Check if turn has already ended (shouldn't show menu if so)
            if (_playerTurnFlow.GetCurrentState() == PlayerTurnStates.TurnEnded)
            {
                "Turn already ended, skipping action menu".LogWarning();
                return;
            }

            var result = ShowActionMenu();
            if (!result.Success)
            {
                result.ErrorMessage.LogError();
            }
        }

        private void HandleDestinationSelectedState()
        {
            var validation = OperationResultGuards.RequireNotNull(
                _pendingDestination,
                nameof(_pendingDestination)
            );
            if (!validation.Success)
            {
                _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                return;
            }

            // Check if staying in place - skip move and go straight to action menu
            if (IsDestinationSameAsUnitPosition(_pendingDestination))
            {
                _pendingDestination = null;
                _playerTurnFlow.ActionChosen(PlayerTurnStates.ChoosingAction);
                return;
            }

            var unit = BattleContext.Unit.UnitInstance;
            _playerTurnFlow.StartMove();

            // request camera to follow the destination throughout the move animation
            if (Brain.cameraBrain != null && _pendingDestination != null)
            {
                // unit movement should pan slower than cursor movement
                Brain.cameraBrain.MoveCameraToPosition(
                    _pendingDestination.CoordinatesInt,
                    0.01f * Path.Count
                );
            }

            Brain.PublishCharacterMoveStarted(unit, _pendingDestination);
            var moveRes = BattleContext.MoveUnitToPoint(unit, _pendingDestination);

            if (!moveRes.Success)
            {
                moveRes.ErrorMessage.LogWarning();
                Brain.battleBrain.IsInputEnabled = true;
                _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
            }

            _pendingDestination = null;
        }

        private void HandleTurnEndedState() => CompletePlayerTurn();

        private void CompletePlayerTurn()
        {
            // This is cleanup code that runs when TurnEnded state is reached
            // DO NOT call EndTurn() here - that would try to transition to TurnEnded when we're already there
            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            Brain.cursorBrain.ClearAllowedPositions();

            // Note: PlayerTurnEnded is published by TurnRotisserie, not here
        }

        #endregion
    }
}
