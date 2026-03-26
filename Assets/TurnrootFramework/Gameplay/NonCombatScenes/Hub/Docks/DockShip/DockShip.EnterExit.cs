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
                dockShipUi => dockShipUi.RefreshDockShipDisplay(),
                "DockShip"
            );
        }

        public void NotifyShipExited()
        {
            NotifyVendorExited(
                () => TryGetComponent<DockShipUi>(out var ui) ? ui : null,
                dockShipUi => dockShipUi.DockShipUiFade.Hide(),
                "DockShip"
            );
        }

        #endregion
    }
}
