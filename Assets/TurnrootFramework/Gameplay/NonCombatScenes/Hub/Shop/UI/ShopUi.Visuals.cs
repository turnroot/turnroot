using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [RequireComponent(typeof(Shop))]
    public partial class ShopUi : MonoBehaviour
    {
        private bool CanBuy = true;
        private int CostCache;

        private void ConfigureItemUi(ShopItem item, int SelectionCount)
        {
            CanBuy = true;
            if (item.Item == null || item.UiRefs == null)
            {
                return;
            }

            if (item.CurrentStatus.AvailableQuantity <= 0)
            {
                CanBuy = false;
                item.UiRefs.QuantityText.text = "Sold Out";
                item.UiRefs.PriceText.color = item.UiRefs.TooExpensivePriceColor;
                return;
            }

            if (SelectionCount >= item.CurrentStatus.AvailableQuantity)
            {
                SelectionCount = item.CurrentStatus.AvailableQuantity;
                SelectionCountCache = SelectionCount;
            }

            if (SelectionCount <= 0)
            {
                SelectionCount = 1;
                SelectionCountCache = SelectionCount;
            }

            ItemDescriptionText.text = item.Item.FlavorText;

            if (item.Item.IsWeaponOrMagicSubtype())
            {
                if (WeaponExtraDetails != null)
                {
                    WeaponExtraDetails.SetActive(true);
                }
                if (WeaponMightText != null)
                {
                    WeaponMightText.text = $"{item.Item.Might}";
                }
                if (WeaponHitText != null)
                {
                    WeaponHitText.text = $"{item.Item.Hit}";
                }
                if (WeaponCritText != null)
                {
                    WeaponCritText.text = $"{item.Item.Critical}";
                }
            }
            else
            {
                if (WeaponExtraDetails != null)
                {
                    WeaponExtraDetails.SetActive(false);
                }
            }

            if (WeaponDurabilityText != null)
            {
                WeaponDurabilityText.text = item.Item.IsWeaponOrMagicSubtypeAndIsDurability()
                    ? $"{item.Item.MaxUses}"
                    : "--";
            }

            item.UiRefs.ItemNameText.text = item.Item.Name;
            if (item.Item.IsWeaponOrMagicSubtype())
            {
                item.UiRefs.ItemCategoryText.gameObject.SetActive(true);
                if (item.Item.WeaponType != null)
                {
                    item.UiRefs.ItemCategoryText.text = item.Item.WeaponType.ToString();
                    item.UiRefs.ItemIcon.sprite = item.Item.WeaponType.Icon;
                }
                else
                {
                    item.UiRefs.ItemCategoryText.text = "Unknown Type";
                    item.UiRefs.ItemIcon.sprite = null;
                }
                if (item.Item.MinWeaponTypeAptitude != null)
                {
                    item.UiRefs.LetterIcon.sprite = item.Item.MinWeaponTypeAptitude.GetLetterIcon();
                    item.UiRefs.LetterIcon.color = Color.white;
                }
                else
                {
                    item.UiRefs.LetterIcon.color = new Color(1, 1, 1, 0);
                }
            }
            else
            {
                item.UiRefs.ItemCategoryText.gameObject.SetActive(false);
                item.UiRefs.LetterIcon.color = new Color(1, 1, 1, 0); // hide letter icon for non-weapon items

                var itemTypeIcons = GamewideUiSettings.Instance.ItemTypeIcons;
                if (itemTypeIcons != null && itemTypeIcons.Length > 0)
                {
                    var iconIndex = System.Array.FindIndex(
                        itemTypeIcons,
                        x => x.Subtype == item.Item.Subtype
                    );

                    if (iconIndex >= 0)
                    {
                        var iconEntry = itemTypeIcons[iconIndex];
                        item.UiRefs.ItemIcon.sprite =
                            iconEntry.Icon != null ? iconEntry.Icon : null;
                    }
                    else
                    {
                        item.UiRefs.ItemIcon.sprite = null;
                    }
                }
                else
                {
                    item.UiRefs.ItemIcon.sprite = null; // no icon table provided
                }
            }

            item.UiRefs.PriceText.text = item.CurrentStatus.IsOnSale
                ? $"{item.SalePrice * SelectionCount}G"
                : $"{item.Item.BasePrice * SelectionCount}G";

            item.UiRefs.PriceText.color =
                (
                    brain?.storehouseBrain != null
                    && !brain.storehouseBrain.CanAfford(item.Item.BasePrice * SelectionCount)
                )
                    ? item.UiRefs.TooExpensivePriceColor
                : item.CurrentStatus.IsOnSale ? item.UiRefs.OnSalePriceColor
                : item.UiRefs.DefaultPriceColor;

            if (item.UiRefs.TooExpensivePriceColor == item.UiRefs.PriceText.color)
            {
                CanBuy = false;
            }

            CostCache = item.CurrentStatus.IsOnSale
                ? item.SalePrice * SelectionCount
                : item.Item.BasePrice * SelectionCount;

            item.UiRefs.SaleBadge.gameObject.SetActive(item.CurrentStatus.IsOnSale);
            item.UiRefs.QuantityText.text =
                brain?.storehouseBrain != null
                    ? $"Buy {SelectionCount} of {item.CurrentStatus.AvailableQuantity}\nOwn: {brain.storehouseBrain.GetItemCountInStorehouse(item.Item)}"
                    : $"Buy {SelectionCount} of {item.CurrentStatus.AvailableQuantity}";
        }

        private void PlayPageChangeSound()
        {
            if (AudioPlayer == null || PageChangeAudioClip == null)
            {
                return;
            }

            AudioPlayer.PlayOneShot(PageChangeAudioClip);
        }

        private void InitializePageIndicators()
        {
            if (PageIndicatorContainer == null)
            {
                return;
            }

            // Clear any existing indicators before rebuilding for new page count.
            foreach (var indicator in pageIndicatorObjects)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
            pageIndicatorObjects.Clear();

            for (var i = 0; i < totalPages; i++)
            {
                var pageIndicatorObj = new GameObject($"PageIndicator_{i}", typeof(Image));
                pageIndicatorObj.transform.SetParent(PageIndicatorContainer.transform, false);
                var image = pageIndicatorObj.GetComponent<Image>();
                image.sprite =
                    i == CurrentPage ? ActivePageIndicatorSprite : InactivePageIndicatorSprite;
                var rectTransform = pageIndicatorObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(PageIndicatorSize, PageIndicatorSize);
                pageIndicatorObjects.Add(pageIndicatorObj);
            }
        }

        private void UpdatePaginationIndicators()
        {
            for (var i = 0; i < pageIndicatorObjects.Count; i++)
            {
                var image = pageIndicatorObjects[i]?.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }
                image.sprite =
                    i == CurrentPage ? ActivePageIndicatorSprite : InactivePageIndicatorSprite;
            }
        }

        private void UpdateVisiblePageItems()
        {
            if (itemChoices == null)
            {
                return;
            }

            var startIndex = CurrentPage * ItemsPerPage;
            var endIndex = Mathf.Min(startIndex + ItemsPerPage, itemChoices.Count);

            for (var i = 0; i < itemChoices.Count; i++)
            {
                var choice = itemChoices[i];
                if (choice?.gameObject == null)
                {
                    continue;
                }

                bool isVisible = i >= startIndex && i < endIndex;
                choice.gameObject.SetActive(isVisible);
            }
        }

        private void RefreshSelection()
        {
            if (itemChoices == null || itemChoices.Count == 0)
            {
                $"ShopUi: No item choices available to refresh selection.".LogWarning();
                return;
            }

            CurrentSelectionIndex = Mathf.Clamp(CurrentSelectionIndex, 0, itemChoices.Count - 1);

            // Ensure a valid item is selected on the current page.
            if (itemChoices[CurrentSelectionIndex] == null)
            {
                var start = CurrentPage * ItemsPerPage;
                var end = Mathf.Min(start + ItemsPerPage, itemChoices.Count);
                var found = -1;
                for (var i = start; i < end; i++)
                {
                    if (itemChoices[i] != null)
                    {
                        found = i;
                        break;
                    }
                }
                if (found != -1)
                {
                    CurrentSelectionIndex = found;
                }
            }

            for (var i = 0; i < itemChoices.Count; i++)
            {
                if (itemChoices[i] == null)
                {
                    $"ShopUi: Item choice at index {i} is null.".LogWarning();
                    continue;
                }

                if (i == CurrentSelectionIndex)
                {
                    itemChoices[i].Select();
                    if (
                        ShopData?.ItemsStocked != null
                        && itemChoiceToShopIndex != null
                        && i < itemChoiceToShopIndex.Count
                    )
                    {
                        int shopIndex = itemChoiceToShopIndex[i];
                        if (
                            shopIndex >= 0
                            && shopIndex < ShopData.ItemsStocked.Length
                            && ShopData.ItemsStocked[shopIndex].Item != null
                            && ShopData.ItemsStocked[shopIndex].UiRefs != null
                        )
                        {
                            HandeSelectedItem(ShopData.ItemsStocked[shopIndex]);
                        }
                    }
                }
                else
                {
                    itemChoices[i].Deselect();

                    var uiRefs = itemChoices[i].GetComponent<ShopItemUiRefs>();
                    if (uiRefs != null && uiRefs.QuantityText != null)
                    {
                        uiRefs.QuantityText.text = ""; // clear quantity text on deselect to avoid confusion about which item it applies to
                    }
                    else
                    {
                        $"ShopUi: Missing ShopItemUiRefs/QuantityText for deselected item at index {i}.".LogWarning();
                    }
                }
            }
        }

        private void ClearInstantiatedItems()
        {
            // Destroy all child item objects from the items container.
            if (ItemsParentContainer != null)
            {
                for (int i = ItemsParentContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(ItemsParentContainer.transform.GetChild(i).gameObject);
                }
            }

            // Destroy all child page indicator objects.
            if (PageIndicatorContainer != null)
            {
                for (int i = PageIndicatorContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(PageIndicatorContainer.transform.GetChild(i).gameObject);
                }
            }
            pageIndicatorObjects.Clear();

            // Clear the cached UiRefs on each stocked item so they'll be re-created.
            var stock = ShopData.ItemsStocked;
            if (stock != null)
            {
                for (int i = 0; i < stock.Length; i++)
                {
                    var item = stock[i];
                    item.UiRefs = null;
                    stock[i] = item;
                }
            }

            itemChoices = null;
            itemChoiceToShopIndex = null;
        }
    }
}
