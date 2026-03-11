using System;
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
        /// <typeparam name="T">Any MonoBehaviour with Select/Deselect methods</typeparam>
        /// <param name="action">The input action ("NavigateUp", "NavigateDown", "NavigateLeft", "NavigateRight", "Select")</param>
        /// <param name="managers">Array of UI choice components</param>
        /// <param name="currentIndex">Current selection index (will be modified)</param>
        /// <param name="maxCount">Maximum number of choices</param>
        /// <param name="onSelect">Callback to invoke when selection is confirmed</param>
        /// <param name="navigationSound">Optional audio source for navigation feedback</param>
        /// <param name="navigationClip">Optional audio clip for navigation sound</param>
        public static void HandleNavigation<T>(
            string action,
            T[] managers,
            ref int currentIndex,
            int maxCount,
            Action onSelect,
            AudioSource navigationSound = null,
            AudioClip navigationClip = null
        )
            where T : MonoBehaviour
        {
            // Deselect all choices (skip any that have been destroyed)
            for (int i = 0; i < managers.Length; i++)
            {
                var manager = managers[i];
                if (manager == null)
                {
                    continue;
                }

                try
                {
                    manager.SendMessage("Deselect");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"UiChoiceHandler: exception during Deselect on index {i}: {ex.Message}"
                    );
                }
            }

            if (action == "NavigateUp" || action == "NavigateLeft")
            {
                if (navigationSound != null && navigationClip != null)
                {
                    // audio source may have been destroyed when scene unloaded
                    try
                    {
                        navigationSound.PlayOneShot(navigationClip);
                    }
                    catch (Exception)
                    { /* ignore if source invalid */
                    }
                }
                currentIndex = (currentIndex - 1 + maxCount) % maxCount;
            }
            else if (action == "NavigateDown" || action == "NavigateRight")
            {
                if (navigationSound != null && navigationClip != null)
                {
                    try
                    {
                        navigationSound.PlayOneShot(navigationClip);
                    }
                    catch (Exception) { }
                }
                currentIndex = (currentIndex + 1) % maxCount;
            }
            else if (action == "Select")
            {
                try
                {
                    onSelect?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("UiChoiceHandler onSelect threw: " + ex);
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
                currentIndex = 0;
            }
            // also suppress missing receiver warning
            managers[currentIndex]
                .BroadcastMessage("Select", SendMessageOptions.DontRequireReceiver);
        }
    }
}
