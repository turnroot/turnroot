using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    public partial class BlacksmithUi : MonoBehaviour
    {
        public void HandleItemChangeInput(string action)
        {
            if (paginationHelper == null || itemChoices == null || itemChoices.Count == 0)
            {
                "BlacksmithUi: No item choices available to change selection.".LogWarning();
                return;
            }

            if (action == InputActionConstants.NavigateDown)
            {
                paginationHelper.ChangeSelectionByOffset(1);
            }
            else if (action == InputActionConstants.NavigateUp)
            {
                paginationHelper.ChangeSelectionByOffset(-1);
            }
            else
            {
                return;
            }

            CurrentPage = paginationHelper.CurrentPage;
            CurrentSelectionIndex = paginationHelper.CurrentSelectionIndex;
            AudioPlayer?.PlayOneShot(NavigateAudioClip);
        }

        public void ChangePageInput(string action)
        {
            paginationHelper?.HandleScrollInput(action);
            if (paginationHelper != null)
            {
                CurrentPage = paginationHelper.CurrentPage;
                CurrentSelectionIndex = paginationHelper.CurrentSelectionIndex;
            }
        }

        public void HandleNavigateLeftInput(string action) { }

        public void HandleNavigateRightInput(string action) { }

        public void HandleSelectInput(string action) { }

        public void HandleBackInput(string action) { }
    }
}
