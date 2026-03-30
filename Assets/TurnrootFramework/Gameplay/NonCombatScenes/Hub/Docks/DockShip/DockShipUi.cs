using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    [RequireComponent(typeof(DockShip))]
    public partial class DockShipUi : HubVendorUi
    {
        private DockShip dockShip;

        public DockShip DockShipData => dockShip ??= GetComponent<DockShip>();

        protected override HubVendor Vendor => DockShipData;

        protected override ShopItem[] VendorItems
        {
            get => DockShipData?.NormalGoodsForSale;
            set
            {
                if (DockShipData != null)
                {
                    DockShipData.NormalGoodsForSale = value;
                }
            }
        }

        protected override string VendorDisplayName => DockShipData?.ShipName ?? string.Empty;

        protected override string VendorDescription => string.Empty;

        protected override Brain.Brain BrainReference => DockShipData?.brain;

        protected override bool ShouldRenderVendor =>
            DockShipData != null && DockShipData.CurrentDockShipShopType == DockShipShopType.Normal;

        protected override void Awake()
        {
            dockShip = GetComponent<DockShip>();
            if (dockShip == null)
            {
                $"DockShipUi on '{name}' could not find DockShip component.".LogError();
            }

            base.Awake();
        }
    }
}
