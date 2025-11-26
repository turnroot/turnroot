using NaughtyAttributes;
using Turnroot.Characters;
using UnityEngine;

namespace Assets.Turnroot.Characters
{
    /// <summary>
    /// The roster ScriptableObject holds a list of characters in a roster.
    /// The scriptable object is for pre-gameplay configuration,
    /// while the RosterInstance holds the runtime instance of the roster.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoster", menuName = "Turnroot/Roster")]
    public class Roster : ScriptableObject
    {
        [ReorderableList]
        public CharacterData[] characters;
    }
}
