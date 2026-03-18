using System;
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

        private void OnEnable()
        {
            UiChoice.EnableActions();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UiChoice.DisableActions();
        }

        private void Subscribe()
        {
            if (UiChoice.SelectAction != null)
            {
                UiChoice.SelectAction.performed += HandleSelect;
            }
            if (UiChoice.BackAction != null)
            {
                UiChoice.BackAction.performed += HandleBack;
            }
            if (UiChoice.NavigateUpAction != null)
            {
                UiChoice.NavigateUpAction.performed += HandleNavigateUp;
            }
            if (UiChoice.NavigateDownAction != null)
            {
                UiChoice.NavigateDownAction.performed += HandleNavigateDown;
            }
            if (UiChoice.NavigateLeftAction != null)
            {
                UiChoice.NavigateLeftAction.performed += HandleNavigateLeft;
            }
            if (UiChoice.NavigateRightAction != null)
            {
                UiChoice.NavigateRightAction.performed += HandleNavigateRight;
            }
        }

        private void Unsubscribe()
        {
            if (UiChoice.SelectAction != null)
            {
                UiChoice.SelectAction.performed -= HandleSelect;
            }
            if (UiChoice.BackAction != null)
            {
                UiChoice.BackAction.performed -= HandleBack;
            }
            if (UiChoice.NavigateUpAction != null)
            {
                UiChoice.NavigateUpAction.performed -= HandleNavigateUp;
            }
            if (UiChoice.NavigateDownAction != null)
            {
                UiChoice.NavigateDownAction.performed -= HandleNavigateDown;
            }
            if (UiChoice.NavigateLeftAction != null)
            {
                UiChoice.NavigateLeftAction.performed -= HandleNavigateLeft;
            }
            if (UiChoice.NavigateRightAction != null)
            {
                UiChoice.NavigateRightAction.performed -= HandleNavigateRight;
            }
        }

        private void HandleSelect(InputAction.CallbackContext ctx) => OnInput?.Invoke("Select");

        private void HandleBack(InputAction.CallbackContext ctx) => OnInput?.Invoke("Back");

        private void HandleNavigateUp(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke("NavigateUp");

        private void HandleNavigateDown(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke("NavigateDown");

        private void HandleNavigateLeft(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke("NavigateLeft");

        private void HandleNavigateRight(InputAction.CallbackContext ctx) =>
            OnInput?.Invoke("NavigateRight");

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
