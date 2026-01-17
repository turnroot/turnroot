using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    internal static class InputSettingsHelper
    {
        private const float KEYBOARD_BASE_COOLDOWN = 0.1f;
        private const float GAMEPAD_COOLDOWN = 0.15f;

        public static float GetInputCooldown()
        {
            try
            {
                var settings = GameSettingsLoader.LoadFirst<GameplayPlayerSettings>("GameSettings");
                if (settings == null)
                {
                    return KEYBOARD_BASE_COOLDOWN;
                }

                bool isKeyboard =
                    settings.PreferredInputControl
                    == GameplayPlayerSettings.InputControlType.Keyboard;
                return !isKeyboard
                    ? GAMEPAD_COOLDOWN
                    : settings.SpeedSetting == GameplayPlayerSettings.GameSpeed.Fast
                    ? 0.09f
                    : settings.SpeedSetting == GameplayPlayerSettings.GameSpeed.VeryFast ? 0.08f : KEYBOARD_BASE_COOLDOWN;
            }
            catch
            {
                return KEYBOARD_BASE_COOLDOWN;
            }
        }

        public static bool IsKeyboardPreferred()
        {
            try
            {
                var settings = GameSettingsLoader.LoadFirst<GameplayPlayerSettings>("GameSettings");
                return settings == null
                    || settings.PreferredInputControl
                        == GameplayPlayerSettings.InputControlType.Keyboard;
            }
            catch
            {
                return true;
            }
        }
    }
}
