using System;
using System.Collections.Generic;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubExploreTutorialHandler : MonoBehaviour, ISpecificUiTutorialHandler
    {
        [Tooltip("Ordered list of UIFade panels to display as tutorial pages.")]
        public List<UIFade> Pages = new();

        public Action Completed;

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
                "HubExploreTutorialHandler: Could not find SpecificUiHandler in scene.".LogWarning();
            }
        }

        private void Start()
        {
            if (Pages == null || Pages.Count == 0)
            {
                "HubExploreTutorialHandler: No pages assigned — completing tutorial immediately.".LogWarning();
                Complete();
                return;
            }

            _currentIndex = 0;
            Pages[_currentIndex].Show();
        }

        private void OnDestroy()
        {
            if (
                _specificUiHandler != null
                && ReferenceEquals(_specificUiHandler.ActiveTutorialHandler, this)
            )
            {
                _specificUiHandler.ActiveTutorialHandler = null;
            }
        }

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
            HubDayStateStore.MarkExploreTutorialSeen(_brain);
            Completed?.Invoke();
            Destroy(gameObject);
        }
    }
}
