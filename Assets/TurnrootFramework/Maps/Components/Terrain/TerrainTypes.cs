using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// ScriptableObject that manages the collection of terrain types available in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "Terrain Types", menuName = "Turnroot/Game Settings/Terrain Types")]
    public class TerrainTypes : ScriptableObject
    {
        [SerializeField]
        private TerrainType[] _types;

        public TerrainType[] Types => _types;

        [SerializeField]
        private Dictionary<string, TerrainType> _typeLookup = new();

        private void OnEnable()
        {
            _typeLookup = new Dictionary<string, TerrainType>();
            if (Types != null)
            {
                foreach (var type in Types)
                {
                    if (type == null)
                    {
                        continue;
                    }
                    // ensure each type has an id
                    var idField = typeof(TerrainType).GetField(
                        "_id",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                    if (idField != null)
                    {
                        var val = idField.GetValue(type) as string;
                        if (string.IsNullOrEmpty(val))
                        {
                            idField.SetValue(type, System.Guid.NewGuid().ToString());
                        }
                    }
                    if (!string.IsNullOrEmpty(type.Id))
                    {
                        _typeLookup[type.Id] = type;
                    }
                }
            }
        }

        private void OnValidate() => OnEnable();

        private void OnDisable() => _typeLookup.Clear();

        public void AddType(string name, Color editorColor)
        {
            var newType = new TerrainType(
                name,
                1f,
                1f,
                1f,
                1f,
                1f,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                editorColor
            );
            // ensure id
            var idField = typeof(TerrainType).GetField(
                "_id",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (idField != null)
            {
                idField.SetValue(newType, System.Guid.NewGuid().ToString());
            }

            var newList = new List<TerrainType>(Types ?? new TerrainType[0]) { newType };
            _types = newList.ToArray();
            if (!string.IsNullOrEmpty(newType.Id))
            {
                _typeLookup[newType.Id] = newType;
            }
        }

        public TerrainType GetTypeById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (_typeLookup != null && _typeLookup.TryGetValue(id, out var t))
            {
                return t;
            }
            // fallback: search array
            if (Types != null)
            {
                foreach (var tt in Types)
                {
                    if (tt != null && tt.Id == id)
                    {
                        return tt;
                    }
                }
            }
            return null;
        }

        public static TerrainTypes LoadDefault(string resourcesName = "TerrainTypes")
        {
            var fromResources = Resources.Load<TerrainTypes>(resourcesName);
            if (fromResources != null)
            {
                return fromResources;
            }

            var fromGameSettings = Resources.Load<TerrainTypes>("GameSettings/TerrainTypes");
            if (fromGameSettings != null)
            {
                return fromGameSettings;
            }

            var fromEssentialCores = Resources.Load<TerrainTypes>(
                "EssentialCores/GameSettings/Map/Terrain Types"
            );
            if (fromEssentialCores != null)
            {
                return fromEssentialCores;
            }

#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:TerrainTypes");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainTypes>(path);
            }
#endif

            return null;
        }
    }
}
