using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Example JsonConverter template for a Data->Instance type that requires a constructor.
    /// Adapt the type names and field mapping for your specific instance.
    /// </summary>
    public class SampleInstanceJsonConverter<TData, TInstance> : JsonConverter
        where TInstance : class
        where TData : ScriptableObject
    {
        public override bool CanConvert(System.Type objectType) =>
            typeof(TInstance).IsAssignableFrom(objectType);

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Let default serializer handle writing; override only if necessary
            throw new System.NotImplementedException(
                "This sample converter is read-only by default."
            );
        }

        public override object ReadJson(
            JsonReader reader,
            System.Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            var j = JObject.Load(reader);

            // Try to resolve the template (this assumes the template was serialized as a Unity object token)
            var templateToken = j.SelectToken("_template") ?? j.SelectToken("Template");
            TData template = null;
            if (templateToken != null && templateToken.Type != JTokenType.Null)
            {
                try
                {
                    template = templateToken.ToObject<TData>(serializer);
                }
                catch { }
            }

            TInstance instance = null;
            if (template != null)
            {
                // If you have a constructor like MyInstance(MyData template), invoke it reflectively
                var ctor = typeof(TInstance).GetConstructor(new[] { typeof(TData) });
                if (ctor != null)
                {
                    instance = (TInstance)ctor.Invoke(new object[] { template });
                }
                else
                {
                    // Fall back to uninitialized object if no constructor is found
                    instance = (TInstance)
                        System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                            typeof(TInstance)
                        );
                }
            }
            else
            {
                instance = (TInstance)
                    System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                        typeof(TInstance)
                    );
            }

            // Example: populate implementation-specific private fields with reflection
            // var t = typeof(TInstance);
            // var fi = t.GetField("_someField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            // if (fi != null) fi.SetValue(instance, j.SelectToken("_someField")?.ToObject(fi.FieldType, serializer));

            // Let instance perform any post-deserialize cleanup if it implements a post-deserialize hook
            if (instance is global::Turnroot.Serialization.IPostDeserialize post)
                post.OnAfterDeserialize();

            return instance;
        }
    }
}
