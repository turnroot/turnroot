using System.Collections.Generic;
using TMPro;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.UI;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.Ui;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Abstract
{
    public abstract partial class HubVendorUi : MonoBehaviour
    {
        protected Brain.Brain brain;

        protected abstract HubVendor Vendor { get; }
        protected abstract ShopItem[] VendorItems { get; set; }
        protected abstract string VendorDisplayName { get; }
        protected abstract string VendorDescription { get; }
        protected abstract Brain.Brain BrainReference { get; }
        protected virtual bool ShouldRenderVendor => true;

        [HideInInspector]
        public ShopItemUiRefs[] ItemUiRefs;

        private bool CanBuy = true;

        private int CostCache;
        protected int SelectionCountCache = 1;

        private readonly List<GameObject> pageIndicatorObjects = new();

        public int CurrentPage { get; private set; } = 0;

        public int CurrentSelectionIndex { get; private set; } = 0;

        protected PaginationHelper paginationHelper;

        [Header("UI References")]
        public UIFade VendorUiFade;
        public GameObject ItemPrefab;
        public TextMeshProUGUI ShopNameText;
        public TextMeshProUGUI ShopDescriptionText;
        public TextMeshProUGUI ItemDescriptionText;
        public TextMeshProUGUI TotalGoldText;
        public ScrollDownGold TotalGoldScroll;
        public GameObject ItemsParentContainer;

        [Header("Weapon Details UI")]
        public GameObject WeaponExtraDetails;
        public TextMeshProUGUI WeaponMightText;
        public TextMeshProUGUI WeaponHitText;
        public TextMeshProUGUI WeaponCritText;
        public TextMeshProUGUI WeaponDurabilityText;

        [Header("Page Indicator UI")]
        public GameObject PageIndicatorContainer;
        public Sprite InactivePageIndicatorSprite;
        public Sprite ActivePageIndicatorSprite;
        public float PageIndicatorSize = 30f;
        public int ItemsPerPage = 6;

        public AudioSource AudioPlayer;
        public AudioClip PageChangeAudioClip;
        public AudioClip NavigateAudioClip;

        protected List<UiChoice> itemChoices;
        protected List<int> itemChoiceToVendorIndex;

        private Brain.Brain ResolveBrain() =>
            brain ??= BrainReference ?? Vendor?.brain ?? FindFirstObjectByType<Brain.Brain>();

        protected virtual void Awake() => ResolveBrain();

        protected virtual void NotifyVendorItemSold(ShopItem itemSold) { }

        protected virtual void NotifyVendorItemSold(ShopItem itemSold, int quantity) =>
            NotifyVendorItemSold(itemSold);

        protected virtual void PersistItemQuantity(int vendorIndex, int quantity)
        {
            if (brain == null || Vendor == null || vendorIndex < 0 || VendorItems == null)
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
                        Vendor.name,
                        item.Item.name,
                        quantity
                    );
                }
            }
        }

        public void RefreshVendorDisplay()
        {
            if (!ShouldRenderVendor)
            {
                VendorUiFade?.Hide();
                return;
            }

            ResolveBrain();

            HubVendorUiHelper.ClearInstantiatedItems(
                ItemsParentContainer,
                PageIndicatorContainer,
                pageIndicatorObjects,
                ref itemChoices,
                ref itemChoiceToVendorIndex
            );

            if (ShopNameText != null)
            {
                ShopNameText.text = VendorDisplayName ?? string.Empty;
            }

            if (ShopDescriptionText != null)
            {
                ShopDescriptionText.text = VendorDescription ?? string.Empty;
            }

            var stock = VendorItems;

            SelectionCountCache = 1;
            CostCache = 0;

            // Object.Destroy is deferred to end-of-frame, so Unity's == null check returns false
            // for pending-destroy components. Null the UiRefs on every item now so the rebuild
            // loop below always creates fresh prefabs rather than reusing about-to-be-destroyed refs.
            if (stock != null)
            {
                for (int i = 0; i < stock.Length; i++)
                {
                    var item = stock[i];
                    item.UiRefs = null;
                    stock[i] = item;
                }
            }

            ItemsPerPage = Mathf.Max(1, ItemsPerPage);

            if (stock == null || stock.Length == 0)
            {
                VendorUiFade?.Show();
                return;
            }

            HubVendorUiHelper.UpdateGoldDisplay(TotalGoldText, TotalGoldScroll, brain);

            itemChoices = new List<UiChoice>(stock.Length);
            itemChoiceToVendorIndex = new List<int>(stock.Length);

            for (var i = 0; i < stock.Length; i++)
            {
                var item = stock[i];
                if (item.Item == null || item.CurrentStatus.AvailableQuantity <= 0)
                {
                    continue;
                }

                if (item.UiRefs == null || item.UiRefs.ShopItemChoice == null)
                {
                    var itemUiObj = Instantiate(ItemPrefab, ItemsParentContainer.transform);
                    item.UiRefs = itemUiObj.GetComponent<ShopItemUiRefs>();
                    stock[i] = item;
                }

                if (item.UiRefs != null && item.UiRefs.ShopItemChoice != null)
                {
                    itemChoices.Add(item.UiRefs.ShopItemChoice);
                    itemChoiceToVendorIndex.Add(i);
                    ConfigureItemUi(item, SelectionCountCache, false);
                }
            }

            VendorItems = stock;

            int newPage;
            int newSelectionIndex;

            HubVendorUiHelper.EnsurePagination(
                ref paginationHelper,
                ItemsPerPage,
                PageIndicatorContainer?.transform,
                ActivePageIndicatorSprite,
                InactivePageIndicatorSprite,
                PageIndicatorSize,
                AudioPlayer,
                PageChangeAudioClip,
                itemChoices,
                CurrentSelectionIndex,
                out newPage,
                out newSelectionIndex
            );

            CurrentPage = newPage;
            CurrentSelectionIndex = newSelectionIndex;
            int selectedVendorIndex = GetSelectedVendorIndex();
            if (stock != null && selectedVendorIndex >= 0 && selectedVendorIndex < stock.Length)
            {
                ConfigureItemUi(stock[selectedVendorIndex], SelectionCountCache, true);
            }
            VendorUiFade?.Show();
        }

        // Legacy compatibility wrappers used by existing shop/dock code paths.
        public UIFade ShopUiFade => VendorUiFade;
        public UIFade DockShipUiFade => VendorUiFade;

        public void RefreshShopDisplay() => RefreshVendorDisplay();

        public void RefreshDockShipDisplay() => RefreshVendorDisplay();

        public void HandleConfirmInput(string action) => HandlePurchaseConfirmationInput();

        public void HandleBackInput(string action)
        {
            Vendor?.HandleBackInput(action);
        }
    }
}
