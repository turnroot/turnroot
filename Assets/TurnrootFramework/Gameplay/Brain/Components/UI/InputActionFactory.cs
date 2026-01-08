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
            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateDown()
        {
            var action = new InputAction("NavigateDown", InputActionType.Button);
            action.AddBinding("<Keyboard>/s");
            action.AddBinding("<Keyboard>/downArrow");
            action.Enable();
            return action;
        }

        public static InputAction CreateSelect()
        {
            var action = new InputAction("Select", InputActionType.Button);
            action.AddBinding("<Keyboard>/enter");
            action.AddBinding("<Keyboard>/space");
            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateLeft()
        {
            var action = new InputAction("NavigateLeft", InputActionType.Button);
            action.AddBinding("<Keyboard>/a");
            action.AddBinding("<Keyboard>/leftArrow");
            action.Enable();
            return action;
        }

        public static InputAction CreateNavigateRight()
        {
            var action = new InputAction("NavigateRight", InputActionType.Button);
            action.AddBinding("<Keyboard>/d");
            action.AddBinding("<Keyboard>/rightArrow");
            action.Enable();
            return action;
        }

        public static InputAction CreateBack()
        {
            var action = new InputAction("Back", InputActionType.Button);
            action.AddBinding("<Keyboard>/escape");
            action.AddBinding("<Keyboard>/backspace");
            action.Enable();
            return action;
        }

        public static void SetupMenuNavigation(MenuBase menu)
        {
            // Force refresh menu items to ensure proper detection
            menu.RefreshMenuItems();

            // Create new InputActions with proper bindings for keyboard navigation
            if (menu.navigateUpAction == null || menu.navigateUpAction.bindings.Count == 0)
            {
                menu.navigateUpAction = CreateNavigateUp();
            }

            if (menu.navigateDownAction == null || menu.navigateDownAction.bindings.Count == 0)
            {
                menu.navigateDownAction = CreateNavigateDown();
            }

            if (menu.selectAction == null || menu.selectAction.bindings.Count == 0)
            {
                menu.selectAction = CreateSelect();
            }

            // Enable all actions
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
