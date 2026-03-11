using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components.Notifications
{
    public class Notifications : MonoBehaviour
    {
        public GameObject container;
        public GameObject notificationPrefab;

        private List<Notification> instances = new List<Notification>();

        private void Awake()
        {
            // make sure list is fresh and container cleaned when game starts
            instances = new List<Notification>();
            if (container != null)
            {
                foreach (Transform child in container.transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void ShowNotification(string message, NotificationTypeData type)
        {
            if (container == null || notificationPrefab == null)
            {
                "Container or Notification Prefab is not assigned.".LogError();
                return;
            }

            GameObject newNotification = Instantiate(notificationPrefab, container.transform);
            Notification notificationComponent = newNotification.GetComponent<Notification>();
            notificationComponent.notificationType = type;
            notificationComponent.SetMessage(message);
            notificationComponent.Initialize();
            instances.Add(notificationComponent);
        }

        public void RemoveByIndex(int index)
        {
            if (index < 0 || index >= instances.Count)
            {
                "Invalid notification index.".LogError();
                return;
            }

            Notification toRemove = instances[index];
            instances.RemoveAt(index);
            Destroy(toRemove.gameObject);
        }

        public void ClearAll()
        {
            foreach (var notification in instances)
            {
                Destroy(notification.gameObject);
            }
            instances.Clear();
        }

        public void ClearByType(string typeCategory)
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                if (instances[i].notificationType.category == typeCategory)
                {
                    Destroy(instances[i].gameObject);
                    instances.RemoveAt(i);
                }
            }
        }
    }
}
