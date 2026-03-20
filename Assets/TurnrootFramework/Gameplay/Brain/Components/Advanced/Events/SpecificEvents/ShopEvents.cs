using System;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Shop Events

        public event Action<Shop> OnShopVisited;
        public event Action<Shop> OnShopExited;
        public event Action<Shop, ShopItem[]> OnShopkeeperBuys;
        public event Action<Shop, ShopItem> OnShopkeeperSells;

        public void PublishShopVisited(Shop shop) => OnShopVisited?.Invoke(shop);

        public void PublishShopExited(Shop shop) => OnShopExited?.Invoke(shop);

        public void PublishShopkeeperBuys(Shop shop, ShopItem[] itemsBought) =>
            OnShopkeeperBuys?.Invoke(shop, itemsBought);

        public void PublishShopkeeperSells(Shop shop, ShopItem itemSold) =>
            OnShopkeeperSells?.Invoke(shop, itemSold);

        #endregion
    }
}
