using Coffee.UIEffects;
using NaughtyAttributes;
using TMPro;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UiChoice : MonoBehaviour
    {
        public static InputAction SelectAction => UIInputActionDefaults.Select;
        public static InputAction BackAction => UIInputActionDefaults.Back;
        public static InputAction NavigateUpAction => UIInputActionDefaults.NavigateUp;
        public static InputAction NavigateDownAction => UIInputActionDefaults.NavigateDown;
        public static InputAction NavigateLeftAction => UIInputActionDefaults.NavigateLeft;
        public static InputAction NavigateRightAction => UIInputActionDefaults.NavigateRight;
        public static InputAction StartAction => UIInputActionDefaults.Start;
        public static InputAction RightStickClickAction => UIInputActionDefaults.RightStickClick;

        public static InputAction ScrollLeftAction => UIInputActionDefaults.ScrollLeft;
        public static InputAction ScrollRightAction => UIInputActionDefaults.ScrollRight;

        public RectTransform ToScale => GetComponent<RectTransform>();
        public UIEffect Effect;
        public bool IsActive { get; private set; } = false;
        public bool UseScale = true;
        public bool ChangeTextColor = false;

        [ShowIf(nameof(ChangeTextColor))]
        public TextMeshProUGUI TextToChangeColor;

        [ShowIf(nameof(ChangeTextColor))]
        public Color TextHighlightColor = Color.yellow;
        public Color originalTextColor = Color.black;
        public bool CanBeSelected = true;
        public Color TextColorIfDisabled = Color.gray;

        private void Awake()
        {
            if (!CanBeSelected && TextToChangeColor != null)
            {
                TextToChangeColor.color = TextColorIfDisabled;
            }

            TextToChangeColor.color = originalTextColor;
        }

        public void Select()
        {
            if (!CanBeSelected)
            {
                return;
            }

            IsActive = true;

            ToScale.localScale = UseScale ? Vector3.one * 1.1f : Vector3.one;

            if (ChangeTextColor && TextToChangeColor != null)
            {
                TextToChangeColor.color = TextHighlightColor;
            }

            if (Effect != null)
            {
                Effect.enabled = true;
            }
        }

        public void Deselect()
        {
            IsActive = false;
            ToScale.localScale = Vector3.one;

            if (ChangeTextColor && TextToChangeColor != null)
            {
                TextToChangeColor.color = originalTextColor;
            }

            if (Effect != null)
            {
                Effect.enabled = false;
            }
            else
            {
                "UiChoice.Deselect: Effect is null, skipping effect deactivation".LogWarning();
            }
        }
    }
}
