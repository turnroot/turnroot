namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Partial class handling player settings management.
    /// </summary>
    public partial class GamewideContextBrain
    {
        #region Player Settings Management
        public void UpdatePlayerSetting(string settingName, object value)
        {
            _playerSettingsPersistence.UpdatePlayerSetting(settingName, value);
            Brain.volumeBrain.ApplySettingsToVolumes(PlayerSettings);
        }

        public void SavePlayerSettings() => _playerSettingsPersistence?.SavePlayerSettings();
        #endregion
    }
}
