namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public interface IHubSelectable : IHubVisualFadable
    {
        bool CanSelect { get; }
        void Select();
    }
}
