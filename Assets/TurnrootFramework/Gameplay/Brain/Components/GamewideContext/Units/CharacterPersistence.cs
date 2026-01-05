using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles character save/load to LongTermMemory.
    /// Single responsibility: persist character data.
    /// </summary>
    public class CharacterPersistence
    {
        private readonly LongTermMemory _ltm;
        public Brain brain;

        public CharacterPersistence(Brain brain)
        {
            _ltm = brain.GetComponent<LongTermMemory>();
            this.brain = brain;
        }

        public void SaveCharacter(CharacterInstance instance, bool updateIndex)
        {
            if (instance?.CharacterTemplate == null || !instance.CharacterTemplate.IsUnique)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Can only save unique characters");
#endif
                return;
            }

            try
            {
                // Use existing helper
                var encodeResult = GamewideContextBrainHelpers.EncodeInstanceToString(
                    brain.gamewideContextBrain,
                    instance
                );

                if (!encodeResult.Success)
                {
#if UNITY_EDITOR
                    Debug.LogError($"Failed to encode character: {encodeResult.Error}");
#endif
                    return;
                }

                var encoded = encodeResult.Value;
                var key = BuildCharacterKey(instance.CharacterTemplate);

                _ltm.Remember(key, encoded);

                if (updateIndex)
                {
                    AddToCharacterIndex(instance.CharacterTemplate.name);
                }

#if UNITY_EDITOR
                Debug.Log($"Saved unique character: {instance.CharacterTemplate.DisplayName}");
#endif
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to save character: {ex.Message}");
#endif
            }
        }

        public CharacterInstance RecallCharacter(CharacterData template)
        {
            if (template == null || !template.IsUnique)
            {
                return null;
            }

            try
            {
                var key = BuildCharacterKey(template);
                var encoded = _ltm.Recall(key);

                if (string.IsNullOrEmpty(encoded))
                {
                    return null;
                }

                // Use existing helper
                var decodeResult =
                    GamewideContextBrainHelpers.DecodeInstanceFromString<CharacterInstance>(
                        brain.gamewideContextBrain,
                        encoded
                    );

                if (!decodeResult.Success)
                {
#if UNITY_EDITOR
                    Debug.LogError($"Failed to decode character: {decodeResult.Error}");
#endif
                    return null;
                }

#if UNITY_EDITOR
                Debug.Log($"Recalled unique character: {template.DisplayName}");
#endif
                return decodeResult.Value;
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Failed to recall character: {ex.Message}");
#endif
                return null;
            }
        }

        private string BuildCharacterKey(CharacterData template) =>
            $"{LtmKeys.CharacterKey}.{template.name}";

        private void AddToCharacterIndex(string templateName)
        {
            var indexJson = _ltm.Recall(LtmKeys.UniqueCharacterIndex);
            var index = string.IsNullOrEmpty(indexJson)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(indexJson);

            if (!index.Contains(templateName))
            {
                index.Add(templateName);
                _ltm.Remember(LtmKeys.UniqueCharacterIndex, JsonConvert.SerializeObject(index));
            }
        }
    }
}
