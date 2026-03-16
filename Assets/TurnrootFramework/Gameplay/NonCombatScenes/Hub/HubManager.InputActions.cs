using Turnroot.UI;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Input Actions

        public void HandleLocationInput(string action)
        {
            if (subLocations == null || subLocations.Length == 0)
            {
                "HubManager: No sublocations assigned".LogError();
                return;
            }

            if (InputProvider != null)
            {
                InputProvider.Navigate(
                    action,
                    LocationChoices,
                    ref currentIndex,
                    LocationChoices?.Length ?? 0,
                    () =>
                    {
                        var selectedLocation = subLocations[currentIndex];
                        if (selectedLocation.CanBeVisitedToday())
                        {
                            selectedLocation.PlayerVisit();
                        }
                    }
                );
            }
            else
            {
                UiChoiceHandler.HandleNavigation(
                    action,
                    LocationChoices,
                    ref currentIndex,
                    LocationChoices?.Length ?? 0,
                    () =>
                    {
                        var selectedLocation = subLocations[currentIndex];
                        if (selectedLocation.CanBeVisitedToday())
                        {
                            selectedLocation.PlayerVisit();
                        }
                    }
                );
            }

            UpdateChoiceSelection();
        }

        #endregion
    }
}
