using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Gameplay.Objects;

namespace Assets.Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Ensures ObjectItemInstance is reconstructed via its constructor so the
    /// private template backing field is populated and any initialization runs.
    /// </summary>
    public class ObjectItemInstanceJsonConverter : JsonConverter
    {
        private const string TemplateField = "_template";

        public override bool CanConvert(Type objectType)
        {
            return typeof(ObjectItemInstance).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var instance = value as ObjectItemInstance;
            if (instance == null)
            {
                writer.WriteNull();
                return;
            }

            var token = SerializeInstance(instance, serializer);
            token.WriteTo(writer);
        }

        private JObject SerializeInstance(ObjectItemInstance instance, JsonSerializer serializer)
        {
            var token = new JObject();

            var template = instance.Template;
            if (template != null)
            {
                token[TemplateField] = JToken.FromObject(template, serializer);
            }

            return token;
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            var token = JObject.Load(reader);
            var template = ResolveTemplate(token, serializer);
            var instance = CreateInstance(template);

            if (instance is global::Turnroot.Serialization.IPostDeserialize post)
            {
                post.OnAfterDeserialize();
            }

            return instance;
        }

        private ObjectItem ResolveTemplate(JObject token, JsonSerializer serializer)
        {
            var templateToken = token.SelectToken(TemplateField) ?? token.SelectToken("Template");

            if (templateToken?.Type == JTokenType.Null || templateToken == null)
            {
                return null;
            }

            try
            {
                return templateToken.ToObject<ObjectItem>(serializer);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    $"Failed to deserialize ObjectItem template: {ex.Message}"
                );
                return null;
            }
        }

        private ObjectItemInstance CreateInstance(ObjectItem template)
        {
            if (template != null)
            {
                return new ObjectItemInstance(template);
            }

            // Create uninitialized instance if no template available
            return (ObjectItemInstance)
                System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                    typeof(ObjectItemInstance)
                );
        }
    }
}
