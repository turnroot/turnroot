using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Turnroot.Graphics2D
{
    public class KeyboardButton
        : MonoBehaviour,
            IPointerClickHandler,
            IPointerEnterHandler,
            IPointerExitHandler
    {
        [SerializeField]
        private Image background;

        [SerializeField]
        private TextMeshProUGUI label;

        private string keyValue;
        private Color normalColor;
        private Color highlightColor;
        private Color pressedColor;

        private Color invalidColor = new(1f, 0.5f, 0.5f);
        private bool isHighlighted = false;
        private bool isMouseHover = false;

        private Action<KeyboardButton> onClickCallback;
        private Action<KeyboardButton> onHoverCallback;

        public void Initialize(
            string key,
            Color normal,
            Color highlight,
            Color pressed,
            Action<KeyboardButton> onClick = null,
            Action<KeyboardButton> onHover = null
        )
        {
            keyValue = key;
            normalColor = normal;
            highlightColor = highlight;
            pressedColor = pressed;
            onClickCallback = onClick;
            onHoverCallback = onHover;

            // Set up the button
            if (label != null)
            {
                label.text = key;
                label.fontSize = key.Length > 1 ? 24f : 36f; // Smaller font for special keys
                label.raycastTarget = false; // Ensure label doesn't block input
            }

            if (background != null)
            {
                background.color = normalColor;
                background.raycastTarget = true; // Ensure background can receive mouse input
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (background != null)
            {
                // Highlight takes priority, then hover, then normal
                if (isHighlighted)
                {
                    background.color = highlightColor;
                }
                else if (isMouseHover)
                {
                    background.color = Color.Lerp(normalColor, highlightColor, 0.4f);
                }
                else
                {
                    background.color = normalColor;
                }
            }
        }

        public void Press()
        {
            if (background != null)
            {
                // Flash pressed color
                background.color = pressedColor;
                Invoke(nameof(ResetColor), 0.1f);
            }
        }

        private void ResetColor()
        {
            if (background != null)
            {
                background.color = isHighlighted ? highlightColor : normalColor;
            }
        }

        public void FlashInvalid(string msg = null, ScreenKeyboard keyboard = null)
        {
            if (keyboard != null)
            {
                keyboard.displayText.text = msg ?? "Invalid Input!";
            }
            if (background != null)
            {
                background.color = invalidColor;
                Invoke(nameof(ResetColor), 0.25f);
                keyboard?.Invoke(nameof(UpdateDisplay), 0.4f);
            }
        }

        public string GetKeyValue() => keyValue;

        public void UpdateDisplay(string displayText)
        {
            if (label != null)
            {
                label.text = displayText;
            }
        }

        public void SetShiftActive(bool active)
        {
            if (label != null && keyValue == "SHIFT")
            {
                // Visual indicator for shift state
                label.color = active ? new Color(0.5f, 1f, 0.5f) : Color.white;
            }
        }

        public void SetCapsLockActive(bool active)
        {
            if (label != null && keyValue == "SHIFT")
            {
                // Visual indicator for caps lock state (stronger color)
                label.color = active ? Color.yellow : Color.white;
            }
        }

        // Mouse interaction handlers
        public void OnPointerClick(PointerEventData eventData)
        {
            Press();
            onClickCallback?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isMouseHover = true;
            UpdateVisualState();
            onHoverCallback?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isMouseHover = false;
            UpdateVisualState();
        }
    }
}
