using System;
using Turnroot.GameSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Turnroot.UI.Components.SimpleButton
{
    /// <summary>
    /// Defines the semantic role of a button for input action mapping and behavior.
    /// </summary>
    public enum SimpleButtonRole
    {
        Confirm,
        Back,
        Details,
    }

    /// <summary>
    /// A UI button component with color transitions, input action support, and hover/click handling.
    /// </summary>
    public class SimpleButton
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler
    {
        [HideInInspector]
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
            // Use the explicitly assigned action over the inspector reference.
            UnsubscribeFromSelectAction();
            SelectAction = action;
            SubscribeToSelectAction();
        }

        private InputAction GetEffectiveSelectAction()
        {
            return SelectAction != null
                ? SelectAction
                : Role switch
                {
                    SimpleButtonRole.Back => UIInputActionDefaults.Back,
                    SimpleButtonRole.Details => UIInputActionDefaults.ToggleDetails,
                    _ => UIInputActionDefaults.Select,
                };
        }

        private void SubscribeToSelectAction()
        {
            var action = GetEffectiveSelectAction();
            if (action == null)
            {
                return;
            }

            try
            {
                action.performed -= OnSelectActionPerformed;
            }
            catch { }

            action.performed += OnSelectActionPerformed;

            if (gameObject.activeInHierarchy)
            {
                action.Enable();
            }
        }

        private static bool IsSharedUiInputAction(InputAction action)
        {
            return action == UIInputActionDefaults.Select
                || action == UIInputActionDefaults.Back
                || action == UIInputActionDefaults.NavigateUp
                || action == UIInputActionDefaults.NavigateDown
                || action == UIInputActionDefaults.NavigateLeft
                || action == UIInputActionDefaults.NavigateRight
                || action == UIInputActionDefaults.Start
                || action == UIInputActionDefaults.Confirm
                || action == UIInputActionDefaults.Cancel
                || action == UIInputActionDefaults.Menu
                || action == UIInputActionDefaults.Navigate
                || action == UIInputActionDefaults.RotateCamera
                || action == UIInputActionDefaults.RightStickMove
                || action == UIInputActionDefaults.ToggleDetails;
        }

        private void UnsubscribeFromSelectAction()
        {
            var action = GetEffectiveSelectAction();
            if (action == null)
            {
                return;
            }

            try
            {
                action.performed -= OnSelectActionPerformed;
            }
            catch { }

            // Do not disable shared UI actions: they should stay enabled at all times.
            if (!IsSharedUiInputAction(action))
            {
                action.Disable();
            }
        }

        public void Select() => StartCoroutine(SelectCoroutine());

        private System.Collections.IEnumerator SelectCoroutine()
        {
            Color currentColor = ButtonImage != null ? ButtonImage.color : NormalColor;
            yield return StartCoroutine(TweenColors(currentColor, SelectedColor));

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

        private void Awake() => _uiSettings = GamewideUiSettings.Instance;

        private void OnEnable() => SubscribeToSelectAction();

        private void OnDisable() => UnsubscribeFromSelectAction();

        private void OnSelectActionPerformed(InputAction.CallbackContext context)
        {
            // Back and Details buttons should ALWAYS work regardless of hover state
            if (Role is SimpleButtonRole.Back or SimpleButtonRole.Details)
            {
                Select();
                return;
            }

            // For menu items, only respond if hovered
            var menuItem = GetComponent<MenuItemBase>();
            if (menuItem != null && menuItem.ParentMenu != null)
            {
                if (!_isHovered)
                {
                    return;
                }
            }

            Select();
        }
    }
}
