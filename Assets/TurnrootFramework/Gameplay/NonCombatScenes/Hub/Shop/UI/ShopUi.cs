using TMPro;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [RequireComponent(typeof(Shop))]
    public partial class ShopUi : MonoBehaviour
    {
        public Shop ShopData => GetComponent<Shop>();

        private Brain.Brain brain;

        [HideInInspector]
        public ShopItemUiRefs[] ItemUiRefs;

        private System.Collections.Generic.List<UiChoice> itemChoices;
        private System.Collections.Generic.List<int> itemChoiceToShopIndex;

        [Header("UI References")]
        public UIFade ShopUiFade;
        public GameObject ItemPrefab;
        public TextMeshProUGUI ShopNameText;
        public TextMeshProUGUI ShopDescriptionText;
        public TextMeshProUGUI ItemDescriptionText;
        public TextMeshProUGUI TotalGoldText;
        public ScrollDownNumber TotalGoldScroll;
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

        public AudioSource AudioPlayer;
        public AudioClip PageChangeAudioClip;
        public AudioClip NavigateAudioClip;

        private readonly System.Collections.Generic.List<GameObject> pageIndicatorObjects = new();

        private int totalPages;
        public int CurrentPage { get; private set; } = 0;

        public int CurrentSelectionIndex { get; private set; } = 0;

        public void RefreshShopDisplay()
        {
            // Destroy previously instantiated item UI objects so they don't accumulate
            // across visits to different shops
            ClearInstantiatedItems();

            if (ShopNameText != null)
            {
                ShopNameText.text = ShopData.ShopName ?? string.Empty;
            }

            if (ShopDescriptionText != null)
            {
                ShopDescriptionText.text = ShopData.ShopDescription ?? string.Empty;
            }

            var stock = ShopData.ItemsStocked;

            // Reset selection state on full rebuild so stale quantity settings don't stick.
            SelectionCountCache = 1;
            CostCache = 0;

            ItemsPerPage = Mathf.Max(1, ItemsPerPage);

            if (stock == null || stock.Length == 0)
            {
                ShopUiFade.Show();
                return;
            }

            brain = ShopData.brain;
            if (TotalGoldText != null && brain?.storehouseBrain != null)
            {
                TotalGoldText.text = $"Gold: {brain.storehouseBrain.PlayerGold}G";
            }
            else if (TotalGoldText != null)
            {
                TotalGoldText.text = "Gold: ???";
                TotalGoldScroll.StartNumber =
                    brain.storehouseBrain != null ? brain.storehouseBrain.PlayerGold : 0;
            }

            // Preserve currently selected shop item index alignment through refresh.
            // This ensures page/selection logic remains consistent when an item is removed
            // from visible stock due sale.
            int previousSelectedShopIndex = -1;
            if (
                itemChoiceToShopIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToShopIndex.Count
            )
            {
                previousSelectedShopIndex = itemChoiceToShopIndex[CurrentSelectionIndex];
            }

            itemChoices = new System.Collections.Generic.List<UiChoice>(stock.Length);
            itemChoiceToShopIndex = new System.Collections.Generic.List<int>(stock.Length);

            for (var i = 0; i < stock.Length; i++)
            {
                var item = stock[i];
                if (item.Item == null || item.CurrentStatus.AvailableQuantity <= 0)
                {
                    continue;
                }

                if (item.UiRefs == null || item.UiRefs.ShopItemChoice == null)
                {
                    var itemUiObj = Instantiate(ItemPrefab, ItemsParentContainer.transform);
                    item.UiRefs = itemUiObj.GetComponent<ShopItemUiRefs>();
                    stock[i] = item;
                }

                if (item.UiRefs != null && item.UiRefs.ShopItemChoice != null)
                {
                    itemChoices.Add(item.UiRefs.ShopItemChoice);
                    itemChoiceToShopIndex.Add(i);
                    ConfigureItemUi(item, SelectionCountCache);
                }
            }
            ShopData.ItemsStocked = stock;
            totalPages = Mathf.CeilToInt((float)itemChoices.Count / ItemsPerPage);

            if (itemChoices.Count == 0)
            {
                CurrentPage = 0;
                CurrentSelectionIndex = 0;
            }
            else if (previousSelectedShopIndex >= 0)
            {
                int newSelectionIndex = itemChoiceToShopIndex.IndexOf(previousSelectedShopIndex);
                if (newSelectionIndex >= 0)
                {
                    CurrentSelectionIndex = newSelectionIndex;
                    CurrentPage = CurrentSelectionIndex / ItemsPerPage;
                }
                else
                {
                    CurrentSelectionIndex = Mathf.Clamp(
                        CurrentSelectionIndex,
                        0,
                        itemChoices.Count - 1
                    );
                    CurrentPage = CurrentSelectionIndex / ItemsPerPage;
                }
            }
            else
            {
                CurrentPage = 0;
                CurrentSelectionIndex = 0;
            }

            InitializePageIndicators();
            UpdateVisiblePageItems();
            RefreshSelection();
            UpdatePaginationIndicators();

            ShopUiFade.Show();
        }
    }
}
