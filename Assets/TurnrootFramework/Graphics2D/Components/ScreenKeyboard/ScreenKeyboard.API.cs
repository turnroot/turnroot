namespace Turnroot.Graphics2D
{
    public partial class ScreenKeyboard
    {
        // Public API
        public string GetCurrentText() => currentText;

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

        public bool IsShiftActive() => isShiftActive;

        public bool IsCapsLockActive() => isCapsLockActive;
    }
}
