using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI
{
    [RequireComponent(typeof(Brain))]
    public class UIInputActionBootstrap : BrainComponent
    {
        [Header("UI Action References")]
        public InputActionReference Select;
        public InputActionReference Back;
        public InputActionReference NavigateUp;
        public InputActionReference NavigateDown;
        public InputActionReference NavigateLeft;
        public InputActionReference NavigateRight;
        public InputActionReference ScrollLeft;
        public InputActionReference ScrollRight;
        public InputActionReference Navigate;
        public InputActionReference Confirm;
        public InputActionReference Cancel;
        public InputActionReference Menu;
        public InputActionReference RotateCamera;
        public InputActionReference Start;
        public InputActionReference ToggleDetails;

        protected override void Awake()
        {
            base.Awake();
            InitializeActions();
        }

        protected override void SubscribeToBrainEvents()
        {
            // No brain events required for this component.
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            // No brain events required for this component.
        }

        private void InitializeActions()
        {
            UIInputActionDefaults.Initialize(
                Select,
                Back,
                NavigateUp,
                NavigateDown,
                NavigateLeft,
                NavigateRight,
                ScrollLeft,
                ScrollRight,
                Navigate,
                Confirm,
                Cancel,
                Menu,
                RotateCamera,
                Start,
                ToggleDetails
            );

            // Enable everything immediately so all consumers can listen to all actions.
            EnableAllActions();

            // Notify any systems that are waiting for the input actions to be ready.
            _brain?.NotifyInputsReady();
        }

        private void EnableAllActions()
        {
            void TryEnable(InputAction action)
            {
                if (action != null && !action.enabled)
                {
                    action.Enable();
                }
            }

            TryEnable(UIInputActionDefaults.Select);
            TryEnable(UIInputActionDefaults.Back);
            TryEnable(UIInputActionDefaults.NavigateUp);
            TryEnable(UIInputActionDefaults.NavigateDown);
            TryEnable(UIInputActionDefaults.NavigateLeft);
            TryEnable(UIInputActionDefaults.NavigateRight);
            TryEnable(UIInputActionDefaults.ScrollLeft);
            TryEnable(UIInputActionDefaults.ScrollRight);
            TryEnable(UIInputActionDefaults.Navigate);
            TryEnable(UIInputActionDefaults.Confirm);
            TryEnable(UIInputActionDefaults.Cancel);
            TryEnable(UIInputActionDefaults.Menu);
            TryEnable(UIInputActionDefaults.RotateCamera);
            TryEnable(UIInputActionDefaults.Start);
            TryEnable(UIInputActionDefaults.ToggleDetails);
        }
    }
}
