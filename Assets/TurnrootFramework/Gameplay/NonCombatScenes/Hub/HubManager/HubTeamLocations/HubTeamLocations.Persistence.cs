using System.Collections.Generic;
using Turnroot.Characters;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        private Dictionary<int, HubSublocationName> LoadSavedPlacement(PlayerTeamRoster roster) =>
            HubDayStateStore.GetTeamPlacements();

        private void SavePlacement(Dictionary<int, HubSublocationName> map) =>
            HubDayStateStore.SaveTeamPlacements(_brain, map);

        private Dictionary<string, HubSublocationName> LoadSavedNonRosterPlacement() =>
            HubDayStateStore.GetNonRosterPlacements();

        private void SaveNonRosterPlacement(Dictionary<string, HubSublocationName> map) =>
            HubDayStateStore.SaveNonRosterPlacements(_brain, map);
    }
}
