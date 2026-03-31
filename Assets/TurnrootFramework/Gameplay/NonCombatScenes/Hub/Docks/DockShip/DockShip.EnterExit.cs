using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    public partial class DockShip : HubVendor
    {
        #region Enter/Exit

        public void NotifyShipVisited()
        {
            var currentDate = _brain?.ltm?.GetGameDate() ?? default;
            if (currentDate != _lastVisitedDate)
            {
                _lastVisitedDate = currentDate;
                IncreaseTrust(1.1f);
                NotifyVendorVisited(
                    () => TryGetComponent<DockShipUi>(out var ui) ? ui : null,
                    dockShipUi =>
                    {
                        InitializeStockIfNeeded(currentDate);

                        dockShipUi.MainOverlayUiFade?.Hide();
                        dockShipUi.RefreshDockShipDisplay();
                    },
                    "DockShip"
                );
            }
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
