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
#if UNITY_EDITOR
            // In the editor, prefer any asset outside Assets/TurnrootFramework/ so a project-level
            // override takes precedence over the package default without needing a Resources folder.
            var guids = UnityEditor.AssetDatabase.FindAssets("t:TerrainTypes");
            string fallbackPath = null;
            foreach (var guid in guids)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!p.StartsWith("Assets/TurnrootFramework/"))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainTypes>(p);
                }

                fallbackPath ??= p;
            }
            return fallbackPath != null ? UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainTypes>(fallbackPath) : null;
#else
            // At runtime, check a well-known override path first so the project can shadow the
            // package default by placing a TerrainTypes asset at:
            //   Assets/<anywhere>/Resources/TurnrootOverrides/TerrainTypes.asset
            var fromOverride = Resources.Load<TerrainTypes>("TurnrootOverrides/TerrainTypes");
            if (fromOverride != null)
                return fromOverride;

            var fromResources = Resources.Load<TerrainTypes>(resourcesName);
            if (fromResources != null)
                return fromResources;

            var fromGameSettings = Resources.Load<TerrainTypes>("GameSettings/TerrainTypes");
            if (fromGameSettings != null)
                return fromGameSettings;

            return Resources.Load<TerrainTypes>("EssentialCores/GameSettings/Map/Terrain Types");
#endif
        }
    }
}
