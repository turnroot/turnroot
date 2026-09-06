using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components.Menu.Submenu
{
    /// <summary>
    /// Represents a single row in a panel-based menu with support for sliders, toggles, buttons, and carousels.
    /// </summary>
    public class PanelRow : MonoBehaviour
    {
        /// <summary>
        /// Defines the type of interactive component displayed in this row.
        /// </summary>
        public enum RowType
        {
            Slider,
            Toggles,
            UiChoice,
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

        [ShowIf("rowType", RowType.UiChoice)]
        public UiChoice[] buttonComponents;

        [ShowIf("rowType", RowType.Carousel)]
        public MenuCarousel carouselComponent;

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
                RowType.UiChoice when HasElements(buttonComponents) => buttonComponents.Length - 1,
                RowType.Carousel when carouselComponent != null => carouselComponent.Options.Count
                    - 1,
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
                case RowType.UiChoice:
                    UpdateButtonVisuals();
                    break;
                case RowType.Carousel:
                    UpdateCarouselVisuals();
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
                case RowType.UiChoice:
                    ClearButtonVisuals();
                    break;
                case RowType.Carousel:
                    break;
            }
        }

        private void UpdateToggleVisuals()
        {
            if (!HasElements(toggleComponents))
            {
                return;
            }

            for (int i = 0; i < toggleComponents.Length; i++)
            {
                toggleComponents[i]?.SetHighlighted(i == currentSelectionIndex);
            }
        }

        private void ClearToggleVisuals()
        {
            if (!HasElements(toggleComponents))
            {
                return;
            }

            for (int i = 0; i < toggleComponents.Length; i++)
            {
                toggleComponents[i]?.SetHighlighted(false);
            }
        }

        private void UpdateButtonVisuals()
        {
            if (!HasElements(buttonComponents))
            {
                return;
            }

            for (int i = 0; i < buttonComponents.Length; i++)
            {
                var choice = buttonComponents[i];
                if (choice == null)
                {
                    continue;
                }

                if (i == currentSelectionIndex)
                {
                    choice.Select();
                }
                else
                {
                    choice.Deselect();
                }

                var textComponent = choice.GetComponentInChildren<TMPro.TextMeshProUGUI>();
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
            {
                return;
            }

            for (int i = 0; i < buttonComponents.Length; i++)
            {
                buttonComponents[i]?.Deselect();

                var textComponent = buttonComponents[i]
                    ?.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.fontStyle = TMPro.FontStyles.Normal;
                }
            }
        }

        private void UpdateCarouselVisuals()
        {
            carouselComponent?.UpdateDisplay();
        }

        public void SelectRow()
        {
            if (labelText != null)
            {
                labelText.fontStyle = TMPro.FontStyles.Bold;
            }

            if (rowType == RowType.Carousel)
            {
                UpdateCarouselVisuals();
            }

            isSelected = true;
        }

        public void DeselectRow()
        {
            if (labelText != null)
            {
                labelText.fontStyle = TMPro.FontStyles.Normal;
            }

            if (rowType == RowType.Carousel)
            {
                UpdateCarouselVisuals();
            }

            isSelected = false;
        }

        public bool HandleInput(SubmenuRowInput input)
        {
            return isSelected && input switch
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
                {
                    // quality slider uses 4 discrete steps (0,1/3,2/3,1),
                    // other sliders use y 0.1 increment
                    float step;
                    string normalized = string.Empty;
                    string[] candidates = new string[]
                    {
                        sliderComponent?.gameObject.name,
                        gameObject.name,
                        labelText?.text,
                    };

                    foreach (var c in candidates)
                    {
                        if (string.IsNullOrEmpty(c))
                        {
                            continue;
                        }

                        var lower = c.ToLower();
                        var sb = new System.Text.StringBuilder();
                        foreach (var ch in lower)
                        {
                            if (char.IsLetterOrDigit(ch))
                            {
                                sb.Append(ch);
                            }
                        }
                        normalized = sb.ToString();
                        if (!string.IsNullOrEmpty(normalized))
                        {
                            break;
                        }
                    }

                    if (normalized == "quality")
                    {
                        // Move exactly one tenth per input; with slider max=0.3 this yields four steps
                        step = 0.1f * delta;
                    }
                    else
                    {
                        step = 0.1f * delta; // legacy behavior for other sliders
                    }

                    AdjustSlider(step);
                    return true;
                }

                case RowType.Toggles:
                    return NavigateElements(toggleComponents, delta, UpdateToggleVisuals);

                case RowType.UiChoice:
                    return NavigateElements(buttonComponents, delta, UpdateButtonVisuals);

                case RowType.Carousel:
                    return HandleCarouselNavigation(delta);

                default:
                    return false;
            }
        }

        private bool NavigateElements<T>(T[] elements, int delta, System.Action updateVisuals)
        {
            if (!HasElements(elements))
            {
                return false;
            }

            var oldIndex = currentSelectionIndex;
            currentSelectionIndex = Mathf.Clamp(currentSelectionIndex, 0, elements.Length - 1);
            currentSelectionIndex =
                (currentSelectionIndex + delta + elements.Length) % elements.Length;
            updateVisuals?.Invoke();
            return true;
        }

        private bool HandleCarouselNavigation(int delta)
        {
            if (carouselComponent == null || carouselComponent.Options.Count == 0)
            {
                return false;
            }

            if (delta > 0)
            {
                carouselComponent.IncrementIndex();
            }
            else if (delta < 0)
            {
                carouselComponent.DecrementIndex();
            }

            currentSelectionIndex = carouselComponent.CurrentIndex;
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
                    {
                        return false;
                    }

                    currentSelectionIndex = Mathf.Clamp(
                        currentSelectionIndex,
                        0,
                        toggleComponents.Length - 1
                    );
                    toggleComponents[currentSelectionIndex]?.Toggle();
                    return true;

                case RowType.UiChoice:
                    if (!HasElements(buttonComponents))
                    {
                        return false;
                    }

                    buttonComponents[currentSelectionIndex]?.Select();
                    return true;

                case RowType.Carousel:
                    return true;

                default:
                    return false;
            }
        }

        private void AdjustSlider(float delta)
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

        private static bool HasElements<T>(T[] array) => array != null && array.Length > 0;
    }
}
