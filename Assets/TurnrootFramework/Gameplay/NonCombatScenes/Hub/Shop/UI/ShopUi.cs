using TMPro;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities.AbstractScripts;
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

        public AudioSource PageChangeAudioSource;
        public AudioClip PageChangeAudioClip;

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
            }

            itemChoices = new System.Collections.Generic.List<UiChoice>(stock.Length);

            for (var i = 0; i < stock.Length; i++)
            {
                var item = stock[i];
                if (item.Item == null)
                {
                    continue;
                }

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

                if (item.UiRefs != null && item.UiRefs.ShopItemChoice != null)
                {
                    itemChoices.Add(item.UiRefs.ShopItemChoice);
                    ConfigureItemUi(item, SelectionCountCache);
                }
            }
            ShopData.ItemsStocked = stock;
            totalPages = Mathf.CeilToInt((float)ShopData.ItemsStocked.Length / ItemsPerPage);
            CurrentPage = 0;

            InitializePageIndicators();
            UpdateVisiblePageItems();
            RefreshSelection();
            UpdatePaginationIndicators();

            ShopUiFade.Show();
        }
    }
}
