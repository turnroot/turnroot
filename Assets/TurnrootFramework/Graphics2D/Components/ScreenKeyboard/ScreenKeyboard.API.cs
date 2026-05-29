namespace Turnroot.Graphics2D
{
    public partial class ScreenKeyboard
    {
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
    }
}
