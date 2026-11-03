using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.GameSettings;
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
                ConfigureSoldOutItem(item, updateGlobal);
                return;
            }

            selectionCount = ClampSelectionCount(item, selectionCount, updateGlobal);
            if (updateGlobal)
            {
                ConfigureSelectedItemDetails(item);
            }

            ConfigureItemIdentity(item);
            ConfigurePriceAndQuantity(item, selectionCount, updateGlobal);
        }

        private void ConfigureSoldOutItem(ShopItem item, bool updateGlobal)
        {
            if (updateGlobal)
            {
                CanBuy = false;
            }

            item.UiRefs.QuantityText.text = "Sold Out";
            item.UiRefs.PriceText.color = item.UiRefs.TooExpensivePriceColor;
            item.UiRefs.SaleBadge.gameObject.SetActive(false);
        }

        private int ClampSelectionCount(ShopItem item, int selectionCount, bool updateGlobal)
        {
            selectionCount = selectionCount >= item.CurrentStatus.AvailableQuantity
                ? item.CurrentStatus.AvailableQuantity
                : selectionCount;
            selectionCount = selectionCount <= 0 ? 1 : selectionCount;

            if (updateGlobal)
            {
                SelectionCountCache = selectionCount;
            }

            return selectionCount;
        }

        private void ConfigureSelectedItemDetails(ShopItem item)
        {
            ItemDescriptionText.text = item.Item.FlavorText;
            bool isWeapon = item.Item.IsWeaponOrMagicSubtype();
            WeaponExtraDetails?.SetActive(isWeapon);

            if (isWeapon)
            {
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

            if (WeaponDurabilityText != null)
            {
                WeaponDurabilityText.text = item.Item.IsWeaponOrMagicSubtypeAndIsDurability()
                    ? $"{item.Item.MaxUses}"
                    : "--";
            }
        }

        private void ConfigureItemIdentity(ShopItem item)
        {
            item.UiRefs.ItemNameText.text = item.Item.Name;
            if (item.Item.IsWeaponOrMagicSubtype())
            {
                ConfigureWeaponIdentity(item);
                return;
            }

            item.UiRefs.ItemCategoryText.gameObject.SetActive(false);
            item.UiRefs.LetterIcon.color = new Color(1, 1, 1, 0);
            item.UiRefs.ItemIcon.sprite = FindItemTypeIcon(item);
        }

        private void ConfigureWeaponIdentity(ShopItem item)
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

        private Sprite FindItemTypeIcon(ShopItem item)
        {
            var itemTypeIcons = GamewideUiSettings.Instance.ItemTypeIcons;
            if (itemTypeIcons == null || itemTypeIcons.Length == 0)
            {
                return null;
            }

            int iconIndex = System.Array.FindIndex(
                itemTypeIcons,
                icon => icon.Subtype == item.Item.Subtype
            );
            return iconIndex >= 0 ? itemTypeIcons[iconIndex].Icon : null;
        }

        private void ConfigurePriceAndQuantity(
            ShopItem item,
            int selectionCount,
            bool updateGlobal
        )
        {
            int unitPrice = item.CurrentStatus.IsOnSale ? item.SalePrice : item.Item.BasePrice;
            int totalCost = unitPrice * selectionCount;
            bool isTooExpensive =
                brain?.storehouseBrain != null && !brain.storehouseBrain.CanAfford(totalCost);

            item.UiRefs.PriceText.text = $"{totalCost}G";
            item.UiRefs.PriceText.color = isTooExpensive
                ? item.UiRefs.TooExpensivePriceColor
                : item.CurrentStatus.IsOnSale
                    ? item.UiRefs.OnSalePriceColor
                    : item.UiRefs.DefaultPriceColor;

            if (updateGlobal)
            {
                CanBuy = !isTooExpensive;
                CostCache = totalCost;
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

        protected void InitializePageIndicators() => paginationHelper?.InitializePageIndicators();

        protected void UpdatePaginationIndicators() =>
            paginationHelper?.UpdatePaginationIndicators();

        protected void UpdateVisiblePageItems() => paginationHelper?.UpdateVisiblePageItems();

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
