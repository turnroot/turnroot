using System;
using Turnroot.GameSettings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Turnroot.UI.Components.SimpleButton
{
    public enum SimpleButtonRole
    {
        Confirm,
        Back,
        Next,
    }

    public class SimpleButton
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler
    {
        public InputAction SelectAction;
        public event Action OnSelected;
        public UnityEvent OnSelectedInspector;

        public SimpleButtonRole Role;

        public void Select() => StartCoroutine(SelectCoroutine());

        private System.Collections.IEnumerator SelectCoroutine()
        {
            Color currentColor = ButtonImage != null ? ButtonImage.color : NormalColor;
            yield return StartCoroutine(TweenColors(currentColor, SelectedColor));

            // After the coroutine finished, and if it's a selection
            OnSelected?.Invoke();
            OnSelectedInspector?.Invoke();
        }

        private System.Collections.IEnumerator TweenColors(Color startColor, Color endColor)
        {
            float elapsed = 0f;

            Color initialButtonColor = startColor;
            Color initialTextColor = startColor;

            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Duration);

                if (ButtonImage != null)
                {
                    ButtonImage.color = Color.Lerp(initialButtonColor, endColor, t);
                }
                if (ButtonText != null)
                {
                    ButtonText.color = Color.Lerp(initialTextColor, endColor, t);
                }

                yield return null;
            }

            if (ButtonImage != null)
            {
                ButtonImage.color = endColor;
            }
            if (ButtonText != null)
            {
                ButtonText.color = endColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var coroutine = StartCoroutine(TweenColors(NormalColor, HoveredColor));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var coroutine = StartCoroutine(TweenColors(HoveredColor, NormalColor));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Prevent the click from bubbling to other handlers
            eventData.Use();

            Select();
        }

        public Image ButtonImage;
        public TMPro.TextMeshProUGUI ButtonText;

        private GamewideUiSettings _uiSettings;

        private Color NormalColor => _uiSettings?.GridListFilmstripButtonNormalColor ?? Color.white;
        private Color SelectedColor =>
            _uiSettings?.GridListFilmstripButtonSelectedColor ?? Color.yellow;
        private Color HoveredColor =>
            _uiSettings?.GridListFilmstripButtonHoveredColor ?? Color.cyan;
        private float Duration => _uiSettings?.ButtonTransitionDuration ?? 0.12f;

        private void Awake()
        {
            _uiSettings = Turnroot.Utilities.GameSettingsLoader.LoadFirst<GamewideUiSettings>();
#if UNITY_EDITOR
            if (_uiSettings == null)
            {
                Debug.LogWarning("SimpleButton: GamewideUiSettings not found!");
            }
            if (ButtonImage == null)
            {
                Debug.LogWarning("SimpleButton: ButtonImage is not assigned!");
            }
            if (ButtonText == null)
            {
                Debug.LogWarning("SimpleButton: ButtonText is not assigned!");
            }
#endif
        }

        private void OnEnable()
        {
            if (SelectAction != null)
            {
                SelectAction.Enable();
                SelectAction.performed += OnSelectActionPerformed;
            }
        }

        private void OnDisable()
        {
            if (SelectAction != null)
            {
                SelectAction.performed -= OnSelectActionPerformed;
                SelectAction.Disable();
            }
        }

        private void OnSelectActionPerformed(InputAction.CallbackContext context) => Select();
    }
}
