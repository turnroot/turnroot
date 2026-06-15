using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class GamewideContextBrain
    {
        public Color AvatarHairColor { get; private set; }
        public Color AvatarEyeColor { get; private set; }
        public Color AvatarSkinColor { get; private set; }

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
