using System.Collections.Generic;
using UnityEngine;
using static Turnroot.Characters.Roster;

namespace Turnroot.Characters
{
    /// <summary>
    /// Runtime component that holds the runtime instances for a `Roster` ScriptableObject.
    /// </summary>
    public class RosterInstance<T> : MonoBehaviour
        where T : Roster
    {
        [SerializeField]
        public T roster;
        private readonly List<CharacterInstance> _instances = new();
        public IReadOnlyList<CharacterInstance> Instances => _instances;

        public UnitPlacement GetPlacementFor(CharacterData data)
        {
            foreach (var placement in roster.characters)
            {
                if (placement.CharacterData == data)
                {
                    return placement;
                }
            }

            return default;
        }

        public void SetOrder(CharacterData data, int order)
        {
            for (int i = 0; i < roster.characters.Length; i++)
            {
                if (roster.characters[i].CharacterData == data)
                {
                    roster.characters[i].Order = order;
                    return;
                }
            }
        }

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
