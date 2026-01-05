using NaughtyAttributes;
using Turnroot.UI.Components.Menu.Submenu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Turnroot.UI.Components.Menu.Submenu
{
    public enum SubmenuRowInput
    {
        Left,
        Right,
        Select,
    }

    public class PanelRow : MonoBehaviour
    {
        public enum RowType
        {
            Slider,
            Toggles,
            Button,
            Carousel,
        }

        public RowType rowType;

        public bool isSelected;

        public int rowIndex;

        [HideIf("rowType", RowType.Slider)]
        public int currentSelectionIndex;
        public string label;
        public TMPro.TextMeshProUGUI labelText;

        [ShowIf("rowType", RowType.Slider)]
        public Slider sliderComponent;

        [ShowIf("rowType", RowType.Toggles)]
        public Toggle[] toggleComponents;

        [ShowIf("rowType", RowType.Button)]
        public Button[] buttonComponents;

        [ShowIf("rowType", RowType.Carousel)]
        public MenuCarousel carouselComponent;

        public void InitializeRow()
        {
            if (rowType == RowType.Slider)
            {
                // TODO: Initialize slider component
            }
            else if (rowType == RowType.Toggles)
            {
                // TODO: Initialize toggles component
            }
            else if (rowType == RowType.Button)
            {
                // TODO: Initialize button component
            }
            else if (rowType == RowType.Carousel)
            {
                // TODO: Initialize carousel component
            }
        }

        public void SelectRow()
        {
            labelText.fontStyle = TMPro.FontStyles.Bold;
            isSelected = true;
        }

        public void DeselectRow()
        {
            labelText.fontStyle = TMPro.FontStyles.Normal;
            isSelected = false;
        }

        public bool HandleInput(SubmenuRowInput input)
        {
            if (!isSelected)
            {
                return false;
            }
            switch (input)
            {
                case SubmenuRowInput.Left:
                    HandleInputLeftRight(SubmenuRowInput.Left);
                    return true;
                case SubmenuRowInput.Right:
                    HandleInputLeftRight(SubmenuRowInput.Right);
                    return true;
                case SubmenuRowInput.Select:
                    HandleInputSelect();
                    return true;
                default:
                    return false;
            }
        }

        public void HandleInputLeftRight(SubmenuRowInput direction)
        {
            switch (rowType)
            {
                case RowType.Slider:
                    AdjustSlider(.1f * (direction == SubmenuRowInput.Left ? -1 : 1));
                    break;
                case RowType.Toggles:
                    if (toggleComponents == null || toggleComponents.Length == 0)
                    {
                        return;
                    }
                    // Deselect current
                    toggleComponents[currentSelectionIndex].isOn = false;
                    // Move direction with wrapping
                    currentSelectionIndex =
                        (
                            currentSelectionIndex
                            + (direction == SubmenuRowInput.Left ? -1 : 1)
                            + toggleComponents.Length
                        ) % toggleComponents.Length;
                    // Select new
                    toggleComponents[currentSelectionIndex].isOn = true;
                    break;
                case RowType.Button:
                    if (buttonComponents == null || buttonComponents.Length == 0)
                    {
                        return;
                    }
                    // Deselect current
                    buttonComponents[currentSelectionIndex].OnDeselect(null);
                    // Move direction with wrapping
                    currentSelectionIndex =
                        (
                            currentSelectionIndex
                            + (direction == SubmenuRowInput.Left ? -1 : 1)
                            + buttonComponents.Length
                        ) % buttonComponents.Length;
                    // Select new
                    buttonComponents[currentSelectionIndex].Select();
                    break;
                // TODO: Handle Carousel
            }
        }

        public void HandleInputSelect()
        {
            switch (rowType)
            {
                case RowType.Slider:
                    // No action on select for slider
                    break;
                case RowType.Toggles:
                    if (toggleComponents == null || toggleComponents.Length == 0)
                    {
                        return;
                    }
                    // Toggle the current selection
                    toggleComponents[currentSelectionIndex].isOn = !toggleComponents[
                        currentSelectionIndex
                    ].isOn;
                    break;
                case RowType.Button:
                    if (buttonComponents == null || buttonComponents.Length == 0)
                    {
                        return;
                    }
                    // Invoke the button's onClick event
                    buttonComponents[currentSelectionIndex].onClick.Invoke();
                    break;
                    // TODO: Handle Carousel
            }
        }

        public void AdjustSlider(float delta)
        {
            if (rowType != RowType.Slider || sliderComponent == null)
            {
                return;
            }
            sliderComponent.value = Mathf.Clamp(
                sliderComponent.value + delta,
                sliderComponent.minValue,
                sliderComponent.maxValue
            );
        }
    }

    public class PanelRows : MonoBehaviour
    {
        public PanelRow[] panelRows;

        public void Awake()
        {
            var index = -1;
            foreach (var row in panelRows)
            {
                row.InitializeRow();
                row.rowIndex = index;
                index++;
            }
        }

        public InputAction navigateUpAction;
        public InputAction navigateDownAction;
        public InputAction navigateLeftAction;
        public InputAction navigateRightAction;
        public InputAction selectAction;
    }
}
