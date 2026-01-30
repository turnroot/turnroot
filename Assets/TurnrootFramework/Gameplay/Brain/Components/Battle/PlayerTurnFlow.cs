using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
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
                _battleBrain.Brain.OnUnitFinishedMovingAfterAction +=
                    HandleUnitFinishedMovingAfterAction;
                _battleBrain.Brain.OnMoveAnimationCompleted += HandleUnitMoveAnimationCompleted;
            }
        }

        private void OnDestroy()
        {
            if (_battleBrain?.Brain != null)
            {
                _battleBrain.Brain.OnPlayerUndoAction -= HandlePlayerUndoAction;
                _battleBrain.Brain.OnUnitFinishedMovingAfterAction -=
                    HandleUnitFinishedMovingAfterAction;
                _battleBrain.Brain.OnMoveAnimationCompleted -= HandleUnitMoveAnimationCompleted;
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
            var res = _currentState.TransitionToState(PlayerTurnStates.UnitSelected);
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

        public void SelectDestination(MapGridPoint destination)
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.DestinationSelected);
            if (res.Success)
            {
                _battleBrain.Brain.PublishPlayerChoseMoveTile(_activePlayerUnit, destination);
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        // Called by input controller to start the move and lock input until move/animation finishes
        public void StartMove()
        {
            if (GetCurrentState() == PlayerTurnStates.ExecutingMove)
            {
                TurnrootLogger.Log(
                    "StartMove called but flow already in ExecutingMove - ignoring",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var res = _currentState.TransitionToState(PlayerTurnStates.ExecutingMove);
            if (res.Success)
            {
                // Freeze input globally at the battle level so all controllers halt processing
                if (_battleBrain != null)
                {
                    _battleBrain.IsInputEnabled = false;
                }

                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void CompleteMove()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.ChoosingAction);
            if (res.Success)
            {
                // Re-enable input now that visuals/animation are finished
                if (_battleBrain != null)
                {
                    _battleBrain.IsInputEnabled = true;
                }
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
            }
        }

        public void ConfirmAction()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.ConfirmAction);
            if (res.Success)
            {
                // Notify systems that an action is about to start using typed events
                var prev = _currentState.PreviousState;
                switch (prev)
                {
                    case PlayerTurnStates.AttackActionChosenTargetSelected:
                        _battleBrain.Brain.PublishAttackStarted(_activePlayerUnit);
                        break;
                    case PlayerTurnStates.HealActionChosenTargetSelected:
                        _battleBrain.Brain.PublishHealStarted(_activePlayerUnit);
                        break;
                    case PlayerTurnStates.UseItemActionChosenItemSelected:
                        // TODO: pass item; this flow currently doesn't track item in flow - keep generic publish for now
                        _battleBrain.Brain.PublishUseItemStarted(_activePlayerUnit, null);
                        break;
                    case PlayerTurnStates.WaitActionChosen:
                        // Waiting will immediately end the turn via Context.EndTurn; publish EndTurnCompleted when done by Context
                        break;
                    default:
                        break;
                }

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
            // Allow undoing the last move if we're currently in the ChoosingAction state immediately after a move.
            var current = GetCurrentState();
            if (current == PlayerTurnStates.ChoosingAction)
            {
                var brain = _battleBrain?.Brain;
                if (brain == null)
                {
                    return;
                }

                var commands = brain.Commands;
                var history = commands.GetHistory();
                if (commands.CanUndo && history.Count > 0)
                {
                    var last = history[history.Count - 1];
                    if (last is Turnroot.Gameplay.Brain.Commands.MoveCommand)
                    {
                        // Undo move command (will move unit back via MoveUnit)
                        var undone = brain.UndoCommand();
                        if (undone)
                        {
                            // Return flow to UnitSelected so player can reselect a tile/action
                            var res = _currentState.TransitionToState(
                                PlayerTurnStates.UnitSelected
                            );
                            if (res.Success)
                            {
                                _battleBrain.Brain.PublishPlayerTurnStateChanged(
                                    _currentState.CurrentState
                                );
                                // Re-announce the active unit so listeners (UI) recompute valid tiles
                                _battleBrain.Brain.PublishPlayerControlledUnitActivated(
                                    _activePlayerUnit
                                );
                            }
                        }
                    }
                }
            }
        }

        private void HandleUnitFinishedMovingAfterAction(CharacterInstance unit)
        {
            if (_activePlayerUnit == null || unit == null)
            {
                return;
            }

            if (_activePlayerUnit != unit)
            {
                return;
            }

            if (GetCurrentState() == PlayerTurnStates.ExecutingMove)
            {
                CompleteMove();
            }
        }

        private void HandleUnitMoveAnimationCompleted(CharacterInstance unit)
        {
            // Transition the flow from ExecutingMove -> ChoosingAction when the visual animation finishes
            if (_activePlayerUnit == null || unit == null)
            {
                return;
            }

            if (_activePlayerUnit != unit)
            {
                return;
            }

            if (GetCurrentState() == PlayerTurnStates.ExecutingMove)
            {
                CompleteMove();
            }
        }

        public OperationResult SpecialTurnReset()
        {
            var res = _currentState.TransitionToState(PlayerTurnStates.UnitSelected);
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
