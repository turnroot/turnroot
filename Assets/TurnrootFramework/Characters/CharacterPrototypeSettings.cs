using UnityEngine;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "CharacterPrototypeSettings",
        menuName = "Turnroot/Game Settings/CharacterPrototypeSettings"
    )]
    public class CharacterPrototypeSettings : SingletonScriptableObject<CharacterPrototypeSettings>
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Defer the update to avoid issues during asset import
            UnityEditor.EditorApplication.delayCall += UpdateAllCharacters;
        }

        private void UpdateAllCharacters()
        {
            // Check if we're in the middle of asset importing
            if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
            {
                return;
            }

            // Find all Character assets in the project
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Character");

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                CharacterData character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(
                    path
                );

                if (character != null)
                {
                    // Mark the character as dirty so it will be saved with updated settings
                    UnityEditor.EditorUtility.SetDirty(character);
                }
            }

            // Save all marked assets
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"Updated {guids.Length} Character assets with new settings.");
        }
#endif
    }
}
