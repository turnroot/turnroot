using System.Collections.Generic;
using NUnit.Framework;
using Turnroot.Gameplay.Objects;
using Turnroot.Tests.Editor.Helpers;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class CharacterInventoryRoundTripTests
    {
        private const string TestFolder = "Assets/Resources/TestData";
        private const string TemplateA = TestFolder + "/inv_item_a.asset";
        private const string TemplateB = TestFolder + "/inv_item_b.asset";

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
            AssetDatabase.DeleteAsset(TemplateA);
            AssetDatabase.DeleteAsset(TemplateB);
            AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void CharacterInventory_RoundTrip_PreservesItemsAndCount()
        {
            var a = TestFixtures.CreateTemplate<ObjectItem>(TemplateA, "InvA");
            var b = TestFixtures.CreateTemplate<ObjectItem>(TemplateB, "InvB");

            var inv = new CharacterInventoryInstance(4);
            inv.AddToInventory(new ObjectItemInstance(a));
            inv.AddToInventory(new ObjectItemInstance(b));

            var (go, brain, ltm, gw) = TestFixtures.CreateBrainWithLtmAndGw("test-brain");

            var encoded = gw.EncodeInstanceToString(inv);
            Assert.IsNotNull(encoded);

            var decoded = gw.DecodeInstanceFromString<CharacterInventoryInstance>(encoded);
            Assert.IsNotNull(decoded);
            Assert.AreEqual(2, decoded.InventoryItems.Count);
            Assert.IsNotNull(decoded.InventoryItems[0].Template);
            // Compare against the actual asset's name (AssetDatabase may change the stored asset name
            // to match the filename when creating assets) — use the created asset's runtime name.
            var created = AssetDatabase.LoadAssetAtPath<ObjectItem>(TemplateA);
            Assert.IsNotNull(created, "Expected created asset to exist at TemplateA");
            Assert.AreEqual(created.name, decoded.InventoryItems[0].Template.name);

            TestFixtures.DestroyGameObject(go);
            TestFixtures.CleanupTemplate(TemplateA);
            TestFixtures.CleanupTemplate(TemplateB);
        }
    }
}
