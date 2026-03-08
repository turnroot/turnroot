using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
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
            var result = _rosterManager?.GetPersistentPlayerRosterInstance();
            if (result == null)
            {
                "GamewideContextBrain.GetPersistentPlayerTeamRosterInstance: _rosterManager returned null".LogWarning();
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
                .Where(p => p.CharacterData != null && selectedTemplates.Contains(p.CharacterData))
                .ToArray();
        }
        #endregion

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
