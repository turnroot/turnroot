using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Runtime component that holds the runtime instances for a `Roster` ScriptableObject.
    /// </summary>
    public class GenericRosterInstance : MonoBehaviour
    {
        [SerializeField]
        public GenericRoster roster;
        private readonly List<CharacterInstance> _instances = new();
        public IReadOnlyList<CharacterInstance> Instances => _instances;

        public CharacterInstance GetInstanceFor(CharacterData data) =>
            _instances.Find(i => i.CharacterTemplate == data);

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

        public void AddInstances(IEnumerable<CharacterInstance> instances)
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

        public void Clear() => _instances.Clear();
    }
}
