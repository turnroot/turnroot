using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Graphics2D
{
    public class ScreenKeyboard : MonoBehaviour
    {
        public System.Action<string> OnSubmit;

        [Header("References")]
        [SerializeField]
        private TextMeshProUGUI displayText;

        [SerializeField]
        private Transform keyboardContainer;

        [SerializeField]
        private Transform bottomRowContainer;

        [SerializeField]
        private Transform submitRowContainer;

        [SerializeField]
        private KeyboardButton buttonPrefab;

        [Header("Layout Settings")]
        [SerializeField]
        private float buttonSpacing = 10f;

        [SerializeField]
        private Vector2 buttonSize = new(80f, 80f);

        [Header("Visual Settings")]
        [SerializeField]
        private Color normalColor = new(0.2f, 0.2f, 0.2f);

        [SerializeField]
        private Color highlightColor = new(0.4f, 0.6f, 1f);

        [SerializeField]
        private Color pressedColor = new(0.3f, 0.5f, 0.9f);

        // Keyboard layout (QWERTY style, similar to Switch)
        private static readonly string[][] keyboardLayout = new string[][]
        {
            new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
            new string[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
            new string[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", "-" },
            new string[] { "z", "x", "c", "v", "b", "n", "m", ".", "!", "?" },
            new string[] { "SHIFT", "SPACE", "BACK" },
            new string[] { "SUBMIT" },
        };

        private KeyboardButton[,] buttons;
        private int currentRow = 0;
        private int currentCol = 0;
        private bool isShiftActive = false;
        private bool isCapsLockActive = false;
        private string currentText = "";

        private int maxCols;
        private int maxRows;

        public void HandleInput(string action)
        {
            Debug.Log($"Received input action: {action}");
            switch (action)
            {
                case "NavigateUp":
                    ProcessInput(Vector2.up);
                    break;
                case "NavigateDown":
                    ProcessInput(Vector2.down);
                    break;
                case "NavigateLeft":
                    ProcessInput(Vector2.left);
                    break;
                case "NavigateRight":
                    ProcessInput(Vector2.right);
                    break;
                case "Select":
                    ProcessSelect();
                    break;
            }
        }

        private void Start()
        {
            BuildKeyboard();
            UpdateDisplay();
            HighlightCurrentButton();
        }

        private void BuildKeyboard()
        {
            maxRows = keyboardLayout.Length;
            maxCols = 0;

            // Find max columns (excluding the bottom two special rows)
            for (int row = 0; row < keyboardLayout.Length - 2; row++)
            {
                if (keyboardLayout[row].Length > maxCols)
                {
                    maxCols = keyboardLayout[row].Length;
                }
            }

            buttons = new KeyboardButton[maxRows, maxCols];

            // Build main keyboard rows (all but the last two special rows)
            for (int row = 0; row < keyboardLayout.Length - 2; row++)
            {
                for (int col = 0; col < keyboardLayout[row].Length; col++)
                {
                    string key = keyboardLayout[row][col];
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

            // Build bottom row separately (SHIFT, SPACE, BACK)
            BuildBottomRow();

            // Build submit row separately
            BuildSubmitRow();
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

        private void BuildSubmitRow()
        {
            int submitRow = keyboardLayout.Length - 1;
            string[] submitRowKeys = keyboardLayout[submitRow];

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

        public void ProcessInput(Vector2 direction)
        {
            if (direction.magnitude < 0.5f)
            {
                return;
            }

            // Unhighlight current button
            if (buttons[currentRow, currentCol] != null)
            {
                buttons[currentRow, currentCol].SetHighlighted(false);
            }

            // Navigate
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                // Horizontal movement
                if (direction.x > 0)
                {
                    MoveRight();
                }
                else
                {
                    MoveLeft();
                }
            }
            else
            {
                // Vertical movement
                if (direction.y > 0)
                {
                    MoveUp();
                }
                else
                {
                    MoveDown();
                }
            }

            HighlightCurrentButton();
        }

        public void ProcessSelect()
        {
            if (buttons[currentRow, currentCol] != null)
            {
                buttons[currentRow, currentCol].Press();
                string key = buttons[currentRow, currentCol].GetKeyValue();
                ProcessKey(key);
            }
        }

        private void ProcessKey(string key)
        {
            switch (key)
            {
                case "BACK":
                    if (currentText.Length > 0)
                    {
                        currentText = currentText.Substring(0, currentText.Length - 1);
                    }

                    break;

                case "SPACE":
                    currentText += " ";
                    break;

                case "SHIFT":
                    // Single tap = shift, double tap = caps lock
                    if (isShiftActive)
                    {
                        isCapsLockActive = !isCapsLockActive;
                        isShiftActive = isCapsLockActive;
                    }
                    else
                    {
                        isShiftActive = true;
                    }
                    UpdateKeyboardCase();
                    return; // Don't reset shift here

                case "SUBMIT":
                    OnSubmit?.Invoke(currentText);
                    return;

                default:
                    string textToAdd = key;
                    if (isShiftActive || isCapsLockActive)
                    {
                        textToAdd = textToAdd.ToUpper();
                    }

                    currentText += textToAdd;

                    // Reset shift if not caps lock
                    if (isShiftActive && !isCapsLockActive)
                    {
                        isShiftActive = false;
                        UpdateKeyboardCase();
                    }
                    break;
            }

            UpdateDisplay();
        }

        private void UpdateKeyboardCase()
        {
            for (int row = 0; row < maxRows; row++)
            {
                for (int col = 0; col < maxCols; col++)
                {
                    if (buttons[row, col] != null)
                    {
                        string key = buttons[row, col].GetKeyValue();
                        if (key.Length == 1 && char.IsLetter(key[0]))
                        {
                            string displayKey =
                                (isShiftActive || isCapsLockActive) ? key.ToUpper() : key.ToLower();
                            buttons[row, col].UpdateDisplay(displayKey);
                        }
                    }
                }
            }

            // Update shift button appearance
            UpdateShiftButtonVisual();
        }

        private void UpdateShiftButtonVisual()
        {
            // Find shift button and update its visual state
            for (int row = 0; row < maxRows; row++)
            {
                for (int col = 0; col < maxCols; col++)
                {
                    if (buttons[row, col] != null && buttons[row, col].GetKeyValue() == "SHIFT")
                    {
                        if (isCapsLockActive)
                        {
                            buttons[row, col].SetCapsLockActive(true);
                        }
                        else if (isShiftActive)
                        {
                            buttons[row, col].SetShiftActive(true);
                        }
                        else
                        {
                            buttons[row, col].SetShiftActive(false);
                        }
                    }
                }
            }
        }

        private void UpdateDisplay()
        {
            if (displayText != null)
            {
                displayText.text = currentText;
            }
        }

        private void HighlightCurrentButton()
        {
            if (buttons[currentRow, currentCol] != null)
            {
                buttons[currentRow, currentCol].SetHighlighted(true);
            }
        }

        private void MoveUp()
        {
            int startRow = currentRow;
            do
            {
                currentRow--;
                if (currentRow < 0)
                {
                    currentRow = maxRows - 1; // Wrap to bottom
                }

                // Find valid button in this row
                if (FindNearestButtonInRow(currentRow))
                {
                    return;
                }
            } while (currentRow != startRow);
        }

        private void MoveDown()
        {
            int startRow = currentRow;
            do
            {
                currentRow++;
                if (currentRow >= maxRows)
                {
                    currentRow = 0; // Wrap to top
                }

                // Find valid button in this row
                if (FindNearestButtonInRow(currentRow))
                {
                    return;
                }
            } while (currentRow != startRow);
        }

        private void MoveLeft()
        {
            int startCol = currentCol;
            do
            {
                currentCol--;
                if (currentCol < 0)
                {
                    currentCol = maxCols - 1; // Wrap to right
                }

                if (buttons[currentRow, currentCol] != null)
                {
                    return;
                }
            } while (currentCol != startCol);
        }

        private void MoveRight()
        {
            int startCol = currentCol;
            do
            {
                currentCol++;
                if (currentCol >= maxCols)
                {
                    currentCol = 0; // Wrap to left
                }

                if (buttons[currentRow, currentCol] != null)
                {
                    return;
                }
            } while (currentCol != startCol);
        }

        private bool FindNearestButtonInRow(int row)
        {
            // First try the same column
            if (buttons[row, currentCol] != null)
            {
                return true;
            }

            // Find nearest valid button in this row
            int leftDist = maxCols;
            int rightDist = maxCols;
            int leftCol = -1;
            int rightCol = -1;

            // Search left
            for (int col = currentCol - 1; col >= 0; col--)
            {
                if (buttons[row, col] != null)
                {
                    leftCol = col;
                    leftDist = currentCol - col;
                    break;
                }
            }

            // Search right
            for (int col = currentCol + 1; col < maxCols; col++)
            {
                if (buttons[row, col] != null)
                {
                    rightCol = col;
                    rightDist = col - currentCol;
                    break;
                }
            }

            // Choose nearest
            if (leftCol >= 0 && leftDist <= rightDist)
            {
                currentCol = leftCol;
                return true;
            }
            else if (rightCol >= 0)
            {
                currentCol = rightCol;
                return true;
            }

            // Try to find any valid button in this row
            for (int col = 0; col < maxCols; col++)
            {
                if (buttons[row, col] != null)
                {
                    currentCol = col;
                    return true;
                }
            }

            return false;
        }

        // Public API
        public string GetCurrentText()
        {
            return currentText;
        }

        public void SetText(string text)
        {
            currentText = text;
            UpdateDisplay();
        }

        public void ClearText()
        {
            currentText = "";
            UpdateDisplay();
        }

        public bool IsShiftActive()
        {
            return isShiftActive;
        }

        public bool IsCapsLockActive()
        {
            return isCapsLockActive;
        }

        // Mouse interaction callbacks
        private void OnButtonClicked(KeyboardButton button)
        {
            string key = button.GetKeyValue();
            ProcessKey(key);
        }

        private void OnButtonHovered(KeyboardButton button)
        {
            // Optional: Could update current selection when hovering
            // For now, we'll keep keyboard navigation separate from mouse hover
        }
    }
}
