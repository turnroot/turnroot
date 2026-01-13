using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    internal static class BattleInputSettings
    {
        public static float GetInputCooldown()
        {
            try
            {
                var s = GameSettingsLoader.LoadFirst<GameplayPlayerSettings>("GameSettings");
                return s == null ? 0.1f
                    : s.PreferredInputControl != GameplayPlayerSettings.InputControlType.Keyboard
                        ? 0.15f
                    : s.SpeedSetting == GameplayPlayerSettings.GameSpeed.Fast ? 0.09f
                    : s.SpeedSetting == GameplayPlayerSettings.GameSpeed.VeryFast ? 0.08f
                    : 0.1f;
            }
            catch
            {
                return 0.1f;
            }
        }

        public static bool IsKeyboardPreferred()
        {
            try
            {
                var s = GameSettingsLoader.LoadFirst<GameplayPlayerSettings>("GameSettings");
                return s == null
                    || s.PreferredInputControl == GameplayPlayerSettings.InputControlType.Keyboard;
            }
            catch
            {
                return true;
            }
        }
    }
}
