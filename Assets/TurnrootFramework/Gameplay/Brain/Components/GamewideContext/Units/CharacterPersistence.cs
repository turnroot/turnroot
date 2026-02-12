using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;

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
            var encodeResult = GamewideContextBrainHelpers.EncodeInstanceToString(
                brain.gamewideContextBrain,
                instance
            );

            if (!encodeResult.Success)
            {
                return;
            }

            var encoded = encodeResult.Value;
            var key = BuildCharacterKey(instance.CharacterTemplate);

            _ltm.Remember(key, encoded);

            if (updateIndex)
            {
                AddToCharacterIndex(instance.CharacterTemplate.name);
            }
        }

        public CharacterInstance RecallCharacter(CharacterData template)
        {
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
                    $"Failed to decode character: {decodeResult.Error}".LogWarning(
                        "CharacterPersistence"
                    );
                    return null;
                }
                return decodeResult.Value;
            }
            catch (Exception ex)
            {
                $"Failed to recall character: {ex.Message}".LogWarning("CharacterPersistence");
                return null;
            }
        }

        private string BuildCharacterKey(CharacterData template) =>
            $"{LtmKeys.CharacterKey}.{template.name}";

        private void AddToCharacterIndex(string templateName)
        {
            GamewideContextBrainHelpers.AddToIndexIfMissing(
                _ltm,
                LtmKeys.UniqueCharacterIndex,
                templateName
            );
        }
    }
}
