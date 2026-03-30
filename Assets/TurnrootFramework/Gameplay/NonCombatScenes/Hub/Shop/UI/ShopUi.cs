using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [RequireComponent(typeof(Shop))]
    public partial class ShopUi : HubVendorUi
    {
        public Shop ShopData => GetComponent<Shop>();

        protected override HubVendor Vendor => ShopData;

        protected override ShopItem[] VendorItems
        {
            get => ShopData.ItemsStocked;
            set => ShopData.ItemsStocked = value;
        }

        protected override string VendorDisplayName => ShopData.ShopName;

        protected override string VendorDescription => ShopData.ShopDescription;

        protected override Brain.Brain BrainReference => ShopData.brain;

        protected override void NotifyVendorItemSold(ShopItem itemSold)
        {
            ShopData.NotifyShopkeeperSells(itemSold);
        }
    }
}
