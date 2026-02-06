using Turnroot.Gameplay.PlayerSettings;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Provides helper methods for retrieving player input settings such as cooldown times and control type preferences.
    /// </summary>
    internal static class InputSettingsHelper
    {
        private const float KEYBOARD_BASE_COOLDOWN = 0.1f;
        private const float GAMEPAD_COOLDOWN = 0.15f;

        public static float GetInputCooldown()
        {
            try
            {
                var settings = GameplayPlayerSettings.Instance;
                if (settings == null)
                {
                    return KEYBOARD_BASE_COOLDOWN;
                }

                bool isKeyboard =
                    settings.PreferredInputControl
                    == GameplayPlayerSettings.InputControlType.Keyboard;
                return !isKeyboard ? GAMEPAD_COOLDOWN
                    : settings.SpeedSetting == GameplayPlayerSettings.GameSpeed.Fast ? 0.09f
                    : settings.SpeedSetting == GameplayPlayerSettings.GameSpeed.VeryFast ? 0.08f
                    : KEYBOARD_BASE_COOLDOWN;
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
                var settings = GameplayPlayerSettings.Instance;
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
