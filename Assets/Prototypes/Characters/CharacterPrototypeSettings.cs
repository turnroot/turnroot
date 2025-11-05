using UnityEngine;

namespace Assets.Prototypes.Characters
{
    [CreateAssetMenu(
        fileName = "CharacterPrototypeSettings",
        menuName = "Game Settings/CharacterPrototypeSettings"
    )]
    public class CharacterPrototypeSettings : ScriptableObject
    {
        public bool UseAccentColors = true;

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
                Character character = UnityEditor.AssetDatabase.LoadAssetAtPath<Character>(path);

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
