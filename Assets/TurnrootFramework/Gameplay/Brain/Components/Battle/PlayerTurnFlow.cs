using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Components.Battle
{
    [RequireComponent(typeof(BattleBrain))]
    public class PlayerTurnFlow : MonoBehaviour
    {
        private PlayerTurnState _currentState;
        private CharacterInstance _activePlayerUnit;
        private MapGridPoint _selectedDestination;
        private CharacterInstance _selectedTarget;

        private BattleBrain _battleBrain;

        public void Intialize()
        {
            _currentState ??= new PlayerTurnState();
        }

        public void StartPlayerTurn()
        {
            _activePlayerUnit = _battleBrain.BattleObject.Context.Unit.UnitInstance;
            _currentState.TransitionToState(PlayerTurnStates.NoUnitSelected);
            _battleBrain.Brain.PublishPlayerTurnStarted(_activePlayerUnit);
        }

        public OperationResult SelectUnit() =>
            _currentState.TransitionToState(PlayerTurnStates.NoActionChosen);

        public OperationResult DeselectUnit() =>
            _currentState.TransitionToState(PlayerTurnStates.NoUnitSelected);

        public OperationResult ActionChosen(PlayerTurnStates actionState) =>
            _currentState.TransitionToState(actionState);

        public OperationResult UndoActionChoice() =>
            _currentState.TransitionToState(PlayerTurnStates.NoActionChosen);

        public OperationResult SelectTargetOrDestination(PlayerTurnStates targetSelectedState) =>
            _currentState.TransitionToState(targetSelectedState);

        public OperationResult ConfirmAction() =>
            _currentState.TransitionToState(PlayerTurnStates.ConfirmAction);

        public OperationResult UndoTargetOrDestinationChoice(
            PlayerTurnStates actionChoosingState
        ) => _currentState.TransitionToState(actionChoosingState);

        public OperationResult EndTurn() =>
            _currentState.TransitionToState(PlayerTurnStates.TurnEnded);

        public OperationResult SpecialTurnReset() =>
            _currentState.TransitionToState(PlayerTurnStates.NoActionChosen);

        public OperationResult WaitAndEndTurn() =>
            _currentState.TransitionToState(PlayerTurnStates.TurnEnded);

        // TODO: State transition methods
        // TODO: Subscribe to relevant Brain events (unit activated, turn ended)
        // TODO: Publish player-specific events (state changed, action confirmed)
    }
}
