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
            SelectionCountCache = 1;
            CostCache = 0;

            if (paginationHelper == null || itemChoices == null || itemChoices.Count == 0)
            {
                $"ShopUi: No item choices available to change selection.".LogWarning();
                return;
            }

            if (action == InputActionConstants.NavigateDown)
            {
                paginationHelper.ChangeSelectionByOffset(1);
            }
            else if (action == InputActionConstants.NavigateUp)
            {
                paginationHelper.ChangeSelectionByOffset(-1);
            }

            CurrentPage = paginationHelper.CurrentPage;
            CurrentSelectionIndex = paginationHelper.CurrentSelectionIndex;

            AudioPlayer?.PlayOneShot(NavigateAudioClip);
        }

        public void ChangePageInput(string action)
        {
            paginationHelper?.HandleScrollInput(action);
            if (paginationHelper != null)
            {
                CurrentPage = paginationHelper.CurrentPage;
                CurrentSelectionIndex = paginationHelper.CurrentSelectionIndex;
            }
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

                    brain.storehouseBrain.AddMaterials(
                        ShopData.ItemsStocked[shopIndex].Item,
                        SelectionCountCache,
                        true
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
