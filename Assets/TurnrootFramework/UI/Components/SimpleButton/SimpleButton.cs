using System;
using Turnroot.GameSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Turnroot.UI.Components.SimpleButton
{
    public enum SimpleButtonRole
    {
        Confirm,
        Back,
        Details,
    }

    public class SimpleButton
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler
    {
        public InputAction SelectAction;
        public event Action OnSelected;
        public SimpleButtonRole Role;

        /// <summary>
        /// Assigns the input action to use for selection and ensures the performed
        /// callback is hooked up immediately. This avoids missing the subscription
        /// when the action is assigned after Unity's OnEnable is called on the component.
        /// </summary>
        public void AssignSelectAction(InputAction action)
        {
            // Remove any previous subscription to avoid duplicates
            if (SelectAction != null)
            {
                try
                {
                    SelectAction.performed -= OnSelectActionPerformed;
                }
                catch { }
            }

            SelectAction = action;

            if (SelectAction != null)
            {
                // Ensure callback is attached and action is enabled if component is active
                try
                {
                    SelectAction.performed -= OnSelectActionPerformed;
                }
                catch { }
                SelectAction.performed += OnSelectActionPerformed;

                if (gameObject.activeInHierarchy)
                {
                    SelectAction.Enable();
                }
            }
        }

        public void Select()
        {
#if UNITY_EDITOR
            Debug.Log($"SimpleButton.Select() called on {gameObject.name}, Role: {Role}");
#endif
            StartCoroutine(SelectCoroutine());
        }

        private System.Collections.IEnumerator SelectCoroutine()
        {
            Color currentColor = ButtonImage != null ? ButtonImage.color : NormalColor;
            yield return StartCoroutine(TweenColors(currentColor, SelectedColor));

#if UNITY_EDITOR
            Debug.Log(
                $"SimpleButton: About to invoke OnSelected. Subscribers: {OnSelected?.GetInvocationList()?.Length ?? 0}"
            );
#endif

            // After the coroutine finished, and if it's a selection
            OnSelected?.Invoke();

            // Transition back to normal color after action is invoked
            yield return StartCoroutine(TweenColors(SelectedColor, NormalColor));
        }

        private System.Collections.IEnumerator TweenColors(Color startColor, Color endColor)
        {
            float elapsed = 0f;

            Color initialButtonColor = startColor;

            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Duration);

                if (ButtonImage != null)
                {
                    ButtonImage.color = Color.Lerp(initialButtonColor, endColor, t);
                }

                yield return null;
            }

            if (ButtonImage != null)
            {
                ButtonImage.color = endColor;
            }
        }

        private bool _isHovered = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            var coroutine = StartCoroutine(TweenColors(NormalColor, HoveredColor));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            var coroutine = StartCoroutine(TweenColors(HoveredColor, NormalColor));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Prevent the click from bubbling to other handlers
            eventData.Use();

            Select();
        }

        public Image ButtonImage;

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
#endif
        }

        private void OnEnable()
        {
            if (SelectAction != null)
            {
                // Always remove first to avoid duplicate subscriptions
                try
                {
                    SelectAction.performed -= OnSelectActionPerformed;
                }
                catch { }

                SelectAction.performed += OnSelectActionPerformed;
                SelectAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (SelectAction != null)
            {
                try
                {
                    SelectAction.performed -= OnSelectActionPerformed;
                }
                catch { }
                SelectAction.Disable();
            }
        }

        private void OnSelectActionPerformed(InputAction.CallbackContext context)
        {
            // Back and Details buttons should ALWAYS work regardless of hover state
            if (Role == SimpleButtonRole.Back || Role == SimpleButtonRole.Details)
            {
                Select();
                return;
            }

            // For menu items, only respond if hovered
            var menuItem = GetComponent<Turnroot.UI.Components.MenuItemBase>();
            if (menuItem != null && menuItem.ParentMenu != null)
            {
                if (!_isHovered)
                {
#if UNITY_EDITOR
                    Debug.Log($"SimpleButton: Ignored select for {gameObject.name} (not hovered)");
#endif
                    return;
                }
            }

            Select();
        }
    }
}
