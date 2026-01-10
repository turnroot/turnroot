using Turnroot.UI.Components.Menu;
using UnityEngine.InputSystem;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public static class InputActionFactory
    {
        public static InputAction CreateNavigateUp()
        {
            var action = new InputAction("NavigateUp", InputActionType.Button);
            action.AddBinding("<Keyboard>/w");
            action.AddBinding("<Keyboard>/upArrow");
            action.AddBinding("<Gamepad>/dpad/up");
            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateDown()
        {
            var action = new InputAction("NavigateDown", InputActionType.Button);
            action.AddBinding("<Keyboard>/s");
            action.AddBinding("<Keyboard>/downArrow");
            action.AddBinding("<Gamepad>/dpad/down");
            action.Enable();
            return action;
        }

        public static InputAction CreateSelect()
        {
            var action = new InputAction("Select", InputActionType.Button);
            action.AddBinding("<Keyboard>/enter");
            action.AddBinding("<Keyboard>/space");
            action.AddBinding("<Gamepad>/submit");
            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateLeft()
        {
            var action = new InputAction("NavigateLeft", InputActionType.Button);
            action.AddBinding("<Keyboard>/a");
            action.AddBinding("<Keyboard>/leftArrow");
            action.AddBinding("<Gamepad>/dpad/left");
            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateRight()
        {
            var action = new InputAction("NavigateRight", InputActionType.Button);
            action.AddBinding("<Keyboard>/d");
            action.AddBinding("<Keyboard>/rightArrow");
            action.AddBinding("<Gamepad>/dpad/right");
            action.Enable();
            return action;
        }

        public static InputAction CreateBack()
        {
            var action = new InputAction("Back", InputActionType.Button);
            action.AddBinding("<Keyboard>/escape");
            action.AddBinding("<Keyboard>/backspace");
            action.AddBinding("<Gamepad>/cancel");
            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateVector()
        {
            var action = new InputAction("NavigateVector", InputActionType.Value);
            action.expectedControlType = "Vector2";
            action.AddBinding("<Gamepad>/leftStick");
            action.AddBinding("<Gamepad>/dpad");
            action.AddBinding("<Keyboard>/wASD");
            action.AddBinding("<Keyboard>/arrowKeys");
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
