namespace Turnroot.UI.Components.GridMenu
{
    public partial class GridMenu
    {
        protected override void HandleKeyboardNavigation()
        {
            base.HandleKeyboardNavigation(); // handles up/down via base

            if (UIInputActionDefaults.NavigateLeft?.WasPressedThisFrame() == true)
            {
                NavigateLeft();
            }

            if (UIInputActionDefaults.NavigateRight?.WasPressedThisFrame() == true)
            {
                NavigateRight();
            }
        }
    }
}
