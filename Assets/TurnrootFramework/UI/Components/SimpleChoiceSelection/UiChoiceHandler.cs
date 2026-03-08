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
            // Deselect all choices
            foreach (var manager in managers)
            {
                manager.SendMessage("Deselect");
            }

            if (action == "NavigateUp" || action == "NavigateLeft")
            {
                navigationSound?.PlayOneShot(navigationClip);
                currentIndex = (currentIndex - 1 + maxCount) % maxCount;
            }
            else if (action == "NavigateDown" || action == "NavigateRight")
            {
                navigationSound?.PlayOneShot(navigationClip);
                currentIndex = (currentIndex + 1) % maxCount;
            }
            else if (action == "Select")
            {
                onSelect?.Invoke();
                return;
            }

            // Select the current choice
            managers[currentIndex].SendMessage("Select");
        }
    }
}
