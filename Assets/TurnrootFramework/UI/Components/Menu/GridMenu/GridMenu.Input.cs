namespace Turnroot.UI.Components.GridMenu
{
    public partial class GridMenu
    {
        protected override void HandleKeyboardNavigation()
        {
            base.HandleKeyboardNavigation(); // handles up/down via base

            var left = NavigateLeftAction?.action ?? UIInputActionDefaults.NavigateLeft;
            if (left?.WasPressedThisFrame() == true)
            {
                NavigateLeft();
            }

            var right = NavigateRightAction?.action ?? UIInputActionDefaults.NavigateRight;
            if (right?.WasPressedThisFrame() == true)
            {
                NavigateRight();
            }
        }
    }
}
