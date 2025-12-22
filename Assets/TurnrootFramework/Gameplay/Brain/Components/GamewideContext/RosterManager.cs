using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles roster lifecycle: instantiation, caching, recall, lookup.
    /// Single responsibility: manage roster instances.
    /// </summary>
    public class RosterManager
    {
        private readonly GamewideContextBrain _gwcb;
        private readonly Brain _brain;
        private readonly CharacterFactory _characterFactory;
        private readonly RosterPersistence _persistence;

        private readonly SingleValueCache<List<GenericRosterInstance>> _rosterCache = new();

        public RosterManager(GamewideContextBrain gwcb, Brain brain)
        {
            _gwcb = gwcb;
            _brain = brain;
            _characterFactory = new CharacterFactory(gwcb);
            _persistence = new RosterPersistence(gwcb.GetComponent<LongTermMemory>());
        }

        public void OnRostersReady() => InvalidateCache();

        public void InvalidateCache() => _rosterCache.Invalidate();

        public void InvalidateCache(CharacterInstance _) => InvalidateCache();

        #region Roster Instantiation

        public GenericRosterInstance InstantiateGenericRoster(GenericRoster roster, bool register)
        {
            if (roster == null)
            {
                Debug.LogWarning("Cannot instantiate null roster");
                _brain?.PublishRostersFailed();
                return null;
            }

            var existing = FindExistingRosterInstance(roster);
            if (existing != null)
            {
                return HandleExistingRoster(existing, roster, register);
            }

            return CreateNewRosterInstance(roster, register);
        }

        private GenericRosterInstance FindExistingRosterInstance(GenericRoster roster) =>
            GetCachedInstances().FirstOrDefault(r => r?.roster == roster);

        private GenericRosterInstance HandleExistingRoster(
            GenericRosterInstance existing,
            GenericRoster roster,
            bool register
        )
        {
            if (HasInstancesPopulated(existing, roster))
            {
                Debug.Log($"Roster '{roster.name}' already populated, skipping");
                return existing;
            }

            PopulateRoster(existing, roster);

            if (register)
            {
                _persistence.RegisterRoster(roster);
            }

            _brain?.PublishRostersReady();
            return existing;
        }

        private GenericRosterInstance CreateNewRosterInstance(GenericRoster roster, bool register)
        {
            var go = new GameObject($"RosterInstance - {roster.name}");
            var instance = go.AddComponent<GenericRosterInstance>();
            instance.roster = roster;

            PopulateRoster(instance, roster);

            if (register)
            {
                _persistence.RegisterRoster(roster);
            }

            _brain?.PublishRostersReady();
            return instance;
        }

        private void PopulateRoster(GenericRosterInstance instance, GenericRoster roster)
        {
            var characters = new List<CharacterInstance>();

            foreach (var unit in roster.characters)
            {
                if (unit.CharacterData == null)
                    continue;

                var character = _characterFactory.CreateOrRecall(unit.CharacterData);
                if (character != null)
                {
                    characters.Add(character);
                }
            }

            instance.AddInstances(characters);
            Debug.Log($"Populated '{instance.name}' with {characters.Count} characters");
        }

        private bool HasInstancesPopulated(GenericRosterInstance instance, GenericRoster roster)
        {
            return roster.characters.Any(cd =>
                cd.CharacterData != null && instance.GetInstanceFor(cd.CharacterData) != null
            );
        }

        #endregion

        #region Roster Recall

        public void RecallGenericRosters(List<GenericRoster> rosters)
        {
            if (rosters == null || rosters.Count == 0)
            {
                Debug.LogWarning("No rosters configured to recall");
                return;
            }

            var indexedRosters = _persistence.GetIndexedRosterIds();

            if (indexedRosters.Count > 0)
            {
                RecallFromIndex(rosters, indexedRosters);
            }
            else
            {
                RegisterAllRosters(rosters);
            }
        }

        public void RecallPlayerRoster(PlayerTeamRoster roster)
        {
            // TODO: Implement player-specific recall logic
            Debug.Log("Player roster recall not yet implemented");
        }

        private void RecallFromIndex(List<GenericRoster> rosters, List<string> indexedIds)
        {
            foreach (var id in indexedIds)
            {
                var roster = rosters.FirstOrDefault(r => r?.Id == id);
                if (roster != null && _persistence.HasRosterInLTM(roster))
                {
                    InstantiateGenericRoster(roster, register: false);
                }
            }
        }

        private void RegisterAllRosters(List<GenericRoster> rosters)
        {
            foreach (var roster in rosters.Where(r => r != null))
            {
                InstantiateGenericRoster(roster, register: true);
            }
        }

        #endregion

        #region Instance Lookup

        public CharacterInstance FindInstanceByTemplate(CharacterData template)
        {
            if (template == null)
                return null;

            return GetCachedInstances()
                .Select(r => r.GetInstanceFor(template))
                .FirstOrDefault(i => i != null);
        }

        public List<CharacterInstance> GetAllActiveInstances() =>
            GetCachedInstances()
                .Where(r => r?.Instances != null)
                .SelectMany(r => r.Instances)
                .ToList();

        private List<GenericRosterInstance> GetCachedInstances()
        {
            return _rosterCache.GetOrCompute(() =>
            {
                var rosters = UnityEngine.Object.FindObjectsByType<GenericRosterInstance>(
                    FindObjectsSortMode.None
                );
                var instances = rosters.Where(r => r != null).ToList();
                Debug.Log($"Roster cache refreshed: {instances.Count} active");
                return instances;
            });
        }

        #endregion
    }
}
