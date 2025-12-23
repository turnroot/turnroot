using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles roster lifecycle: instantiation, caching, recall, lookup.
    /// Single responsibility: manage roster instances.
    /// </summary>
    public class RosterManager
    {
        private readonly Brain _brain;
        private readonly CharacterFactory _characterFactory;
        private readonly RosterPersistence _persistence;

        // Explicitly track persistent rosters we create
        private readonly List<GenericRosterInstance> _persistentRosters = new();

        private PlayerTeamRosterInstance _persistentPlayerRoster = null;

        public RosterManager(Brain brain, RosterPersistence persistence = null)
        {
            _brain = brain;
            _characterFactory = new CharacterFactory(brain.GetComponent<LongTermMemory>());
            _persistence = persistence;
        }

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
            return existing != null
                ? HandleExistingRoster(existing, roster, register)
                : CreateNewRosterInstance(roster, register);
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

            if (register && _persistence != null)
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

            _persistentRosters.Add(instance);

            PopulateRoster(instance, roster);

            if (register && _persistence != null)
            {
                _persistence.RegisterRoster(roster);
            }

            _brain?.PublishRostersReady();
            return instance;
        }

        public PlayerTeamRosterInstance InstantiatePlayerTeamRoster(PlayerTeamRoster roster)
        {
            if (roster == null)
            {
                Debug.LogWarning("Cannot instantiate null player team roster");
                _brain?.PublishRostersFailed();
                return null;
            }

            if (_persistentPlayerRoster != null && _persistentPlayerRoster.roster == roster)
            {
                Debug.Log($"Player team roster '{roster.name}' already exists, returning");
                return _persistentPlayerRoster;
            }

            var go = new GameObject($"PlayerTeamRosterInstance - {roster.name}");
            var instance = go.AddComponent<PlayerTeamRosterInstance>();
            instance.roster = roster;

            _persistentPlayerRoster = instance;

            PopulatePlayerTeamRoster(instance, roster);

            // Register the player roster in LTM if persistence is available
            if (_persistence != null)
            {
                _persistence.RegisterPlayerRoster(roster);
            }

            // Subscribe to runtime changes so we can request a save when roster mutates
            instance.OnRosterModified += () => _brain?.PublishSavePlayerRosterRequested();

            _brain?.PublishRostersReady();
            return instance;
        }

        private void PopulateRoster(GenericRosterInstance instance, GenericRoster roster)
        {
            var characters = new List<CharacterInstance>();

            foreach (var unit in roster.characters)
            {
                if (unit.CharacterData == null)
                {
                    continue;
                }

                var character = _characterFactory.CreateOrRecall(unit.CharacterData);
                if (character != null)
                {
                    characters.Add(character);
                }
            }

            instance.AddInstances(characters);
            Debug.Log($"Populated '{instance.name}' with {characters.Count} characters");
        }

        private void PopulatePlayerTeamRoster(
            PlayerTeamRosterInstance instance,
            PlayerTeamRoster roster
        )
        {
            var characters = new List<CharacterInstance>();

            foreach (var unit in roster.characters)
            {
                if (unit == null)
                {
                    continue;
                }

                var character = _characterFactory.CreateOrRecall(unit.CharacterData);
                if (character != null)
                {
                    characters.Add(character);
                }
            }

            instance.AddInstances(characters);
            Debug.Log($"Populated '{instance.name}' with {characters.Count} player characters");
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

            if (_persistence == null)
            {
                // No persistence available, just register all provided rosters
                RegisterAllRosters(rosters);
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

        public PlayerTeamRosterInstance RecallPlayerTeamRoster(PlayerTeamRoster roster)
        {
            if (roster == null)
            {
                Debug.LogWarning("No player team roster configured to recall");
                return null;
            }

            // Always instantiate a runtime instance for the given roster (creating if necessary)
            var instance = InstantiatePlayerTeamRoster(roster);

            // If this is the first time we've seen the roster, register it in LTM
            if (_persistence != null && !_persistence.HasPlayerRosterInLTM(roster))
            {
                _persistence.RegisterPlayerRoster(roster);
            }

            return instance;
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
            {
                return null;
            }

            // Check generic rosters
            var found = GetCachedInstances()
                .Select(r => r.GetInstanceFor(template))
                .FirstOrDefault(i => i != null);

            if (found != null)
            {
                return found;
            }

            // Check player roster too
            return _persistentPlayerRoster?.GetInstanceFor(template);
        }

        public List<CharacterInstance> GetAllActiveInstances()
        {
            var instances = GetCachedInstances()
                .Where(r => r?.Instances != null)
                .SelectMany(r => r.Instances)
                .ToList();

            // Include player roster instances too
            if (_persistentPlayerRoster?.Instances != null)
            {
                instances.AddRange(_persistentPlayerRoster.Instances);
            }

            return instances;
        }

        private List<GenericRosterInstance> GetCachedInstances()
        {
            // No searching needed - we tracked them as we created them
            return _persistentRosters;
        }

        #endregion
    }
}
