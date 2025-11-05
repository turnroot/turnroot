using Assets.Prototypes.Graphics.Portrait;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GraphicsPrototypesSettings",
    menuName = "Game Settings/GraphicsPrototypesSettings"
)]
public class GraphicsPrototypesSettings : ScriptableObject
{
    public int portraitRenderWidth = 512;
    public int portraitRenderHeight = 512;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Defer the update to avoid issues during asset import
        UnityEditor.EditorApplication.delayCall += UpdateAllImageStacks;
    }

    private void UpdateAllImageStacks()
    {
        // Check if we're in the middle of asset importing
        if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
        {
            return;
        }

        // Find all ImageStack assets in the project
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ImageStack");

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ImageStack imageStack = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageStack>(path);

            if (imageStack != null)
            {
                // Mark the image stack as dirty so it will be saved with updated settings
                UnityEditor.EditorUtility.SetDirty(imageStack);
            }
        }

        // Save all marked assets
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"Updated {guids.Length} ImageStack assets with new settings.");
    }
#endif
}
