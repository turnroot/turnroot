using Assets.Turnroot.Gameplay.Brain;
using NUnit.Framework;
using Turnroot.Characters;
using Turnroot.Tests.Editor.Helpers;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class CharacterInstanceRoundTripTests
    {
        // Tests use TestFixtures helpers for creating template assets and brain instances

        [Test]
        public void CharacterInstance_EncodeDecode_RoundTripPreservesIdAndRuntimeFields()
        {
            // Previously we had to silence a noisy GUID conflict error; that issue
            // has been fixed so no test log expectation is required here.
            var template = TestFixtures.CreateCharacterTemplate();
            AssetDatabase.Refresh();

            var (go, brain, ltm, gw) = TestFixtures.CreateBrainWithLtmAndGw("test-brain");

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

            TestFixtures.DestroyGameObject(go);
            TestFixtures.CleanupTemplate();
        }

        [Test]
        public void Encode_PersistsHashToLongTermMemory()
        {
            var template = TestFixtures.CreateCharacterTemplate();
            var (go, brain, ltm, gw) = TestFixtures.CreateBrainWithLtmAndGw("test-brain");

            var instance = CharacterInstance.Create(template);
            var encoded = gw.EncodeInstanceToString(instance);
            Assert.IsNotNull(encoded);

            // Ensure LTM contains the stored hash for the instance
            var wrapper =
                Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.DecodeWrapperFromBase64(
                    encoded
                );
            var wrapperHash = wrapper?.Hash;

            var rawKey = $"GWB.InstanceHash.{typeof(CharacterInstance).FullName}.{instance.Id}";
            var keyHash =
                Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.ComputeFNV1a64Hex(
                    rawKey
                );
            var key = $"GWB.InstanceHash.{typeof(CharacterInstance).FullName}.{keyHash}";
            var stored = ltm.Recall(key);
            Assert.IsNotNull(stored, "Expected ledger entry in LongTermMemory");
            Assert.AreEqual(wrapperHash, stored, "Stored LTM hash should match wrapper hash");

            TestFixtures.DestroyGameObject(go);
            TestFixtures.CleanupTemplate();
        }
    }
}
