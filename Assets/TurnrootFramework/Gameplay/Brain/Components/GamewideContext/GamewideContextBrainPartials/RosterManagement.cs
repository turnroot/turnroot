using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Partial class providing roster and character management API methods.
    /// </summary>
    public partial class GamewideContextBrain
    {
        #region Roster Management API
        public GenericRosterInstance GetOrCreateGenericRoster(
            GenericRoster roster,
            bool register = false
        )
        {
            if (roster == null)
            {
                "Cannot get/create null roster".LogWarning();
                return null;
            }

            if (_activeRosterInstances.TryGetValue(roster.Id, out var existing))
            {
                return existing as GenericRosterInstance;
            }

            var result = _rosterManager.InstantiateGenericRoster(roster, register);
            if (!result.Success)
            {
                return null;
            }

            _activeRosterInstances[roster.Id] = result.Value;
            return result.Value;
        }

        public PlayerTeamRosterInstance GetOrCreatePlayerTeamRoster(PlayerTeamRoster roster)
        {
            if (roster == null)
            {
                "GamewideContextBrain.GetOrCreatePlayerTeamRoster: roster parameter is null".LogError();
                return null;
            }

            if (_activeRosterInstances.TryGetValue(roster.Id, out var existing))
            {
                var existingPlayerRoster = existing as PlayerTeamRosterInstance;
                if (existingPlayerRoster != null)
                {
                    return existingPlayerRoster;
                }
                else
                {
                    $"GamewideContextBrain.GetOrCreatePlayerTeamRoster: Cached instance is not PlayerTeamRosterInstance type!".LogError();
                }
            }

            var result = _rosterManager.InstantiatePlayerTeamRoster(roster);

            if (!result.Success)
            {
                $"GamewideContextBrain.GetOrCreatePlayerTeamRoster: Failed to instantiate - {result.Error}".LogError();
                return null;
            }

            if (result.Value == null)
            {
                "GamewideContextBrain.GetOrCreatePlayerTeamRoster: InstantiatePlayerTeamRoster returned null value despite Success=true".LogError();
                return null;
            }

            _activeRosterInstances[roster.Id] = result.Value;
            return result.Value;
        }

        public void RecallGenericRosters(List<GenericRoster> rosters) =>
            _rosterManager?.RecallGenericRosters(rosters);

        public PlayerTeamRosterInstance RecallPlayerTeamRoster(PlayerTeamRoster roster) =>
            _rosterManager?.RecallPlayerTeamRoster(roster);

        public PlayerTeamRosterInstance GetPersistentPlayerTeamRosterInstance()
        {
            // Ensure persistent roster is loaded and available for Hub and other non-battle paths.
            if (GamewidePersistentPlayerRoster == null)
            {
                CreateOrRecallGamewidePersistentPlayerRoster();
            }

            if (_rosterManager == null)
            {
                // In case this is called before Awake or when roster manager is not yet set.
                _rosterPersistence ??= new RosterPersistence(GetComponent<LongTermMemory>());
                _rosterManager = new RosterManager(
                    _brain ?? GetComponent<Brain>(),
                    _rosterPersistence
                );
            }

            var result = _rosterManager.GetPersistentPlayerRosterInstance();
            if (result == null && GamewidePersistentPlayerRoster != null)
            {
                var instantiateResult = _rosterManager.InstantiatePlayerTeamRoster(
                    GamewidePersistentPlayerRoster
                );
                if (instantiateResult.Success && instantiateResult.Value != null)
                {
                    result = instantiateResult.Value;
                    _activeRosterInstances[GamewidePersistentPlayerRoster.Id] = result;
                }
            }

            if (result == null)
            {
                // No persistent roster yet; this is expected in some early Hub scenarios.
                "GamewideContextBrain.GetPersistentPlayerTeamRosterInstance: no player roster available yet".LogInfo();
            }

            return result;
        }

        public List<CharacterInstance> GetSelectedForBattlePlayerTeamUnits()
        {
            var rosterInstance = GetPersistentPlayerTeamRosterInstance();
            if (rosterInstance == null)
            {
                "GamewideContextBrain.GetSelectedForBattlePlayerTeamUnits: Roster instance is null".LogWarning();
                return new List<CharacterInstance>();
            }

            return RosterFilters.FilterUnitsSelectedForBattle(rosterInstance);
        }

        public Characters.Roster.UnitPlacement[] GetSelectedForBattlePlayerTeamPlacements()
        {
            var instance = GetPersistentPlayerTeamRosterInstance();
            var placements =
                instance.GetPlacements()
                ?? GamewidePersistentPlayerRoster.characters
                ?? new Characters.Roster.UnitPlacement[0];

            var selectedInstances = GetSelectedForBattlePlayerTeamUnits();
            var selectedTemplates = new HashSet<CharacterData>(
                selectedInstances.Select(i => i.CharacterTemplate)
            );

            return placements
                .Where(p =>
                    p.CharacterData != null && selectedTemplates.ContainsMatching(p.CharacterData)
                )
                .ToArray();
        }
        #endregion

        /// <summary>
        /// Returns the <see cref="CharacterInstance"/> whose <c>CharacterTemplate.Which</c> is
        /// <see cref="Characters.Components.CharacterWhich.AVATAR"/> from the persistent player
        /// roster, creating/recalling the roster if it is not yet loaded.
        /// Returns <c>null</c> and logs a warning when no avatar entry is found.
        /// </summary>
        public CharacterInstance GetOrCreateAvatarInstance()
        {
            var rosterInstance = GetPersistentPlayerTeamRosterInstance();
            if (rosterInstance == null)
            {
                "GamewideContextBrain.GetOrCreateAvatarInstance: no persistent player roster available.".LogWarning();
                return null;
            }

            var avatar = rosterInstance.Instances.FirstOrDefault(i =>
                i?.CharacterTemplate != null && !i.CharacterTemplate.IsNotAvatar
            );

            if (avatar == null)
            {
                "GamewideContextBrain.GetOrCreateAvatarInstance: no Avatar character found in persistent player roster.".LogWarning();
            }

            return avatar;
        }

        #region Character Management API
        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            _rosterManager.FindInstanceByTemplate(template);

        public List<CharacterInstance> GetAllActiveInstances() =>
            _rosterManager.GetAllActiveInstances() ?? new List<CharacterInstance>();

        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            _characterPersistence.SaveCharacter(instance, updateIndex: false);
        #endregion
    }
}
