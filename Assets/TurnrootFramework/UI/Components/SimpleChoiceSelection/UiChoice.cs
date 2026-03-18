using Coffee.UIEffects;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI
{
    /// <summary>
    /// simple reusable UI component for selectable choices in menus
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UiChoice : MonoBehaviour
    {
        // static shared input actions used by all menus
        public static InputAction SelectAction => UIInputActionDefaults.Select;
        public static InputAction BackAction => UIInputActionDefaults.Back;
        public static InputAction NavigateUpAction => UIInputActionDefaults.NavigateUp;
        public static InputAction NavigateDownAction => UIInputActionDefaults.NavigateDown;
        public static InputAction NavigateLeftAction => UIInputActionDefaults.NavigateLeft;
        public static InputAction NavigateRightAction => UIInputActionDefaults.NavigateRight;

        private static int _actionEnableCount = 0;

        public static void EnableActions()
        {
            if (_actionEnableCount++ == 0)
            {
                UIInputActionDefaults.Select?.Enable();
                UIInputActionDefaults.Back?.Enable();
                UIInputActionDefaults.NavigateUp?.Enable();
                UIInputActionDefaults.NavigateDown?.Enable();
                UIInputActionDefaults.NavigateLeft?.Enable();
                UIInputActionDefaults.NavigateRight?.Enable();
            }
        }

        public static void DisableActions()
        {
            if (--_actionEnableCount <= 0)
            {
                UIInputActionDefaults.Select?.Disable();
                UIInputActionDefaults.Back?.Disable();
                UIInputActionDefaults.NavigateUp?.Disable();
                UIInputActionDefaults.NavigateDown?.Disable();
                UIInputActionDefaults.NavigateLeft?.Disable();
                UIInputActionDefaults.NavigateRight?.Disable();
                _actionEnableCount = 0;
            }
        }

        public RectTransform ToScale => GetComponent<RectTransform>();
        public UIEffect Effect;
        public bool IsActive { get; private set; } = false;
        public bool UseScale = true;
        public bool ChangeTextColor = false;

        [ShowIf(nameof(ChangeTextColor))]
        public TextMeshProUGUI TextToChangeColor;

        [ShowIf(nameof(ChangeTextColor))]
        public Color TextHighlightColor = Color.yellow;
        private Color originalTextColor;
        public bool CanBeSelected = true;

        [ShowIf(nameof(ChangeTextColor))]
        public Color TextColorIfDisabled = Color.gray;

        private void Awake()
        {
            if (ChangeTextColor && TextToChangeColor != null)
            {
                originalTextColor = TextToChangeColor.color;
            }
            if (!CanBeSelected && ChangeTextColor && TextToChangeColor != null)
            {
                TextToChangeColor.color = TextColorIfDisabled;
            }
        }

        public void Select()
        {
            if (!CanBeSelected)
            {
                return;
            }

            IsActive = true;
            if (UseScale)
            {
                ToScale.localScale = Vector3.one * 1.1f;
            }
            if (ChangeTextColor && TextToChangeColor != null)
            {
                TextToChangeColor.color = TextHighlightColor;
            }
            Effect.enabled = true;
        }

        public void Deselect()
        {
            if (!CanBeSelected)
            {
                return;
            }
            IsActive = false;
            if (UseScale)
            {
                ToScale.localScale = Vector3.one;
            }
            if (ChangeTextColor && TextToChangeColor != null)
            {
                TextToChangeColor.color = originalTextColor;
            }
            Effect.enabled = false;
        }
    }
}
