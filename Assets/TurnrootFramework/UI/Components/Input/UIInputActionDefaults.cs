using UnityEngine.InputSystem;

namespace Turnroot.UI
{
    /// <summary>
    /// Holds the shared input actions for menu navigation and UI interaction.
    /// This allows a single bootstrap component to assign them from an
    /// InputActionAsset (via InputActionReference) and have the rest of the UI
    /// code use a stable static reference.
    /// </summary>
    public static class UIInputActionDefaults
    {
        public static InputAction Select;
        public static InputAction Back;
        public static InputAction NavigateUp;
        public static InputAction NavigateDown;
        public static InputAction NavigateLeft;
        public static InputAction NavigateRight;

        // Additional actions used by other subsystems
        public static InputAction Navigate;
        public static InputAction Confirm;
        public static InputAction Cancel;
        public static InputAction Menu;
        public static InputAction RotateMapCamera;
        public static InputAction Start;

        public static void Initialize(
            InputActionReference select,
            InputActionReference back,
            InputActionReference navigateUp,
            InputActionReference navigateDown,
            InputActionReference navigateLeft,
            InputActionReference navigateRight,
            InputActionReference navigate,
            InputActionReference confirm,
            InputActionReference cancel,
            InputActionReference menu,
            InputActionReference rotateMapCamera,
            InputActionReference start
        )
        {
            Select = select?.action;
            Back = back?.action;
            NavigateUp = navigateUp?.action;
            NavigateDown = navigateDown?.action;
            NavigateLeft = navigateLeft?.action;
            NavigateRight = navigateRight?.action;

            Navigate = navigate?.action;
            Confirm = confirm?.action;
            Cancel = cancel?.action;
            Menu = menu?.action;
            RotateMapCamera = rotateMapCamera?.action;
            Start = start?.action;
        }
    }
}
