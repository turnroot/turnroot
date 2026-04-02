using System.Collections;
using UnityEngine;

namespace Turnroot.Graphics2D
{
    public partial class ScreenKeyboard
    {
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
                    if (numbersOnly && maximumNumber > 0)
                    {
                        if (int.TryParse(currentText, out int proposedNumber))
                        {
                            if (proposedNumber > maximumNumber)
                            {
                                // Reject this input - would exceed maximum
                                // add feedback
                                StartCoroutine(
                                    FlashInvalidInput("Number must be <" + maximumNumber)
                                );
                                return;
                            }
                        }
                        else
                        {
                            StartCoroutine(FlashInvalidInput("Invalid number"));
                            return;
                        }
                    }

                    if (currentText.Length == 0)
                    {
                        StartCoroutine(FlashInvalidInput("Cannot be empty"));
                        return;
                    }

                    if (currentText.Length >= 100)
                    {
                        StartCoroutine(FlashInvalidInput("Too long"));
                        return;
                    }

                    OnSubmit?.Invoke(currentText);
                    return;

                default:
                    string textToAdd = key;
                    if (isShiftActive || isCapsLockActive)
                    {
                        textToAdd = textToAdd.ToUpper();
                    }

                    // Validate maximum number in numbers only mode
                    if (numbersOnly && maximumNumber > 0)
                    {
                        string proposedText = currentText + textToAdd;
                        if (int.TryParse(proposedText, out int proposedNumber))
                        {
                            if (proposedNumber > maximumNumber)
                            {
                                // Reject this input - would exceed maximum
                                // add feedback
                                StartCoroutine(FlashInvalidInput("Too large"));
                                return;
                            }
                        }
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

        private IEnumerator FlashInvalidInput(string msg = null)
        {
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.FlashInvalid(msg, this);
                }
            }

            yield return null;
        }

        public void ResetDisplayText()
        {
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
    }
}
