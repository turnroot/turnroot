using System.IO;
using UnityEditor;
using UnityEngine;

namespace Turnroot.UI.Components.Notifications.Editor
{
    public class RecreateNotificationTypes
    {
        [MenuItem("Tools/Turnroot/Recreate Notification Type Assets")]
        public static void RecreateAssets()
        {
            string folderPath = "Assets/Demos/Resources/Components/UI/NotificationTypes";

            // Data from the corrupted assets
            var typesData = new[]
            {
                new
                {
                    name = "Battle",
                    category = "battle",
                    color = new Color(1f, 0.2509804f, 0.2509804f, 1f),
                    iconGuid = "f2247ac8d739b5e49a3e9dbb47792e84",
                },
                new
                {
                    name = "Exploration",
                    category = "exploration",
                    color = new Color(0.9372549f, 0.88235295f, 0.14117648f, 1f),
                    iconGuid = "9685e67314468cd4884bf6ae6dbe6f6f",
                },
                new
                {
                    name = "Fishing",
                    category = "fishing",
                    color = new Color(0.37254903f, 1f, 0.6392157f, 1f),
                    iconGuid = "35bd142a7c11d934baa1c3e5f9a0513e",
                },
                new
                {
                    name = "Ship",
                    category = "ship",
                    color = new Color(0.33333334f, 0.7176471f, 1f, 1f),
                    iconGuid = "f871f0b26542efc4d87fc293139e5ec8",
                },
                new
                {
                    name = "Interaction",
                    category = "interaction",
                    color = new Color(1f, 0.627451f, 0.9098039f, 1f),
                    iconGuid = "a007f32800deb384dad126502d80c12a",
                },
            };

            // Delete old assets
            foreach (var data in typesData)
            {
                string assetPath = $"{folderPath}/{data.name}.asset";
                if (File.Exists(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    Debug.Log($"Deleted old asset: {assetPath}");
                }
            }

            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Create new assets
            foreach (var data in typesData)
            {
                NotificationTypeData asset =
                    ScriptableObject.CreateInstance<NotificationTypeData>();
                asset.category = data.category;
                asset.color = data.color;

                // Load icon by GUID
                string iconPath = AssetDatabase.GUIDToAssetPath(data.iconGuid);
                if (!string.IsNullOrEmpty(iconPath))
                {
                    asset.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                }

                string assetPath = $"{folderPath}/{data.name}.asset";
                AssetDatabase.CreateAsset(asset, assetPath);
                Debug.Log($"Created new asset: {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Successfully recreated all notification type assets!");
        }
    }
}
