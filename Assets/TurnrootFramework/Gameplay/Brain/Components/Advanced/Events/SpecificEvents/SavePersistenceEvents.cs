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

        public event Action OnGraphicsQualityChanged;

        public void PublishGraphicsQualityChanged() => OnGraphicsQualityChanged?.Invoke();

        // Save File Management Events
        public event Action<string> OnUpdateSaveFileName;
        public event Action<int> OnUpdateSaveFileProgress;
        public event Action<string> OnSetSaveFileCurrentScene;
        public event Action<SaveFileSubfolders> OnSwitchActiveSaveFile;

        public void PublishUpdateSaveFileName(string fileName) =>
            OnUpdateSaveFileName?.Invoke(fileName);

        public void PublishUpdateSaveFileProgress(int progress) =>
            OnUpdateSaveFileProgress?.Invoke(progress);

        public void PublishSetSaveFileCurrentScene(string sceneName) =>
            OnSetSaveFileCurrentScene?.Invoke(sceneName);

        public void PublishSwitchActiveSaveFile(SaveFileSubfolders subfolder) =>
            OnSwitchActiveSaveFile?.Invoke(subfolder);

        #endregion
    }
}
