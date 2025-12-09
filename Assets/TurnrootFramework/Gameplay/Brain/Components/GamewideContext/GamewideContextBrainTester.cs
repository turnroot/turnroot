#if UNITY_EDITOR
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Editor helper component with NaughtyAttributes buttons to exercise GamewideContextBrain serialization.
    /// Place this alongside a GamewideContextBrain on a GameObject and assign a CharacterData in the inspector.
    /// This type is editor-only (compile guarded).
    /// </summary>
    [ExecuteAlways]
    public class GamewideContextBrainTester : MonoBehaviour
    {
        [Required]
        public GamewideContextBrain Brain;

        public Turnroot.Gameplay.Brain.GamewideContextBrain.TamperPolicy TestPolicy = Assets
            .Turnroot
            .Gameplay
            .Brain
            .GamewideContextBrain
            .TamperPolicy
            .Replace;

        [Required]
        public CharacterData TestCharacter;

        [TextArea(5, 10)]
        public string LastEncoded;

        [Button("Encode & Decode Test")]
        public void EncodeDecodeTest()
        {
            if (Brain == null || TestCharacter == null)
            {
                Debug.LogWarning("GamewideContextBrain or TestCharacter not assigned.");
                return;
            }

            Brain.Policy = TestPolicy;
            var instance = CharacterInstance.Create(TestCharacter);
            var encoded = Brain.EncodeInstanceToString(instance);
            LastEncoded = encoded;
            if (encoded == null)
            {
                Debug.LogError("Failed to encode instance.");
                return;
            }

            var decoded = Brain.DecodeInstanceFromString<CharacterInstance>(encoded);
            if (decoded == null)
            {
                Debug.LogError("Decoded instance is null or decoding failed.");
                return;
            }

            if (decoded.Id == instance.Id && decoded.CurrentLevel == instance.CurrentLevel)
            {
                Debug.Log($"Round-trip OK for {TestCharacter.name} (id={decoded.Id}).");
            }
            else
            {
                Debug.LogError(
                    $"Round-trip mismatch for {TestCharacter.name}: id {instance.Id} / {decoded?.Id}"
                );
            }

            // Read ledger entry if available
            try
            {
                var ltm = Brain.GetComponent<LongTermMemory>();
                var rawKey = $"GWB.InstanceHash.{typeof(CharacterInstance).FullName}.{instance.Id}";
                var keyHash = Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.ComputeFNV1a64Hex(
                    rawKey
                );
                var key = $"GWB.InstanceHash.{typeof(CharacterInstance).FullName}.{keyHash}";
                var stored = ltm?.Recall(key);
                Debug.Log($"Ledger key: {key} => {stored}");
            }
            catch (System.Exception) { }
        }

        [Button("Test Decode")]
        public void TestDecode()
        {
            if (Brain == null)
            {
                Debug.LogWarning("GamewideContextBrain not assigned.");
                return;
            }

            if (string.IsNullOrEmpty(LastEncoded))
            {
                Debug.LogWarning("No last encoded string to decode.");
                return;
            }

            Brain.Policy = TestPolicy;
            var decoded = Brain.DecodeInstanceFromString<CharacterInstance>(LastEncoded);
            if (decoded == null)
            {
                Debug.LogError("Decoded instance is null or decoding failed.");
                return;
            }

            Debug.Log(
                $"Decoded instance: id={decoded.Id}, level={decoded.CurrentLevel} from last encoded string."
            );
        }

        [Button("Tamper Last Encoded and Decode")]
        public void TamperLastEncodedAndDecode()
        {
            if (string.IsNullOrEmpty(LastEncoded))
            {
                Debug.LogWarning("No last encoded string — run EncodeDecodeTest first.");
                return;
            }

            try
            {
                var wrapper =
                    Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.DecodeWrapperAsJObject(
                        LastEncoded
                    );
                var payload = JsonConvert.DeserializeObject<JObject>((string)wrapper["Payload"]);
                var levelToken = payload.SelectToken("_currentLevel");
                if (levelToken != null && levelToken.Type == JTokenType.Integer)
                {
                    var lvl = levelToken.ToObject<int>();
                    payload["_currentLevel"] = lvl + 1;
                }
                wrapper["Payload"] = payload.ToString(Formatting.None);
                var tampered = JsonConvert.SerializeObject(wrapper);
                var tamperedBase64 = System.Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(tampered)
                );

                Brain.Policy = TestPolicy;
                var decoded = Brain.DecodeInstanceFromString<CharacterInstance>(tamperedBase64);
                Debug.Log(
                    decoded == null
                        ? "Tamper detected (decoded null)."
                        : $"Tamper decode result: id={decoded.Id}, level={decoded.CurrentLevel}"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Tamper test failed: {e.Message}");
            }
        }

        [Button("Tamper & Update Hash (simulate attacker)")]
        public void TamperAndUpdateHash()
        {
            if (string.IsNullOrEmpty(LastEncoded))
            {
                Debug.LogWarning("No last encoded string — run EncodeDecodeTest first.");
                return;
            }

            try
            {
                var wrapper =
                    Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.DecodeWrapperAsJObject(
                        LastEncoded
                    );
                var payload = JsonConvert.DeserializeObject<JObject>((string)wrapper["Payload"]);
                var levelToken = payload.SelectToken("_currentLevel");
                if (levelToken != null && levelToken.Type == JTokenType.Integer)
                {
                    var lvl = levelToken.ToObject<int>();
                    payload["_currentLevel"] = lvl + 1;
                }
                wrapper["Payload"] = payload.ToString(Formatting.None);

                // recompute the wrapper hash to match the modified payload (attacker updates hash)
                var newHash =
                    Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.RecomputeHashFromWrapperJObject(
                        wrapper
                    );
                wrapper["Hash"] = newHash;

                var tamperedBase64 =
                    Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.EncodeJObjectToBase64(
                        wrapper
                    );

                Brain.Policy = TestPolicy;
                var decoded = Brain.DecodeInstanceFromString<CharacterInstance>(tamperedBase64);
                Debug.Log(
                    decoded == null
                        ? "Tamper detected (decoded null)."
                        : $"Tamper decode result: id={decoded.Id}, level={decoded.CurrentLevel}"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Tamper test failed: {e.Message}");
            }
        }

        [Button("Copy last encoded string")]
        public void CopyLastEncodedToClipboard()
        {
            if (string.IsNullOrEmpty(LastEncoded))
            {
                Debug.LogWarning("No last encoded string to copy.");
                return;
            }

            UnityEditor.EditorGUIUtility.systemCopyBuffer = LastEncoded;
            Debug.Log("Last encoded string copied to clipboard.");
        }
    }
}
#endif
