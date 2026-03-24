using System.Collections.Generic;
using System.Linq;
using TMPro;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.Ui;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    public struct BlacksmithRepairItem
    {
        public ObjectItemInstance ItemToRepair;
        public bool BelongsToCharacter; // if false, belongs to storehouse

        public CharacterInstance CharacterOwner; // null if belongs to storehouse

        public BlacksmithRepairItem(
            ObjectItemInstance itemToRepair,
            bool belongsToCharacter,
            CharacterInstance characterOwner
        )
        {
            ItemToRepair = itemToRepair;
            BelongsToCharacter = belongsToCharacter;
            CharacterOwner = characterOwner;
        }

        public BlacksmithRepairItem(ObjectItemInstance itemToRepair)
        {
            ItemToRepair = itemToRepair;
            BelongsToCharacter = false;
            CharacterOwner = null;
        }
    }

    public struct BlacksmithForgeableItem
    {
        public ObjectItemInstance ItemToForge;
        public bool BelongsToCharacter;

        public CharacterInstance CharacterOwner;

        public BlacksmithForgeableItem(
            ObjectItemInstance itemToForge,
            bool belongsToCharacter,
            CharacterInstance characterOwner
        )
        {
            ItemToForge = itemToForge;
            BelongsToCharacter = belongsToCharacter;
            CharacterOwner = characterOwner;
        }

        public BlacksmithForgeableItem(ObjectItemInstance itemToForge)
        {
            ItemToForge = itemToForge;
            BelongsToCharacter = false;
            CharacterOwner = null;
        }
    }

    public enum BlacksmithMode
    {
        Repair,
        Forge,
    }

    [RequireComponent(typeof(Blacksmith))]
    public partial class BlacksmithUi : MonoBehaviour
    {
        private BlacksmithRepairItem[] repairableItems;
        private BlacksmithForgeableItem[] forgeableItems;

        private PaginationHelper paginationHelper;

        private List<UiChoice> itemChoices;
        private List<int> itemChoiceToIndex;

        public Blacksmith BlacksmithData => GetComponent<Blacksmith>();

        private Brain.Brain brain => BlacksmithData._brain;

        [HideInInspector]
        public BlacksmithItemRefs[] ItemUiRefs;

        [Header("UI References")]
        public UIFade BlacksmithUiFade;
        public GameObject ItemPrefab;
        public TextMeshProUGUI BlacksmithModeText;
        public BlacksmithMode CurrentMode { get; private set; } = BlacksmithMode.Repair;

        public bool CanForge = true;
        public bool CanRepair = true;
        public TextMeshProUGUI TotalGoldText;
        public ScrollDownNumber TotalGoldScroll;
        public GameObject ItemsParentContainer;

        private int SelectionCountCache = 1;
        private int CostCache = 0;

        [Header("Page Indicator UI")]
        public GameObject PageIndicatorContainer;
        public Sprite InactivePageIndicatorSprite;
        public Sprite ActivePageIndicatorSprite;
        public float PageIndicatorSize = 30f;
        public int ItemsPerPage = 12;

        public AudioSource AudioPlayer;
        public AudioClip PageChangeAudioClip;
        public AudioClip NavigateAudioClip;
        private readonly List<GameObject> pageIndicatorObjects = new();
        private int totalPages;
        public int CurrentPage { get; private set; } = 0;
        public int CurrentSelectionIndex { get; private set; } = 0;

        private void Awake()
        {
            var settings = GameplayGeneralSettings.Instance;
            if (settings != null)
            {
                CanForge = settings.WeaponsCanBeForged;
                CanRepair = settings.WeaponsCanBeRepaired;
            }

            // Keep fallback defaults to true, but avoid constructor-time Resources.Load calls.
        }

        public void RefreshBlacksmithDisplay()
        {
            // Sync gold display
            if (TotalGoldText != null && brain?.storehouseBrain != null)
            {
                TotalGoldText.text = $"Gold: {brain.storehouseBrain.PlayerGold}G";
            }
            else if (TotalGoldText != null)
            {
                TotalGoldText.text = "Gold: ???";
                TotalGoldScroll.StartNumber =
                    brain?.storehouseBrain != null ? brain.storehouseBrain.PlayerGold : 0;
            }

            SelectionCountCache = 1;
            CostCache = 0;

            // Clearing is handled in BuildItemListForCurrentMode, to reuse across refresh cycles.

            if (CurrentMode == BlacksmithMode.Repair && CanRepair)
            {
                GetRepairableItems();
            }
            else if (CurrentMode == BlacksmithMode.Forge && CanForge)
            {
                GetForgeableItems();
            }
            else if (!CanRepair && !CanForge)
            {
                // shouldn't be here if both are false, but just in case
                "Blacksmith cannot repair or forge any items based on current game settings.".LogWarning();
                return;
            }

            BuildItemListForCurrentMode();

            paginationHelper ??= new PaginationHelper(
                ItemsPerPage,
                PageIndicatorContainer?.transform,
                ActivePageIndicatorSprite,
                InactivePageIndicatorSprite,
                PageIndicatorSize,
                AudioPlayer,
                PageChangeAudioClip
            );

            paginationHelper.ItemsPerPage = ItemsPerPage;
            paginationHelper.SetItemChoices(itemChoices, CurrentSelectionIndex);
            CurrentPage = paginationHelper.CurrentPage;
            CurrentSelectionIndex = paginationHelper.CurrentSelectionIndex;

            BlacksmithUiFade.Show();
        }

        private void ConfigureRepairItemUi(
            BlacksmithRepairItem itemData,
            BlacksmithItemRefs refs,
            int selectionCount
        )
        {
            if (refs == null || itemData.ItemToRepair == null)
            {
                return;
            }

            var instance = itemData.ItemToRepair;
            var template = instance.Template;

            refs.ItemNameText.text = template.Name;
            refs.UsesText.text =
                template != null && template.Durability
                    ? $"Uses: {instance.CurrentUses}/{template.MaxUses}"
                    : string.Empty;
            refs.RepairsText.text = $"Repair: +{selectionCount}";
            refs.GoldCostText.text = $"{GetRepairCost(itemData) * selectionCount}G";
            if (template.RepairItem != null)
            {
                refs.RepairItemNameText.text = template.RepairItem.Name;
                if (template.OneRepairItemCoversFullRepair)
                {
                    refs.RepairItemCostText.text = $"x1";
                }
                else
                {
                    var RepairItemCount = Mathf.CeilToInt(
                        (template.MaxUses - instance.CurrentUses)
                            / (float)template.RepairItemAmountPerUse
                    );
                    refs.RepairItemCostText.text = $"x{RepairItemCount}";
                }
            }
            else
            {
                refs.RepairItemNameText.text = "--";
                refs.RepairItemCostText.text = "--";
            }

            CostCache = GetRepairCost(itemData) * selectionCount;
        }

        private int GetRepairCost(BlacksmithRepairItem itemData)
        {
            return itemData.ItemToRepair?.Template?.RepairPricePerUse ?? 0;
        }

        private void ConfigureForgeItemUi(
            BlacksmithForgeableItem itemData,
            BlacksmithItemRefs refs,
            int selectionCount
        )
        {
            if (refs == null || itemData.ItemToForge == null)
            {
                return;
            }

            var instance = itemData.ItemToForge;
            var template = instance.Template;

            refs.ItemNameText.text = template?.Name ?? "Unknown";
            refs.UsesText.text =
                template != null && template.Durability
                    ? $"Uses: {instance.CurrentUses}/{template.MaxUses}"
                    : string.Empty;
            refs.RepairsText.text = "Forgeable";
            refs.GoldCostText.text =
                template?.ForgeOptions != null && template.ForgeOptions.Length > 0
                    ? $"{template.ForgeOptions[0].Price * selectionCount}G"
                    : "0G";
            refs.RepairItemNameText.text = string.Empty;
            refs.RepairItemCostText.text = string.Empty;

            CostCache =
                template?.ForgeOptions != null && template.ForgeOptions.Length > 0
                    ? template.ForgeOptions[0].Price * selectionCount
                    : 0;
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
            itemChoices = null;
            itemChoiceToIndex = null;
        }
    }
}
