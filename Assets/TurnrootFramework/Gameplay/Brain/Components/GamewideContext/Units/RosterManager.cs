using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
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
        private readonly Brain _brain;
        private readonly CharacterFactory _characterFactory;
        private readonly RosterPersistence _persistence;
        private readonly CharacterPersistence _characterPersistence;

        // Explicitly track persistent rosters we create
        private readonly List<GenericRosterInstance> _persistentRosters = new();

        private PlayerTeamRosterInstance _persistentPlayerRoster = null;

        public RosterManager(Brain brain, RosterPersistence persistence = null)
        {
            _brain = brain;
            _characterFactory = new CharacterFactory(brain.GetComponent<LongTermMemory>());
            _characterPersistence = new CharacterPersistence(brain);
            _persistence = persistence;
        }

        #region Roster Instantiation

        public GenericRosterInstance InstantiateGenericRoster(GenericRoster roster, bool register)
        {
            if (roster == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Cannot instantiate null roster");
#endif
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
                TurnrootLogger.Log($"Roster '{roster.name}' already populated, skipping");

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

            // Initialize runtime placements copy so runtime modifications don't hit the template
            instance.InitializeRuntimePlacementsFromTemplate();

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
                TurnrootLogger.Log(
                    "Cannot instantiate null player team roster",
                    TurnrootLogger.LogLevel.Warning
                );
                _brain?.PublishRostersFailed();
                return null;
            }

            if (_persistentPlayerRoster != null && _persistentPlayerRoster.roster == roster)
            {
                return _persistentPlayerRoster;
            }

            var go = new GameObject($"PlayerTeamRosterInstance - {roster.name}");
            var instance = go.AddComponent<PlayerTeamRosterInstance>();
            instance.roster = roster;

            // Initialize runtime copy of placements so we don't mutate the template
            instance.InitializeRuntimePlacementsFromTemplate();

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

            // Use runtime placements if the instance has them, otherwise fall back to the template
            var placements = instance.GetPlacements();

            foreach (var unit in placements)
            {
                if (unit.CharacterData == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"PopulateRoster: Skipping placement with null CharacterData in '{instance.name}'"
                    );
#endif
                    TurnrootLogger.Log(
                        $"PopulateRoster: Skipping placement with null CharacterData in '{instance.name}'",
                        TurnrootLogger.LogLevel.Warning
                    );
                    continue;
                }

                var character = _characterFactory.CreateOrRecall(unit.CharacterData);
                if (character != null)
                {
                    characters.Add(character);

                    // Persist unique characters explicitly (factory no longer auto-saves)
                    if (character.CharacterTemplate?.IsUnique == true)
                    {
                        _characterPersistence.SaveCharacter(character, updateIndex: true);
                    }
                }
            }

            instance.AddInstances(characters);
            TurnrootLogger.Log($"Populated '{instance.name}' with {characters.Count} characters");
        }

        private void PopulatePlayerTeamRoster(
            PlayerTeamRosterInstance instance,
            PlayerTeamRoster roster
        )
        {
            var characters = new List<CharacterInstance>();

            // Use runtime placements on the instance if available (will be initialized by caller)
            var placements = instance.GetPlacements();

            foreach (var unit in placements)
            {
                if (unit == null)
                {
                    continue;
                }

                if (unit.CharacterData == null)
                {
                    TurnrootLogger.Log(
                        $"PopulatePlayerTeamRoster: Skipping placement with null CharacterData in '{instance.name}'",
                        TurnrootLogger.LogLevel.Error
                    );
                    continue;
                }

                var character = _characterFactory.CreateOrRecall(unit.CharacterData);
                if (character != null)
                {
                    characters.Add(character);

                    if (character.CharacterTemplate?.IsUnique == true)
                    {
                        _characterPersistence.SaveCharacter(character, updateIndex: true);
                    }
                }
            }

            instance.AddInstances(characters);
        }

        /// <summary>
        /// Apply decoded persistent player roster payload onto an existing runtime instance.
        /// This will overwrite runtime placements and repopulate instances from decoded data.
        /// </summary>
        public void ApplyDecodedPlayerRoster(
            PlayerTeamRosterInstance instance,
            PlayerTeamRoster decoded
        )
        {
            if (instance == null || decoded == null)
            {
                return;
            }

            // Apply placements first so PopulatePlayerTeamRoster uses them
            instance.ApplyDecodedPlacements(decoded.characters);

            // Clear existing instances and repopulate
            instance.Clear();
            PopulatePlayerTeamRoster(instance, decoded);
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
                TurnrootLogger.Log(
                    "No rosters configured to recall",
                    TurnrootLogger.LogLevel.Warning
                );
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
                TurnrootLogger.Log(
                    "No player team roster configured to recall",
                    TurnrootLogger.LogLevel.Warning
                );
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

        /// <summary>
        /// Returns the currently active runtime PlayerTeamRosterInstance if any.
        /// </summary>
        public PlayerTeamRosterInstance GetPersistentPlayerRosterInstance() =>
            _persistentPlayerRoster;

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

        private List<GenericRosterInstance> GetCachedInstances() =>
            // No searching needed - we tracked them as we created them
            _persistentRosters;

        #endregion
    }
}
