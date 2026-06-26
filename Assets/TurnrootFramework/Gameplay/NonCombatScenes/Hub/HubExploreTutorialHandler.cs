using System;
using System.Collections.Generic;
using TMPro;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct HubExploreTutorialPage
    {
        public string Title;

        [TextArea(5, 20)]
        public string KeyboardText;

        [TextArea(5, 20)]
        public string GamepadText;
        public UIFade Fade;

        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ContentText;
    }

    public class HubExploreTutorialHandler : MonoBehaviour, IPageHandler
    {
        [Tooltip("Ordered list of UIFade panels to display as tutorial pages.")]
        public List<HubExploreTutorialPage> Pages = new();

        public Action Completed;

        private int _currentIndex = -1;
        private Brain.Brain _brain;
        private SpecificUiHandler _specificUiHandler;

        private bool UsingGamepad = false;

        public int CurrentPageIndex
        {
            get => _currentIndex;
            set => _currentIndex = value;
        }

        public int PageCount => Pages?.Count ?? 0;

        private void Awake()
        {
            _brain = GetAndCacheBrain.GetBrain();
            _specificUiHandler = FindFirstObjectByType<SpecificUiHandler>();

            if (_specificUiHandler != null)
            {
                _specificUiHandler.ActivePageHandler = this;
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

            this.BeginPageSequence();
        }

        private void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;

        private void OnDisable() => InputSystem.onDeviceChange -= OnDeviceChange;

        private void OnDeviceChange(InputDevice device, InputDeviceChange change) =>
            UsingGamepad = Gamepad.all.Count > 0;

        public void RefreshDevices() => UsingGamepad = Gamepad.all.Count > 0;

        private void OnDestroy()
        {
            if (
                _specificUiHandler != null
                && ReferenceEquals(_specificUiHandler.ActivePageHandler, this)
            )
            {
                _specificUiHandler.ActivePageHandler = null;
            }
        }

        public UIFade GetPageFade(int index) => Pages[index].Fade;

        public void OnPageShown(int index) => SetupPage(index);

        public void OnPagesCompleted() => Complete();

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
