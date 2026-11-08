using System;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Battle Turn Events

        /// <summary>Fires when the battle begins a new round, before its first faction phase.</summary>
        public event Action OnBattleRoundStarted;

        /// <summary>Fires when the current battle round is complete.</summary>
        public event Action OnBattleRoundEnded;

        /// <summary>Fires once when the player phase begins; supplies its first active unit.</summary>
        public event Action<CharacterInstance> OnPlayerPhaseStarted;

        /// <summary>Fires after all eligible player units have completed their turns.</summary>
        public event Action OnPlayerPhaseEnded;

        /// <summary>Fires when the player unit-turn state machine changes state.</summary>
        public event Action<PlayerTurnStates> OnPlayerUnitTurnStateChanged;

        /// <summary>Fires when the player requests undo of the most recent eligible action.</summary>
        public event Action OnPlayerUndoAction;

        /// <summary>Fires when the enemy faction phase begins.</summary>
        public event Action OnEnemyPhaseStarted;

        /// <summary>Fires after all eligible enemy units have completed their turns.</summary>
        public event Action OnEnemyPhaseEnded;

        /// <summary>Fires when the third-party faction phase begins.</summary>
        public event Action OnThirdPartyPhaseStarted;

        /// <summary>Fires after all eligible third-party units have completed their turns.</summary>
        public event Action OnThirdPartyPhaseEnded;

        /// <summary>Fires when the rotisserie closes the outgoing unit's turn.</summary>
        public event Action<CharacterInstance> OnUnitTurnEnded;

        /// <summary>Fires after a unit becomes active and its turn begins.</summary>
        public event Action<CharacterInstance> OnUnitTurnStarted;

        /// <summary>Fires when a player's Wait action is chosen and should be presented or handled.</summary>
        public event Action<CharacterInstance> OnWaitActionRequested;

        /// <summary>Notifies listeners that a new battle round has begun.</summary>
        public void PublishBattleRoundStarted() => OnBattleRoundStarted?.Invoke();

        /// <summary>Notifies listeners that the current battle round has ended.</summary>
        public void PublishBattleRoundEnded() => OnBattleRoundEnded?.Invoke();

        /// <summary>Notifies listeners that the player phase began and identifies its first active unit.</summary>
        /// <param name="firstActiveUnit">The first living player unit activated in this phase.</param>
        public void PublishPlayerPhaseStarted(CharacterInstance firstActiveUnit) =>
            OnPlayerPhaseStarted?.Invoke(firstActiveUnit);

        /// <summary>Notifies listeners that the player faction phase has ended.</summary>
        public void PublishPlayerPhaseEnded() => OnPlayerPhaseEnded?.Invoke();

        /// <summary>Publishes a change in the player's unit-turn state machine.</summary>
        /// <param name="newState">The state reached by the player unit-turn flow.</param>
        public void PublishPlayerUnitTurnStateChanged(PlayerTurnStates newState) =>
            OnPlayerUnitTurnStateChanged?.Invoke(newState);

        /// <summary>Requests undo of the most recent player action when undo is available.</summary>
        public void PublishPlayerUndoAction() => OnPlayerUndoAction?.Invoke();

        /// <summary>Notifies listeners that a player's Wait action was chosen.</summary>
        /// <param name="unit">The player unit that chose Wait.</param>
        public void PublishWaitActionRequested(CharacterInstance unit) =>
            OnWaitActionRequested?.Invoke(unit);

        /// <summary>Notifies listeners that the enemy faction phase began.</summary>
        public void PublishEnemyPhaseStarted() => OnEnemyPhaseStarted?.Invoke();

        /// <summary>Notifies listeners that the enemy faction phase has ended.</summary>
        public void PublishEnemyPhaseEnded() => OnEnemyPhaseEnded?.Invoke();

        /// <summary>Notifies listeners that the third-party faction phase began.</summary>
        public void PublishThirdPartyPhaseStarted() => OnThirdPartyPhaseStarted?.Invoke();

        /// <summary>Notifies listeners that the third-party faction phase has ended.</summary>
        public void PublishThirdPartyPhaseEnded() => OnThirdPartyPhaseEnded?.Invoke();

        /// <summary>Notifies listeners that the given unit's turn has ended.</summary>
        /// <param name="unit">The unit whose turn just ended.</param>
        public void PublishUnitTurnEnded(CharacterInstance unit) => OnUnitTurnEnded?.Invoke(unit);

        /// <summary>Notifies listeners that the given unit's turn has started.</summary>
        /// <param name="unit">The unit that became active.</param>
        public void PublishUnitTurnStarted(CharacterInstance unit) =>
            OnUnitTurnStarted?.Invoke(unit);

        #endregion
    }
}
