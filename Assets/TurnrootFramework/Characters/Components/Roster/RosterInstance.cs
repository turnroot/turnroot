using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Assets.Turnroot.Characters
{
    /// <summary>
    /// Runtime component that holds the runtime instances for a `Roster` ScriptableObject.
    /// </summary>
    public class RosterInstance : MonoBehaviour
    {
        [SerializeField]
        public Roster roster;
        private readonly List<CharacterInstance> _instances = new List<CharacterInstance>();
        public IReadOnlyList<CharacterInstance> Instances => _instances;

#if UNITY_EDITOR
        // Initializer for tests
        public void InitializeFromRoster(Roster roster)
        {
            this.roster = roster;
            _instances.Clear();

            if (roster?.characters == null)
                return;

            foreach (var characterData in roster.characters)
            {
                if (characterData == null)
                    continue;

                _instances.Add(CharacterInstance.Create(characterData));
            }
        }
#endif

        // Lookup helper
        public CharacterInstance GetInstanceFor(CharacterData data)
        {
            return _instances.Find(i => i.CharacterTemplate == data);
        }
    }
}
