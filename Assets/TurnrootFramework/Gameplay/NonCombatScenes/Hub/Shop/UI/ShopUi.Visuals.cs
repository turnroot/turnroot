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
            paginationHelper?.InitializePageIndicators();
        }

        private void UpdatePaginationIndicators()
        {
            paginationHelper?.UpdatePaginationIndicators();
        }

        private void UpdateVisiblePageItems()
        {
            paginationHelper?.UpdateVisiblePageItems();
        }

        private void RefreshSelection()
        {
            paginationHelper?.RefreshSelection();
            if (paginationHelper != null)
            {
                CurrentPage = paginationHelper.CurrentPage;
                CurrentSelectionIndex = paginationHelper.CurrentSelectionIndex;
            }
        }

        private void ClearInstantiatedItems()
        {
            if (ItemsParentContainer != null)
            {
                for (int i = ItemsParentContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(ItemsParentContainer.transform.GetChild(i).gameObject);
                }
            }

            if (PageIndicatorContainer != null)
            {
                for (int i = PageIndicatorContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(PageIndicatorContainer.transform.GetChild(i).gameObject);
                }
            }

            pageIndicatorObjects.Clear();

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
