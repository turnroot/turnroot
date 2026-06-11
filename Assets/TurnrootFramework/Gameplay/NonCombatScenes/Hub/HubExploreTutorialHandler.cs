using System;
using System.Collections.Generic;
using TMPro;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public struct HubExploreTutorialPage
    {
        public string Title;
        public string KeyboardText;
        public string GamepadText;
        public UIFade Fade;

        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ContentText;
    }

    public class HubExploreTutorialHandler : MonoBehaviour, ISpecificUiTutorialHandler
    {
        [Tooltip("Ordered list of UIFade panels to display as tutorial pages.")]
        public List<HubExploreTutorialPage> Pages = new();

        public Action Completed;

        private int _currentIndex = -1;
        private Brain.Brain _brain;
        private SpecificUiHandler _specificUiHandler;

        private bool UsingGamepad = false;

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
            Pages[_currentIndex].Fade.Show();
        }

        private void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;

        private void OnDisable() => InputSystem.onDeviceChange -= OnDeviceChange;

        private void OnDeviceChange(InputDevice device, InputDeviceChange change) =>
            UsingGamepad = Gamepad.all.Count > 0;

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

            Pages[_currentIndex].Fade.Hide();
            _currentIndex++;

            if (_currentIndex >= Pages.Count)
            {
                Complete();
                return;
            }

            SetupPage(_currentIndex);
        }

        private void GoBack()
        {
            if (_currentIndex <= 0)
            {
                return;
            }

            Pages[_currentIndex].Fade.Hide();
            _currentIndex--;
            SetupPage(_currentIndex);
        }

        private void Complete()
        {
            HubDayStateStore.MarkExploreTutorialSeen(_brain);
            Completed?.Invoke();
            Destroy(gameObject);
        }

        public void SetupPage(int index)
        {
            if (Pages == null || index < 0 || index >= Pages.Count)
            {
                "HubExploreTutorialHandler: Invalid page index {index}.".LogWarning();
                return;
            }

            var page = Pages[index];
            page.TitleText.text = page.Title;
            page.ContentText.text = UsingGamepad ? page.GamepadText : page.KeyboardText;
            page.Fade.Show();
        }
    }
}
