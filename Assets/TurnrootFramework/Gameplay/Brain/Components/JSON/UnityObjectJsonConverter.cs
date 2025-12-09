using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Serializes UnityEngine.Object references to a small JSON token containing type, name, and asset path.
    /// Attempts to resolve references via GUID/path in editor, or Resources.Load at runtime.
    /// </summary>
    public class UnityObjectJsonConverter : JsonConverter
    {
        private const string UnityMarker = "__unity";
        private const string TypeField = "type";
        private const string NameField = "name";
        private const string GuidField = "guid";
        private const string AssetPathField = "assetPath";

        public override bool CanConvert(Type objectType)
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
            var token = CreateUnityObjectToken(obj);
            token.WriteTo(writer);
        }

        private JObject CreateUnityObjectToken(UnityEngine.Object obj)
        {
            var token = new JObject
            {
                [UnityMarker] = true,
                [TypeField] = obj.GetType().AssemblyQualifiedName,
                [NameField] = obj.name,
            };

#if UNITY_EDITOR
            AddEditorMetadata(token, obj);
#endif

            return token;
        }

#if UNITY_EDITOR
        private void AddEditorMetadata(JObject token, UnityEngine.Object obj)
        {
            try
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    token[AssetPathField] = path;

                    try
                    {
                        token[GuidField] = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to get GUID for asset at {path}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get asset path: {ex.Message}");
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
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var token = JObject.Load(reader);
            return token?[UnityMarker] == null ? null : ResolveUnityObject(token, objectType);
        }

        private object ResolveUnityObject(JObject token, Type objectType)
        {
            var typeName = token.Value<string>(TypeField);
            var name = token.Value<string>(NameField);
            var guid = token.Value<string>(GuidField);
            var assetPath = token.Value<string>(AssetPathField);

            var targetType = ResolveType(typeName, objectType);
            if (targetType == null)
            {
                return null;
            }

#if UNITY_EDITOR
            var editorAsset = TryLoadFromEditor(guid, assetPath, targetType);
            if (editorAsset != null)
            {
                return editorAsset;
            }
#endif

            return TryLoadFromResources(name, targetType);
        }

        private Type ResolveType(string typeName, Type fallbackType)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return fallbackType;
            }

            try
            {
                return Type.GetType(typeName) ?? Type.GetType(typeName.Split(',')[0]);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to resolve type '{typeName}': {ex.Message}");
                return fallbackType;
            }
        }

#if UNITY_EDITOR
        private UnityEngine.Object TryLoadFromEditor(string guid, string assetPath, Type targetType)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                var asset = LoadAssetByGuid(guid, targetType);
                if (asset != null)
                {
                    return asset;
                }
            }

            return !string.IsNullOrEmpty(assetPath) ? LoadAssetByPath(assetPath, targetType) : null;
        }

        private UnityEngine.Object LoadAssetByGuid(string guid, Type targetType)
        {
            try
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath(path, targetType);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load asset by GUID '{guid}': {ex.Message}");
            }
            return null;
        }

        private UnityEngine.Object LoadAssetByPath(string assetPath, Type targetType)
        {
            try
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, targetType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load asset at path '{assetPath}': {ex.Message}");
                return null;
            }
        }
#endif

        private UnityEngine.Object TryLoadFromResources(string name, Type targetType)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                return Resources.Load(name, targetType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load resource '{name}': {ex.Message}");
                return null;
            }
        }
    }
}
