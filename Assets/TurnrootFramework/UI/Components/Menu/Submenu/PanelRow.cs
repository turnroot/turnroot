using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components.Menu.Submenu
{
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

        [HideInInspector]
        public bool isSelected;

        [HideInInspector]
        public int rowIndex;

        [HideInInspector]
        public int currentSelectionIndex;
        public TMPro.TextMeshProUGUI labelText;

        [ShowIf("rowType", RowType.Slider)]
        public Slider sliderComponent;

        [ShowIf("rowType", RowType.Toggles)]
        public Toggle[] toggleComponents;

        [ShowIf("rowType", RowType.Button)]
        public Button[] buttonComponents;

        [ShowIf("rowType", RowType.Carousel)]
        public MenuCarousel carouselComponent;

        // Store original colors for each toggle
        private ColorBlock[] originalToggleColors;

        public void InitializeRow()
        {
            if (
                rowType == RowType.Toggles
                && toggleComponents != null
                && toggleComponents.Length > 0
            )
            {
                // Store original color blocks
                originalToggleColors = new ColorBlock[toggleComponents.Length];
                for (int i = 0; i < toggleComponents.Length; i++)
                {
                    if (toggleComponents[i] != null)
                    {
                        originalToggleColors[i] = toggleComponents[i].colors;
                    }
                    else { }
                }
            }

            else if (rowType == RowType.Slider)
            {
                // TODO: Initialize slider component
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

        public void SetFocused(bool focused)
        {
            if (focused)
            {
                SelectRow();
                // Reset selection index when row gains focus
                ResetSelectionIndex();
                UpdateSelectionVisuals();
            }
            else
            {
                DeselectRow();
                ClearSelectionVisuals();
                // Clear selection index when row loses focus
                currentSelectionIndex = 0;
            }
        }

        private void ResetSelectionIndex()
        {
            // Reset to first item when row gains focus
            currentSelectionIndex = 0;

            // Ensure we have valid selection bounds
            switch (rowType)
            {
                case RowType.Toggles:
                    if (toggleComponents != null && toggleComponents.Length > 0)
                    {
                        currentSelectionIndex = Mathf.Clamp(
                            currentSelectionIndex,
                            0,
                            toggleComponents.Length - 1
                        );
                    }
                    break;
                case RowType.Button:
                    if (buttonComponents != null && buttonComponents.Length > 0)
                    {
                        currentSelectionIndex = Mathf.Clamp(
                            currentSelectionIndex,
                            0,
                            buttonComponents.Length - 1
                        );
                    }
                    break;
            }
        }

        private void UpdateSelectionVisuals()
        {
            switch (rowType)
            {
                case RowType.Toggles:
                    UpdateToggleVisuals();
                    break;
                case RowType.Button:
                    UpdateButtonVisuals();
                    break;
            }
        }

        private void ClearSelectionVisuals()
        {
            switch (rowType)
            {
                case RowType.Toggles:
                    ClearToggleVisuals();
                    break;
                case RowType.Button:
                    ClearButtonVisuals();
                    break;
                default:
                    break;
            }
        }

        private void UpdateToggleVisuals()
        {
            // TODO: Somewhere this is doing the wrong thing visually - investigate
            if (toggleComponents == null || toggleComponents.Length == 0)
            {
                return;
            }

            for (int i = 0; i < toggleComponents.Length; i++)
            {
                if (
                    toggleComponents[i] != null
                    && originalToggleColors != null
                    && i < originalToggleColors.Length
                )
                {
                    ColorBlock colors = originalToggleColors[i];

                    if (i == currentSelectionIndex)
                    {
                        // Highlight selected toggle by forcing it to selected state
                        toggleComponents[i].targetGraphic.color = colors.highlightedColor;
                    }
                    else
                    {
                        // Reset to normal color
                        toggleComponents[i].targetGraphic.color = colors.normalColor;
                    }
                }
                else { }
            }
        }

        private void ClearToggleVisuals()
        {
            if (toggleComponents == null || toggleComponents.Length == 0)
            {
                return;
            }

            for (int i = 0; i < toggleComponents.Length; i++)
            {
                if (
                    toggleComponents[i] != null
                    && toggleComponents[i].targetGraphic != null
                    && originalToggleColors != null
                    && i < originalToggleColors.Length
                )
                {
                    // Restore original normal color
                    toggleComponents[i].targetGraphic.color = originalToggleColors[i].normalColor;
                }
                else { }
            }
        }

        private void UpdateButtonVisuals()
        {
            if (buttonComponents == null || buttonComponents.Length == 0)
            {
                return;
            }

            for (int i = 0; i < buttonComponents.Length; i++)
            {
                var textComponent = buttonComponents[i]
                    .GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.fontStyle =
                        i == currentSelectionIndex
                            ? TMPro.FontStyles.Bold
                            : TMPro.FontStyles.Normal;
                }
            }
        }

        private void ClearButtonVisuals()
        {
            if (buttonComponents == null || buttonComponents.Length == 0)
            {
                return;
            }

            for (int i = 0; i < buttonComponents.Length; i++)
            {
                var textComponent = buttonComponents[i]
                    .GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.fontStyle = TMPro.FontStyles.Normal;
                }
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
                    // Ensure currentSelectionIndex is within bounds before navigation
                    currentSelectionIndex = Mathf.Clamp(
                        currentSelectionIndex,
                        0,
                        toggleComponents.Length - 1
                    );

                    // Move direction with wrapping
                    currentSelectionIndex =
                        (
                            currentSelectionIndex
                            + (direction == SubmenuRowInput.Left ? -1 : 1)
                            + toggleComponents.Length
                        ) % toggleComponents.Length;

                    // Update visual feedback
                    UpdateToggleVisuals();
                    break;
                case RowType.Button:
                    if (buttonComponents == null || buttonComponents.Length == 0)
                    {
                        return;
                    }
                    // Ensure currentSelectionIndex is within bounds before navigation
                    currentSelectionIndex = Mathf.Clamp(
                        currentSelectionIndex,
                        0,
                        buttonComponents.Length - 1
                    );

                    // Move direction with wrapping
                    currentSelectionIndex =
                        (
                            currentSelectionIndex
                            + (direction == SubmenuRowInput.Left ? -1 : 1)
                            + buttonComponents.Length
                        ) % buttonComponents.Length;
                    // Update visual feedback
                    UpdateButtonVisuals();
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
                    if (toggleComponents == null)
                    {
                        return;
                    }
                    if (toggleComponents.Length == 0)
                    {
                        return;
                    }

                    // Ensure currentSelectionIndex is within bounds
                    currentSelectionIndex = Mathf.Clamp(
                        currentSelectionIndex,
                        0,
                        toggleComponents.Length - 1
                    );

                    if (toggleComponents[currentSelectionIndex] == null)
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
}
