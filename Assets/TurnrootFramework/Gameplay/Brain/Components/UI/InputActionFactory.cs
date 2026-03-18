using Turnroot.UI;
using Turnroot.UI.Components.Menu;
using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.Brain.Segments
{
    /// <summary>
    /// Factory class for creating and configuring InputActions for menu navigation and gameplay controls.
    /// </summary>
    public static class InputActionFactory
    {
        public static InputAction CreateNavigateUp()
        {
            return UIInputActionDefaults.NavigateUp;
        }

        public static InputAction CreateNavigateDown()
        {
            return UIInputActionDefaults.NavigateDown;
        }

        public static InputAction CreateSelect()
        {
            return UIInputActionDefaults.Select;
        }

        public static InputAction CreateNavigateLeft()
        {
            return UIInputActionDefaults.NavigateLeft;
        }

        public static InputAction CreateNavigateRight()
        {
            return UIInputActionDefaults.NavigateRight;
        }

        public static InputAction CreateBack()
        {
            return UIInputActionDefaults.Back;
        }

        public static InputAction CreateDetails()
        {
            return UIInputActionDefaults.Confirm;
        }

        public static InputAction CreateNavigateVector()
        {
            return UIInputActionDefaults.Navigate;
        }

        public static void SetupMenuNavigation(MenuBase menu)
        {
            // Menu input actions are now shared across the UI via UIInputActionDefaults.
            // Ensure defaults are enabled so per-menu components can respond to input.
            UIInputActionDefaults.NavigateUp?.Enable();
            UIInputActionDefaults.NavigateDown?.Enable();
            UIInputActionDefaults.Select?.Enable();
        }

        public static void CleanupMenuNavigation(MenuBase menu)
        {
            // We do not dispose shared UI actions here; just disable them when the menu is no longer active.
            UIInputActionDefaults.NavigateUp?.Disable();
            UIInputActionDefaults.NavigateDown?.Disable();
            UIInputActionDefaults.Select?.Disable();
        }
    }
}
