using System;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Save/Persistence Events

        public event Action OnSavePlayerRosterRequested;

        // New event variant: request a player roster save with a specific lastSavedBattleTurn value.
        public event Action<int> OnSavePlayerRosterRequestedWithTurn;
        public event Action OnSavePlayerSettingsRequested;

        public void PublishSavePlayerRosterRequested() => OnSavePlayerRosterRequested.Invoke();

        public void PublishSavePlayerRosterRequested(int lastSavedBattleTurn) =>
            OnSavePlayerRosterRequestedWithTurn?.Invoke(lastSavedBattleTurn);

        public void PublishSavePlayerSettingsRequested() => OnSavePlayerSettingsRequested.Invoke();

        public event Action<PlayerSettings.GameplayPlayerSettings.InputControlType> OnInputControlTypeChanged;

        public void PublishInputControlTypeChanged(
            PlayerSettings.GameplayPlayerSettings.InputControlType newType
        ) => OnInputControlTypeChanged.Invoke(newType);

        #endregion
    }
}
