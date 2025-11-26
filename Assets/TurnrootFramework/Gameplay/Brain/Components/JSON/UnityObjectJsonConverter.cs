using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Serializes UnityEngine.Object references to a small JSON token containing type, name, and asset path.
    /// </summary>
    public class UnityObjectJsonConverter : JsonConverter
    {
        public override bool CanConvert(System.Type objectType)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var obj = (UnityEngine.Object)value;
            var j = new JObject
            {
                ["__unity"] = true,
                ["type"] = value.GetType().AssemblyQualifiedName,
                ["name"] = obj.name,
            };

#if UNITY_EDITOR
            try
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    j["assetPath"] = path;
                    try
                    {
                        j["guid"] = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                    }
                    catch { }
                }
            }
            catch { }
#endif

            j.WriteTo(writer);
        }

        public override object ReadJson(
            JsonReader reader,
            System.Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var j = JObject.Load(reader);
            if (j == null || j["__unity"] == null)
                return null;

            // Try to resolve by GUID/path (editor), or by Resources load (runtime by path/name)
            try
            {
                var typeName = j.Value<string>("type");
                var name = j.Value<string>("name");
                var guid = j.Value<string>("guid");
                var assetPath = j.Value<string>("assetPath");

                System.Type targetType = null;
                if (!string.IsNullOrEmpty(typeName))
                {
                    targetType =
                        System.Type.GetType(typeName)
                        ?? System.Type.GetType(typeName.Split(',')[0]);
                }

#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(guid))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path) && targetType != null)
                    {
                        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath(path, targetType);
                        if (asset != null)
                            return asset;
                    }
                }
                if (!string.IsNullOrEmpty(assetPath) && targetType != null)
                {
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, targetType);
                    if (asset != null)
                        return asset;
                }
#endif

                // Runtime fallback: try Resources.Load by name (assumes asset placed in Resources and name unique)
                if (!string.IsNullOrEmpty(name) && targetType != null)
                {
                    var resource = Resources.Load(name, targetType);
                    if (resource != null)
                        return resource;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    $"UnityObjectJsonConverter: failed to resolve object reference: {e.Message}"
                );
            }

            return null;
        }
    }
}
