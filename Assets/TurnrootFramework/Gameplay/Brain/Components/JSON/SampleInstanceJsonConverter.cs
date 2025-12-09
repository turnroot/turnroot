using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Generic JsonConverter template for Data->Instance types that require a constructor.
    /// This is a reusable converter pattern for any instance type backed by a ScriptableObject template.
    ///
    /// Usage:
    /// settings.Converters.Add(new SampleInstanceJsonConverter&lt;MyData, MyInstance&gt;());
    ///
    /// To customize for a specific type, either:
    /// 1. Use this generic converter directly, or
    /// 2. Create a concrete implementation (see ObjectItemInstanceJsonConverter for an example)
    /// </summary>
    /// <typeparam name="TData">The ScriptableObject data type (template)</typeparam>
    /// <typeparam name="TInstance">The instance type that wraps the data</typeparam>
    public class SampleInstanceJsonConverter<TData, TInstance> : JsonConverter
        where TInstance : class
        where TData : ScriptableObject
    {
        private const string TemplateField = "_template";

        public override bool CanConvert(Type objectType) =>
            typeof(TInstance).IsAssignableFrom(objectType);

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Default serialization - override in concrete implementations if needed
            throw new NotImplementedException(
                "SampleInstanceJsonConverter is read-only by default. "
                    + "Override WriteJson in a concrete implementation if write support is needed."
            );
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

            // Allow instances to perform post-deserialization cleanup
            if (instance is Turnroot.Serialization.IPostDeserialize post)
            {
                post.OnAfterDeserialize();
            }

            return instance;
        }

        /// <summary>
        /// Resolves the template (TData) from the JSON token.
        /// Override this method if your template is stored under a different field name.
        /// </summary>
        protected virtual TData ResolveTemplate(JObject token, JsonSerializer serializer)
        {
            var templateToken = token.SelectToken(TemplateField) ?? token.SelectToken("Template");

            if (templateToken?.Type == JTokenType.Null || templateToken == null)
            {
                return null;
            }

            try
            {
                return templateToken.ToObject<TData>(serializer);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"Failed to deserialize {typeof(TData).Name} template: {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Creates an instance of TInstance given a template.
        /// Override this method if your instance type uses a different constructor signature.
        /// </summary>
        protected virtual TInstance CreateInstance(TData template)
        {
            if (template != null)
            {
                // Try to find a constructor that takes TData
                var ctor = typeof(TInstance).GetConstructor(new[] { typeof(TData) });
                if (ctor != null)
                {
                    return (TInstance)ctor.Invoke(new object[] { template });
                }

                Debug.LogWarning(
                    $"No constructor found for {typeof(TInstance).Name} that takes {typeof(TData).Name}. "
                        + "Falling back to uninitialized object."
                );
            }

            // Fall back to uninitialized object
            return (TInstance)
                System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                    typeof(TInstance)
                );
        }
    }
}
