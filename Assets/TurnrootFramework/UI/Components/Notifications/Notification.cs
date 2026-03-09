using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components.Notifications
{
    public class Notification : MonoBehaviour
    {
        public NotificationTypeData notificationType;
        public TextMeshProUGUI text;
        public string message;
        public Image icon;
        public Image background;

        public void Initialize()
        {
            if (notificationType != null)
            {
                background.color = notificationType.color;
                icon.sprite = notificationType.icon;
                text.text = message;
            }
        }

        public void SetMessage(string newMessage)
        {
            message = newMessage;
            if (text != null)
            {
                text.text = message;
            }
        }
    }
}
