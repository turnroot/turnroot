using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.Utilities;

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
            if (
                itemChoiceToVendorIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToVendorIndex.Count
            )
            {
                return itemChoiceToVendorIndex[CurrentSelectionIndex];
            }

            return CurrentSelectionIndex;
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
                ConfigureItemUi(stock[vendorIndex], SelectionCountCache);
            }
            AudioPlayer?.PlayOneShot(NavigateAudioClip);
        }

        public void HandlePurchaseConfirmationInput()
        {
            if (!CanBuy)
            {
                return;
            }

            int vendorIndex = GetSelectedVendorIndex();
            var stock = VendorItems;

            if (stock == null || vendorIndex < 0 || vendorIndex >= stock.Length)
            {
                return;
            }

            int currentGold = brain?.storehouseBrain != null ? brain.storehouseBrain.PlayerGold : 0;
            if (TotalGoldScroll != null)
            {
                TotalGoldScroll.StartNumber = currentGold;
                TotalGoldScroll.EndNumber = currentGold - CostCache;
            }

            if (CanBuy)
            {
                TotalGoldScroll?.StartScroll();

                NotifyVendorItemSold(stock[vendorIndex]);

                if (brain?.storehouseBrain != null)
                {
                    brain.storehouseBrain.SpendGold(CostCache, true);
                    brain.storehouseBrain.SaveGoldToLTM();

                    var purchasedItem = stock[vendorIndex].Item;
                    brain.storehouseBrain.AddMaterials(purchasedItem, SelectionCountCache, true);
                    $"{GetType().Name}.HandlePurchaseConfirmationInput: added {SelectionCountCache}x '{purchasedItem?.Name ?? "<null>"}' to storehouse".LogInfo(
                        GetType().Name
                    );

                    var item = stock[vendorIndex];
                    item.CurrentStatus.AvailableQuantity -= SelectionCountCache;
                    if (item.CurrentStatus.AvailableQuantity < 0)
                    {
                        item.CurrentStatus.AvailableQuantity = 0;
                    }

                    stock[vendorIndex] = item;
                    VendorItems = stock;

                    PersistItemQuantity(vendorIndex, item.CurrentStatus.AvailableQuantity);

                    RefreshVendorDisplay();
                }
            }
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
