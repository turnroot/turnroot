using TMPro;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Graphics2D
{
    public partial class ScreenKeyboard : MonoBehaviour
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
        private bool numbersOnly = false;

        [SerializeField, Tooltip("Maximum allowed number when in numbers only mode (0 = no limit)")]
        private int maximumNumber = 0;

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

        // Number pad layout (numbers only mode) - 3x4 grid
        private static readonly string[][] numberPadLayout = new string[][]
        {
            new string[] { "1", "2", "3" },
            new string[] { "4", "5", "6" },
            new string[] { "7", "8", "9" },
            new string[] { "BACK", "0", "SUBMIT" },
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
            switch (action)
            {
                case InputActionConstants.NavigateUp:
                    ProcessInput(Vector2.up);
                    break;
                case InputActionConstants.NavigateDown:
                    ProcessInput(Vector2.down);
                    break;
                case InputActionConstants.NavigateLeft:
                    ProcessInput(Vector2.left);
                    break;
                case InputActionConstants.NavigateRight:
                    ProcessInput(Vector2.right);
                    break;
                case InputActionConstants.Submit:
                case InputActionConstants.Select:
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
    }
}
