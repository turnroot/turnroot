using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Runtime component that holds the runtime instances for a `Roster` ScriptableObject.
    /// </summary>
    public class RosterInstance : MonoBehaviour
    {
        [SerializeField]
        public Roster roster;
        private readonly List<CharacterInstance> _instances = new();
        public IReadOnlyList<CharacterInstance> Instances => _instances;

#if UNITY_EDITOR
        // Initializer for tests
        public void InitializeFromRoster(Roster roster)
        {
            this.roster = roster;
            _instances.Clear();

            if (roster?.characters == null)
            {
                return;
            }

            foreach (var characterData in roster.characters)
            {
                if (characterData == null)
                {
                    continue;
                }

                _instances.Add(CharacterInstance.Create(characterData));
            }
        }
#endif

        // Lookup helper
        public CharacterInstance GetInstanceFor(CharacterData data) => _instances.Find(i => i.CharacterTemplate == data);

        // Runtime API to add instances after construction. Used by GamewideContextBrain
        // to auto-register instances created from a Roster ScriptableObject.
        public void AddInstance(CharacterInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            if (_instances.Contains(instance))
            {
                return;
            }

            _instances.Add(instance);
        }

        public void AddInstances(
            System.Collections.Generic.IEnumerable<CharacterInstance> instances
        )
        {
            if (instances == null)
            {
                return;
            }

            foreach (var inst in instances)
            {
                AddInstance(inst);
            }
        }

        /// <summary>
        /// Clears all instances from this roster. Used for temporary battle rosters.
        /// </summary>
        public void Clear() => _instances.Clear();
    }
}
