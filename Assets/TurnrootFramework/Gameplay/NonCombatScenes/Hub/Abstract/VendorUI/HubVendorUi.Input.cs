using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Abstract
{
    public partial class HubVendorUi
    {
        public void HandleItemChangeInput(string action)
        {
            if (
                HubVendorUiHelper.HandleItemNavigationInput(
                    action,
                    ref paginationHelper,
                    itemChoices,
                    ref SelectionCountCache,
                    ref CostCache,
                    AudioPlayer,
                    NavigateAudioClip,
                    out int newPage,
                    out int newSelection
                )
            )
            {
                CurrentPage = newPage;
                CurrentSelectionIndex = newSelection;

                int vendorIndex = GetSelectedVendorIndex();
                var stock = VendorItems;
                if (stock != null && vendorIndex >= 0 && vendorIndex < stock.Length)
                {
                    ConfigureItemUi(stock[vendorIndex], SelectionCountCache);
                }
            }
        }

        public void ChangePageInput(string action)
        {
            HubVendorUiHelper.HandlePageInput(
                action,
                ref paginationHelper,
                out int newPage,
                out int newSelection
            );
            CurrentPage = newPage;
            CurrentSelectionIndex = newSelection;
        }

        public void ChangePage(int? page = null)
        {
            paginationHelper?.ChangePage(page);
            if (paginationHelper != null)
            {
                CurrentPage = paginationHelper.CurrentPage;
                CurrentSelectionIndex = paginationHelper.CurrentSelectionIndex;
            }
        }

        public int GetSelectedVendorIndex()
        {
            return itemChoiceToVendorIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToVendorIndex.Count
                ? itemChoiceToVendorIndex[CurrentSelectionIndex]
                : CurrentSelectionIndex;
        }

        public void HandleQuantityChangeInput(string action)
        {
            if (action == InputActionConstants.NavigateRight)
            {
                if (!CanBuy)
                {
                    return;
                }
                SelectionCountCache++;
            }
            else if (action == InputActionConstants.NavigateLeft)
            {
                SelectionCountCache--;
            }

            int vendorIndex = GetSelectedVendorIndex();
            var stock = VendorItems;
            if (stock != null && vendorIndex >= 0 && vendorIndex < stock.Length)
            {
                ConfigureItemUi(stock[vendorIndex], SelectionCountCache, true);
            }
            AudioPlayer?.PlayOneShot(NavigateAudioClip);
        }

        public void HandlePurchaseConfirmationInput()
        {
            int vendorIndex = GetSelectedVendorIndex();
            var stock = VendorItems;

            if (stock == null || vendorIndex < 0 || vendorIndex >= stock.Length)
            {
                return;
            }

            var selectedItem = stock[vendorIndex];
            if (selectedItem.Item == null || selectedItem.CurrentStatus.AvailableQuantity <= 0)
            {
                return;
            }

            SelectionCountCache = Mathf.Clamp(
                SelectionCountCache,
                1,
                selectedItem.CurrentStatus.AvailableQuantity
            );

            CostCache = selectedItem.CurrentStatus.IsOnSale
                ? selectedItem.SalePrice * SelectionCountCache
                : selectedItem.Item.BasePrice * SelectionCountCache;

            if (brain?.storehouseBrain == null || !brain.storehouseBrain.CanAfford(CostCache))
            {
                "HubVendorUi.HandlePurchaseConfirmationInput: cannot afford selected item".LogWarning();
                return;
            }

            int currentGold = brain.storehouseBrain.PlayerGold;
            if (TotalGoldScroll != null)
            {
                TotalGoldScroll.StartNumber = currentGold;
                TotalGoldScroll.EndNumber = currentGold - CostCache;
            }

            TotalGoldScroll?.StartScroll();
            NotifyVendorItemSold(selectedItem);

            brain.storehouseBrain.SpendGold(CostCache, true);
            brain.storehouseBrain.SaveGoldToLTM();

            var purchasedItem = selectedItem.Item;
            brain.storehouseBrain.AddMaterials(purchasedItem, SelectionCountCache, true);
            $"{GetType().Name}.HandlePurchaseConfirmationInput: added {SelectionCountCache}x '{purchasedItem?.Name ?? "<null>"}' to storehouse".LogInfo(
                GetType().Name
            );

            selectedItem.CurrentStatus.AvailableQuantity -= SelectionCountCache;
            if (selectedItem.CurrentStatus.AvailableQuantity < 0)
            {
                selectedItem.CurrentStatus.AvailableQuantity = 0;
            }

            stock[vendorIndex] = selectedItem;
            VendorItems = stock;

            PersistItemQuantity(vendorIndex, selectedItem.CurrentStatus.AvailableQuantity);

            NotifyVendorItemSold(selectedItem, SelectionCountCache);
            RefreshVendorDisplay();
        }

        public void HandleSelectedItem(ShopItem selectedItem)
        {
            if (selectedItem.Item == null || selectedItem.UiRefs == null)
            {
                $"{GetType().Name}: Selected item is null or missing UI references, cannot handle selection.".LogWarning();
                return;
            }

            ConfigureItemUi(selectedItem, SelectionCountCache);
        }
    }
}
