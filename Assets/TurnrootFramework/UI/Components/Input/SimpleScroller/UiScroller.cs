using System;
using TMPro;
using UnityEngine;

namespace Turnroot.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UiScroller : MonoBehaviour
    {
        public string[] Choices;
        public string SelectedChoice { get; private set; }

        public GameObject SelectedDecorator;
        public Color TextHighlightColor = Color.yellow;
        public Color originalTextColor;
        public TextMeshProUGUI DisplayText;
        public Action<string> OnChange;
        // Note to self later
        // I'm using Select and Deselect here to move between rows like UIChoice using
        // UIChoiceHandler. Technically this is fully compatible with the UIChoiceHandler,
        // even though it's not a UIChoice, but I can reuse UiChoiceHandler.HandleNavigation
        // at least for up/down

        public void Select()
        {
            if (SelectedDecorator != null)
            {
                SelectedDecorator.SetActive(true);
            }
            DisplayText.color = TextHighlightColor;
        }

        public void Deselect()
        {
            if (SelectedDecorator != null)
            {
                SelectedDecorator.SetActive(false);
            }
            DisplayText.color = originalTextColor;
        }

        public void ScrollLeft()
        {
            if (Choices == null || Choices.Length == 0)
            {
                return;
            }

            var currentIndex = Array.IndexOf(Choices, SelectedChoice);
            var newIndex = (currentIndex - 1 + Choices.Length) % Choices.Length;
            SelectedChoice = Choices[newIndex];
            DisplayText.text = SelectedChoice;
            OnChange?.Invoke(SelectedChoice);
        }

        public void ScrollRight()
        {
            if (Choices == null || Choices.Length == 0)
            {
                return;
            }

            var currentIndex = Array.IndexOf(Choices, SelectedChoice);
            var newIndex = (currentIndex + 1) % Choices.Length;
            SelectedChoice = Choices[newIndex];
            DisplayText.text = SelectedChoice;
            OnChange?.Invoke(SelectedChoice);
        }

        public void SetChoices(string[] choices, string defaultChoice = null, bool selected = false)
        {
            Choices = choices;
            if (Choices == null || Choices.Length == 0)
            {
                SelectedChoice = null;
                DisplayText.text = "";
                return;
            }

            SelectedChoice =
                defaultChoice != null && Array.IndexOf(Choices, defaultChoice) >= 0
                    ? defaultChoice
                    : Choices[0];

            DisplayText.text = SelectedChoice;

            if (selected)
            {
                Select();
            }
            else
            {
                Deselect();
            }
        }
    }
}
