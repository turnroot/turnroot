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
                if (!handler.MissingAvatarWarningLogged)
                {
                    $"{handler.GetType().Name} on {handler.DistanceVisibilityOwnerName} has no AvatarPosition assigned.".LogWarning();
                    handler.MissingAvatarWarningLogged = true;
                }

                if (handler.HideWhenAvatarMissing && handler.IsDistanceVisible)
                {
                    handler.Hide();
                    handler.IsDistanceVisible = false;
                }
                return;
            }

            handler.MissingAvatarWarningLogged = false;

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
