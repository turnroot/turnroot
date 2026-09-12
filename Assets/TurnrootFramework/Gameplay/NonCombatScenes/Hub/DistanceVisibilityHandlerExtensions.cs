using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public static class DistanceVisibilityHandlerExtensions
    {
        public static void UpdateDistanceVisibility(this IDistanceVisibilityHandler handler)
        {
            if (handler == null)
            {
                return;
            }

            if (handler.AvatarPosition == null)
            {
                return;
            }

            float showDistance = Mathf.Max(0f, handler.ShowDistance);
            float hideDistance = Mathf.Max(showDistance, handler.HideDistance);
            float sqrDistance = (
                handler.DistanceVisibilityPosition - handler.AvatarPosition.position
            ).sqrMagnitude;

            bool shouldShow = handler.IsDistanceVisible
                ? sqrDistance <= (hideDistance * hideDistance)
                : sqrDistance <= (showDistance * showDistance);

            if (shouldShow == handler.IsDistanceVisible)
            {
                return;
            }

            if (shouldShow)
            {
                handler.Show();
            }
            else
            {
                handler.Hide();
            }

            handler.IsDistanceVisible = shouldShow;
        }
    }
}
