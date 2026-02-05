using System;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public static partial class GamewideContextBrainHelpers
    {
        #region Default Instance Creation

        public static T CreateDefaultInstanceFromWrapper<T>(SerializedWrapper wrapper)
        {
            var t = typeof(T);

            if (t == typeof(CharacterInstance))
            {
                var characterData = TryExtractCharacterDataFromWrapper(wrapper);
                return characterData != null
                    ? (T)(object)CharacterInstance.Create(characterData)
                    : default;
            }

            if (t.IsValueType)
            {
                return default;
            }

            var ctor = t.GetConstructor(Type.EmptyTypes);
            return ctor != null ? (T)Activator.CreateInstance(t) : default;
        }

        private static CharacterData TryExtractCharacterDataFromWrapper(SerializedWrapper wrapper)
        {
            if (wrapper == null || string.IsNullOrEmpty(wrapper.Payload))
            {
                return null;
            }

            return TryExecute(
                () =>
                {
                    var payloadObj = JObject.Parse(wrapper.Payload);
                    var templateToken =
                        payloadObj.SelectToken("_characterTemplate")
                        ?? payloadObj.SelectToken("CharacterTemplate");

                    if (templateToken?.Type != JTokenType.Object)
                    {
                        return null;
                    }

#if UNITY_EDITOR
                    var characterData = TryLoadCharacterDataInEditor(templateToken);
                    if (characterData != null)
                    {
                        return characterData;
                    }
#endif

                    var name = templateToken.Value<string>("name");
                    return !string.IsNullOrEmpty(name) ? Resources.Load<CharacterData>(name) : null;
                },
                null,
                "Failed to extract CharacterData from wrapper"
            );
        }

#if UNITY_EDITOR
        private static CharacterData TryLoadCharacterDataInEditor(JToken templateToken)
        {
            var guid = templateToken.Value<string>("guid");
            if (!string.IsNullOrEmpty(guid))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var characterData = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(
                        path
                    );
                    if (characterData != null)
                    {
                        return characterData;
                    }
                }
            }

            var assetPath = templateToken.Value<string>("assetPath");
            return !string.IsNullOrEmpty(assetPath)
                ? UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath)
                : null;
        }
#endif

        #endregion
    }
}
