namespace Turnroot.Gameplay.Brain
{
    public partial class GamewideContextBrain
    {
        #region Player Settings Management
        public void UpdatePlayerSetting(string settingName, object value)
        {
            _playerSettingsPersistence?.UpdatePlayerSetting(settingName, value);
            Brain.volumeBrain?.ApplySettingsToVolumes(PlayerSettings);
        }
        #endregion
    }
}
