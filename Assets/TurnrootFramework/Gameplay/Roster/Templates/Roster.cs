using System;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Base class for roster ScriptableObjects that define character placement and status for teams.
    /// </summary>
    public abstract class Roster : ScriptableObject
    {
        /// <summary>
        /// Defines the current status of a unit in the roster.
        /// </summary>
        public enum UnitStatus
        {
            NotSpawned,
            Alive,
            Defeated,
        }

        /// <summary>
        /// Represents a single unit's placement data including spawn position, status, and order in the roster.
        /// </summary>
        [Serializable]
        public class UnitPlacement
        {
            public CharacterData CharacterData;
            public Vector2Int SpawnPosition;
            public UnitStatus Status { get; private set; }

            public void SetStatus(UnitStatus newStatus) => Status = newStatus;

            public bool IsActiveRightNow { get; private set; }

            public void SetActiveRightNow(bool isActive) => IsActiveRightNow = isActive;

            public int Order;
        }

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

        /// <summary>
        /// Specialized roster types must provide their underlying placement array via this property.
        /// This allows derived classes to expose typed serialized arrays while letting code work
        /// with the general UnitPlacement type.
        /// </summary>
        public abstract UnitPlacement[] characters { get; set; }

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
#if UNITY_EDITOR
                        Debug.LogWarning("Could not mark Roster asset dirty to save generated ID.");
#endif
                    }
                }
            }
            catch
            {
#if UNITY_EDITOR
                Debug.LogWarning("Could not auto-assign Roster ID.");
#endif
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
