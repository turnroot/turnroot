using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public partial class SpecificUiHandler : MonoBehaviour
    {
        public DockShipUi RetrieveDockShipUi()
        {
            var activeVendor = _activeDockShip as Abstract.HubVendor;
            if (activeVendor != null)
            {
                if (_activeDockShip.TryGetComponent<DockShipUi>(out var dockShipUi))
                {
                    return dockShipUi;
                }
            }
            return null;
        }

        public void HandleDockShopSelection(string action)
        {
            if (_activeDockShip == null)
                return;
            if (_activeDockShip.CurrentDockShipShopType == DockShipShopType.Normal)
            {
                RetrieveDockShipUi()?.HandleConfirmInput(action);
            }
        }

        public void HandleDockShopPageChange(string action)
        {
            if (_activeDockShip == null)
                return;
            if (_activeDockShip.CurrentDockShipShopType == DockShipShopType.Normal)
            {
                RetrieveDockShipUi()?.HandleItemChangeInput(action);
            }
        }

        public void HandleDockShopUpDown(string action)
        {
            if (_activeDockShip == null)
                return;
            if (_activeDockShip.CurrentDockShipShopType == DockShipShopType.Normal)
            {
                RetrieveDockShipUi()?.HandleItemChangeInput(action);
            }
        }

        public void HandleDockShopLeftRight(string action)
        {
            if (_activeDockShip == null)
                return;
            if (_activeDockShip.CurrentDockShipShopType == DockShipShopType.Normal)
            {
                RetrieveDockShipUi()?.HandleQuantityChangeInput(action);
            }
        }

        public void HandleDockShopBack(string action)
        {
            if (_activeDockShip == null)
            {
                CompleteExit();
                return;
            }

            if (_activeDockShip.CurrentDockShipShopType == DockShipShopType.Normal)
            {
                var activeVendor = _activeDockShip as Abstract.HubVendor;
                if (activeVendor != null)
                {
                    bool hasExitDialogue = activeVendor.HasFarewellDialogue();
                    activeVendor.HandleBackInput(action);

                    if (hasExitDialogue)
                    {
                        _waitingForShopExitDialogue = true;
                        SubscribeToConversationFinished();
                        return;
                    }
                }
                CompleteExit();
            }
        }
    }
}
