using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Graphics2D
{
    public partial class ScreenKeyboard
    {
        private void BuildKeyboard()
        {
            // Select layout based on numbersOnly mode
            string[][] activeLayout = numbersOnly ? numberPadLayout : keyboardLayout;

            maxRows = activeLayout.Length;
            maxCols = 0;

            // Find max columns (excluding the bottom special row(s))
            int specialRowsCount = numbersOnly ? 1 : 2; // Number pad has 1 special row, full keyboard has 2
            for (int row = 0; row < activeLayout.Length - specialRowsCount; row++)
            {
                if (activeLayout[row].Length > maxCols)
                {
                    maxCols = activeLayout[row].Length;
                }
            }

            buttons = new KeyboardButton[maxRows, maxCols];

            // Build main keyboard rows (all but the bottom special row(s))
            for (int row = 0; row < activeLayout.Length - specialRowsCount; row++)
            {
                for (int col = 0; col < activeLayout[row].Length; col++)
                {
                    string key = activeLayout[row][col];
                    KeyboardButton button = Instantiate(buttonPrefab, keyboardContainer);

                    // Standard sizing for main keyboard
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.sizeDelta = buttonSize;

                    // Initialize button with mouse callbacks
                    button.Initialize(
                        key,
                        normalColor,
                        highlightColor,
                        pressedColor,
                        OnButtonClicked,
                        OnButtonHovered
                    );
                    buttons[row, col] = button;
                }
            }

            // Build bottom row(s) separately
            if (numbersOnly)
            {
                BuildNumberPadBottomRow();
            }
            else
            {
                BuildBottomRow();
                BuildSubmitRow();
            }
        }

        private void BuildBottomRow()
        {
            int bottomRow = keyboardLayout.Length - 2; // Second-to-last row (SHIFT, SPACE, BACK)
            string[] bottomRowKeys = keyboardLayout[bottomRow];

            // SHIFT button (left side)
            string shiftKey = bottomRowKeys[0]; // "SHIFT"
            KeyboardButton shiftButton = Instantiate(buttonPrefab, bottomRowContainer);
            RectTransform shiftRect = shiftButton.GetComponent<RectTransform>();
            shiftRect.sizeDelta = buttonSize;

            // Add LayoutElement to control sizing in HorizontalLayoutGroup
            LayoutElement shiftLayout = shiftButton.gameObject.AddComponent<LayoutElement>();
            shiftLayout.preferredWidth = buttonSize.x;
            shiftLayout.preferredHeight = buttonSize.y;
            shiftLayout.flexibleWidth = 0;

            shiftButton.Initialize(
                shiftKey,
                normalColor,
                highlightColor,
                pressedColor,
                OnButtonClicked,
                OnButtonHovered
            );
            buttons[bottomRow, 0] = shiftButton;

            // SPACE button (center, expands to fill)
            string spaceKey = bottomRowKeys[1]; // "SPACE"
            KeyboardButton spaceButton = Instantiate(buttonPrefab, bottomRowContainer);
            RectTransform spaceRect = spaceButton.GetComponent<RectTransform>();
            spaceRect.sizeDelta = new Vector2(buttonSize.x, buttonSize.y);

            // Add LayoutElement with flexible width to expand
            LayoutElement spaceLayout = spaceButton.gameObject.AddComponent<LayoutElement>();
            spaceLayout.preferredHeight = buttonSize.y;
            spaceLayout.flexibleWidth = 1; // This makes it expand to fill available space

            spaceButton.Initialize(
                spaceKey,
                normalColor,
                highlightColor,
                pressedColor,
                OnButtonClicked,
                OnButtonHovered
            );
            buttons[bottomRow, 1] = spaceButton;

            // BACK button (right side)
            string backKey = bottomRowKeys[2]; // "BACK"
            KeyboardButton backButton = Instantiate(buttonPrefab, bottomRowContainer);
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.sizeDelta = buttonSize;

            // Add LayoutElement to control sizing in HorizontalLayoutGroup
            LayoutElement backLayout = backButton.gameObject.AddComponent<LayoutElement>();
            backLayout.preferredWidth = buttonSize.x;
            backLayout.preferredHeight = buttonSize.y;
            backLayout.flexibleWidth = 0;

            backButton.Initialize(
                backKey,
                normalColor,
                highlightColor,
                pressedColor,
                OnButtonClicked,
                OnButtonHovered
            );
            buttons[bottomRow, 2] = backButton;
        }

        private void BuildNumberPadBottomRow()
        {
            int bottomRow = numberPadLayout.Length - 1; // Last row (BACK, 0, SUBMIT)
            string[] bottomRowKeys = numberPadLayout[bottomRow];

            // BACK button (left)
            string backKey = bottomRowKeys[0]; // "BACK"
            KeyboardButton backButton = Instantiate(buttonPrefab, bottomRowContainer);
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.sizeDelta = buttonSize;

            LayoutElement backLayout = backButton.gameObject.AddComponent<LayoutElement>();
            backLayout.preferredWidth = buttonSize.x;
            backLayout.preferredHeight = buttonSize.y;
            backLayout.flexibleWidth = 0;

            backButton.Initialize(
                backKey,
                normalColor,
                highlightColor,
                pressedColor,
                OnButtonClicked,
                OnButtonHovered
            );
            buttons[bottomRow, 0] = backButton;

            // 0 button (center)
            string zeroKey = bottomRowKeys[1]; // "0"
            KeyboardButton zeroButton = Instantiate(buttonPrefab, bottomRowContainer);
            RectTransform zeroRect = zeroButton.GetComponent<RectTransform>();
            zeroRect.sizeDelta = buttonSize;

            LayoutElement zeroLayout = zeroButton.gameObject.AddComponent<LayoutElement>();
            zeroLayout.preferredWidth = buttonSize.x;
            zeroLayout.preferredHeight = buttonSize.y;
            zeroLayout.flexibleWidth = 0;

            zeroButton.Initialize(
                zeroKey,
                normalColor,
                highlightColor,
                pressedColor,
                OnButtonClicked,
                OnButtonHovered
            );
            buttons[bottomRow, 1] = zeroButton;

            // SUBMIT button (right)
            string submitKey = bottomRowKeys[2]; // "SUBMIT"
            KeyboardButton submitButton = Instantiate(buttonPrefab, bottomRowContainer);
            RectTransform submitRect = submitButton.GetComponent<RectTransform>();
            submitRect.sizeDelta = buttonSize;

            LayoutElement submitLayout = submitButton.gameObject.AddComponent<LayoutElement>();
            submitLayout.preferredWidth = buttonSize.x;
            submitLayout.preferredHeight = buttonSize.y;
            submitLayout.flexibleWidth = 0;

            submitButton.Initialize(
                submitKey,
                normalColor,
                highlightColor,
                pressedColor,
                OnButtonClicked,
                OnButtonHovered
            );
            buttons[bottomRow, 2] = submitButton;
        }

        private void BuildSubmitRow()
        {
            string[][] activeLayout = numbersOnly ? numberPadLayout : keyboardLayout;
            int submitRow = activeLayout.Length - 1;
            string[] submitRowKeys = activeLayout[submitRow];

            // SUBMIT button (full width)
            string submitKey = submitRowKeys[0]; // "SUBMIT"
            KeyboardButton submitButton = Instantiate(buttonPrefab, submitRowContainer);
            RectTransform submitRect = submitButton.GetComponent<RectTransform>();
            submitRect.sizeDelta = buttonSize;

            // Add LayoutElement with flexible width to expand to full width
            LayoutElement submitLayout = submitButton.gameObject.AddComponent<LayoutElement>();
            submitLayout.preferredHeight = buttonSize.y;
            submitLayout.flexibleWidth = 1; // This makes it expand to fill available space

            submitButton.Initialize(
                submitKey,
                normalColor,
                highlightColor,
                pressedColor,
                OnButtonClicked,
                OnButtonHovered
            );
            buttons[submitRow, 0] = submitButton;
        }
    }
}
