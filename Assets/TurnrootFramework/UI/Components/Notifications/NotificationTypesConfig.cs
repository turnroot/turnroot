using UnityEngine;

namespace Turnroot.UI.Components.Notifications
{
    [CreateAssetMenu(fileName = "NotificationType", menuName = "Turnroot/UI/Notification Type")]
    public class NotificationTypeData : ScriptableObject
    {
        public string category;
        public Color color;
        public Sprite icon;
    }
}
