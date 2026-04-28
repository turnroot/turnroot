using Turnroot.Characters;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubTeamLocations
    {
        private System.Collections.Generic.Dictionary<int, HubSublocationName> LoadSavedPlacement(
            PlayerTeamRoster roster
        )
        {
            return HubDayStateStore.GetTeamPlacements();
        }

        private void SavePlacement(
            PlayerTeamRoster roster,
            System.Collections.Generic.Dictionary<int, HubSublocationName> map
        )
        {
            HubDayStateStore.SaveTeamPlacements(_brain, map);
        }

        private System.Collections.Generic.Dictionary<
            string,
            HubSublocationName
        > LoadSavedNonRosterPlacement()
        {
            return HubDayStateStore.GetNonRosterPlacements();
        }

        private void SaveNonRosterPlacement(
            System.Collections.Generic.Dictionary<string, HubSublocationName> map
        )
        {
            HubDayStateStore.SaveNonRosterPlacements(_brain, map);
        }
    }
}
