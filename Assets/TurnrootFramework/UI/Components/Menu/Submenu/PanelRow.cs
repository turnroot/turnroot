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
        public SimpleToggle[] toggleComponents;

        [ShowIf("rowType", RowType.Button)]
        public Button[] buttonComponents;

        [ShowIf("rowType", RowType.Carousel)]
        public MenuCarousel carouselComponent;

        public void InitializeRow()
        {
            // TODO: Initialize slider component
            // TODO: Initialize button component
            // TODO: Initialize carousel component
        }

        public void SetFocused(bool focused)
        {
            if (focused)
            {
                SelectRow();
                ResetSelectionIndex();
                UpdateSelectionVisuals();
            }
            else
            {
                DeselectRow();
                ClearSelectionVisuals();
                currentSelectionIndex = 0;
            }
        }

        private void ResetSelectionIndex()
        {
            currentSelectionIndex = 0;
            currentSelectionIndex = Mathf.Clamp(currentSelectionIndex, 0, GetMaxSelectionIndex());
        }

        private int GetMaxSelectionIndex()
        {
            return rowType switch
            {
                RowType.Toggles when HasElements(toggleComponents) => toggleComponents.Length - 1,
                RowType.Button when HasElements(buttonComponents) => buttonComponents.Length - 1,
                _ => 0,
            };
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
            }
        }

        private void UpdateToggleVisuals()
        {
            if (!HasElements(toggleComponents))
                return;

            for (int i = 0; i < toggleComponents.Length; i++)
            {
                if (toggleComponents[i] != null)
                {
                    toggleComponents[i].SetHighlighted(i == currentSelectionIndex);
                }
            }
        }

        private void ClearToggleVisuals()
        {
            if (!HasElements(toggleComponents))
                return;

            for (int i = 0; i < toggleComponents.Length; i++)
            {
                toggleComponents[i]?.SetHighlighted(false);
            }
        }

        private void UpdateButtonVisuals()
        {
            if (!HasElements(buttonComponents))
                return;

            for (int i = 0; i < buttonComponents.Length; i++)
            {
                var textComponent = buttonComponents[i]
                    ?.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.fontStyle =
                        (i == currentSelectionIndex)
                            ? TMPro.FontStyles.Bold
                            : TMPro.FontStyles.Normal;
                }
            }
        }

        private void ClearButtonVisuals()
        {
            if (!HasElements(buttonComponents))
                return;

            for (int i = 0; i < buttonComponents.Length; i++)
            {
                var textComponent = buttonComponents[i]
                    ?.GetComponentInChildren<TMPro.TextMeshProUGUI>();
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
                return false;

            return input switch
            {
                SubmenuRowInput.Left => HandleInputLeftRight(SubmenuRowInput.Left),
                SubmenuRowInput.Right => HandleInputLeftRight(SubmenuRowInput.Right),
                SubmenuRowInput.Select => HandleInputSelect(),
                _ => false,
            };
        }

        private bool HandleInputLeftRight(SubmenuRowInput direction)
        {
            int delta = direction == SubmenuRowInput.Left ? -1 : 1;

            switch (rowType)
            {
                case RowType.Slider:
                    AdjustSlider(0.1f * delta);
                    return true;

                case RowType.Toggles:
                    return NavigateElements(toggleComponents, delta, UpdateToggleVisuals);

                case RowType.Button:
                    return NavigateElements(buttonComponents, delta, UpdateButtonVisuals);

                // TODO: Handle Carousel
                default:
                    return false;
            }
        }

        private bool NavigateElements<T>(T[] elements, int delta, System.Action updateVisuals)
        {
            if (!HasElements(elements))
                return false;

            currentSelectionIndex = Mathf.Clamp(currentSelectionIndex, 0, elements.Length - 1);
            currentSelectionIndex =
                (currentSelectionIndex + delta + elements.Length) % elements.Length;
            updateVisuals?.Invoke();
            return true;
        }

        private bool HandleInputSelect()
        {
            switch (rowType)
            {
                case RowType.Slider:
                    return false;

                case RowType.Toggles:
                    if (!HasElements(toggleComponents))
                        return false;

                    currentSelectionIndex = Mathf.Clamp(
                        currentSelectionIndex,
                        0,
                        toggleComponents.Length - 1
                    );
                    toggleComponents[currentSelectionIndex]?.Toggle();
                    return true;

                case RowType.Button:
                    if (!HasElements(buttonComponents))
                        return false;

                    buttonComponents[currentSelectionIndex]?.onClick.Invoke();
                    return true;

                // TODO: Handle Carousel
                default:
                    return false;
            }
        }

        private void AdjustSlider(float delta)
        {
            if (rowType != RowType.Slider || sliderComponent == null)
                return;

            sliderComponent.value = Mathf.Clamp(
                sliderComponent.value + delta,
                sliderComponent.minValue,
                sliderComponent.maxValue
            );
        }

        private static bool HasElements<T>(T[] array) => array != null && array.Length > 0;
    }
}
