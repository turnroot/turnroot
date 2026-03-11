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

        private void OnButtonHovered(KeyboardButton button)
        {
            // Optional: Could update current selection when hovering
            // For now, we'll keep keyboard navigation separate from mouse hover
        }
    }
}
