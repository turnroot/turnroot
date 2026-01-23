using Turnroot.Audio.PreferredBattleMusic;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.PlayerSettings
{
    [CreateAssetMenu(
        fileName = "GameplayPlayerSettings",
        menuName = "Turnroot/Gameplay/Gameplay Player Settings"
    )]
    public class GameplayPlayerSettings : SingletonScriptableObject<GameplayPlayerSettings>
    {
        // there are no headers or decorators because the player will never interact with this directly

        public enum DifficultyLevel
        {
            Easy,
            Normal,
            Hard,
            Extreme,
        }

        public enum GameSpeed
        {
            Normal,
            Fast,
            VeryFast,
        }

        public enum SkipEnemyTurn
        {
            Movement,
            AllExceptAttacks,
            EntireTurn,
            None,
        }

        public enum BattleGridDisplay
        {
            UnitSelected,
            AlwaysOn,
            AlwaysOff,
        }

        public enum BattleGridDisplayStyle
        {
            Subtle,
            Colorful,
            Intense,
        }

        public enum StartTurnUnit
        {
            Avatar,
            LastUnit,
            LowestHpUnit,
        }

        public enum InputControlType
        {
            Keyboard,
            Gamepad,
        }

        // Input binding helper struct: stores a storage name and default binding path and exposes a simple enum
        [System.Serializable]
        public struct InputBindingOption
        {
            public enum Option
            {
                Default,
            }

            public string storageName; // key used for persistence
            public string defaultPath; // default input path (e.g. "<Keyboard>/w")

            public InputBindingOption(string storageName, string defaultPath)
            {
                this.storageName = storageName;
                this.defaultPath = defaultPath;
            }

            public string GetStorageName(Option _) => storageName;

            public string GetDefaultPath(Option _) => defaultPath;
        }

        public enum InputBindingKey
        {
            NavigateUp_GamepadDpadUp,
            NavigateDown_GamepadDpadDown,
            Select_GamepadSubmit,
            NavigateLeft_GamepadDpadLeft,
            NavigateRight_GamepadDpadRight,
            Back_GamepadCancel,
            Details_GamepadButtonWest,
            NavigateVector_GamepadLeftStick,
            NavigateVector_GamepadDpad,
        }

        [Header("Gamepad Bindings")]
        // Only expose gamepad options in the inspector for player customization
        public InputBindingOption NavigateUp_GamepadDpadUp = new(
            "NavigateUp_GamepadDpadUp",
            "<Gamepad>/dpad/up"
        );
        public InputBindingOption NavigateDown_GamepadDpadDown = new(
            "NavigateDown_GamepadDpadDown",
            "<Gamepad>/dpad/down"
        );
        public InputBindingOption Select_GamepadSubmit = new(
            "Select_GamepadSubmit",
            "<Gamepad>/submit"
        );
        public InputBindingOption NavigateLeft_GamepadDpadLeft = new(
            "NavigateLeft_GamepadDpadLeft",
            "<Gamepad>/dpad/left"
        );
        public InputBindingOption NavigateRight_GamepadDpadRight = new(
            "NavigateRight_GamepadDpadRight",
            "<Gamepad>/dpad/right"
        );
        public InputBindingOption Back_GamepadCancel = new(
            "Back_GamepadCancel",
            "<Gamepad>/cancel"
        );
        public InputBindingOption Details_GamepadButtonWest = new(
            "Details_GamepadButtonWest",
            "<Gamepad>/buttonWest"
        );
        public InputBindingOption NavigateVector_GamepadLeftStick = new(
            "NavigateVector_GamepadLeftStick",
            "<Gamepad>/leftStick"
        );
        public InputBindingOption NavigateVector_GamepadDpad = new(
            "NavigateVector_GamepadDpad",
            "<Gamepad>/dpad"
        );

        // Returns the configured binding (or default) for a given key
        public string GetBinding(InputBindingKey key)
        {
            switch (key)
            {
                case InputBindingKey.NavigateUp_GamepadDpadUp:
                    return NavigateUp_GamepadDpadUp.defaultPath;
                case InputBindingKey.NavigateDown_GamepadDpadDown:
                    return NavigateDown_GamepadDpadDown.defaultPath;
                case InputBindingKey.Select_GamepadSubmit:
                    return Select_GamepadSubmit.defaultPath;
                case InputBindingKey.NavigateLeft_GamepadDpadLeft:
                    return NavigateLeft_GamepadDpadLeft.defaultPath;
                case InputBindingKey.NavigateRight_GamepadDpadRight:
                    return NavigateRight_GamepadDpadRight.defaultPath;
                case InputBindingKey.Back_GamepadCancel:
                    return Back_GamepadCancel.defaultPath;
                case InputBindingKey.Details_GamepadButtonWest:
                    return Details_GamepadButtonWest.defaultPath;
                case InputBindingKey.NavigateVector_GamepadLeftStick:
                    return NavigateVector_GamepadLeftStick.defaultPath;
                case InputBindingKey.NavigateVector_GamepadDpad:
                    return NavigateVector_GamepadDpad.defaultPath;
                default:
                    return string.Empty;
            }
        }

        // Logical actions exposed to the UI (e.g., Select, Back, Details...)
        public enum LogicalAction
        {
            Select,
            Back,
            Details,
            NavigateUp,
            NavigateDown,
            NavigateLeft,
            NavigateRight,
            NavigateVector,
        }

        // Selected option per logical action (defaults to existing defaults)
        [Header("Action Selections")]
        public InputBindingKey Selected_Select = InputBindingKey.Select_GamepadSubmit;
        public InputBindingKey Selected_Back = InputBindingKey.Back_GamepadCancel;
        public InputBindingKey Selected_Details = InputBindingKey.Details_GamepadButtonWest;
        public InputBindingKey Selected_NavigateUp = InputBindingKey.NavigateUp_GamepadDpadUp;
        public InputBindingKey Selected_NavigateDown = InputBindingKey.NavigateDown_GamepadDpadDown;
        public InputBindingKey Selected_NavigateLeft = InputBindingKey.NavigateLeft_GamepadDpadLeft;
        public InputBindingKey Selected_NavigateRight =
            InputBindingKey.NavigateRight_GamepadDpadRight;
        public InputBindingKey Selected_NavigateVector =
            InputBindingKey.NavigateVector_GamepadLeftStick;

        private string SelectedPrefKey(LogicalAction action) => $"binding_selected_{action}";

        // Returns the persisted (or default) selected option for an action
        public InputBindingKey GetSelectedOptionForAction(LogicalAction action)
        {
            var pref = SelectedPrefKey(action);
            if (UnityEngine.PlayerPrefs.HasKey(pref))
            {
                var s = UnityEngine.PlayerPrefs.GetString(pref);
                if (System.Enum.TryParse<InputBindingKey>(s, out var parsed))
                {
                    return parsed;
                }
            }

            return action switch
            {
                LogicalAction.Select => Selected_Select,
                LogicalAction.Back => Selected_Back,
                LogicalAction.Details => Selected_Details,
                LogicalAction.NavigateUp => Selected_NavigateUp,
                LogicalAction.NavigateDown => Selected_NavigateDown,
                LogicalAction.NavigateLeft => Selected_NavigateLeft,
                LogicalAction.NavigateRight => Selected_NavigateRight,
                LogicalAction.NavigateVector => Selected_NavigateVector,
                _ => Selected_Select,
            };
        }

        public event System.Action BindingsChanged;

        public void SetSelectedOptionForAction(LogicalAction action, InputBindingKey key)
        {
            var pref = SelectedPrefKey(action);
            UnityEngine.PlayerPrefs.SetString(pref, key.ToString());
            UnityEngine.PlayerPrefs.Save();

            // Notify listeners that bindings have changed
            BindingsChanged?.Invoke();
        }

        public void NotifyBindingsChanged() => BindingsChanged?.Invoke();

        // Convenience: return the binding path for the chosen option for an action
        public string GetBindingForSelectedOption(LogicalAction action) =>
            GetBinding(GetSelectedOptionForAction(action));

        // Return all available options for a logical action (for UI dropdowns)
        public InputBindingKey[] GetOptionsForAction(LogicalAction action)
        {
            return action switch
            {
                LogicalAction.Select => new[] { InputBindingKey.Select_GamepadSubmit },
                LogicalAction.Back => new[] { InputBindingKey.Back_GamepadCancel },
                LogicalAction.Details => new[] { InputBindingKey.Details_GamepadButtonWest },
                LogicalAction.NavigateUp => new[] { InputBindingKey.NavigateUp_GamepadDpadUp },
                LogicalAction.NavigateDown => new[]
                {
                    InputBindingKey.NavigateDown_GamepadDpadDown,
                },
                LogicalAction.NavigateLeft => new[]
                {
                    InputBindingKey.NavigateLeft_GamepadDpadLeft,
                },
                LogicalAction.NavigateRight => new[]
                {
                    InputBindingKey.NavigateRight_GamepadDpadRight,
                },
                LogicalAction.NavigateVector => new[]
                {
                    InputBindingKey.NavigateVector_GamepadLeftStick,
                    InputBindingKey.NavigateVector_GamepadDpad,
                },
                _ => new InputBindingKey[] { },
            };
        }

        // Returns a friendly label for an InputBindingKey (used for dropdown labels)
        public string GetLabelFor(InputBindingKey key)
        {
            return key switch
            {
                InputBindingKey.NavigateUp_GamepadDpadUp => "Gamepad: D-Pad Up",
                InputBindingKey.NavigateDown_GamepadDpadDown => "Gamepad: D-Pad Down",
                InputBindingKey.Select_GamepadSubmit => "Gamepad: Submit",
                InputBindingKey.NavigateLeft_GamepadDpadLeft => "Gamepad: D-Pad Left",
                InputBindingKey.NavigateRight_GamepadDpadRight => "Gamepad: D-Pad Right",
                InputBindingKey.Back_GamepadCancel => "Gamepad: Cancel",
                InputBindingKey.Details_GamepadButtonWest => "Gamepad: Button West (X)",
                InputBindingKey.NavigateVector_GamepadLeftStick => "Gamepad: Left Stick",
                InputBindingKey.NavigateVector_GamepadDpad => "Gamepad: D-Pad",
                _ => key.ToString(),
            };
        }

        // gameplay
        public bool Permadeath;
        public bool TutorialPrompts = true;
        public DifficultyLevel GameDifficulty = DifficultyLevel.Normal;
        public GameSpeed SpeedSetting = GameSpeed.Normal;
        public SkipEnemyTurn SkipEnemyTurnAnimations = SkipEnemyTurn.Movement;
        public bool AutoEndTurn = true;

        public StartTurnUnit StartUnitSetting = StartTurnUnit.Avatar;
        public InputControlType PreferredInputControl = InputControlType.Keyboard;
        public bool DisableTurnwheel = false;
        public bool DisableTacticalLens = false;
        public BattleGridDisplay BattleGridSetting = BattleGridDisplay.AlwaysOn;
        public BattleGridDisplayStyle BattleGridStyle = BattleGridDisplayStyle.Subtle;

        // graphics
        // Brightness mapped to URP Color Adjustments.postExposure (range: -2..2)
        public float Brightness = 0.0f;

        // Contrast mapped to URP Color Adjustments.contrast (range: -50..50)
        public float Contrast = 0.0f;
        public float Quality = 1f;

        /// <summary>
        /// Discrete quality step (0..3) mapped from stored gameplay quality values.
        /// PlayerSettings.Quality will be one of {0, 0.1, 0.2, 0.3} and we map
        /// those to steps 0..3 by multiplying by 10 and clamping to [0,3].
        /// </summary>
        public int QualityStep => Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(Quality) * 10f), 0, 3);
        public bool Subtitles = true;
        public bool Bloom = true;
        public bool LensFlare = false;
        public bool DepthOfField = true;
        public bool AnimatedCameraMovement = true;

        // audio
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.8f;
        public float VoiceVolume = 0.8f;
        public bool MusicWhenPaused = true;
        public SongChoice PreferredBattleMusic = SongChoice.Default;
    }
}
