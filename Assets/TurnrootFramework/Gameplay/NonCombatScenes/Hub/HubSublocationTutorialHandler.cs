using System.Collections.Generic;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    /// <summary>
    /// Handles the first-visit tutorial for a hub sublocation.
    /// Attach to a prefab that is instantiated by HubSubLocation on first visit.
    /// Assign an ordered list of UIFade panels in the inspector; the handler shows
    /// them one at a time and hands input back to the sublocation when all are done.
    /// </summary>
    public class HubSublocationTutorialHandler : MonoBehaviour
    {
        [Tooltip("Ordered list of UIFade panels to display as tutorial pages.")]
        public List<UIFade> Pages = new();

        private int _currentIndex = -1;
        private Brain.Brain _brain;
        private SpecificUiHandler _specificUiHandler;

        private void Awake()
        {
            _brain = FindFirstObjectByType<Brain.Brain>();
            _specificUiHandler = FindFirstObjectByType<SpecificUiHandler>();

            if (_specificUiHandler != null)
            {
                _specificUiHandler.ActiveTutorialHandler = this;
            }
            else
            {
                "HubSublocationTutorialHandler: Could not find SpecificUiHandler in scene.".LogWarning();
            }
        }

        private void Start()
        {
            if (Pages == null || Pages.Count == 0)
            {
                "HubSublocationTutorialHandler: No pages assigned — completing tutorial immediately.".LogWarning();
                Complete();
                return;
            }

            _currentIndex = 0;
            Pages[_currentIndex].Show();
        }

        private void OnDestroy()
        {
            if (_specificUiHandler != null && _specificUiHandler.ActiveTutorialHandler == this)
            {
                _specificUiHandler.ActiveTutorialHandler = null;
            }
        }

        /// <summary>
        /// Called by SpecificUiHandler.HandleInput when this handler is active.
        /// </summary>
        public void HandleInput(string action)
        {
            if (
                action
                is InputActionConstants.Select
                    or InputActionConstants.Start
                    or InputActionConstants.Submit
                    or InputActionConstants.Confirm
            )
            {
                Advance();
            }
            else if (action is InputActionConstants.Back or InputActionConstants.Cancel)
            {
                GoBack();
            }
        }

        private void Advance()
        {
            if (_currentIndex < 0 || Pages == null || Pages.Count == 0)
            {
                return;
            }

            Pages[_currentIndex].Hide();
            _currentIndex++;

            if (_currentIndex >= Pages.Count)
            {
                Complete();
                return;
            }

            Pages[_currentIndex].Show();
        }

        private void GoBack()
        {
            if (_currentIndex <= 0)
            {
                return;
            }

            Pages[_currentIndex].Hide();
            _currentIndex--;
            Pages[_currentIndex].Show();
        }

        private void Complete()
        {
            _brain?.PublishHubSublocationTutorialCompleted();
            Destroy(gameObject);
        }
    }
}
