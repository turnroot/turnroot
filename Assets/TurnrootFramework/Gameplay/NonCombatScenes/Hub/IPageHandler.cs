using Turnroot.Utilities.AbstractScripts;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public interface IPageHandler
    {
        int CurrentPageIndex { get; set; }
        int PageCount { get; }
        UIFade GetPageFade(int index);
        void OnPageShown(int index);
        void OnPagesCompleted();
    }
}
