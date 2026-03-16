using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    [System.Serializable]
    public struct SmuggledItem
    {
        public ShopItem Item;
        public int MinimumTrustRequired;

        public bool IsAvailable(int currentTrust) => currentTrust >= MinimumTrustRequired;

        public ShopItem.Status Refresh(GameDate currentDay, int currentTrust)
        {
            if (!IsAvailable(currentTrust))
            {
                var status = Item.CurrentStatus;
                status.AvailableQuantity = 0;
                status.IsOnSale = false;
                return status;
            }

            return Item.Refresh(currentDay);
        }
    }
}
