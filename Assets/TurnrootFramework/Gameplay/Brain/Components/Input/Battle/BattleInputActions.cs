using Turnroot.UI;
using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Encapsulates creation and lifecycle of the input actions used by battle input controller.
    /// Keeps wiring in one place and makes the controller easier to test.
    /// </summary>
    internal class BattleInputActions
    {
        public InputAction Navigate { get; private set; }
        public InputAction Confirm { get; private set; }
        public InputAction Cancel { get; private set; }
        public InputAction Menu { get; private set; }

        public InputAction RotateCamera { get; private set; }

        public BattleInputActions()
        {
            Navigate = UIInputActionDefaults.Navigate;
            Confirm = UIInputActionDefaults.Confirm;
            Cancel = UIInputActionDefaults.Cancel;
            Menu = UIInputActionDefaults.Menu;
            RotateCamera = UIInputActionDefaults.RotateCamera;
        }

        public void Enable()
        {
            Navigate?.Enable();
            Confirm?.Enable();
            Cancel?.Enable();
            Menu?.Enable();
            RotateCamera?.Enable();
        }

        public void Disable()
        {
            Navigate?.Disable();
            Confirm?.Disable();
            Cancel?.Disable();
            Menu?.Disable();
            RotateCamera?.Disable();
        }

        public void Dispose()
        {
            Navigate?.Dispose();
            Confirm?.Dispose();
            Cancel?.Dispose();
            Menu?.Dispose();
            RotateCamera?.Dispose();

            Navigate = null;
            Confirm = null;
            Cancel = null;
            Menu = null;
            RotateCamera = null;
        }

        // The battle input actions are now sourced from the shared UI input defaults.
        // This ensures bindings remain consistent and configurable via InputActionAsset.
    }
}
