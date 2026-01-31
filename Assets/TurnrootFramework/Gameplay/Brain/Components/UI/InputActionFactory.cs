using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI.Components.Menu;
using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.Brain.Segments
{
    public static class InputActionFactory
    {
        public static InputAction CreateNavigateUp()
        {
            var action = new InputAction("NavigateUp", InputActionType.Button);
            // Default keyboard bindings
            action.AddBinding("<Keyboard>/w");
            action.AddBinding("<Keyboard>/upArrow");

            // Default gamepad bindings (customization removed)
            action.AddBinding("<Gamepad>/dpad/up");

            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateDown()
        {
            var action = new InputAction("NavigateDown", InputActionType.Button);
            // Default keyboard bindings
            action.AddBinding("<Keyboard>/s");
            action.AddBinding("<Keyboard>/downArrow");

            // Default gamepad bindings (customization removed)
            action.AddBinding("<Gamepad>/dpad/down");

            action.Enable();
            return action;
        }

        public static InputAction CreateSelect()
        {
            var action = new InputAction("Select", InputActionType.Button);
            // Default keyboard bindings
            action.AddBinding("<Keyboard>/enter");
            action.AddBinding("<Keyboard>/space");

            // Default gamepad bindings (customization removed)
            action.AddBinding("<Gamepad>/submit");
            action.AddBinding("<Gamepad>/buttonSouth");

            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateLeft()
        {
            var action = new InputAction("NavigateLeft", InputActionType.Button);
            // Default keyboard bindings
            action.AddBinding("<Keyboard>/a");
            action.AddBinding("<Keyboard>/leftArrow");

            // Default gamepad bindings (customization removed)
            action.AddBinding("<Gamepad>/dpad/left");

            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateRight()
        {
            var action = new InputAction("NavigateRight", InputActionType.Button);
            // Default keyboard bindings
            action.AddBinding("<Keyboard>/d");
            action.AddBinding("<Keyboard>/rightArrow");

            // Default gamepad bindings (customization removed)
            action.AddBinding("<Gamepad>/dpad/right");

            action.Enable();
            return action;
        }

        public static InputAction CreateBack()
        {
            var action = new InputAction("Back", InputActionType.Button);
            // Default keyboard bindings
            action.AddBinding("<Keyboard>/escape");
            action.AddBinding("<Keyboard>/backspace");

            // Default gamepad bindings (customization removed)
            action.AddBinding("<Gamepad>/cancel");
            action.AddBinding("<Gamepad>/buttonEast");

            action.Enable();
            return action;
        }

        public static InputAction CreateDetails()
        {
            var action = new InputAction("Details", InputActionType.Button);
            // Default keyboard binding
            action.AddBinding("<Keyboard>/x");

            // Default gamepad binding (customization removed)
            action.AddBinding("<Gamepad>/buttonWest");

            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateVector()
        {
            var action = new InputAction("NavigateVector", InputActionType.Value);
            action.expectedControlType = "Vector2";

            // Default keyboard bindings
            action.AddBinding("<Keyboard>/wASD");
            action.AddBinding("<Keyboard>/arrowKeys");

            // Add both gamepad sources by default (customization removed)
            action.AddBinding("<Gamepad>/leftStick");
            action.AddBinding("<Gamepad>/dpad");

            action.Enable();
            return action;
        }

        public static void SetupMenuNavigation(MenuBase menu)
        {
            // Force refresh menu items to ensure proper detection
            menu.RefreshMenuItems();

            // Always recreate fresh InputActions for a menu to avoid carrying over stale or disposed actions
            // Clean up any existing actions first to avoid leaking resources
            if (menu.navigateUpAction != null)
            {
                try
                {
                    menu.navigateUpAction.Disable();
                }
                catch { }
                try
                {
                    menu.navigateUpAction.Dispose();
                }
                catch { }
            }
            menu.navigateUpAction = CreateNavigateUp();

            if (menu.navigateDownAction != null)
            {
                try
                {
                    menu.navigateDownAction.Disable();
                }
                catch { }
                try
                {
                    menu.navigateDownAction.Dispose();
                }
                catch { }
            }
            menu.navigateDownAction = CreateNavigateDown();

            if (menu.selectAction != null)
            {
                try
                {
                    menu.selectAction.Disable();
                }
                catch { }
                try
                {
                    menu.selectAction.Dispose();
                }
                catch { }
            }
            menu.selectAction = CreateSelect();

            // Enable all actions (Create* already enables, but ensure state)
            menu.navigateUpAction?.Enable();
            menu.navigateDownAction?.Enable();
            menu.selectAction?.Enable();
        }

        public static void CleanupMenuNavigation(MenuBase menu)
        {
            menu.navigateUpAction?.Disable();
            menu.navigateDownAction?.Disable();
            menu.selectAction?.Disable();
        }
    }
}
