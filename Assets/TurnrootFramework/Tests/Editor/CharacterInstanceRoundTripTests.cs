using Assets.Turnroot.Gameplay.Brain;
using NUnit.Framework;
using Turnroot.Characters;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class CharacterInstanceRoundTripTests
    {
        private const string TestFolder = "Assets/Resources/TestData";
        private const string TemplatePath = TestFolder + "/test_character_data.asset";

        [SetUp]
        public void Setup()
        {
            // Ensure Resources/TestData folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "TestData");
        }

        [TearDown]
        public void TearDown()
        {
            // cleanup assets
            AssetDatabase.DeleteAsset(TemplatePath);
            AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void CharacterInstance_EncodeDecode_RoundTripPreservesIdAndRuntimeFields()
        {
            // Create a CharacterData asset in Resources so the converter can resolve it by path
            var template = ScriptableObject.CreateInstance<CharacterData>();
            template.name = "TestCharacterTemplate";
            AssetDatabase.CreateAsset(template, TemplatePath);
            AssetDatabase.SaveAssets();
            // In some environments we see an unrelated package GUID conflict error during
            // AssetDatabase ops. It's noisy and not relevant to this test — explicitly
            // expect the error log so the test runner doesn't treat it as an unexpected failure.
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "GUID");
            AssetDatabase.Refresh();

            // Create a GameObject with Brain + LongTermMemory + GamewideContextBrain
            var go = new GameObject("test-brain");
            go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<GamewideContextBrain>();

            // Create instance from template and mutate runtime state
            var instance = CharacterInstance.Create(template);
            instance.LevelUp(); // mutate level

            // Ensure mutated value is present
            Assert.Greater(instance.CurrentLevel, template.Level);

            // Encode and decode
            var encoded = gw.EncodeInstanceToString(instance);
            Assert.IsNotNull(encoded);

            var decoded = gw.DecodeInstanceFromString<CharacterInstance>(encoded);
            Assert.IsNotNull(decoded);

            // Verify ID and runtime field preserved
            Assert.AreEqual(instance.Id, decoded.Id);
            Assert.AreEqual(instance.CurrentLevel, decoded.CurrentLevel);

            // cleanup
            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void Encode_PersistsHashToLongTermMemory()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            template.name = "TestCharacterTemplate";
            AssetDatabase.CreateAsset(template, TemplatePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var go = new GameObject("test-brain");
            go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            var ltm = go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<GamewideContextBrain>();

            var instance = CharacterInstance.Create(template);
            var encoded = gw.EncodeInstanceToString(instance);
            Assert.IsNotNull(encoded);

            // Ensure LTM contains the stored hash for the instance
            var wrapperJson = System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(encoded)
            );
            var wrapperObj = Newtonsoft.Json.Linq.JObject.Parse(wrapperJson);
            var wrapperHash = (string)wrapperObj["Hash"];

            var rawKey = $"GWB.InstanceHash.{typeof(CharacterInstance).FullName}.{instance.Id}";
            var keyHash =
                Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.ComputeFNV1a64Hex(
                    rawKey
                );
            var key = $"GWB.InstanceHash.{typeof(CharacterInstance).FullName}.{keyHash}";
            var stored = ltm.Recall(key);
            Assert.IsNotNull(stored, "Expected ledger entry in LongTermMemory");
            Assert.AreEqual(wrapperHash, stored, "Stored LTM hash should match wrapper hash");

            GameObject.DestroyImmediate(go);
        }
    }
}
