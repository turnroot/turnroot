using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;

namespace Assets.Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Custom converter that reconstructs CharacterInstance using its constructor and populates runtime/private fields reflectively.
    /// </summary>
    public class CharacterInstanceJsonConverter : JsonConverter
    {
        public override bool CanConvert(System.Type objectType)
        {
            return typeof(CharacterInstance).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var instance = value as CharacterInstance;
            if (instance == null)
            {
                writer.WriteNull();
                return;
            }

            var j = new JObject();

            // id
            j["_id"] = instance.Id;

            // Character template reference -> write as compact unity token
            var template = instance.CharacterTemplate;
            if (template != null)
            {
                var tkn = new JObject
                {
                    ["__unity"] = true,
                    ["type"] = template.GetType().AssemblyQualifiedName,
                    ["name"] = template.name,
                };
#if UNITY_EDITOR
                try
                {
                    var path = UnityEditor.AssetDatabase.GetAssetPath(template);
                    if (!string.IsNullOrEmpty(path))
                    {
                        tkn["assetPath"] = path;
                        try
                        {
                            tkn["guid"] = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                        }
                        catch { }
                    }
                }
                catch { }
#endif
                j["_characterTemplate"] = tkn;
            }

            // other runtime fields
            j["_currentLevel"] = JToken.FromObject(instance.CurrentLevel, serializer);
            j["_currentExp"] = JToken.FromObject(instance.CurrentExp, serializer);
            j["_runtimeBoundedStats"] = JToken.FromObject(instance.RuntimeBoundedStats, serializer);
            j["_runtimeUnboundedStats"] = JToken.FromObject(
                instance.RuntimeUnboundedStats,
                serializer
            );
            j["_inventoryInstance"] = JToken.FromObject(instance.InventoryInstance, serializer);
            j["_skillInstances"] = JToken.FromObject(instance.SkillInstances, serializer);
            j["_supportRelationships"] = JToken.FromObject(
                instance
                    .GetType()
                    .GetField(
                        "_supportRelationships",
                        System.Reflection.BindingFlags.Instance
                            | System.Reflection.BindingFlags.NonPublic
                    )
                    ?.GetValue(instance),
                serializer
            );

            j.WriteTo(writer);
        }

        public override object ReadJson(
            JsonReader reader,
            System.Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            var j = JObject.Load(reader);
            JToken templateToken =
                j.SelectToken("_characterTemplate") ?? j.SelectToken("CharacterTemplate");
            CharacterData template = null;
            if (templateToken != null && templateToken.Type != JTokenType.Null)
            {
                try
                {
                    template = templateToken.ToObject<CharacterData>(serializer);
                }
                catch { }
            }

            CharacterInstance instance = null;
            if (template != null)
            {
                instance = new CharacterInstance(template);
            }
            else
            {
                instance = (CharacterInstance)
                    System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                        typeof(CharacterInstance)
                    );
            }

            var t = typeof(CharacterInstance);
            void SetField(string fieldName, JToken token)
            {
                if (token == null || token.Type == JTokenType.Null)
                    return;
                var fi = t.GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                );
                if (fi == null)
                    return;
                var val = token.ToObject(fi.FieldType, serializer);
                fi.SetValue(instance, val);
            }

            SetField("_id", j.SelectToken("_id") ?? j.SelectToken("Id"));
            SetField(
                "_currentLevel",
                j.SelectToken("_currentLevel") ?? j.SelectToken("CurrentLevel")
            );
            SetField("_currentExp", j.SelectToken("_currentExp") ?? j.SelectToken("CurrentExp"));
            SetField(
                "_runtimeBoundedStats",
                j.SelectToken("_runtimeBoundedStats") ?? j.SelectToken("RuntimeBoundedStats")
            );
            SetField(
                "_runtimeUnboundedStats",
                j.SelectToken("_runtimeUnboundedStats") ?? j.SelectToken("RuntimeUnboundedStats")
            );
            SetField(
                "_inventoryInstance",
                j.SelectToken("_inventoryInstance") ?? j.SelectToken("InventoryInstance")
            );
            SetField(
                "_skillInstances",
                j.SelectToken("_skillInstances") ?? j.SelectToken("SkillInstances")
            );
            SetField(
                "_supportRelationships",
                j.SelectToken("_supportRelationships") ?? j.SelectToken("SupportRelationships")
            );

            instance?.OnAfterDeserialize();
            return instance;
        }
    }
}
