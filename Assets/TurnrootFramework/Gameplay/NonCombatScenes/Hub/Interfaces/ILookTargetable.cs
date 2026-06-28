namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public interface ILookTargetable : IHubSelectable
    {
        float LookDistance { get; }
    }
}
