using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public static class PageHandlerExtensions
    {
        public static void BeginPageSequence(this IPageHandler handler)
        {
            if (handler == null)
            {
                return;
            }

            if (handler.PageCount <= 0)
            {
                handler.OnPagesCompleted();
                return;
            }

            handler.CurrentPageIndex = 0;
            handler.ShowPage(handler.CurrentPageIndex);
        }

        public static void HandlePageInput(this IPageHandler handler, string action)
        {
            if (handler == null)
            {
                return;
            }

            if (
                action
                is InputActionConstants.Select
                    or InputActionConstants.Start
                    or InputActionConstants.Submit
                    or InputActionConstants.Confirm
            )
            {
                handler.AdvancePage();
            }
            else if (action is InputActionConstants.Back or InputActionConstants.Cancel)
            {
                handler.GoBackPage();
            }
        }

        public static void AdvancePage(this IPageHandler handler)
        {
            if (handler == null || handler.CurrentPageIndex < 0 || handler.PageCount <= 0)
            {
                return;
            }

            handler.GetPageFade(handler.CurrentPageIndex)?.Hide();
            handler.CurrentPageIndex++;

            if (handler.CurrentPageIndex >= handler.PageCount)
            {
                handler.OnPagesCompleted();
                return;
            }

            handler.ShowPage(handler.CurrentPageIndex);
        }

        public static void GoBackPage(this IPageHandler handler)
        {
            if (handler == null || handler.CurrentPageIndex <= 0 || handler.PageCount <= 0)
            {
                return;
            }

            handler.GetPageFade(handler.CurrentPageIndex)?.Hide();
            handler.CurrentPageIndex--;
            handler.ShowPage(handler.CurrentPageIndex);
        }

        public static void ShowPage(this IPageHandler handler, int index)
        {
            if (handler == null || index < 0 || index >= handler.PageCount)
            {
                $"{handler?.GetType().Name ?? nameof(IPageHandler)}: Invalid page index {index}.".LogWarning();
                return;
            }

            handler.OnPageShown(index);
            handler.GetPageFade(index)?.Show();
        }

        public static void HideAllPages(this IPageHandler handler)
        {
            if (handler == null || handler.PageCount <= 0)
            {
                return;
            }

            for (int i = 0; i < handler.PageCount; i++)
            {
                handler.GetPageFade(i)?.Hide();
            }
        }
    }
}
