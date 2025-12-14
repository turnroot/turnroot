using System;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// The roster ScriptableObject holds a list of characters in a roster.
    /// The scriptable object is for pre-gameplay configuration,
    /// while the RosterInstance holds the runtime instance of the roster.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoster", menuName = "Turnroot/Roster")]
    public class Roster : ScriptableObject
    {
        [SerializeField]
        [Tooltip(
            "How this roster is identified durably at runtime. Set this with something human readable if you want, or leave it blank to auto-generate"
        )]
        private string _id;

        [Tooltip(
            "How this roster is identified durably at runtime. Set this with something human readable if you want, or leave it blank to auto-generate"
        )]
        public string Id
        {
            get => _id;
            private set => _id = value;
        }

        [ReorderableList]
        public CharacterData[] characters;

#if UNITY_EDITOR
        private void OnValidate()
        {
            try
            {
                if (string.IsNullOrEmpty(Id))
                {
                    var path = UnityEditor.AssetDatabase.GetAssetPath(this);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            Id = guid;
                        }
                    }

                    if (string.IsNullOrEmpty(Id))
                    {
                        Id = Guid.NewGuid().ToString("N");
                    }

                    try
                    {
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                    catch
                    {
                        Debug.LogWarning("Could not mark Roster asset dirty to save generated ID.");
                    }
                }
            }
            catch
            {
                Debug.LogWarning("Could not auto-assign Roster ID.");
            }
        }
#else
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_id))
                _id = Guid.NewGuid().ToString("D");
        }
#endif
    }
}
