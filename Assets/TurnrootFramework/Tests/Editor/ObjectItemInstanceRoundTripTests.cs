using NUnit.Framework;
using Turnroot.Gameplay.Objects;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class ObjectItemInstanceRoundTripTests
    {
        private const string TestFolder = "Assets/Resources/TestData";
        private const string TemplatePath = TestFolder + "/test_object_item.asset";

        [SetUp]
        public void Setup()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "TestData");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TemplatePath);
            AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void ObjectItemInstance_RoundTrip_PreservesTemplateReference()
        {
            var template = ScriptableObject.CreateInstance<ObjectItem>();
            template.name = "TestObjectItem";
            AssetDatabase.CreateAsset(template, TemplatePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var go = new GameObject("test-brain");
            go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<Assets.Turnroot.Gameplay.Brain.GamewideContextBrain>();

            var oi = new ObjectItemInstance(template);
            var encoded = gw.EncodeInstanceToString(oi);
            Assert.IsNotNull(encoded);

            var decoded = gw.DecodeInstanceFromString<ObjectItemInstance>(encoded);
            Assert.IsNotNull(decoded);
            Assert.IsNotNull(decoded.Template);
            Assert.AreEqual(template.name, decoded.Template.name);

            GameObject.DestroyImmediate(go);
        }
    }
}
