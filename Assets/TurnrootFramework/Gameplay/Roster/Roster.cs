using System;
using UnityEngine;

namespace Turnroot.Characters
{
    public class Roster : ScriptableObject
    {
        [SerializeField]
        [Tooltip(
            "How this roster is identified durably at runtime. Set this with something human readable if you want, or leave it blank to auto-generate"
        )]
        protected string _id;

        [Tooltip(
            "How this roster is identified durably at runtime. Set this with something human readable if you want, or leave it blank to auto-generate"
        )]
        public string Id
        {
            get => _id;
            set => _id = value;
        }

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
