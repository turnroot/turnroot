namespace Turnroot.UI.Components.GridMenu
{
    public partial class GridMenu
    {
        protected override void HandleKeyboardNavigation()
        {
            base.HandleKeyboardNavigation(); // handles up/down via base

            if (NavigateLeftAction != null && NavigateLeftAction.WasPressedThisFrame())
            {
                NavigateLeft();
            }

            if (NavigateRightAction != null && NavigateRightAction.WasPressedThisFrame())
            {
                NavigateRight();
            }
        }
    }
}
