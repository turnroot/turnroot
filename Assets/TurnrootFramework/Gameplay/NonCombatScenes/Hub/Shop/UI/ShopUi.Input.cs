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
            if (itemChoices == null)
            {
                $"ShopUi: No item choices available to change selection.".LogWarning();
                return;
            }

            int candidateIndex = CurrentSelectionIndex;

            if (action == InputActionConstants.NavigateDown)
            {
                candidateIndex++;
            }
            else if (action == InputActionConstants.NavigateUp)
            {
                candidateIndex--;
            }

            if (itemChoices.Count == 0)
            {
                return;
            }

            if (candidateIndex >= itemChoices.Count)
            {
                candidateIndex = 0;
                ChangePage(0);
            }
            else if (candidateIndex < 0)
            {
                candidateIndex = itemChoices.Count - 1;
                ChangePage(-1);
            }
            else if (candidateIndex >= (CurrentPage + 1) * ItemsPerPage)
            {
                ChangePage(CurrentPage + 1);
            }
            else if (candidateIndex < CurrentPage * ItemsPerPage)
            {
                ChangePage(CurrentPage - 1);
            }

            CurrentSelectionIndex = candidateIndex;
            RefreshSelection();
            AudioPlayer.PlayOneShot(NavigateAudioClip);
        }

        public void ChangePageInput(string action)
        {
            if (action == InputActionConstants.ScrollLeft)
            {
                ChangePage(CurrentPage - 1);
            }
            else if (action == InputActionConstants.ScrollRight)
            {
                ChangePage(CurrentPage + 1);
            }
        }

        public void ChangePage(int? page = null)
        {
            if (totalPages <= 1)
            {
                return;
            }

            if (page == null)
            {
                page = CurrentPage;
            }
            else if (page == -1)
            {
                page = Mathf.Max(0, totalPages - 1); // wrap around to last page
            }
            else
            {
                page = Mathf.Clamp(page.Value, 0, Mathf.Max(0, totalPages - 1));
            }

            if (page.Value == CurrentPage)
            {
                return;
            }

            CurrentPage = page.Value;
            CurrentSelectionIndex = CurrentPage * ItemsPerPage;

            PlayPageChangeSound();
            UpdateVisiblePageItems();
            UpdatePaginationIndicators();
            RefreshSelection();
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
