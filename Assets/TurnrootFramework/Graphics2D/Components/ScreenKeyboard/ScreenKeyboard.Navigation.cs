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
            buttons[currentRow, currentCol]?.SetHighlighted(false);

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

        public void ProcessSelect(int min = 0, int max = 0)
        {
            if (buttons[currentRow, currentCol] != null)
            {
                buttons[currentRow, currentCol].Press();
                string key = buttons[currentRow, currentCol].GetKeyValue();
                ProcessKey(key, min, max);
            }
        }

        private void ProcessKey(string key, int min = 0, int max = 0)
        {
            switch (key)
            {
                case "BACK":
                    RemoveLastCharacter();
                    break;
                case "SPACE":
                    currentText += " ";
                    break;
                case "SHIFT":
                    ToggleShift();
                    return;
                case "SUBMIT":
                    SubmitCurrentText(min, max);
                    return;
                default:
                    AppendKey(key);
                    break;
            }
            UpdateDisplay();
        }

        private void RemoveLastCharacter()
        {
            if (currentText.Length > 0)
            {
                currentText = currentText.Substring(0, currentText.Length - 1);
            }
        }

        private void ToggleShift()
        {
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
        }

        private void SubmitCurrentText(int min, int max)
        {
            if (!ValidateSubmissionNumber())
            {
                return;
            }

            if (currentText.Length == 0)
            {
                StartCoroutine(FlashInvalidInput("Cannot be empty"));
                return;
            }

            if (currentText.Length >= max)
            {
                StartCoroutine(FlashInvalidInput("Too long"));
                return;
            }

            if (currentText.Length < min)
            {
                StartCoroutine(FlashInvalidInput("Too short"));
                return;
            }

            OnSubmit?.Invoke(currentText);
        }

        private bool ValidateSubmissionNumber()
        {
            if (!numbersOnly || maximumNumber <= 0)
            {
                return true;
            }

            if (!int.TryParse(currentText, out int proposedNumber))
            {
                StartCoroutine(FlashInvalidInput("Invalid number"));
                return false;
            }

            if (proposedNumber > maximumNumber)
            {
                StartCoroutine(FlashInvalidInput("Number must be <" + maximumNumber));
                return false;
            }

            return true;
        }

        private void AppendKey(string key)
        {
            string textToAdd = isShiftActive || isCapsLockActive ? key.ToUpper() : key;
            if (
                numbersOnly
                && maximumNumber > 0
                && int.TryParse(currentText + textToAdd, out int proposedNumber)
                && proposedNumber > maximumNumber
            )
            {
                StartCoroutine(FlashInvalidInput("Too large"));
                return;
            }

            currentText += textToAdd;
            if (isShiftActive && !isCapsLockActive)
            {
                isShiftActive = false;
                UpdateKeyboardCase();
            }
        }

        private IEnumerator FlashInvalidInput(string msg = null)
        {
            foreach (var button in buttons)
            {
                button?.FlashInvalid(msg, this);
            }

            yield return null;
        }

        public void ResetDisplayText() => UpdateDisplay();

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
            UpdateShiftButtonVisual();
            if (displayText != null)
            {
                displayText.text = currentText;
            }
        }

        private void HighlightCurrentButton()
        {
            buttons[currentRow, currentCol]?.SetHighlighted(true);
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
