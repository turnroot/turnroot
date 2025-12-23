using System;
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

        /// <summary>
        /// Fired when the runtime roster contents or ordering are modified.
        /// Subscribers (e.g., RosterManager) can react and trigger persistence.
        /// </summary>
        public event Action OnRosterModified;

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
                    OnRosterModified?.Invoke();
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
            OnRosterModified?.Invoke();
        }

        public void AddInstances(IEnumerable<CharacterInstance> instances)
        {
            if (instances == null)
            {
                return;
            }

            bool anyAdded = false;
            foreach (var inst in instances)
            {
                if (!_instances.Contains(inst))
                {
                    _instances.Add(inst);
                    anyAdded = true;
                }
            }

            if (anyAdded)
            {
                OnRosterModified?.Invoke();
            }
        }

        public void Clear()
        {
            if (_instances.Count == 0)
                return;
            _instances.Clear();
            OnRosterModified?.Invoke();
        }
    }
}
