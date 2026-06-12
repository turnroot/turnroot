using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.GameSettings;
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
        private InputAction _subscribedRightStickMove;

        private InputAction _subscribedScrollLeft;
        private InputAction _subscribedScrollRight;

        private readonly Dictionary<string, Coroutine> _repeatCoroutines = new();

        private void Awake()
        {
            // Always register for initialization so we can bind once the shared actions are ready.
            if (!_initializedHandlerRegistered)
            {
                UIInputActionDefaults.WhenInitialized(Subscribe);
                _initializedHandlerRegistered = true;
            }
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => StopAllHeldRepeats();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
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
                    && _subscribedRightStickMove == UIInputActionDefaults.RightStickMove
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
            _subscribedRightStickMove = UIInputActionDefaults.RightStickMove;

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
                _subscribedNavigateUp.started += OnNavigateUpStarted;
                _subscribedNavigateUp.canceled += OnNavigateUpCanceled;
            }
            if (_subscribedNavigateDown != null)
            {
                _subscribedNavigateDown.performed += HandleNavigateDown;
                _subscribedNavigateDown.started += OnNavigateDownStarted;
                _subscribedNavigateDown.canceled += OnNavigateDownCanceled;
            }
            if (_subscribedNavigateLeft != null)
            {
                _subscribedNavigateLeft.performed += HandleNavigateLeft;
                _subscribedNavigateLeft.started += OnNavigateLeftStarted;
                _subscribedNavigateLeft.canceled += OnNavigateLeftCanceled;
            }
            if (_subscribedNavigateRight != null)
            {
                _subscribedNavigateRight.performed += HandleNavigateRight;
                _subscribedNavigateRight.started += OnNavigateRightStarted;
                _subscribedNavigateRight.canceled += OnNavigateRightCanceled;
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
            if (_subscribedRightStickMove != null)
            {
                _subscribedRightStickMove.started += HandleRightStickMove;
                _subscribedRightStickMove.performed += HandleRightStickMove;
                _subscribedRightStickMove.canceled += HandleRightStickMove;
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
                _subscribedNavigateUp.started -= OnNavigateUpStarted;
                _subscribedNavigateUp.canceled -= OnNavigateUpCanceled;
                _subscribedNavigateUp = null;
            }
            if (_subscribedNavigateDown != null)
            {
                _subscribedNavigateDown.performed -= HandleNavigateDown;
                _subscribedNavigateDown.started -= OnNavigateDownStarted;
                _subscribedNavigateDown.canceled -= OnNavigateDownCanceled;
                _subscribedNavigateDown = null;
            }
            if (_subscribedNavigateLeft != null)
            {
                _subscribedNavigateLeft.performed -= HandleNavigateLeft;
                _subscribedNavigateLeft.started -= OnNavigateLeftStarted;
                _subscribedNavigateLeft.canceled -= OnNavigateLeftCanceled;
                _subscribedNavigateLeft = null;
            }
            if (_subscribedNavigateRight != null)
            {
                _subscribedNavigateRight.performed -= HandleNavigateRight;
                _subscribedNavigateRight.started -= OnNavigateRightStarted;
                _subscribedNavigateRight.canceled -= OnNavigateRightCanceled;
                _subscribedNavigateRight = null;
            }
            StopAllHeldRepeats();
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
            if (_subscribedRightStickMove != null)
            {
                _subscribedRightStickMove.started -= HandleRightStickMove;
                _subscribedRightStickMove.performed -= HandleRightStickMove;
                _subscribedRightStickMove.canceled -= HandleRightStickMove;
                _subscribedRightStickMove = null;
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

        // Hold-repeat started/canceled handlers
        private void OnNavigateUpStarted(InputAction.CallbackContext ctx) =>
            StartHeldRepeat(InputActionConstants.NavigateUp);

        private void OnNavigateUpCanceled(InputAction.CallbackContext ctx) =>
            StopHeldRepeat(InputActionConstants.NavigateUp);

        private void OnNavigateDownStarted(InputAction.CallbackContext ctx) =>
            StartHeldRepeat(InputActionConstants.NavigateDown);

        private void OnNavigateDownCanceled(InputAction.CallbackContext ctx) =>
            StopHeldRepeat(InputActionConstants.NavigateDown);

        private void OnNavigateLeftStarted(InputAction.CallbackContext ctx) =>
            StartHeldRepeat(InputActionConstants.NavigateLeft);

        private void OnNavigateLeftCanceled(InputAction.CallbackContext ctx) =>
            StopHeldRepeat(InputActionConstants.NavigateLeft);

        private void OnNavigateRightStarted(InputAction.CallbackContext ctx) =>
            StartHeldRepeat(InputActionConstants.NavigateRight);

        private void OnNavigateRightCanceled(InputAction.CallbackContext ctx) =>
            StopHeldRepeat(InputActionConstants.NavigateRight);

        private void StartHeldRepeat(string actionName)
        {
            StopHeldRepeat(actionName);
            if (isActiveAndEnabled)
            {
                _repeatCoroutines[actionName] = StartCoroutine(HoldRepeatCoroutine(actionName));
            }
        }

        private void StopHeldRepeat(string actionName)
        {
            if (_repeatCoroutines.TryGetValue(actionName, out var coroutine) && coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            _repeatCoroutines.Remove(actionName);
        }

        private void StopAllHeldRepeats()
        {
            foreach (var coroutine in _repeatCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _repeatCoroutines.Clear();
        }

        private IEnumerator HoldRepeatCoroutine(string actionName)
        {
            var settings = GameplayInputSettings.Instance;
            float initialDelay = settings != null ? settings.InitialRepeatDelay : 0.4f;
            float interval = settings != null ? settings.RepeatInterval : 0.1f;
            const float minInterval = 0.016f; // cap at ~60Hz
            yield return new WaitForSecondsRealtime(initialDelay);
            while (true)
            {
                OnInput?.Invoke(actionName);
                yield return new WaitForSecondsRealtime(interval);
                interval = Mathf.Max(minInterval, interval - 0.005f);
            }
        }

        private void HandleScrollLeft(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.ScrollLeft);

        private void HandleScrollRight(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.ScrollRight);

        private void HandleRightStickMove(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.RightStickMove);

        private void HandleStart(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke(InputActionConstants.Start);

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
