using Turnroot.Characters;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Tests.Editor.Helpers
{
    /// <summary>
    /// Small test fixture helpers used by multiple editor tests. Centralizes GameObject
    /// creation for brain/ltm/gamewide context and helpers to create / cleanup template assets.
    /// </summary>
    public static class TestFixtures
    {
        public const string TestFolder = "Assets/Resources/TestData";

        public static (
            GameObject go,
            Assets.Turnroot.Gameplay.Brain.Brain brain,
            LongTermMemory ltm,
            Assets.Turnroot.Gameplay.Brain.GamewideContextBrain gw
        ) CreateBrainWithLtmAndGw(string name = "test-brain")
        {
            var go = new GameObject(name);
            var brain = go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            var ltm = go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<Assets.Turnroot.Gameplay.Brain.GamewideContextBrain>();
            return (go, brain, ltm, gw);
        }

        public static CharacterData CreateCharacterTemplate(
            string path = null,
            string name = "TestCharacterTemplate"
        )
        {
            if (string.IsNullOrEmpty(path))
                path = TestFolder + "/test_character_data.asset";

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "TestData");

            var template = ScriptableObject.CreateInstance<CharacterData>();
            template.name = name;
            AssetDatabase.CreateAsset(template, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return template;
        }

        public static T CreateTemplate<T>(string path = null, string name = "TestTemplate")
            where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(path))
                path = TestFolder + "/test_template.asset";

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "TestData");

            var template = ScriptableObject.CreateInstance<T>();
            template.name = name;
            AssetDatabase.CreateAsset(template, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return template;
        }

        public static void CleanupTemplate(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                path = TestFolder + "/test_character_data.asset";
            AssetDatabase.DeleteAsset(path);
            // Attempt to remove the folder if empty
            try
            {
                AssetDatabase.DeleteAsset(TestFolder);
            }
            catch { }
            AssetDatabase.Refresh();
        }

        public static void DestroyGameObject(GameObject go)
        {
            if (go == null)
                return;
            Object.DestroyImmediate(go);
        }
    }
}
