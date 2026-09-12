using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public interface IDistanceVisibilityHandler
    {
        Transform AvatarPosition { get; }
        float ShowDistance { get; }
        float HideDistance { get; }
        bool IsDistanceVisible { get; set; }
        Vector3 DistanceVisibilityPosition { get; }
        string DistanceVisibilityOwnerName { get; }
        void Show();
        void Hide();
    }
}
