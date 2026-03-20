using TMPro;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [RequireComponent(typeof(Shop))]
    public partial class ShopUi : MonoBehaviour
    {
        private int SelectionCountCache = 1;

        public void HandleItemChangeInput(string action)
        {
            SelectionCountCache = 1;
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

        public void ChangePage(int page = 0)
        {
            if (totalPages <= 1)
            {
                return;
            }
            if (page == 0)
            {
                page = CurrentPage;
            }
            else if (page == -1)
            {
                page = Mathf.Max(0, totalPages - 1); // wrap around to last page
            }
            else
            {
                page = Mathf.Clamp(page, 0, Mathf.Max(0, totalPages - 1));
            }

            if (page == CurrentPage)
            {
                return;
            }

            CurrentPage = page;
            CurrentSelectionIndex = CurrentPage * ItemsPerPage;

            PlayPageChangeSound();
            UpdateVisiblePageItems();
            UpdatePaginationIndicators();
            RefreshSelection();
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

            ConfigureItemUi(ShopData.ItemsStocked[CurrentSelectionIndex], SelectionCountCache);
        }

        public void HandlePurchaseConfirmationInput() { }

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
