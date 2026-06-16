using System;
using System.Collections;
using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UiScroller : MonoBehaviour
    {
        public static InputAction SelectAction => UIInputActionDefaults.Select;
        public static InputAction BackAction => UIInputActionDefaults.Back;
        public static InputAction NavigateUpAction => UIInputActionDefaults.NavigateUp;
        public static InputAction NavigateDownAction => UIInputActionDefaults.NavigateDown;
        public static InputAction NavigateLeftAction => UIInputActionDefaults.NavigateLeft;
        public static InputAction NavigateRightAction => UIInputActionDefaults.NavigateRight;
        public static InputAction StartAction => UIInputActionDefaults.Start;
        public string[] Choices;
        public string SelectedChoice { get; private set; }
        public UIEffect LeftEffect;
        public UIEffect RightEffect;

        public GameObject SelectedDecorator;
        public Color TextHighlightColor = Color.yellow;
        public Color originalTextColor;
        public TextMeshProUGUI DisplayText;
        private IEnumerator _activeCoroutine;

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
            _activeCoroutine = PlayScrollEffects(LeftEffect);
            StartCoroutine(_activeCoroutine);
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
            _activeCoroutine = PlayScrollEffects(RightEffect);
            StartCoroutine(_activeCoroutine);
        }

        private void OnDisable()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
        }

        private IEnumerator PlayScrollEffects(UIEffect effect)
        {
            if (effect != null)
            {
                effect.enabled = true;
                yield return new WaitForSeconds(.3f);
                effect.enabled = false;
            }
        }
    }
}
