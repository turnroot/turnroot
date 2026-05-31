using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI
{
    [RequireComponent(typeof(Brain))]
    public class UIInputActionBootstrap : BrainComponent
    {
        protected override void Awake()
        {
            base.Awake();
            InitializeActions();
        }

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }

        private void InitializeActions()
        {
            var s = GameplayInputSettings.Instance;
            if (s == null)
            {
                "UIInputActionBootstrap: GameplayInputSettings asset not found in Resources!".LogError();
                return;
            }

            UIInputActionDefaults.Initialize(
                s.Select,
                s.Back,
                s.NavigateUp,
                s.NavigateDown,
                s.NavigateLeft,
                s.NavigateRight,
                s.ScrollLeft,
                s.ScrollRight,
                s.Navigate,
                s.Confirm,
                s.Cancel,
                s.Menu,
                s.RotateCamera,
                s.Start,
                s.RightStickClick,
                s.ToggleDetails,
                s.Special
            );

            // Enable everything immediately so all consumers can listen to all actions.
            EnableAllActions();

            // Notify any systems that are waiting for the input actions to be ready.
            _brain?.NotifyInputsReady();
        }

        private void EnableAllActions()
        {
            static void TryEnable(InputAction action)
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
            TryEnable(UIInputActionDefaults.RightStickClick);
            TryEnable(UIInputActionDefaults.Special);
        }
    }
}
