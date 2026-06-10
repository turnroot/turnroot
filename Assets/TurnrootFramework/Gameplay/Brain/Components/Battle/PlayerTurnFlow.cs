using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Components.Battle
{
    /// <summary>
    /// Manages the state machine and flow control for player-controlled unit turns during battle.
    /// </summary>
    [RequireComponent(typeof(BattleBrain))]
    public class PlayerTurnFlow : MonoBehaviour
    {
        private PlayerTurnState _currentState;
        private CharacterInstance _activePlayerUnit;
        private BattleBrain _battleBrain;
        private Utilities.AbstractScripts.BattleSceneFlow _cachedSceneFlow;

        public PlayerTurnStates GetCurrentState() =>
            _currentState?.CurrentState ?? PlayerTurnStates.Inactive;

        /// <summary>
        /// Helper to transition states and publish events, reducing boilerplate.
        /// </summary>
        /// <param name="newState">The state to transition to</param>
        /// <param name="publishEvent">Optional event to publish on successful transition</param>
        /// <returns>True if transition succeeded</returns>
        private bool TransitionAndPublish(
            PlayerTurnStates newState,
            System.Action publishEvent = null
        )
        {
            var res = _currentState.TransitionToState(newState);
            if (res.Success)
            {
                publishEvent?.Invoke();
                _battleBrain.Brain.PublishPlayerTurnStateChanged(_currentState.CurrentState);
                return true;
            }
            return false;
        }

        public void Intialize()
        {
            _currentState ??= new PlayerTurnState();
            _battleBrain ??= GetComponent<BattleBrain>();
            _cachedSceneFlow ??= FindFirstObjectByType<Utilities.AbstractScripts.BattleSceneFlow>();

            if (_battleBrain?.Brain != null)
            {
                _battleBrain.Brain.OnPlayerUndoAction += HandlePlayerUndoAction;
                _battleBrain.Brain.OnUnitFinishedMovingAfterAction +=
                    HandleUnitFinishedMovingAfterAction;
                _battleBrain.Brain.OnMoveAnimationCompleted += HandleUnitMoveAnimationCompleted;
                // Listen for wait confirmation coming from UI
                _battleBrain.Brain.OnWaitActionConfirmed += HandleWaitActionConfirmed;

                // Keep active unit in sync with selection events
                _battleBrain.Brain.OnPlayerControlledUnitActivated += HandlePlayerUnitActivated;
            }
        }

        private void OnDestroy() => CleanupBattle();

        private void HandlePlayerUnitActivated(CharacterInstance unit) => _activePlayerUnit = unit;

        /// <summary>
        /// Cleanup method to be called when battle ends.
        /// Unsubscribes from events and clears cached references.
        /// </summary>
        public void CleanupBattle()
        {
            if (_battleBrain?.Brain != null)
            {
                _battleBrain.Brain.OnPlayerUndoAction -= HandlePlayerUndoAction;
                _battleBrain.Brain.OnUnitFinishedMovingAfterAction -=
                    HandleUnitFinishedMovingAfterAction;
                _battleBrain.Brain.OnMoveAnimationCompleted -= HandleUnitMoveAnimationCompleted;
                _battleBrain.Brain.OnWaitActionConfirmed -= HandleWaitActionConfirmed;

                _battleBrain.Brain.OnPlayerControlledUnitActivated -= HandlePlayerUnitActivated;
            }
            _cachedSceneFlow = null;
            _activePlayerUnit = null;
        }

        public void StartPlayerTurn()
        {
            _activePlayerUnit = _battleBrain.BattleObject.Context.Unit.UnitInstance;
            TransitionAndPublish(
                PlayerTurnStates.NoUnitSelected,
                () => _battleBrain.Brain.PublishPlayerTurnStarted(_activePlayerUnit)
            );
        }

        public void SelectUnit(CharacterInstance unit)
        {
            _activePlayerUnit = unit; // Update active to the selected unit
            TransitionAndPublish(
                PlayerTurnStates.UnitSelected,
                () => _battleBrain.Brain.PublishPlayerControlledUnitActivated(_activePlayerUnit)
            );
        }

        public void DeselectUnit() => TransitionAndPublish(PlayerTurnStates.NoUnitSelected);

        public void ActionChosen(PlayerTurnStates actionState) => TransitionAndPublish(actionState);

        public void SelectTargetOrDestination(PlayerTurnStates targetSelectedState) =>
            TransitionAndPublish(targetSelectedState);

        public void SelectDestination(MapGridPoint destination)
        {
            TransitionAndPublish(
                PlayerTurnStates.DestinationSelected,
                () => _battleBrain.Brain.PublishPlayerChoseMoveTile(_activePlayerUnit, destination)
            );
        }

        // Called by input controller to start the move and lock input until move/animation finishes
        public void StartMove()
        {
            if (GetCurrentState() == PlayerTurnStates.ExecutingMove)
            {
                "StartMove called but flow already in ExecutingMove - ignoring".LogWarning();
                return;
            }

            TransitionAndPublish(
                PlayerTurnStates.ExecutingMove,
                () =>
                {
                    // Freeze input globally at the battle level so all controllers halt processing
                    if (_battleBrain != null)
                    {
                        _battleBrain.IsInputEnabled = false;
                    }
                }
            );
        }

        public void CompleteMove()
        {
            TransitionAndPublish(
                PlayerTurnStates.ChoosingAction,
                () =>
                {
                    // Re-enable input now that visuals/animation are finished
                    if (_battleBrain != null)
                    {
                        _battleBrain.IsInputEnabled = true;
                    }
                }
            );
        }

        /// <summary>
        /// Transitions from a *TargetSelected state to ConfirmAction (show forecast/preview).
        /// Called when the player first presses A on a target.
        /// </summary>
        public void ConfirmAction() => TransitionAndPublish(PlayerTurnStates.ConfirmAction);

        /// <summary>
        /// Transitions from ConfirmAction to ExecutingAction and fires the appropriate action event.
        /// Called when the player presses A a second time to confirm the forecast.
        /// </summary>
        public void ExecuteConfirmedAction()
        {
            var prev = _currentState.PreviousState;

            TransitionAndPublish(
                PlayerTurnStates.ExecutingAction,
                () =>
                {
                    switch (prev)
                    {
                        case PlayerTurnStates.AttackActionChosenTargetSelected:
                            _battleBrain.Brain.PublishAttackStarted(_activePlayerUnit);
                            break;
                        case PlayerTurnStates.HealActionChosenTargetSelected:
                            _battleBrain.Brain.PublishHealStarted(_activePlayerUnit);
                            break;
                        case PlayerTurnStates.UseItemActionChosenItemSelected:
                            // TODO: pass item; this flow currently doesn't track item in flow
                            _battleBrain.Brain.PublishUseItemStarted(_activePlayerUnit, null);
                            break;
                        case PlayerTurnStates.WaitActionChosen:
                            var playerSettings = _battleBrain
                                ?.Brain
                                ?.gamewideContextBrain
                                ?.PlayerSettings;
                            if (playerSettings != null && playerSettings.AutoEndTurn)
                            {
                                EndTurn();
                            }
                            else
                            {
                                _battleBrain.Brain.PublishWaitActionRequested(_activePlayerUnit);
                            }
                            break;
                    }
                }
            );
        }

        public void CancelTargetOrDestinationChoice(PlayerTurnStates actionChoosingState) =>
            TransitionAndPublish(actionChoosingState);

        public void EndTurn() =>
            // Note: This direct EndTurn bypasses interrupt checking.
            // For wait actions, use WaitAndEndTurn() instead.
            // PlayerTurnEnded is published by TurnRotisserie.ProgressToNextPhase()
            TransitionAndPublish(PlayerTurnStates.TurnEnded);

        private void HandlePlayerUndoAction()
        {
            // Allow undoing the last move if we're currently in the ChoosingAction state immediately after a move.
            var current = GetCurrentState();
            if (current == PlayerTurnStates.ChoosingAction)
            {
                var undone = TryUndoLastMove();
                if (undone)
                {
                    // Return flow to UnitSelected so player can reselect a tile/action
                    TransitionAndPublish(
                        PlayerTurnStates.UnitSelected,
                        () =>
                        {
                            // Re-announce the active unit so listeners (UI) recompute valid tiles
                            _battleBrain.Brain.PublishPlayerControlledUnitActivated(
                                _activePlayerUnit
                            );
                        }
                    );
                }
            }
        }

        private bool TryUndoLastMove()
        {
            var brain = _battleBrain?.Brain;
            if (brain == null)
            {
                return false;
            }

            var commands = brain.Commands;
            var history = commands.GetHistory();
            if (!commands.CanUndo || history.Count == 0)
            {
                return false;
            }

            var last = history[history.Count - 1];
            if (last is Commands.MoveCommand)
            {
                // Undo move command (will move unit back via MoveUnit)
                return brain.UndoCommand();
            }

            return false;
        }

        private void HandleWaitActionConfirmed(CharacterInstance unit)
        {
            if (_activePlayerUnit == null || unit == null)
            {
                return;
            }

            if (_activePlayerUnit != unit)
            {
                return;
            }

            // Only confirm waiting if we're in WaitActionChosen state
            var current = GetCurrentState();
            if (current == PlayerTurnStates.WaitActionChosen)
            {
                // This will trigger the full wait flow including interrupt checks
                WaitAndEndTurn();
            }
        }

        private void HandleUnitFinishedMovingAfterAction(CharacterInstance unit)
        {
            TryCompleteMoveForActiveUnit(unit);
        }

        private void HandleUnitMoveAnimationCompleted(CharacterInstance unit)
        {
            // Transition the flow from ExecutingMove -> ChoosingAction when the visual animation finishes.
            TryCompleteMoveForActiveUnit(unit);
        }

        private void TryCompleteMoveForActiveUnit(CharacterInstance unit)
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
            // If turn already ended, ignore this call (may happen due to race conditions)
            if (_currentState.CurrentState == PlayerTurnStates.TurnEnded)
            {
                "WaitAndEndTurn called but turn is already ended, ignoring".LogWarning();
                return;
            }

            // First transition to WaitActionChosen
            if (!TransitionAndPublish(PlayerTurnStates.WaitActionChosen))
            {
                return;
            }

            // Check if any interrupts are queued (e.g., conversations)
            if (_cachedSceneFlow != null && _cachedSceneFlow.IsInterruptQueued)
            {
                // Ensure we're in the correct scene flow state before processing interrupt
                if (
                    _cachedSceneFlow.CurrentMiniBattleState
                    == Utilities.AbstractScripts.MiniBattleState.NoBattlePlayerInput
                )
                {
                    // Queue the turn end to happen after interrupt completes
                    _cachedSceneFlow.QueueInterrupt(
                        _cachedSceneFlow.CurrentInterrupt,
                        onCompleted: () => CompleteTurnEnd()
                    );
                    _cachedSceneFlow.ProgressMiniBattleState();
                    return;
                }
                else
                {
                    $"PlayerTurnFlow: Interrupt queued but scene flow in unexpected state: {_cachedSceneFlow.CurrentMiniBattleState}".LogWarning();
                }
            }

            // No interrupt or scene flow not ready, proceed immediately to turn end
            CompleteTurnEnd();
        }

        private void CompleteTurnEnd()
        {
            // Clear dynamic participant data (Targets and AdjacentUnits) when turn ends
            var context = _battleBrain?.BattleObject.Context;
            if (context != null)
            {
                context.ClearParticipantDynamicData();
            }

            // Note: PlayerTurnEnded is published by TurnRotisserie.ProgressToNextPhase()
            // to ensure proper ordering with other phase transitions
            TransitionAndPublish(PlayerTurnStates.TurnEnded);
        }
    }
}
