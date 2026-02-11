using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Settings for character prototypes and character-related editor updates.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterPrototypeSettings",
        menuName = "Turnroot/Game Settings/CharacterPrototypeSettings"
    )]
    public class CharacterPrototypeSettings : SingletonScriptableObject<CharacterPrototypeSettings>
    {
#if UNITY_EDITOR
        private void OnValidate() => UnityEditor.EditorApplication.delayCall += UpdateAllCharacters;

        private void UpdateAllCharacters()
        {
            if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
            {
                return;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Character");

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                CharacterData character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(
                    path
                );

                if (character != null)
                {
                    UnityEditor.EditorUtility.SetDirty(character);
                }
            }

            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
