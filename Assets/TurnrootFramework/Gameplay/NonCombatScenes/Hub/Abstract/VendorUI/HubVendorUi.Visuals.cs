using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Abstract
{
    public partial class HubVendorUi
    {
        protected void ConfigureItemUi(ShopItem item, int selectionCount, bool updateGlobal = true)
        {
            if (item.Item == null || item.UiRefs == null)
            {
                return;
            }

            if (updateGlobal)
            {
                CanBuy = true;
            }

            if (item.CurrentStatus.AvailableQuantity <= 0)
            {
                if (updateGlobal)
                {
                    CanBuy = false;
                }
                item.UiRefs.QuantityText.text = "Sold Out";
                item.UiRefs.PriceText.color = item.UiRefs.TooExpensivePriceColor;
                item.UiRefs.SaleBadge.gameObject.SetActive(false);
                return;
            }

            if (selectionCount >= item.CurrentStatus.AvailableQuantity)
            {
                selectionCount = item.CurrentStatus.AvailableQuantity;
                if (updateGlobal)
                {
                    SelectionCountCache = selectionCount;
                }
            }

            if (selectionCount <= 0)
            {
                selectionCount = 1;
                if (updateGlobal)
                {
                    SelectionCountCache = selectionCount;
                }
            }

            if (updateGlobal)
            {
                ItemDescriptionText.text = item.Item.FlavorText;

                if (item.Item.IsWeaponOrMagicSubtype())
                {
                    WeaponExtraDetails?.SetActive(true);
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
                    WeaponExtraDetails?.SetActive(false);
                }

                if (WeaponDurabilityText != null)
                {
                    WeaponDurabilityText.text = item.Item.IsWeaponOrMagicSubtypeAndIsDurability()
                        ? $"{item.Item.MaxUses}"
                        : "--";
                }
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
                item.UiRefs.LetterIcon.color = new Color(1, 1, 1, 0);

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
                    item.UiRefs.ItemIcon.sprite = null;
                }
            }

            item.UiRefs.PriceText.text = item.CurrentStatus.IsOnSale
                ? $"{item.SalePrice * selectionCount}G"
                : $"{item.Item.BasePrice * selectionCount}G";

            var isTooExpensive =
                brain?.storehouseBrain != null
                && !brain.storehouseBrain.CanAfford(item.Item.BasePrice * selectionCount);

            item.UiRefs.PriceText.color =
                isTooExpensive ? item.UiRefs.TooExpensivePriceColor
                : item.CurrentStatus.IsOnSale ? item.UiRefs.OnSalePriceColor
                : item.UiRefs.DefaultPriceColor;

            if (isTooExpensive && updateGlobal)
            {
                CanBuy = false;
            }

            if (updateGlobal)
            {
                CostCache = item.CurrentStatus.IsOnSale
                    ? item.SalePrice * selectionCount
                    : item.Item.BasePrice * selectionCount;
            }

            item.UiRefs.SaleBadge.gameObject.SetActive(item.CurrentStatus.IsOnSale);
            item.UiRefs.QuantityText.text =
                brain?.storehouseBrain != null
                    ? $"Buy {selectionCount} of {item.CurrentStatus.AvailableQuantity}\nOwn: {brain.storehouseBrain.GetItemCountInStorehouse(item.Item)}"
                    : $"Buy {selectionCount} of {item.CurrentStatus.AvailableQuantity}";
        }

        protected void PlayPageChangeSound()
        {
            if (AudioPlayer == null || PageChangeAudioClip == null)
            {
                return;
            }
            AudioPlayer.PlayOneShot(PageChangeAudioClip);
        }

        protected void InitializePageIndicators()
        {
            paginationHelper?.InitializePageIndicators();
        }

        protected void UpdatePaginationIndicators()
        {
            paginationHelper?.UpdatePaginationIndicators();
        }

        protected void UpdateVisiblePageItems()
        {
            paginationHelper?.UpdateVisiblePageItems();
        }

        protected void RefreshSelection()
        {
            paginationHelper?.RefreshSelection();
            if (paginationHelper != null)
            {
                CurrentPage = paginationHelper.CurrentPage;
                CurrentSelectionIndex = paginationHelper.CurrentSelectionIndex;
            }
        }

        protected void ClearInstantiatedItems()
        {
            HubVendorUiHelper.ClearInstantiatedItems(
                ItemsParentContainer,
                PageIndicatorContainer,
                pageIndicatorObjects,
                ref itemChoices,
                ref itemChoiceToVendorIndex
            );

            var stock = VendorItems;
            if (stock != null)
            {
                for (int i = 0; i < stock.Length; i++)
                {
                    var item = stock[i];
                    item.UiRefs = null;
                    stock[i] = item;
                }
            }
        }
    }
}
