using Turnroot.Gameplay.PlayerSettings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.GridMenu
{
    public class ActionBindableGridMenuItem : GridMenuItem
    {
        [Header("Action Binding")]
        [Tooltip("Logical action this item invokes (used when its gamepad binding is pressed)")]
        public GameplayPlayerSettings.LogicalAction logicalAction = GameplayPlayerSettings
            .LogicalAction
            .Details;

        [Tooltip(
            "Selectable gamepad binding that triggers this item (limited to options for the selected action)"
        )]
        public GameplayPlayerSettings.InputBindingKey gamepadBinding = GameplayPlayerSettings
            .InputBindingKey
            .Details_GamepadButtonWest;

        // Runtime assigned action for the custom binding
        private InputAction _customInputAction;
        private System.Action<InputAction.CallbackContext> _performedHandler;

        private void OnEnable()
        {
            var settings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<GameplayPlayerSettings>();
            if (settings != null)
            {
                settings.BindingsChanged += OnSettingsBindingsChanged;
            }

            SetupCustomBinding();
        }

        private void OnDisable()
        {
            var settings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<GameplayPlayerSettings>();
            if (settings != null)
            {
                settings.BindingsChanged -= OnSettingsBindingsChanged;
            }

            CleanupCustomBinding();
        }

        private void OnValidate()
        {
            var settings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<GameplayPlayerSettings>();
            if (settings != null)
            {
                var opts = settings.GetOptionsForAction(logicalAction);
                if (
                    opts != null
                    && opts.Length > 0
                    && System.Array.IndexOf(opts, gamepadBinding) < 0
                )
                {
                    gamepadBinding = opts[0];
                }
            }

            if (Application.isPlaying)
            {
                SetupCustomBinding();
            }
        }

        private void OnSettingsBindingsChanged()
        {
            SetupCustomBinding();
        }

        private void SetupCustomBinding()
        {
            CleanupCustomBinding();

            if (!Application.isPlaying)
            {
                return;
            }

            var sb = GetComponent<Turnroot.UI.Components.SimpleButton.SimpleButton>();
            if (sb == null)
            {
                return;
            }

            var settings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<GameplayPlayerSettings>();
            if (settings == null)
            {
                return;
            }

            var path = settings.GetBinding(gamepadBinding);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _customInputAction = new InputAction(
                $"GridItem_{gameObject.name}",
                InputActionType.Button
            );
            _customInputAction.AddBinding(path);

            _performedHandler = (ctx) =>
            {
                Select();
            };
            _customInputAction.performed += _performedHandler;
            _customInputAction.Enable();

            sb.AssignSelectAction(_customInputAction);
        }

        private void CleanupCustomBinding()
        {
            if (_customInputAction != null)
            {
                try
                {
                    if (_performedHandler != null)
                    {
                        _customInputAction.performed -= _performedHandler;
                    }
                }
                catch { }

                try
                {
                    _customInputAction.Disable();
                    _customInputAction.Dispose();
                }
                catch { }

                _customInputAction = null;
                _performedHandler = null;
            }
        }
    }
}
