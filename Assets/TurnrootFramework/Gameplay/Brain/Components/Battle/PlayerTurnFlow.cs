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

        private BattleBrain _battleBrain;

        public PlayerTurnStates GetCurrentState() =>
            _currentState?.CurrentState ?? PlayerTurnStates.Inactive;

        public void Intialize()
        {
            _currentState ??= new PlayerTurnState();
            _battleBrain ??= GetComponent<BattleBrain>();

            if (_battleBrain?.Brain != null)
            {
                _battleBrain.Brain.OnPlayerUndoAction += HandlePlayerUndoAction;
            }
#if UNITY_EDITOR
            Debug.Log("PlayerTurnFlow: Initialized and subscribed to Brain events");
#endif
        }

        private void OnDestroy()
        {
            if (_battleBrain?.Brain != null)
            {
                _battleBrain.Brain.OnPlayerUndoAction -= HandlePlayerUndoAction;
            }
        }

        public void StartPlayerTurn()
        {
            _activePlayerUnit = _battleBrain.BattleObject.Context.Unit.UnitInstance;
            var res = _currentState.TransitionToState(PlayerTurnStates.NoUnitSelected);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnStarted(_activePlayerUnit);
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void SelectUnit()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.NoActionChosen);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerControlledUnitActivated(_activePlayerUnit);
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void DeselectUnit()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.NoUnitSelected);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void ActionChosen(PlayerTurnStates actionState)
        {
            var res = _currentState.TransitionToState(actionState);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void SelectTargetOrDestination(PlayerTurnStates targetSelectedState)
        {
            var res = _currentState.TransitionToState(targetSelectedState);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void ConfirmAction()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.ConfirmAction);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void CancelTargetOrDestinationChoice(PlayerTurnStates actionChoosingState)
        {
            var res = _currentState.TransitionToState(actionChoosingState);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void EndTurn()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.TurnEnded);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnEnded();
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        private void HandlePlayerUndoAction()
        {
            // TODO: Handle undo event
        }

        public OperationResult SpecialTurnReset()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.NoActionChosen);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
            return res;
        }

        public void WaitAndEndTurn()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.TurnEnded);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerTurnEnded();
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }
    }
}
