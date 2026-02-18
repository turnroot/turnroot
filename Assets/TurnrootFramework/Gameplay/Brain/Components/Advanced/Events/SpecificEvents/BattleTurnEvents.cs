using System;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Battle Turn Events

        public event Action OnTurnBegin;
        public event Action OnTurnEnded;
        public event Action<CharacterInstance> OnPlayerTurnStarted;
        public event Action OnPlayerTurnEnded;
        public event Action<PlayerTurnStates> OnPlayerTurnStateChanged;
        public event Action OnPlayerUndoAction;
        public event Action OnEnemyTurnStarted;
        public event Action OnEnemyTurnEnded;
        public event Action OnThirdPartyTurnStarted;
        public event Action OnThirdPartyTurnEnded;
        public event Action<CharacterInstance> OnUnitTurnEnded;
        public event Action<CharacterInstance> OnWaitActionRequested;
        public event Action<CharacterInstance> OnWaitActionConfirmed;

        public void PublishTurnBegin() => OnTurnBegin?.Invoke();

        public void PublishTurnEnded() => OnTurnEnded?.Invoke();

        public void PublishPlayerTurnStarted(CharacterInstance unit) =>
            OnPlayerTurnStarted?.Invoke(unit);

        public void PublishPlayerTurnEnded() => OnPlayerTurnEnded?.Invoke();

        public void PublishPlayerTurnStateChanged(PlayerTurnStates newState) =>
            OnPlayerTurnStateChanged?.Invoke(newState);

        public void PublishPlayerUndoAction() => OnPlayerUndoAction?.Invoke();

        public void PublishWaitActionRequested(CharacterInstance unit) =>
            OnWaitActionRequested?.Invoke(unit);

        public void PublishWaitActionConfirmed(CharacterInstance unit) =>
            OnWaitActionConfirmed?.Invoke(unit);

        public void PublishEnemyTurnStarted() => OnEnemyTurnStarted?.Invoke();

        public void PublishEnemyTurnEnded() => OnEnemyTurnEnded?.Invoke();

        public void PublishThirdPartyTurnStarted() => OnThirdPartyTurnStarted?.Invoke();

        public void PublishThirdPartyTurnEnded() => OnThirdPartyTurnEnded?.Invoke();

        public void PublishUnitTurnEnded(CharacterInstance unit) => OnUnitTurnEnded?.Invoke(unit);

        #endregion
    }
}
