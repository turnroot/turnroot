using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    public partial class DockShip : HubVendor
    {
        #region Enter/Exit

        public void NotifyShipVisited()
        {
            NotifyVendorVisited(
                () => TryGetComponent<DockShipUi>(out var ui) ? ui : null,
                dockShipUi =>
                {
                    // Ensure stock quantities are initialized before rendering the vendor UI.
                    // RefreshShipForNewDay is only called on date-change; on same-day re-entry
                    // the quantities are still uninitialized unless we do this here.
                    GameDate currentDate = _brain?.ltm?.GetGameDate() ?? default;
                    InitializeStockIfNeeded(currentDate);

                    dockShipUi.MainOverlayUiFade?.Hide();
                    dockShipUi.RefreshDockShipDisplay();
                },
                "DockShip"
            );
        }

        public void NotifyShipExited()
        {
            NotifyVendorExited(
                () => TryGetComponent<DockShipUi>(out var ui) ? ui : null,
                dockShipUi =>
                {
                    dockShipUi.MainOverlayUiFade?.Show();
                    dockShipUi.DockShipUiFade.Hide();
                },
                "DockShip"
            );
        }

        #endregion
    }
}
