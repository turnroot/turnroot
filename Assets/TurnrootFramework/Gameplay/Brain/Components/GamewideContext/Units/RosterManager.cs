using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages roster instantiation, population, and character persistence for both generic and player team rosters.
    /// </summary>
    public class RosterManager
    {
        private readonly Brain _brain;
        private readonly CharacterFactory _characterFactory;
        private readonly RosterPersistence _persistence;
        private readonly CharacterPersistence _characterPersistence;
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

        public OperationResult<GenericRosterInstance> InstantiateGenericRoster(
            GenericRoster roster,
            bool register
        )
        {
            var validation = OperationResultGuards.RequireNotNull(roster, nameof(roster));
            if (!validation.Success)
            {
                _brain.PublishRostersFailed();
                return OperationResult<GenericRosterInstance>.Failure(validation.ErrorMessage);
            }

            var existing = FindExistingRosterInstance(roster);
            return existing != null
                ? HandleExistingRoster(existing, roster, register)
                : CreateNewRosterInstance(roster, register);
        }

        private GenericRosterInstance FindExistingRosterInstance(GenericRoster roster) =>
            GetCachedInstances().FirstOrDefault(r => r?.roster == roster);

        private OperationResult<GenericRosterInstance> HandleExistingRoster(
            GenericRosterInstance existing,
            GenericRoster roster,
            bool register
        )
        {
            if (HasInstancesPopulated(existing, roster))
            {
                return OperationResult<GenericRosterInstance>.SuccessResult(existing);
            }

            var populateResult = PopulateRoster(existing, roster);
            if (!populateResult.Success)
            {
                return OperationResult<GenericRosterInstance>.Failure(populateResult.ErrorMessage);
            }

            if (register && _persistence != null)
            {
                _persistence.RegisterRoster(roster);
            }

            _brain.PublishRostersReady();
            return OperationResult<GenericRosterInstance>.SuccessResult(existing);
        }

        private OperationResult<GenericRosterInstance> CreateNewRosterInstance(
            GenericRoster roster,
            bool register
        )
        {
            var go = new GameObject($"RosterInstance - {roster.name}");
            var instance = go.AddComponent<GenericRosterInstance>();
            instance.roster = roster;
            instance.InitializeRuntimePlacementsFromTemplate();

            _persistentRosters.Add(instance);

            var populateResult = PopulateRoster(instance, roster);
            if (!populateResult.Success)
            {
                return OperationResult<GenericRosterInstance>.Failure(populateResult.ErrorMessage);
            }

            if (register && _persistence != null)
            {
                _persistence.RegisterRoster(roster);
            }

            _brain.PublishRostersReady();
            return OperationResult<GenericRosterInstance>.SuccessResult(instance);
        }

        public OperationResult<PlayerTeamRosterInstance> InstantiatePlayerTeamRoster(
            PlayerTeamRoster roster
        )
        {
            var validation = OperationResultGuards.RequireNotNull(roster, nameof(roster));
            if (!validation.Success)
            {
                _brain.PublishRostersFailed();
                return OperationResult<PlayerTeamRosterInstance>.Failure(validation.ErrorMessage);
            }

            // Check if we already have a persistent roster instance for this roster
            if (_persistentPlayerRoster != null)
            {
                if (_persistentPlayerRoster.roster == roster)
                {
                    return OperationResult<PlayerTeamRosterInstance>.SuccessResult(
                        _persistentPlayerRoster
                    );
                }
                else
                {
                    $"RosterManager: _persistentPlayerRoster exists but has different roster ('{_persistentPlayerRoster.roster?.name}' vs '{roster.name}')".LogWarning(
                        "RosterManager"
                    );
                }
            }
            var go = new GameObject($"PlayerTeamRosterInstance - {roster.name}");
            var instance = go.AddComponent<PlayerTeamRosterInstance>();
            instance.roster = roster;
            instance.InitializeRuntimePlacementsFromTemplate();

            _persistentPlayerRoster = instance;

            var populateResult = PopulatePlayerTeamRoster(instance, roster);
            if (!populateResult.Success)
            {
                $"RosterManager: PopulatePlayerTeamRoster failed - {populateResult.ErrorMessage}".LogError(
                    "RosterManager"
                );
                return OperationResult<PlayerTeamRosterInstance>.Failure(
                    populateResult.ErrorMessage
                );
            }

            if (_persistence != null)
            {
                _persistence.RegisterPlayerRoster(roster);
            }

            instance.OnRosterModified += () =>
                _brain.PublishSavePlayerRosterRequested(
                    _brain?.battleBrain?.CurrentTurnNumber ?? 0
                );

            _brain.PublishRostersReady();
            return OperationResult<PlayerTeamRosterInstance>.SuccessResult(instance);
        }

        private OperationResult PopulateRoster(GenericRosterInstance instance, GenericRoster roster)
        {
            var placements = instance.GetPlacements();
            var characters = BuildCharacterListFromPlacements(
                placements,
                logOnNull: true,
                instanceName: instance.name
            );

            instance.AddInstances(characters);
            return OperationResult.Successful();
        }

        private OperationResult PopulatePlayerTeamRoster(
            PlayerTeamRosterInstance instance,
            PlayerTeamRoster roster
        )
        {
            var placements = instance.GetPlacements();
            var characters = BuildCharacterListFromPlacements(
                placements,
                logOnNull: false,
                instanceName: instance.name
            );

            instance.AddInstances(characters);
            return OperationResult.Successful();
        }

        public OperationResult ApplyDecodedPlayerRoster(
            PlayerTeamRosterInstance instance,
            PlayerTeamRoster decoded
        )
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(instance, nameof(instance)),
                OperationResultGuards.RequireNotNull(decoded, nameof(decoded))
            );
            if (!validation.Success)
            {
                return validation;
            }

            instance.ApplyDecodedPlacements(decoded.characters);
            instance.Clear();
            return PopulatePlayerTeamRoster(instance, decoded);
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
                return;
            }

            if (_persistence == null)
            {
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
                return null;
            }

            var result = InstantiatePlayerTeamRoster(roster);
            if (!result.Success)
            {
                $"Failed to recall player roster: {result.Error}".LogError("RosterManager");
                return null;
            }

            if (_persistence != null && !_persistence.HasPlayerRosterInLTM(roster))
            {
                _persistence.RegisterPlayerRoster(roster);
            }

            return result.Value;
        }

        public PlayerTeamRosterInstance GetPersistentPlayerRosterInstance()
        {
            if (_persistentPlayerRoster == null)
            {
                "RosterManager: _persistentPlayerRoster is NULL".LogWarning("RosterManager");
                return null;
            }

            return _persistentPlayerRoster;
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

            var found = GetCachedInstances()
                .Select(r => r.GetInstanceFor(template))
                .FirstOrDefault(i => i != null);

            return found ?? _persistentPlayerRoster?.GetInstanceFor(template);
        }

        private List<CharacterInstance> BuildCharacterListFromPlacements(
            Characters.Roster.UnitPlacement[] placements,
            bool logOnNull,
            string instanceName = ""
        )
        {
            var characters = new List<CharacterInstance>();
            if (placements == null || placements.Length == 0)
            {
                return characters;
            }

            foreach (var unit in placements)
            {
                if (unit == null || unit.CharacterData == null)
                {
                    if (logOnNull)
                    {
                        $"Skipping placement with null CharacterData in '{instanceName}'".LogWarning(
                            "RosterManager"
                        );
                    }
                    continue;
                }

                var character = _characterFactory.CreateOrRecall(unit.CharacterData);
                if (character != null)
                {
                    // notify other systems (skills, UI, etc.) that this instance now has a class
                    if (character.CurrentClass != null && _brain != null)
                    {
                        _brain.PublishCharacterClassChanged(character);
                    }

                    characters.Add(character);

                    if (character.CharacterTemplate?.IsUnique == true)
                    {
                        _characterPersistence.SaveCharacter(character, updateIndex: true);
                    }
                }
            }

            return characters;
        }

        public List<CharacterInstance> GetAllActiveInstances()
        {
            var instances = GetCachedInstances()
                .Where(r => r?.Instances != null)
                .SelectMany(r => r.Instances)
                .ToList();

            if (_persistentPlayerRoster?.Instances != null)
            {
                instances.AddRange(_persistentPlayerRoster.Instances);
            }

            return instances;
        }

        private List<GenericRosterInstance> GetCachedInstances() => _persistentRosters;

        #endregion
    }
}
