namespace Turnroot.Graphics2D
{
    public partial class ScreenKeyboard
    {
        // Mouse interaction callbacks
        private void OnButtonClicked(KeyboardButton button)
        {
            string key = button.GetKeyValue();
            ProcessKey(key);
        }

        private void OnButtonHovered(KeyboardButton button) { }
    }
}
