using System;
using Turnroot.Utilities;
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
        public static bool Initialized { get; private set; }
        public static event Action OnInitialized;

        public static void WhenInitialized(Action callback)
        {
            // Always subscribe so that re-initialization (e.g. after a reload)
            // will trigger the callback again.
            OnInitialized += callback;

            // If we're already initialized, invoke immediately so callers don't
            // have to wait for the next Initialize() call.
            if (Initialized)
            {
                callback?.Invoke();
            }
        }

        public static void RemoveInitializedHandler(Action callback)
        {
            OnInitialized -= callback;
        }

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
        public static InputAction RotateCamera;
        public static InputAction RotateMapCamera; // legacy alias
        public static InputAction Start;
        public static InputAction ToggleDetails;
        public static InputAction ScrollLeft;
        public static InputAction ScrollRight;

        private static bool _enforceActionsAlwaysEnabled;

        public static void Initialize(
            InputActionReference select,
            InputActionReference back,
            InputActionReference navigateUp,
            InputActionReference navigateDown,
            InputActionReference navigateLeft,
            InputActionReference navigateRight,
            InputActionReference scrollLeft,
            InputActionReference scrollRight,
            InputActionReference navigate,
            InputActionReference confirm,
            InputActionReference cancel,
            InputActionReference menu,
            InputActionReference rotateCamera,
            InputActionReference start,
            InputActionReference toggleDetails
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
            RotateCamera = rotateCamera?.action;
            Start = start?.action;
            ToggleDetails = toggleDetails?.action;
            ScrollLeft = scrollLeft?.action;
            ScrollRight = scrollRight?.action;

            // Always keep the shared UI actions enabled and prevent any other code
            // from disabling them.
            EnableAllSharedActions();
            EnsureSharedActionsStayEnabled();

            // Expose legacy name for backwards compatibility
            RotateMapCamera = RotateCamera;

            Initialized = true;

            "UIInputActionDefaults initialized".LogInfo("UIInputActionDefaults");

            OnInitialized?.Invoke();

            if (Select == null)
            {
                "Select action is null".LogWarning("UIInputActionDefaults");
            }
            if (Start == null)
            {
                "Start action is null".LogWarning("UIInputActionDefaults");
            }
        }

        private static void EnableAllSharedActions()
        {
            void TryEnable(InputAction action)
            {
                if (action != null && !action.enabled)
                {
                    action.Enable();
                }
            }

            TryEnable(Select);
            TryEnable(Back);
            TryEnable(NavigateUp);
            TryEnable(NavigateDown);
            TryEnable(NavigateLeft);
            TryEnable(NavigateRight);
            TryEnable(Navigate);
            TryEnable(Confirm);
            TryEnable(Cancel);
            TryEnable(Menu);
            TryEnable(RotateCamera);
            TryEnable(Start);
            TryEnable(ToggleDetails);
            TryEnable(ScrollLeft);
            TryEnable(ScrollRight);
        }

        private static void EnsureSharedActionsStayEnabled()
        {
            if (_enforceActionsAlwaysEnabled)
            {
                return;
            }

            _enforceActionsAlwaysEnabled = true;
            InputSystem.onActionChange += OnInputActionChange;
        }

        private static void OnInputActionChange(object actionObj, InputActionChange change)
        {
            // Only react when a shared action is explicitly disabled.
            if (change != InputActionChange.ActionDisabled)
            {
                return;
            }

            if (actionObj is not InputAction action)
            {
                return;
            }

            if (IsSharedAction(action))
            {
                // Re-enable immediately if something disables it.
                action.Enable();
                "UIInputActionDefaults: Re-enabled shared input action".LogInfo(
                    "UIInputActionDefaults"
                );
            }
        }

        private static bool IsSharedAction(InputAction action)
        {
            return action == Select
                || action == Back
                || action == NavigateUp
                || action == NavigateDown
                || action == NavigateLeft
                || action == NavigateRight
                || action == Navigate
                || action == Confirm
                || action == Cancel
                || action == Menu
                || action == RotateCamera
                || action == Start
                || action == ToggleDetails
                || action == ScrollLeft
                || action == ScrollRight;
        }
    }
}
