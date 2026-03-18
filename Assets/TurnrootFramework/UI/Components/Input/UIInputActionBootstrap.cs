using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI
{
    /// <summary>
    /// Bootstrap component that assigns shared InputActionReference bindings to the
    /// common UI input action defaults used across the project.
    /// Place this on a root scene object (e.g. GameManager) and configure it in the
    /// inspector once.
    /// </summary>
    public class UIInputActionBootstrap : MonoBehaviour
    {
        [Header("UI Action References")]
        public InputActionReference Select;
        public InputActionReference Back;
        public InputActionReference NavigateUp;
        public InputActionReference NavigateDown;
        public InputActionReference NavigateLeft;
        public InputActionReference NavigateRight;

        [Header("General Input Action References")]
        public InputActionReference Navigate;
        public InputActionReference Confirm;
        public InputActionReference Cancel;
        public InputActionReference Menu;
        public InputActionReference RotateMapCamera;
        public InputActionReference Start;

        private void Awake()
        {
            UIInputActionDefaults.Initialize(
                Select,
                Back,
                NavigateUp,
                NavigateDown,
                NavigateLeft,
                NavigateRight,
                Navigate,
                Confirm,
                Cancel,
                Menu,
                RotateMapCamera,
                Start
            );
        }
    }
}
