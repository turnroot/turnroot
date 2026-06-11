using System.Collections.Generic;
using TMPro;
using Turnroot.Characters;
using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.UI;
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

        private Brain.Brain brain => BlacksmithData.brain;

        [HideInInspector]
        public BlacksmithItemRefs[] ItemUiRefs;

        [Header("UI References")]
        public UIFade BlacksmithUiFade;
        public GameObject ItemPrefab;
        public TextMeshProUGUI BlacksmithModeText;
        public BlacksmithMode CurrentMode { get; private set; } = BlacksmithMode.Repair;

        [HideInInspector]
        public bool CanForge = true;

        [HideInInspector]
        public bool CanRepair = true;
        public TextMeshProUGUI TotalGoldText;
        public ScrollDownGold TotalGoldScroll;
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

        [Header("Forge Options Panel")]
        public GameObject ForgeOptionsPanel;
        public GameObject ForgeOptionPrefab;
        public GameObject ForgeOptionsListContainer;
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
        }

        public void SetMode(BlacksmithMode mode)
        {
            CurrentMode = mode;
            RefreshBlacksmithDisplay(false);
        }

        public void RefreshBlacksmithDisplay(bool show = true)
        {
            HubVendorUiHelper.UpdateGoldDisplay(TotalGoldText, TotalGoldScroll, brain);

            SelectionCountCache = 1;
            CostCache = 0;

            if (CurrentMode == BlacksmithMode.Repair && CanRepair)
            {
                GetRepairableItems();
            }
            else if (CurrentMode == BlacksmithMode.Forge && CanForge)
            {
                GetForgeableItems();
            }

            BuildItemListForCurrentMode();

            int newPage;
            int newSelectionIndex;

            HubVendorUiHelper.EnsurePagination(
                ref paginationHelper,
                ItemsPerPage,
                PageIndicatorContainer?.transform,
                ActivePageIndicatorSprite,
                InactivePageIndicatorSprite,
                PageIndicatorSize,
                AudioPlayer,
                PageChangeAudioClip,
                itemChoices,
                CurrentSelectionIndex,
                out newPage,
                out newSelectionIndex
            );

            CurrentPage = newPage;
            CurrentSelectionIndex = newSelectionIndex;

            if (show)
            {
                BlacksmithUiFade.Show();
            }
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

            if (itemData.BelongsToCharacter)
            {
                refs.OwnerPortraitParent.SetActive(true);
                refs.OwnerPortrait.sprite = itemData
                    .CharacterOwner
                    .CharacterTemplate
                    .DefaultPortrait
                    ?.RuntimeSprite;
            }
            else
            {
                refs.OwnerPortraitParent.SetActive(false);
            }

            refs.ItemNameText.text = template.Name;
            refs.UsesText.text =
                template != null && template.Durability
                    ? $"Uses: {template.MaxUses - instance.CurrentUses}/{template.MaxUses}"
                    : string.Empty;
            refs.RepairsText.text = $"Repair: +{selectionCount}";

            var repairPricePerUse = template?.RepairPricePerUse ?? 0;
            if (repairPricePerUse <= 0)
            {
                repairPricePerUse = 0;
            }
            var repairGoldCost = Mathf.CeilToInt(repairPricePerUse * selectionCount);
            refs.GoldCostText.text = $"{repairGoldCost}G";

            if (template.RepairItem != null)
            {
                refs.RepairItemNameText.text = template.RepairItem.Name;
                if (template.OneRepairItemCoversFullRepair)
                {
                    refs.RepairItemCostText.text = $"x1";
                }
                else
                {
                    // Use the selected repair step count, not current damage, to compute required materials.
                    var RepairItemCount = Mathf.CeilToInt(
                        selectionCount / (float)template.RepairItemAmountPerUse
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

        private int GetRepairCost(BlacksmithRepairItem itemData) =>
            itemData.ItemToRepair == null ? 0 : itemData.ItemToRepair.Template.RepairPricePerUse;

        private int GetStorehouseRepairLimit(ObjectItemInstance item)
        {
            if (item == null || item.Template == null || brain?.storehouseBrain == null)
            {
                return 0;
            }

            var template = item.Template;
            if (!template.Repairable || !template.Durability)
            {
                return 0;
            }

            var goldLimit =
                template.RepairPricePerUse > 0
                    ? brain.storehouseBrain.PlayerGold / template.RepairPricePerUse
                    : int.MaxValue;
            int materialLimit = int.MaxValue;
            if (template.RepairNeedsItems && template.RepairItem != null)
            {
                int materialCount = brain.storehouseBrain.GetMaterialCount(template.RepairItem);
                materialLimit = template.OneRepairItemCoversFullRepair
                    ? materialCount > 0
                        ? 1
                        : 0
                    : template.RepairItemAmountPerUse > 0
                        ? materialCount / template.RepairItemAmountPerUse
                        : 0;
            }

            return Mathf.Max(0, Mathf.Min(goldLimit, materialLimit));
        }

        private int GetSelectedRepairMaxCount()
        {
            if (
                CurrentMode != BlacksmithMode.Repair
                || repairableItems == null
                || CurrentSelectionIndex < 0
                || CurrentSelectionIndex >= repairableItems.Length
            )
            {
                return 0;
            }

            var itemToRepair = repairableItems[CurrentSelectionIndex].ItemToRepair;
            if (itemToRepair == null || itemToRepair.Template == null)
            {
                return 0;
            }

            int durabilityLimit = itemToRepair.CurrentUses;
            int storehouseLimit = GetStorehouseRepairLimit(itemToRepair);
            return Mathf.Max(0, Mathf.Min(durabilityLimit, storehouseLimit));
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
            HubVendorUiHelper.ClearInstantiatedItems(
                ItemsParentContainer,
                PageIndicatorContainer,
                pageIndicatorObjects,
                ref itemChoices,
                ref itemChoiceToIndex
            );
        }
    }
}
