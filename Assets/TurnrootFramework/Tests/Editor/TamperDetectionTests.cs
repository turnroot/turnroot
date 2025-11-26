using Newtonsoft.Json;
using NUnit.Framework;
using Turnroot.Characters;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class TamperDetectionTests
    {
        private const string TestFolder = "Assets/Resources/TestData";
        private const string TemplatePath = TestFolder + "/test_character_data.asset";

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
        public void Decode_WhenPayloadTampered_RaisesIllegalModificationEvent()
        {
            // Create a template
            var template = ScriptableObject.CreateInstance<CharacterData>();
            template.name = "TestCharacterTemplate";
            AssetDatabase.CreateAsset(template, TemplatePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Setup brain
            var go = new GameObject("test-brain");
            var brain = go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<Assets.Turnroot.Gameplay.Brain.GamewideContextBrain>();

            bool eventCalled = false;
            string receivedMessage = null;
            brain.OnIllegallyModifiedFileDetected += (msg) =>
            {
                eventCalled = true;
                receivedMessage = msg;
            };

            var instance = new CharacterInstance(template);
            var encoded = gw.EncodeInstanceToString(instance);

            // Decode wrapper -> change payload
            var wrapperJson = System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(encoded)
            );
            var wrapperObj = Newtonsoft.Json.Linq.JObject.Parse(wrapperJson);
            var payloadObj = Newtonsoft.Json.Linq.JObject.Parse((string)wrapperObj["Payload"]);
            var levelToken = payloadObj.SelectToken("_currentLevel");
            if (levelToken != null && levelToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                var lvl = levelToken.ToObject<int>();
                payloadObj["_currentLevel"] = lvl + 1;
            }
            wrapperObj["Payload"] = payloadObj.ToString(Formatting.None);
            var tamperedJson = wrapperObj.ToString(Formatting.None);
            var tamperedBase64 = System.Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(tamperedJson)
            );

            // Decode -- this should trigger the brain event
            var decoded = gw.DecodeInstanceFromString<CharacterInstance>(tamperedBase64);

            Assert.IsTrue(eventCalled, "Expected illegal modification event to be raised");
            Assert.IsNotNull(receivedMessage);

            // The decode should return a safe default instance (replacement) rather than the tampered instance.
            Assert.IsNotNull(decoded, "Expected decoded replacement instance not null");
            Assert.AreNotEqual(
                instance.Id,
                decoded.Id,
                "Expected replacement instance to have a different id"
            );
            // Replacement should be constructed from the template and therefore match template defaults
            Assert.AreEqual(
                template.Level,
                decoded.CurrentLevel,
                "Replacement instance should use template level"
            );

            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void Decode_WhenWrapperHashUpdatedButLedgerNotUpdated_RaisesIllegalModificationEvent()
        {
            // Create a template
            var template = ScriptableObject.CreateInstance<CharacterData>();
            template.name = "TestCharacterTemplate";
            AssetDatabase.CreateAsset(template, TemplatePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Setup brain
            var go = new GameObject("test-brain-ledger");
            var brain = go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            var ltm = go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<Assets.Turnroot.Gameplay.Brain.GamewideContextBrain>();

            bool eventCalled = false;
            brain.OnIllegallyModifiedFileDetected += (msg) =>
            {
                eventCalled = true;
            };

            // Start with proper encoded wrapper
            var instance = new CharacterInstance(template);
            var encoded = gw.EncodeInstanceToString(instance);

            // Tamper payload and re-compute wrapper.Hash but DO NOT update LTM ledger
            var wrapperJson = System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(encoded)
            );
            var wrapperObj = Newtonsoft.Json.Linq.JObject.Parse(wrapperJson);
            var payloadObj = Newtonsoft.Json.Linq.JObject.Parse((string)wrapperObj["Payload"]);
            var levelToken = payloadObj.SelectToken("_currentLevel");
            if (levelToken != null && levelToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                var lvl = levelToken.ToObject<int>();
                payloadObj["_currentLevel"] = lvl + 1;
            }
            wrapperObj["Payload"] = payloadObj.ToString(Formatting.None);
            // recompute a matching hash for the modified payload (attacker updated hash to match)
            var newHash =
                Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.ComputeFNV1a64Hex(
                    payloadObj.ToString(Formatting.None) + "|v:" + (string)wrapperObj["Version"]
                );
            wrapperObj["Hash"] = newHash;

            var tamperedJson = wrapperObj.ToString(Formatting.None);
            var tamperedBase64 = System.Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(tamperedJson)
            );

            // Decode -- since LTM still contains the original hash, this should be flagged as tampering
            var decoded = gw.DecodeInstanceFromString<CharacterInstance>(tamperedBase64);

            Assert.IsTrue(
                eventCalled,
                "Expected illegal modification event to be raised (ledger mismatch)"
            );
            Assert.IsNotNull(decoded, "Expected replacement instance not null");
            Assert.AreNotEqual(
                instance.Id,
                decoded.Id,
                "Expected replacement instance to have a different id"
            );

            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void Decode_WhenPayloadTampered_Replace_UpdatesLedgerEntry()
        {
            // Create a template
            var template = ScriptableObject.CreateInstance<CharacterData>();
            template.name = "TestCharacterTemplate";
            AssetDatabase.CreateAsset(template, TemplatePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Setup brain
            var go = new GameObject("test-brain-replace-ledger");
            var brain = go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            var ltm = go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<Assets.Turnroot.Gameplay.Brain.GamewideContextBrain>();

            bool eventCalled = false;
            brain.OnIllegallyModifiedFileDetected += (msg) =>
            {
                eventCalled = true;
            };

            gw.Policy = Assets.Turnroot.Gameplay.Brain.GamewideContextBrain.TamperPolicy.Replace;

            var instance = new CharacterInstance(template);
            var encoded = gw.EncodeInstanceToString(instance);

            // Tamper payload (don't change wrapper.Hash so recomputed != wrapper.Hash) to trigger Replace
            var wrapperJson = System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(encoded)
            );
            var wrapperObj = Newtonsoft.Json.Linq.JObject.Parse(wrapperJson);
            var payloadObj = Newtonsoft.Json.Linq.JObject.Parse((string)wrapperObj["Payload"]);
            var levelToken = payloadObj.SelectToken("_currentLevel");
            if (levelToken != null && levelToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                var lvl = levelToken.ToObject<int>();
                payloadObj["_currentLevel"] = lvl + 1;
            }
            wrapperObj["Payload"] = payloadObj.ToString(Formatting.None);
            var tamperedJson = wrapperObj.ToString(Formatting.None);
            var tamperedBase64 = System.Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(tamperedJson)
            );

            // Prepare ledger key for the instance
            var rawKey = $"GWB.InstanceHash.{typeof(CharacterInstance).FullName}.{instance.Id}";
            var keyHash =
                Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.ComputeFNV1a64Hex(
                    rawKey
                );
            var key = $"GWB.InstanceHash.{typeof(CharacterInstance).FullName}.{keyHash}";
            var original = ltm.Recall(key);
            Assert.IsNotNull(original, "Expected original ledger entry to exist");

            // Decode (Replace policy should create new instance and write a new ledger entry via Encode)
            var decoded = gw.DecodeInstanceFromString<CharacterInstance>(tamperedBase64);

            Assert.IsTrue(eventCalled, "Expected illegal modification event to be raised");
            Assert.IsNotNull(decoded, "Expected replacement instance not null");
            var updated = ltm.Recall(key);
            Assert.IsNotNull(updated, "Expected ledger entry to still exist");
            Assert.AreNotEqual(
                original,
                updated,
                "Expected ledger entry to change after replacement"
            );

            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void Decode_WhenPayloadTampered_NotifyOnly_PreservesTamperedInstance()
        {
            // Create a template
            var template = ScriptableObject.CreateInstance<CharacterData>();
            template.name = "TestCharacterTemplate";
            AssetDatabase.CreateAsset(template, TemplatePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Setup brain
            var go = new GameObject("test-brain-notify");
            var brain = go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<Assets.Turnroot.Gameplay.Brain.GamewideContextBrain>();

            bool eventCalled = false;
            brain.OnIllegallyModifiedFileDetected += (msg) =>
            {
                eventCalled = true;
            };

            gw.Policy = Assets.Turnroot.Gameplay.Brain.GamewideContextBrain.TamperPolicy.NotifyOnly;

            var instance = new CharacterInstance(template);
            var encoded = gw.EncodeInstanceToString(instance);

            // tamper payload similarly to other test
            var wrapperJson = System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(encoded)
            );
            var wrapperObj = Newtonsoft.Json.Linq.JObject.Parse(wrapperJson);
            var payloadObj = Newtonsoft.Json.Linq.JObject.Parse((string)wrapperObj["Payload"]);
            var levelToken = payloadObj.SelectToken("_currentLevel");
            if (levelToken != null && levelToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                var lvl = levelToken.ToObject<int>();
                payloadObj["_currentLevel"] = lvl + 1;
            }
            wrapperObj["Payload"] = payloadObj.ToString(Formatting.None);
            var tamperedJson = wrapperObj.ToString(Formatting.None);
            var tamperedBase64 = System.Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(tamperedJson)
            );

            var decoded = gw.DecodeInstanceFromString<CharacterInstance>(tamperedBase64);

            Assert.IsTrue(eventCalled, "Expected illegal modification event to be raised");
            Assert.IsNotNull(decoded);
            // Since NotifyOnly returns the decoded (tampered) instance, level should be template.Level + 1
            Assert.AreEqual(template.Level + 1, decoded.CurrentLevel);

            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void Decode_WhenPayloadTampered_Rejects_ReturnsNull()
        {
            // Create a template
            var template = ScriptableObject.CreateInstance<CharacterData>();
            template.name = "TestCharacterTemplate";
            AssetDatabase.CreateAsset(template, TemplatePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Setup brain
            var go = new GameObject("test-brain-reject");
            var brain = go.AddComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
            go.AddComponent<LongTermMemory>();
            var gw = go.AddComponent<Assets.Turnroot.Gameplay.Brain.GamewideContextBrain>();

            bool eventCalled = false;
            brain.OnIllegallyModifiedFileDetected += (msg) =>
            {
                eventCalled = true;
            };

            gw.Policy = Assets.Turnroot.Gameplay.Brain.GamewideContextBrain.TamperPolicy.Reject;

            var instance = new CharacterInstance(template);
            var encoded = gw.EncodeInstanceToString(instance);

            // tamper payload similarly to other test
            var wrapperJson = System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(encoded)
            );
            var wrapperObj = Newtonsoft.Json.Linq.JObject.Parse(wrapperJson);
            var payloadObj = Newtonsoft.Json.Linq.JObject.Parse((string)wrapperObj["Payload"]);
            var levelToken = payloadObj.SelectToken("_currentLevel");
            if (levelToken != null && levelToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                var lvl = levelToken.ToObject<int>();
                payloadObj["_currentLevel"] = lvl + 1;
            }
            wrapperObj["Payload"] = payloadObj.ToString(Formatting.None);
            var tamperedJson = wrapperObj.ToString(Formatting.None);
            var tamperedBase64 = System.Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(tamperedJson)
            );

            var decoded = gw.DecodeInstanceFromString<CharacterInstance>(tamperedBase64);

            Assert.IsTrue(eventCalled, "Expected illegal modification event to be raised");
            Assert.IsNull(decoded, "Expected Reject policy to return null/default");

            GameObject.DestroyImmediate(go);
        }
    }
}
