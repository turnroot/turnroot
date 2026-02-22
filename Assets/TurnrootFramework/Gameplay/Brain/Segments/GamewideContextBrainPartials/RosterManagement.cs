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
                "Cannot get/create null player roster".LogWarning();
                return null;
            }

            if (_activeRosterInstances.TryGetValue(roster.Id, out var existing))
            {
                return existing as PlayerTeamRosterInstance;
            }

            var result = _rosterManager.InstantiatePlayerTeamRoster(roster);
            if (!result.Success)
            {
                return null;
            }

            _activeRosterInstances[roster.Id] = result.Value;
            return result.Value;
        }

        public void RecallGenericRosters(List<GenericRoster> rosters) =>
            _rosterManager?.RecallGenericRosters(rosters);

        public PlayerTeamRosterInstance RecallPlayerTeamRoster(PlayerTeamRoster roster) =>
            _rosterManager?.RecallPlayerTeamRoster(roster);

        public PlayerTeamRosterInstance GetPersistentPlayerTeamRosterInstance() =>
            _rosterManager?.GetPersistentPlayerRosterInstance();

        public List<CharacterInstance> GetSelectedForBattlePlayerTeamUnits() =>
            RosterFilters.FilterUnitsSelectedForBattle(GetPersistentPlayerTeamRosterInstance());

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

