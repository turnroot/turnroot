using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Custom converter that reconstructs CharacterInstance using its constructor and populates runtime/private fields reflectively.
    /// </summary>
    public class CharacterInstanceJsonConverter : JsonConverter
    {
        private const string UnityMarker = "__unity";
        private const BindingFlags PrivateInstanceFlags =
            BindingFlags.Instance | BindingFlags.NonPublic;

        // Field name constants
        /// <summary>
        /// Constants for CharacterInstance field names used during serialization.
        /// </summary>
        private static class FieldNames
        {
            public const string Id = "_id";
            public const string CharacterTemplate = "_characterTemplate";
            public const string CurrentLevel = "_currentLevel";
            public const string CurrentExp = "_currentExp";
            public const string RuntimeBoundedStats = "_runtimeBoundedStats";
            public const string RuntimeUnboundedStats = "_runtimeUnboundedStats";
            public const string InventoryInstance = "_inventoryInstance";
            public const string SkillInstances = "_skill_instances";
            public const string SupportRelationships = "_support_relationships";

            // Additional persistent fields
            public const string ExperienceRanks = "_experienceRanks";
            public const string CurrentClass = "_currentClass";
            public const string EquippedClassHistory = "_equippedClassHistory";
            public const string ActiveStatusEffects = "_activeStatusEffects";
            public const string MapGridPosition = "_mapGridPosition";
            public const string UseBattleModel = "_useBattleModel";
        }

        public override bool CanConvert(Type objectType) =>
            typeof(CharacterInstance).IsAssignableFrom(objectType);

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var instance = value as CharacterInstance;
            if (instance == null)
            {
                writer.WriteNull();
                return;
            }

            var token = SerializeCharacterInstance(instance, serializer);
            token.WriteTo(writer);
        }

        private JObject SerializeCharacterInstance(
            CharacterInstance instance,
            JsonSerializer serializer
        )
        {
            var token = new JObject { [FieldNames.Id] = instance.Id };

            var templateToken = CreateTemplateToken(instance.CharacterTemplate);
            token[FieldNames.CharacterTemplate] =
                templateToken != null ? (JToken)templateToken : JValue.CreateNull();

            token[FieldNames.CurrentLevel] = JToken.FromObject(instance.CurrentLevel, serializer);
            token[FieldNames.CurrentExp] = JToken.FromObject(instance.CurrentExp, serializer);

            token[FieldNames.RuntimeBoundedStats] = SerializeFieldOrNull(
                instance.RuntimeBoundedStats,
                serializer
            );
            token[FieldNames.RuntimeUnboundedStats] = SerializeFieldOrNull(
                instance.RuntimeUnboundedStats,
                serializer
            );
            token[FieldNames.InventoryInstance] = SerializeFieldOrNull(
                instance.InventoryInstance,
                serializer
            );
            token[FieldNames.SkillInstances] = SerializeFieldOrNull(
                instance.SkillInstances,
                serializer
            );

            // Persist additional fields
            token[FieldNames.ExperienceRanks] = SerializeFieldOrNull(
                instance.ExperienceRanks,
                serializer
            );
            token[FieldNames.CurrentClass] = SerializeFieldOrNull(
                instance.CurrentClass,
                serializer
            );
            token[FieldNames.EquippedClassHistory] = SerializeFieldOrNull(
                instance._equippedClassHistory,
                serializer
            );
            token[FieldNames.ActiveStatusEffects] = SerializeFieldOrNull(
                instance.ActiveStatusEffects,
                serializer
            );

            token[FieldNames.MapGridPosition] = JToken.FromObject(
                instance.MapGridPosition,
                serializer
            );

            token[FieldNames.UseBattleModel] = JToken.FromObject(
                instance.UseBattleModel,
                serializer
            );

            var supportRelationships = GetPrivateFieldValue(
                instance,
                FieldNames.SupportRelationships
            );
            token[FieldNames.SupportRelationships] = SerializeFieldOrNull(
                supportRelationships,
                serializer
            );

            return token;
        }

        /// <summary>
        /// Helper method to serialize a field or return null if the value is null.
        /// Reduces repetitive null checking in serialization code.
        /// </summary>
        private static JToken SerializeFieldOrNull(object value, JsonSerializer serializer) =>
            value != null ? JToken.FromObject(value, serializer) : JValue.CreateNull();

        private JObject CreateTemplateToken(CharacterData template)
        {
            if (template == null)
            {
                return null;
            }

            var token = new JObject
            {
                [UnityMarker] = true,
                ["type"] = template.GetType().AssemblyQualifiedName,
                ["name"] = template.name,
            };

#if UNITY_EDITOR
            AddEditorMetadata(token, template);
#endif

            return token;
        }

#if UNITY_EDITOR
        private void AddEditorMetadata(JObject token, CharacterData template)
        {
            try
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(template);
                if (!string.IsNullOrEmpty(path))
                {
                    token["assetPath"] = path;

                    try
                    {
                        token["guid"] = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"Failed to get GUID for template: {ex.Message}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning($"Failed to get template asset path: {ex.Message}");
#endif
            }
        }
#endif

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            var token = JObject.Load(reader);
            var template = ResolveCharacterTemplate(token, serializer);
            var instance = CreateCharacterInstance(template);

            PopulateInstanceFields(instance, token, serializer);
            instance?.OnAfterDeserialize();

            return instance;
        }

        private CharacterData ResolveCharacterTemplate(JObject token, JsonSerializer serializer)
        {
            var templateToken =
                token.SelectToken(FieldNames.CharacterTemplate)
                ?? token.SelectToken("CharacterTemplate");

            if (templateToken?.Type == JTokenType.Null || templateToken == null)
            {
                return null;
            }

            try
            {
                return templateToken.ToObject<CharacterData>(serializer);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    $"Failed to deserialize CharacterData template: {ex.Message}"
                );
                return null;
            }
        }

        private CharacterInstance CreateCharacterInstance(CharacterData template)
        {
            if (template != null)
            {
                return CharacterInstance.Create(template);
            }

            // Create uninitialized instance if no template available
            return (CharacterInstance)
                System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                    typeof(CharacterInstance)
                );
        }

        private void PopulateInstanceFields(
            CharacterInstance instance,
            JObject token,
            JsonSerializer serializer
        )
        {
            if (instance == null)
            {
                return;
            }

            SetFieldFromToken(instance, token, FieldNames.Id, "Id", serializer);
            SetFieldFromToken(instance, token, FieldNames.CurrentLevel, "CurrentLevel", serializer);
            SetFieldFromToken(instance, token, FieldNames.CurrentExp, "CurrentExp", serializer);
            SetFieldFromToken(
                instance,
                token,
                FieldNames.RuntimeBoundedStats,
                "RuntimeBoundedStats",
                serializer
            );
            SetFieldFromToken(
                instance,
                token,
                FieldNames.RuntimeUnboundedStats,
                "RuntimeUnboundedStats",
                serializer
            );
            SetFieldFromToken(
                instance,
                token,
                FieldNames.InventoryInstance,
                "InventoryInstance",
                serializer
            );
            SetFieldFromToken(
                instance,
                token,
                FieldNames.SkillInstances,
                "SkillInstances",
                serializer
            );
            SetFieldFromToken(
                instance,
                token,
                FieldNames.SupportRelationships,
                "SupportRelationships",
                serializer
            );

            // Additional persistent fields
            SetFieldFromToken(
                instance,
                token,
                FieldNames.ExperienceRanks,
                "ExperienceRanks",
                serializer
            );

            SetFieldFromToken(instance, token, FieldNames.CurrentClass, "CurrentClass", serializer);

            SetFieldFromToken(
                instance,
                token,
                FieldNames.EquippedClassHistory,
                "EquippedClassHistory",
                serializer
            );

            SetFieldFromToken(
                instance,
                token,
                FieldNames.ActiveStatusEffects,
                "ActiveStatusEffects",
                serializer
            );

            // Map position and flags
            SetFieldFromToken(
                instance,
                token,
                FieldNames.MapGridPosition,
                "MapGridPosition",
                serializer
            );

            SetFieldFromToken(
                instance,
                token,
                FieldNames.UseBattleModel,
                "UseBattleModel",
                serializer
            );
        }

        private void SetFieldFromToken(
            CharacterInstance instance,
            JObject token,
            string fieldName,
            string fallbackName,
            JsonSerializer serializer
        )
        {
            var fieldToken = token.SelectToken(fieldName) ?? token.SelectToken(fallbackName);
            if (fieldToken?.Type == JTokenType.Null || fieldToken == null)
            {
                return;
            }

            var field = typeof(CharacterInstance).GetField(fieldName, PrivateInstanceFlags);
            if (field == null)
            {
                return;
            }

            try
            {
                var value = fieldToken.ToObject(field.FieldType, serializer);
                field.SetValue(instance, value);
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning($"Failed to set field '{fieldName}': {ex.Message}");
#endif
            }
        }

        private object GetPrivateFieldValue(CharacterInstance instance, string fieldName)
        {
            try
            {
                var field = typeof(CharacterInstance).GetField(fieldName, PrivateInstanceFlags);
                return field?.GetValue(instance);
            }
            catch
            {
                return null;
            }
        }
    }
}
