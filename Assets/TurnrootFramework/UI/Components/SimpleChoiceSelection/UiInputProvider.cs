using System;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI
{
    /// <summary>
    /// Shared component that manages the static UiChoice actions and forwards
    /// them as simple string events.  Also holds audio settings used by
    /// <see cref="UiChoiceHandler"/> during navigation.
    /// </summary>
    [RequireComponent(typeof(GameObject))]
    public class UiInputProvider : MonoBehaviour
    {
        /// <summary>Fired whenever one of the common UI actions is performed.</summary>
        public event Action<string> OnInput;

        [Header("Debug")]
        public bool LogInputActions = true;

        [Header("Audio")]
        public AudioSource UiFx;
        public AudioClip NavigateClip;

        private bool _subscribed;
        private bool _initializedHandlerRegistered;

        // Keep references to the exact actions we subscribed to so we can reliably unsubscribe
        // even if UIInputActionDefaults reinitializes and swaps in new action instances.
        private InputAction _subscribedSelect;
        private InputAction _subscribedBack;
        private InputAction _subscribedNavigateUp;
        private InputAction _subscribedNavigateDown;
        private InputAction _subscribedNavigateLeft;
        private InputAction _subscribedNavigateRight;
        private InputAction _subscribedStart;

        private InputAction _subscribedScrollLeft;
        private InputAction _subscribedScrollRight;

        private void Awake()
        {
            // Always register for initialization so we can bind once the shared actions are ready.
            if (!_initializedHandlerRegistered)
            {
                UIInputActionDefaults.WhenInitialized(Subscribe);
                _initializedHandlerRegistered = true;
            }
        }

        private void OnEnable()
        {
            "UiInputProvider: OnEnable".LogInfo("UiInputProvider");
            Subscribe();
        }

        private void Subscribe()
        {
            if (LogInputActions)
            {
                $"UiInputProvider.Subscribe called (initialized={UIInputActionDefaults.Initialized}, enabled={isActiveAndEnabled}, subscribed={_subscribed})".LogInfo();
            }

            if (_subscribed)
            {
                // If the shared actions were re-created (new instances), re-subscribe.
                if (
                    _subscribedSelect == UiChoice.SelectAction
                    && _subscribedStart == UiChoice.StartAction
                    && _subscribedBack == UiChoice.BackAction
                    && _subscribedNavigateUp == UiChoice.NavigateUpAction
                    && _subscribedNavigateDown == UiChoice.NavigateDownAction
                    && _subscribedNavigateLeft == UiChoice.NavigateLeftAction
                    && _subscribedNavigateRight == UiChoice.NavigateRightAction
                    && _subscribedScrollLeft == UiChoice.ScrollLeftAction
                    && _subscribedScrollRight == UiChoice.ScrollRightAction
                )
                {
                    return;
                }

                Unsubscribe();
            }

            // Subscribe regardless of whether this component is currently enabled.
            // The subscription is just an event handler and can safely run while disabled.

            // If the shared input actions are not yet initialized, wait for the initialization callback.
            if (!UIInputActionDefaults.Initialized)
            {
                return;
            }

            // Ensure any previous subscriptions are cleared (useful if actions were re-created).
            Unsubscribe();

            if (UiChoice.SelectAction == null && UiChoice.StartAction == null)
            {
                "UiInputProvider: Neither Select nor Start input actions are assigned. Check UIInputActionBootstrap configuration.".LogWarning(
                    "UiInputProvider.Subscribe"
                );

                $"UiInputProvider: UIInputActionDefaults.Initialized={UIInputActionDefaults.Initialized} Select={UiChoice.SelectAction} Start={UiChoice.StartAction}".LogWarning(
                    "UiInputProvider.Subscribe"
                );

                // If these actions are still null, keep waiting for initialization.
                // The initialization callback is already registered in OnEnable.

                return;
            }

            // Keep listening for re-initialization so we can re-subscribe if the
            // shared actions are recreated (e.g., on scene reload).

            _subscribed = true;

            _subscribedSelect = UiChoice.SelectAction;
            _subscribedBack = UiChoice.BackAction;
            _subscribedNavigateUp = UiChoice.NavigateUpAction;
            _subscribedNavigateDown = UiChoice.NavigateDownAction;
            _subscribedNavigateLeft = UiChoice.NavigateLeftAction;
            _subscribedNavigateRight = UiChoice.NavigateRightAction;
            _subscribedStart = UiChoice.StartAction;
            _subscribedScrollLeft = UiChoice.ScrollLeftAction;
            _subscribedScrollRight = UiChoice.ScrollRightAction;

            if (_subscribedSelect != null)
            {
                _subscribedSelect.performed += HandleSelect;
            }
            if (_subscribedBack != null)
            {
                _subscribedBack.performed += HandleBack;
            }
            if (_subscribedNavigateUp != null)
            {
                _subscribedNavigateUp.performed += HandleNavigateUp;
            }
            if (_subscribedNavigateDown != null)
            {
                _subscribedNavigateDown.performed += HandleNavigateDown;
            }
            if (_subscribedNavigateLeft != null)
            {
                _subscribedNavigateLeft.performed += HandleNavigateLeft;
            }
            if (_subscribedNavigateRight != null)
            {
                _subscribedNavigateRight.performed += HandleNavigateRight;
            }
            if (_subscribedStart != null)
            {
                _subscribedStart.performed += HandleStart;
            }
            if (_subscribedScrollLeft != null)
            {
                _subscribedScrollLeft.performed += HandleScrollLeft;
            }
            if (_subscribedScrollRight != null)
            {
                _subscribedScrollRight.performed += HandleScrollRight;
            }
        }

        private void Unsubscribe()
        {
            // Allow Subscribe to run again later.
            _subscribed = false;

            if (_subscribedSelect != null)
            {
                _subscribedSelect.performed -= HandleSelect;
                _subscribedSelect = null;
            }
            if (_subscribedBack != null)
            {
                _subscribedBack.performed -= HandleBack;
                _subscribedBack = null;
            }
            if (_subscribedNavigateUp != null)
            {
                _subscribedNavigateUp.performed -= HandleNavigateUp;
                _subscribedNavigateUp = null;
            }
            if (_subscribedNavigateDown != null)
            {
                _subscribedNavigateDown.performed -= HandleNavigateDown;
                _subscribedNavigateDown = null;
            }
            if (_subscribedNavigateLeft != null)
            {
                _subscribedNavigateLeft.performed -= HandleNavigateLeft;
                _subscribedNavigateLeft = null;
            }
            if (_subscribedNavigateRight != null)
            {
                _subscribedNavigateRight.performed -= HandleNavigateRight;
                _subscribedNavigateRight = null;
            }
            if (_subscribedStart != null)
            {
                _subscribedStart.performed -= HandleStart;
                _subscribedStart = null;
            }
            if (_subscribedScrollLeft != null)
            {
                _subscribedScrollLeft.performed -= HandleScrollLeft;
                _subscribedScrollLeft = null;
            }
            if (_subscribedScrollRight != null)
            {
                _subscribedScrollRight.performed -= HandleScrollRight;
                _subscribedScrollRight = null;
            }
            {
                UIInputActionDefaults.RemoveInitializedHandler(Subscribe);
                _initializedHandlerRegistered = false;
            }
        }

        private void HandleSelect(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.Submit);

        private void HandleBack(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.Cancel);

        private void HandleNavigateUp(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.NavigateUp);

        private void HandleNavigateDown(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.NavigateDown);

        private void HandleNavigateLeft(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.NavigateLeft);

        private void HandleNavigateRight(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.NavigateRight);

        private void HandleScrollLeft(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.ScrollLeft);

        private void HandleScrollRight(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.ScrollRight);

        private void HandleStart(InputAction.CallbackContext ctx) => OnInput?.Invoke("Start");

        /// <summary>
        /// Helper wrapping <see cref="UiChoiceHandler.HandleNavigation"/> that
        /// automatically passes this provider's audio settings.
        /// </summary>
        public void Navigate<T>(
            string action,
            T[] managers,
            ref int currentIndex,
            int maxCount,
            Action onSelect
        )
            where T : MonoBehaviour
        {
            UiChoiceHandler.HandleNavigation(
                action,
                managers,
                ref currentIndex,
                maxCount,
                onSelect,
                UiFx,
                NavigateClip
            );
        }
    }
}
