using System;
using Turnroot.Conversations;
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

        // Used to reject the first accidental input event after one-shot conversation finishes.
        private bool _suppressNextInput;

        public void SuppressNextInput()
        {
            _suppressNextInput = true;
            if (LogInputActions)
            {
                "UiInputProvider: suppressing next input event".LogInfo("UiInputProvider");
            }
        }

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

        private void OnDisable()
        {
            "UiInputProvider: OnDisable".LogInfo("UiInputProvider");
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (_initializedHandlerRegistered)
            {
                UIInputActionDefaults.RemoveInitializedHandler(Subscribe);
                _initializedHandlerRegistered = false;
            }
            Unsubscribe();
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

            if (LogInputActions)
            {
                var select = UiChoice.SelectAction;
                var start = UiChoice.StartAction;

                (
                    $"UiInputProvider.Subscribe: Select={(select != null ? select.name : "<null>")}, "
                    + $"Start={(start != null ? start.name : "<null>")}, "
                    + $"Select.enabled={(select != null ? select.enabled.ToString() : "-")}, "
                    + $"Start.enabled={(start != null ? start.enabled.ToString() : "-")}"
                ).LogInfo();
            }

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
        }

        private bool IsConversationActive()
        {
            return ConversationController.Instance != null
                && ConversationController.Instance.IsConversationActive;
        }

        private bool ShouldBlockInput(string action)
        {
            // In case this callback hits a destroyed Unity object, guard against exceptions.
            if (this == null)
            {
                return true;
            }

            // Honor the component enabled state so disabling the provider truly blocks input.
            if (!isActiveAndEnabled)
            {
                if (LogInputActions)
                {
                    $"UiInputProvider: input blocked because provider is disabled ({action})".LogInfo();
                }
                return true;
            }

            if (_suppressNextInput)
            {
                _suppressNextInput = false;
                if (LogInputActions)
                {
                    $"UiInputProvider: blocked one immediate post-conversation input ({action})".LogInfo();
                }
                return true;
            }

            if (IsConversationActive())
            {
                if (LogInputActions)
                {
                    $"UiInputProvider: input blocked because conversation is active ({action})".LogInfo();
                }
                return true;
            }

            return false;
        }

        private void HandleSelect(InputAction.CallbackContext ctx)
        {
            if (ShouldBlockInput("Select"))
            {
                return;
            }
            OnInput?.Invoke("Select");
        }

        private void HandleBack(InputAction.CallbackContext ctx)
        {
            if (ShouldBlockInput("Back"))
            {
                return;
            }
            OnInput?.Invoke("Back");
            Debug.Log("UiInputProvider: Back action received.");
        }

        private void HandleNavigateUp(InputAction.CallbackContext ctx)
        {
            if (ShouldBlockInput("NavigateUp"))
            {
                return;
            }
            OnInput?.Invoke("NavigateUp");
        }

        private void HandleNavigateDown(InputAction.CallbackContext ctx)
        {
            if (ShouldBlockInput("NavigateDown"))
            {
                return;
            }
            OnInput?.Invoke("NavigateDown");
        }

        private void HandleNavigateLeft(InputAction.CallbackContext ctx)
        {
            if (ShouldBlockInput("NavigateLeft"))
            {
                return;
            }
            OnInput?.Invoke("NavigateLeft");
        }

        private void HandleNavigateRight(InputAction.CallbackContext ctx)
        {
            if (ShouldBlockInput("NavigateRight"))
            {
                return;
            }
            OnInput?.Invoke("NavigateRight");
        }

        private void HandleStart(InputAction.CallbackContext ctx)
        {
            if (ShouldBlockInput("Start"))
            {
                return;
            }
            OnInput?.Invoke("Start");
        }

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
