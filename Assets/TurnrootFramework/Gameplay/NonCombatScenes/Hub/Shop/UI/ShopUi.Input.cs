using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [RequireComponent(typeof(Shop))]
    public partial class ShopUi : MonoBehaviour
    {
        private int SelectionCountCache = 1;

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

        public int GetSelectedShopIndex()
        {
            if (
                itemChoiceToShopIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToShopIndex.Count
            )
            {
                return itemChoiceToShopIndex[CurrentSelectionIndex];
            }

            return CurrentSelectionIndex;
        }

        public void HandleQuantityChangeInput(string action)
        {
            if (action == InputActionConstants.NavigateRight)
            {
                if (!CanBuy)
                {
                    return; // already can't afford the selected quantity so couldn't go higher anyway
                }
                SelectionCountCache++;
            }
            else if (action == InputActionConstants.NavigateLeft)
            {
                SelectionCountCache--;
            }

            int shopIndex = GetSelectedShopIndex();
            if (shopIndex >= 0 && shopIndex < ShopData.ItemsStocked.Length)
            {
                ConfigureItemUi(ShopData.ItemsStocked[shopIndex], SelectionCountCache);
            }
            AudioPlayer.PlayOneShot(NavigateAudioClip);
        }

        public void HandlePurchaseConfirmationInput()
        {
            if (!CanBuy)
            {
                return;
            }

            int shopIndex = CurrentSelectionIndex;
            if (
                itemChoiceToShopIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToShopIndex.Count
            )
            {
                shopIndex = itemChoiceToShopIndex[CurrentSelectionIndex];
            }

            if (shopIndex < 0 || shopIndex >= ShopData.ItemsStocked.Length)
            {
                return;
            }

            int currentGold = brain?.storehouseBrain != null ? brain.storehouseBrain.PlayerGold : 0;
            TotalGoldScroll.StartNumber = currentGold;
            TotalGoldScroll.EndNumber = currentGold - CostCache;

            if (CanBuy)
            {
                TotalGoldScroll.StartScroll();
                ShopData.NotifyShopkeeperSells(ShopData.ItemsStocked[shopIndex]);
                if (brain?.storehouseBrain != null)
                {
                    brain.storehouseBrain.SpendGold(CostCache, true);
                    brain.storehouseBrain.SaveGoldToLTM();

                    var purchasedItem = ShopData.ItemsStocked[shopIndex].Item;
                    brain.storehouseBrain.AddMaterials(purchasedItem, SelectionCountCache, true);
                    $"ShopUi.HandlePurchaseConfirmationInput: added {SelectionCountCache}x '{purchasedItem?.Name ?? "<null>"}' to storehouse".LogInfo(
                        "ShopUi"
                    );

                    var item = ShopData.ItemsStocked[shopIndex];
                    item.CurrentStatus.AvailableQuantity -= SelectionCountCache;
                    if (item.CurrentStatus.AvailableQuantity < 0)
                    {
                        item.CurrentStatus.AvailableQuantity = 0;
                    }

                    ShopData.ItemsStocked[shopIndex] = item;

                    var itemName = item.Item != null ? item.Item.name : string.Empty;
                    if (brain != null)
                    {
                        HubDayStateStore.SetShopItemQuantity(
                            brain,
                            ShopData.name,
                            itemName,
                            item.CurrentStatus.AvailableQuantity
                        );
                    }

                    // Rebuild UI after quantity update (handles sold-out removal and updates reliably).
                    RefreshShopDisplay();
                }
            }
        }

        public void HandeSelectedItem(ShopItem selectedItem)
        {
            if (selectedItem.Item == null || selectedItem.UiRefs == null)
            {
                $"ShopUi: Selected item is null or missing UI references, cannot handle selection.".LogWarning();
                return;
            }

            ConfigureItemUi(selectedItem, SelectionCountCache);
        }
    }
}
