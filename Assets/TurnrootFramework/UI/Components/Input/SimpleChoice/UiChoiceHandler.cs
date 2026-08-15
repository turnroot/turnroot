using System;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI
{
    /// <summary>
    /// Generic handler for UI choice navigation in menu systems.
    /// Works with any MonoBehaviour that implements Select/Deselect pattern via SendMessage.
    /// </summary>
    public static class UiChoiceHandler
    {
        /// <summary>
        /// Handles navigation and selection for UI choice components.
        /// </summary>
        /// <typeparam name="T">A <see cref="UiChoice"/> component providing Select/Deselect visual feedback.</typeparam>
        /// <param name="action">The input action string from <see cref="InputActionConstants"/>.</param>
        /// <param name="managers">Array of UiChoice components to navigate.</param>
        /// <param name="currentIndex">Current selection index (will be modified).</param>
        /// <param name="maxCount">Maximum number of choices.</param>
        /// <param name="onSelect">Callback to invoke when selection is confirmed.</param>
        /// <param name="navigationSound">Optional audio source for navigation feedback.</param>
        /// <param name="navigationClip">Optional audio clip for navigation sound.</param>
        public static void HandleNavigation<T>(
            string action,
            T[] managers,
            ref int currentIndex,
            int maxCount,
            Action onSelect,
            AudioSource navigationSound = null,
            AudioClip navigationClip = null
        )
            where T : UiChoice
        {
            // Validate inputs early to avoid unnecessary exceptions
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(managers, nameof(managers))
            );

            if (!validation.Success)
            {
                validation.ErrorMessage.LogWarning();
                return;
            }

            // Deselect all choices (skip any that have been destroyed)
            for (int i = 0; i < managers.Length; i++)
            {
                managers[i]?.Deselect();
            }

            if (action is InputActionConstants.NavigateUp or InputActionConstants.NavigateLeft)
            {
                if (
                    navigationSound != null
                    && navigationClip != null
                    && navigationSound.isActiveAndEnabled
                )
                {
                    navigationSound.PlayOneShot(navigationClip);
                }
                currentIndex = (currentIndex - 1 + maxCount) % maxCount;
            }
            else if (
                action is InputActionConstants.NavigateDown or InputActionConstants.NavigateRight
            )
            {
                if (
                    navigationSound != null
                    && navigationClip != null
                    && navigationSound.isActiveAndEnabled
                )
                {
                    navigationSound.PlayOneShot(navigationClip);
                }
                currentIndex = (currentIndex + 1) % maxCount;
            }
            else if (
                action
                is InputActionConstants.Submit
                    or InputActionConstants.Select
                    or InputActionConstants.Start
                    or InputActionConstants.Confirm
            )
            {
                try
                {
                    onSelect?.Invoke();
                }
                catch (Exception ex)
                {
                    $"UiChoiceHandler onSelect threw: {ex}".LogWarning();
                }
                return;
            }

            // Select the current choice (guard against null)
            if (
                currentIndex < 0
                || currentIndex >= managers.Length
                || managers[currentIndex] == null
            )
            {
                $"UiChoiceHandler: Invalid currentIndex {currentIndex} after navigation.".LogWarning();
                currentIndex = 0;
            }

            managers[currentIndex]?.Select();
        }
    }
}
