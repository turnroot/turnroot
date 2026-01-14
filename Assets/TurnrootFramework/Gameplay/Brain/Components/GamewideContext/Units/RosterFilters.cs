using System.Collections.Generic;
using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain
{
    public static class RosterFilters
    {
        public static List<CharacterInstance> FilterUnitsSelectedForBattle(
            PlayerTeamRosterInstance rosterInstance
        )
        {
            var filteredInstances = new List<CharacterInstance>();
            if (rosterInstance == null || rosterInstance.Instances == null)
            {
                return filteredInstances;
            }

            foreach (var instance in rosterInstance.Instances)
            {
                if (instance.IsSelectedForBattle)
                {
                    filteredInstances.Add(instance);
                }
            }
            return filteredInstances;
        }
    }
}
