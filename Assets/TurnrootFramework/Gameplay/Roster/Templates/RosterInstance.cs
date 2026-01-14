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

        // Runtime copy of placements so we never mutate the ScriptableObject template at runtime
        private UnitPlacement[] _runtimePlacements = null;

        private readonly List<CharacterInstance> _instances = new();
        public IReadOnlyList<CharacterInstance> Instances => _instances;

        /// <summary>
        /// Fired when the runtime roster contents or ordering are modified.
        /// Subscribers (e.g., RosterManager) can react and trigger persistence.
        /// </summary>
        public event Action OnRosterModified;

        /// <summary>
        /// Initialize a runtime copy of placements from the template. Call this when creating an instance.
        /// </summary>
        public void InitializeRuntimePlacementsFromTemplate()
        {
            if (roster?.characters == null)
            {
                _runtimePlacements = new UnitPlacement[0];
                return;
            }

            // Deep copy placements so changes affect only the runtime instance
            _runtimePlacements = new UnitPlacement[roster.characters.Length];
            for (int i = 0; i < roster.characters.Length; i++)
            {
                var src = roster.characters[i];
                _runtimePlacements[i] = new UnitPlacement
                {
                    CharacterData = src.CharacterData,
                    SpawnPosition = src.SpawnPosition,
                    Order = src.Order,
                };

                // Use setters for private set properties
                _runtimePlacements[i].SetStatus(src.Status);
                _runtimePlacements[i].SetActiveRightNow(src.IsActiveRightNow);
            }
        }

        /// <summary>
        /// Apply placements from a decoded payload (e.g., from LTM) into this runtime instance.
        /// This overwrites any existing runtime placements.
        /// </summary>
        public void ApplyDecodedPlacements(UnitPlacement[] placements)
        {
            if (placements == null)
            {
                _runtimePlacements = new UnitPlacement[0];
            }
            else
            {
                // Clone to ensure we own the array
                _runtimePlacements = new UnitPlacement[placements.Length];
                for (int i = 0; i < placements.Length; i++)
                {
                    var src = placements[i];
                    _runtimePlacements[i] = new UnitPlacement
                    {
                        CharacterData = src.CharacterData,
                        SpawnPosition = src.SpawnPosition,
                        Order = src.Order,
                    };

                    // Use setters for private set properties
                    _runtimePlacements[i].SetStatus(src.Status);
                    _runtimePlacements[i].SetActiveRightNow(src.IsActiveRightNow);
                }
            }

            OnRosterModified?.Invoke();
        }

        /// <summary>
        /// Returns the current placements for this instance: runtime copy if present, otherwise the template placements.
        /// </summary>
        public UnitPlacement[] GetPlacements() =>
            _runtimePlacements != null
                ? _runtimePlacements
                : roster?.characters ?? new UnitPlacement[0];

        public UnitPlacement GetPlacementFor(CharacterData data)
        {
            if (_runtimePlacements != null)
            {
                foreach (var placement in _runtimePlacements)
                {
                    if (placement.CharacterData == data)
                    {
                        return placement;
                    }
                }
            }

            // Fallback to template if no runtime placements exist
            if (roster?.characters != null)
            {
                foreach (var placement in roster.characters)
                {
                    if (placement.CharacterData == data)
                    {
                        return placement;
                    }
                }
            }

            return default;
        }

        public void SetOrder(CharacterData data, int order)
        {
            // Ensure runtime placements exist to avoid mutating the template
            if (_runtimePlacements == null)
            {
                InitializeRuntimePlacementsFromTemplate();
            }

            for (int i = 0; i < _runtimePlacements.Length; i++)
            {
                if (_runtimePlacements[i].CharacterData == data)
                {
                    _runtimePlacements[i].Order = order;
                    OnRosterModified?.Invoke();
                    return;
                }
            }

#if UNITY_EDITOR
            Debug.LogWarning($"SetOrder: Character {data?.name} not found in roster");
#endif
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
            {
                return;
            }

            _instances.Clear();
            OnRosterModified?.Invoke();
        }
    }
}
