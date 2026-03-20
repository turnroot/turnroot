using TMPro;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [RequireComponent(typeof(Shop))]
    public class ShopUi : MonoBehaviour
    {
        public Shop ShopData => GetComponent<Shop>();

        [HideInInspector]
        public ShopItemUiRefs[] ItemUiRefs;

        private UiChoice[] itemChoices;

        [Header("UI References")]
        public UIFade ShopUiFade;
        public GameObject ItemPrefab;
        public TextMeshProUGUI ShopNameText;
        public TextMeshProUGUI ShopDescriptionText;
        public TextMeshProUGUI ItemDescriptionText;
        public TextMeshProUGUI TotalGoldText;
        public GameObject ItemsParentContainer;

        [Header("Weapon Details UI")]
        public GameObject WeaponExtraDetails;
        public TextMeshProUGUI WeaponMightText;
        public TextMeshProUGUI WeaponHitText;
        public TextMeshProUGUI WeaponCritText;
        public TextMeshProUGUI WeaponDurabilityText;

        [Header("Page Indicator UI")]
        public GameObject PageIndicatorContainer;
        public Sprite InactivePageIndicatorSprite;
        public Sprite ActivePageIndicatorSprite;
        public float PageIndicatorSize = 30f;
        public int ItemsPerPage = 6;

        private int totalPages;
        public int CurrentPage { get; private set; } = 0;

        public void RefreshShopDisplay()
        {
            ShopNameText.text = ShopData.ShopName;
            ShopDescriptionText.text = ShopData.ShopDescription;
            var stock = ShopData.ItemsStocked;

            TotalGoldText.text = $"Gold: {ShopData.brain.storehouseBrain.PlayerGold}G";

            itemChoices = new UiChoice[stock.Length];

            for (var i = 0; i < stock.Length; i++)
            {
                var item = stock[i];
                if (item.UiRefs == null || item.UiRefs.ShopItemChoice == null)
                {
                    if (item.CurrentStatus.AvailableQuantity <= 0)
                    {
                        continue;
                    }
                    var itemUiObj = Instantiate(ItemPrefab, ItemsParentContainer.transform);
                    item.UiRefs = itemUiObj.GetComponent<ShopItemUiRefs>();
                    stock[i] = item;
                }

                if (item.UiRefs != null)
                {
                    itemChoices[i] = item.UiRefs.ShopItemChoice;
                    // set up item,
                    item.UiRefs.ItemNameText.text = item.Item.Name;
                    if (item.Item.IsWeaponOrMagicSubtype())
                    {
                        item.UiRefs.ItemCategoryText.gameObject.SetActive(true);
                        item.UiRefs.ItemCategoryText.text = item.Item.WeaponType.ToString();
                        item.UiRefs.ItemIcon.sprite = item.Item.WeaponType.Icon;
                        item.UiRefs.LetterIcon.sprite =
                            item.Item.MinWeaponTypeAptitude.GetLetterIcon();
                        item.UiRefs.LetterIcon.color = Color.white; // ensure letter icon is visible for weapon items
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
                                if (iconEntry.Icon != null)
                                {
                                    item.UiRefs.ItemIcon.sprite = iconEntry.Icon;
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
                        else
                        {
                            item.UiRefs.ItemIcon.sprite = null; // no icon table provided
                        }
                    }
                    item.UiRefs.PriceText.text = item.CurrentStatus.IsOnSale
                        ? $"{item.SalePrice}G"
                        : $"{item.Item.BasePrice}G";

                    item.UiRefs.PriceText.color =
                        !ShopData.brain.storehouseBrain.CanAfford(item.Item.BasePrice)
                            ? item.UiRefs.TooExpensivePriceColor
                        : item.CurrentStatus.IsOnSale ? item.UiRefs.OnSalePriceColor
                        : item.UiRefs.DefaultPriceColor;

                    item.UiRefs.SaleBadge.gameObject.SetActive(item.CurrentStatus.IsOnSale);
                    item.UiRefs.QuantityText.text =
                        $"Buy 1 of {item.CurrentStatus.AvailableQuantity}\nOwn: {ShopData.brain.storehouseBrain.GetItemCountInStorehouse(item.Item)}";
                }
            }
            ShopData.ItemsStocked = stock;
            totalPages = Mathf.CeilToInt((float)ShopData.ItemsStocked.Length / ItemsPerPage);
            CurrentPage = 0;

            // Ensure only first item is selected by default
            for (var i = 0; i < itemChoices.Length; i++)
            {
                if (itemChoices[i] == null)
                {
                    continue;
                }
                if (i == 0)
                {
                    itemChoices[0].Select();
                    HandeSelectedItem(ShopData.ItemsStocked[0]);
                }
                else
                {
                    itemChoices[i].Deselect();
                }
            }

            // spawn a page indicator for each page, and set them all to inactive except the first one
            for (var i = 0; i < totalPages; i++)
            {
                var pageIndicatorObj = new GameObject($"PageIndicator_{i}", typeof(Image));
                pageIndicatorObj.transform.SetParent(PageIndicatorContainer.transform);
                var image = pageIndicatorObj.GetComponent<Image>();
                image.sprite =
                    i == CurrentPage ? ActivePageIndicatorSprite : InactivePageIndicatorSprite;
                var rectTransform = pageIndicatorObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(PageIndicatorSize, PageIndicatorSize);
            }

            ShopUiFade.Show();
        }

        public void HandeSelectedItem(ShopItem selectedItem)
        {
            ItemDescriptionText.text = selectedItem.Item.FlavorText;
            if (selectedItem.Item.IsWeaponOrMagicSubtype())
            {
                WeaponExtraDetails.SetActive(true);
                WeaponMightText.text = $"{selectedItem.Item.Might}";
                WeaponHitText.text = $"{selectedItem.Item.Hit}";
                WeaponCritText.text = $"{selectedItem.Item.Critical}";
                if (selectedItem.Item.IsWeaponOrMagicSubtypeAndIsDurability())
                {
                    WeaponDurabilityText.text += $"({selectedItem.Item.MaxUses})";
                }
                else
                {
                    WeaponDurabilityText.text = "--";
                }
            }
            else
            {
                WeaponExtraDetails.SetActive(false);
            }
            if (selectedItem.Item.IsWeaponOrMagicSubtypeAndIsDurability())
            {
                WeaponDurabilityText.text += $"({selectedItem.Item.MaxUses})";
            }
            else
            {
                WeaponDurabilityText.text = "--";
            }
        }
    }
}
