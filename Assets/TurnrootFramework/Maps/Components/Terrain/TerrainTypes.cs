using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

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
        if (_types != null)
        {
            foreach (var type in _types)
            {
                if (type == null)
                    continue;
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
                        idField.SetValue(type, System.Guid.NewGuid().ToString());
                }
                if (!string.IsNullOrEmpty(type.Id))
                {
                    _typeLookup[type.Id] = type;
                }
            }
        }
    }

    private void OnValidate()
    {
        // Ensure the lookup is updated when the asset is modified
        OnEnable();
    }

    private void OnDisable()
    {
        _typeLookup.Clear();
    }

    public void AddType(string name, Color editorColor)
    {
        var newType = new TerrainType(name, 1f, 1f, 1f, 1f, 1f, 1, 1, 1, editorColor);
        // ensure id
        var idField = typeof(TerrainType).GetField(
            "_id",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        if (idField != null)
            idField.SetValue(newType, System.Guid.NewGuid().ToString());
        var newList = new List<TerrainType>(_types ?? new TerrainType[0]) { newType };
        _types = newList.ToArray();
        // update lookup
        if (!string.IsNullOrEmpty(newType.Id))
            _typeLookup[newType.Id] = newType;
    }

    public TerrainType GetTypeById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        if (_typeLookup != null && _typeLookup.TryGetValue(id, out var t))
            return t;
        // fallback: search array
        if (_types != null)
        {
            foreach (var tt in _types)
            {
                if (tt != null && tt.Id == id)
                    return tt;
            }
        }
        return null;
    }

    // Runtime & Editor-friendly loader for the shared TerrainTypes asset.
    // At runtime this tries `Resources.Load<TerrainTypes>(resourcesName)`.
    // In the Editor it also falls back to searching the AssetDatabase.
    public static TerrainTypes LoadDefault(string resourcesName = "TerrainTypes")
    {
        // First try a direct Resources.Load for the given resource name (preserve existing behaviour)
        var fromResources = Resources.Load<TerrainTypes>(resourcesName);
        if (fromResources != null)
            return fromResources;

        // Use centralized loader which searches Resources/GameSettings/* and falls back to editor search
        var byLoader = Turnroot.Utilities.GameSettingsLoader.LoadFirst<TerrainTypes>(
            "GameSettings"
        );
        if (byLoader != null)
            return byLoader;

        return null;
    }
}
