using NUnit.Framework;
using Turnroot.Gameplay.Objects;
using Turnroot.Tests.Editor.Helpers;
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
            var template = TestFixtures.CreateTemplate<ObjectItem>(TemplatePath, "TestObjectItem");
            var (go, brain, ltm, gw) = TestFixtures.CreateBrainWithLtmAndGw("test-brain");

            var oi = new ObjectItemInstance(template);
            var encoded = gw.EncodeInstanceToString(oi);
            Assert.IsNotNull(encoded);

            var decoded = gw.DecodeInstanceFromString<ObjectItemInstance>(encoded);
            Assert.IsNotNull(decoded);
            Assert.IsNotNull(decoded.Template);
            Assert.AreEqual(template.name, decoded.Template.name);

            TestFixtures.DestroyGameObject(go);
            TestFixtures.CleanupTemplate(TemplatePath);
        }
    }
}
