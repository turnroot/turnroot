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

        protected override void PersistItemQuantity(int vendorIndex, int quantity)
        {
            if (brain == null || ShopData == null || VendorItems == null)
            {
                return;
            }

            if (vendorIndex >= 0 && vendorIndex < VendorItems.Length)
            {
                var item = VendorItems[vendorIndex];
                if (item.Item != null)
                {
                    HubDayStateStore.SetShopItemQuantity(
                        brain,
                        ShopData.name,
                        item.Item.name,
                        quantity
                    );
                }
            }
        }
    }
}
