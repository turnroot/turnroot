using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components.Notifications
{
    [RequireComponent(typeof(Notifications))]
    public class NotificationsHelper : MonoBehaviour
    {
        private Notifications notifications;

        public NotificationTypeData[] types;

        public string message;

        private void Awake() => notifications = GetComponent<Notifications>();

        public void Send(int index)
        {
            if (index < 0 || index >= types.Length)
            {
                "Invalid notification type index.".LogError();
                return;
            }

            notifications.ShowNotification(message, types[index]);
        }

        public void SetMessage(string newMessage) => message = newMessage;

        public void HideContainer()
        {
            if (notifications?.container == null)
            {
                "NotificationsHelper: Container is not assigned.".LogWarning();
                return;
            }
            notifications.container.SetActive(false);
        }

        public void ShowContainer()
        {
            if (notifications?.container == null)
            {
                "NotificationsHelper: Container is not assigned.".LogWarning();
                return;
            }
            notifications.container.SetActive(true);
        }
    }
}
