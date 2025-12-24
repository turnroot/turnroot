using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

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
        [ReorderableList]
        [FormerlySerializedAs("characters")]
        [SerializeField]
        private UnitPlacement[] _characters;

        public override UnitPlacement[] characters
        {
            get => _characters;
            set => _characters = value;
        }
    }
}
