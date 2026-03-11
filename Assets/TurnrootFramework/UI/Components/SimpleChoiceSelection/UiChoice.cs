using Coffee.UIEffects;
using NaughtyAttributes;
using TMPro;
using Turnroot.Utilities;
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
        public static InputAction SelectAction { get; private set; }
        public static InputAction BackAction { get; private set; }
        public static InputAction NavigateUpAction { get; private set; }
        public static InputAction NavigateDownAction { get; private set; }
        public static InputAction NavigateLeftAction { get; private set; }
        public static InputAction NavigateRightAction { get; private set; }

        static UiChoice()
        {
            SelectAction = new InputAction(
                "UI_Select",
                InputActionType.Button,
                "<Gamepad>/buttonEast",
                interactions: "press"
            );
            SelectAction.AddBinding("<Keyboard>/enter");
            SelectAction.AddBinding("<Keyboard>/space");

            BackAction = new InputAction(
                "UI_Back",
                InputActionType.Button,
                "<Gamepad>/buttonSouth",
                interactions: "press"
            );
            BackAction.AddBinding("<Keyboard>/backspace");
            BackAction.AddBinding("<Keyboard>/escape");
            BackAction.AddBinding("<Keyboard>/delete");

            NavigateUpAction = new InputAction(
                "UI_NavigateUp",
                InputActionType.Button,
                interactions: "press"
            );
            NavigateUpAction.AddBinding("<Gamepad>/dpad/up");
            NavigateUpAction.AddBinding("<Gamepad>/leftStick/up");
            NavigateUpAction.AddBinding("<Keyboard>/w");
            NavigateUpAction.AddBinding("<Keyboard>/upArrow");

            NavigateDownAction = new InputAction(
                "UI_NavigateDown",
                InputActionType.Button,
                interactions: "press"
            );
            NavigateDownAction.AddBinding("<Gamepad>/dpad/down");
            NavigateDownAction.AddBinding("<Gamepad>/leftStick/down");
            NavigateDownAction.AddBinding("<Keyboard>/s");
            NavigateDownAction.AddBinding("<Keyboard>/downArrow");

            NavigateLeftAction = new InputAction(
                "UI_NavigateLeft",
                InputActionType.Button,
                interactions: "press"
            );
            NavigateLeftAction.AddBinding("<Gamepad>/dpad/left");
            NavigateLeftAction.AddBinding("<Gamepad>/leftStick/left");
            NavigateLeftAction.AddBinding("<Keyboard>/a");
            NavigateLeftAction.AddBinding("<Keyboard>/leftArrow");

            NavigateRightAction = new InputAction(
                "UI_NavigateRight",
                InputActionType.Button,
                interactions: "press"
            );
            NavigateRightAction.AddBinding("<Gamepad>/dpad/right");
            NavigateRightAction.AddBinding("<Gamepad>/leftStick/right");
            NavigateRightAction.AddBinding("<Keyboard>/d");
            NavigateRightAction.AddBinding("<Keyboard>/rightArrow");
        }

        private static int _actionEnableCount = 0;

        public static void EnableActions()
        {
            if (_actionEnableCount++ == 0)
            {
                SelectAction.Enable();
                BackAction.Enable();
                NavigateUpAction.Enable();
                NavigateDownAction.Enable();
                NavigateLeftAction.Enable();
                NavigateRightAction.Enable();
            }
        }

        public static void DisableActions()
        {
            if (--_actionEnableCount <= 0)
            {
                SelectAction.Disable();
                BackAction.Disable();
                NavigateUpAction.Disable();
                NavigateDownAction.Disable();
                NavigateLeftAction.Disable();
                NavigateRightAction.Disable();
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
