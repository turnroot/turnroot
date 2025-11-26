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
        public override bool CanConvert(System.Type objectType)
        {
            return typeof(ObjectItemInstance).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var inst = value as ObjectItemInstance;
            if (inst == null)
            {
                writer.WriteNull();
                return;
            }

            var j = new JObject();

            // Write template using existing UnityObjectJsonConverter
            var template = inst.Template;
            if (template != null)
            {
                j["_template"] = JToken.FromObject(template, serializer);
            }

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

            var templateToken = j.SelectToken("_template") ?? j.SelectToken("Template");
            ObjectItem template = null;
            if (templateToken != null && templateToken.Type != JTokenType.Null)
            {
                try
                {
                    template = templateToken.ToObject<ObjectItem>(serializer);
                }
                catch { }
            }

            ObjectItemInstance instance = null;
            if (template != null)
            {
                instance = new ObjectItemInstance(template);
            }
            else
            {
                instance = (ObjectItemInstance)
                    System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                        typeof(ObjectItemInstance)
                    );
            }

            if (instance is global::Turnroot.Serialization.IPostDeserialize post)
                post.OnAfterDeserialize();

            return instance;
        }
    }
}
