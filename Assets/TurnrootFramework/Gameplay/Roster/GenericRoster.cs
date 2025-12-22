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
    [CreateAssetMenu(fileName = "NewRoster", menuName = "Turnroot/Characters/Generic Roster")]
    public class GenericRoster : Roster
    {
        public enum UnitStatus
        {
            NotSpawned,
            Alive,
            Defeated,
        }

        [Serializable]
        public struct UnitPlacement
        {
            public CharacterData CharacterData;
            public Vector2Int SpawnPosition;
            public UnitStatus Status { get; private set; }

            public void SetStatus(UnitStatus newStatus) => Status = newStatus;

            public bool IsActiveRightNow { get; private set; }

            public void SetActiveRightNow(bool isActive) => IsActiveRightNow = isActive;
        }

        [ReorderableList]
        public UnitPlacement[] characters;
    }
}
