using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using Turnroot.UI;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    public partial class BlacksmithUi : MonoBehaviour
    {
        public IReadOnlyList<BlacksmithRepairItem> GetRepairableItemsList() =>
            repairableItems ?? System.Array.Empty<BlacksmithRepairItem>();

        public IReadOnlyList<BlacksmithForgeableItem> GetForgeableItemsList() =>
            forgeableItems ?? System.Array.Empty<BlacksmithForgeableItem>();

        public void GetForgeableItems()
        {
            var candidates = CollectBlacksmithCandidates(
                itemInstance => itemInstance.IsForgeableWeaponOrMagic(),
                itemInstance => itemInstance.GetDurabilityPercentage(),
                (itemInstance, belongsToCharacter, owner) =>
                    new BlacksmithForgeableItem(itemInstance, belongsToCharacter, owner)
            );

            forgeableItems = candidates
                .OrderBy(tuple => tuple.SortValue)
                .Select(tuple => tuple.Item)
                .ToArray();
        }

        public void GetRepairableItems()
        {
            var candidates = CollectBlacksmithCandidates(
                itemInstance => itemInstance.IsRepairableWeaponAccessoryOrShield(),
                itemInstance => itemInstance.GetDurabilityPercentage(),
                (itemInstance, belongsToCharacter, owner) =>
                    new BlacksmithRepairItem(itemInstance, belongsToCharacter, owner)
            );

            repairableItems = candidates
                .OrderBy(tuple => tuple.SortValue)
                .Select(tuple => tuple.Item)
                .ToArray();
        }

        private struct CandidateEntry<T>
        {
            public T Item;
            public float SortValue;
        }

        private List<CandidateEntry<T>> CollectBlacksmithCandidates<T>(
            System.Func<ObjectItemInstance, bool> filter,
            System.Func<ObjectItemInstance, float> sortValueSelector,
            System.Func<ObjectItemInstance, bool, CharacterInstance, T> creationFunc
        )
        {
            var results = new List<CandidateEntry<T>>();

            var rosterInstance =
                brain?.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
            if (rosterInstance == null)
            {
                "BlacksmithUi.CollectBlacksmithCandidates: no player roster available, skipping character inventory".LogInfo(
                    "BlacksmithUi"
                );
            }
            else
            {
                "BlacksmithUi.CollectBlacksmithCandidates: roster available, scanning characters".LogInfo(
                    "BlacksmithUi"
                );
                foreach (var character in rosterInstance.Instances ?? new List<CharacterInstance>())
                {
                    if (character == null || character.InventoryInstance == null)
                    {
                        continue;
                    }

                    foreach (
                        var itemInstance in character.InventoryInstance.InventoryItems
                            ?? new List<ObjectItemInstance>()
                    )
                    {
                        itemInstance?.SetBrain(brain);

                        var pass = filter(itemInstance);

                        if (!pass)
                        {
                            continue;
                        }

                        results.Add(
                            new CandidateEntry<T>
                            {
                                Item = creationFunc(itemInstance, true, character),
                                SortValue = sortValueSelector(itemInstance),
                            }
                        );
                    }
                }
            }

            var storehouseItems = brain?.storehouseBrain?.GetStoredItems();
            var storehouseCount = storehouseItems?.Count ?? 0;

            if (storehouseItems != null)
            {
                foreach (var itemInstance in storehouseItems)
                {
                    itemInstance?.SetBrain(brain);

                    var templateName = itemInstance?.Template?.name ?? "<null>";
                    var uses = itemInstance.Template.MaxUses - itemInstance.CurrentUses;
                    var pass = filter(itemInstance);

                    if (!pass)
                    {
                        continue;
                    }

                    results.Add(
                        new CandidateEntry<T>
                        {
                            Item = creationFunc(itemInstance, false, null),
                            SortValue = sortValueSelector(itemInstance),
                        }
                    );
                }
            }

            return results;
        }

        private void BuildItemListForCurrentMode()
        {
            int previousSelectedItemIndex = GetSelectedItemIndex();

            ClearInstantiatedItems();
            itemChoices = new List<UiChoice>();
            itemChoiceToIndex = new List<int>();

            for (int i = 0; i < GetCurrentItemCount(); i++)
            {
                BuildItemChoice(i);
            }

            totalPages = Mathf.CeilToInt((float)itemChoices.Count / ItemsPerPage);

            if (itemChoices.Count == 0)
            {
                CurrentPage = 0;
                CurrentSelectionIndex = 0;
                return;
            }

            RestoreSelectedItemIndex(previousSelectedItemIndex);
            CurrentPage = CurrentSelectionIndex / ItemsPerPage;
        }

        private int GetSelectedItemIndex()
        {
            return itemChoiceToIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToIndex.Count
                    ? itemChoiceToIndex[CurrentSelectionIndex]
                    : -1;
        }

        private int GetCurrentItemCount() =>
            CurrentMode == BlacksmithMode.Repair
                ? (repairableItems?.Length ?? 0)
                : (forgeableItems?.Length ?? 0);

        private void BuildItemChoice(int index)
        {
            if (ItemPrefab == null || ItemsParentContainer == null)
            {
                "BlacksmithUi.BuildItemListForCurrentMode: ItemPrefab or ItemsParentContainer is null".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var itemUiObject = Instantiate(ItemPrefab, ItemsParentContainer.transform);
            if (itemUiObject == null)
            {
                $"BlacksmithUi.BuildItemListForCurrentMode: Instantiate returned null for index {index}".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            if (!itemUiObject.TryGetComponent<UiChoice>(out var uiChoice))
            {
                uiChoice = itemUiObject.AddComponent<UiChoice>();
            }

            itemUiObject.TryGetComponent<BlacksmithItemRefs>(out var itemRefs);
            bool canSelect = ConfigureItemForCurrentMode(index, itemRefs);
            uiChoice.CanBeSelected = canSelect;
            if (!canSelect && itemRefs?.ItemNameText != null)
            {
                itemRefs.ItemNameText.color = Color.grey;
            }

            itemChoices.Add(uiChoice);
            itemChoiceToIndex.Add(index);
        }

        private bool ConfigureItemForCurrentMode(int index, BlacksmithItemRefs itemRefs)
        {
            if (
                CurrentMode == BlacksmithMode.Repair
                && repairableItems != null
                && index < repairableItems.Length
            )
            {
                ConfigureRepairItemUi(repairableItems[index], itemRefs, SelectionCountCache);
                return EvaluateCanRepair(repairableItems[index]);
            }

            if (
                CurrentMode == BlacksmithMode.Forge
                && forgeableItems != null
                && index < forgeableItems.Length
            )
            {
                ConfigureForgeItemUi(forgeableItems[index], itemRefs, SelectionCountCache);
                return true;
            }

            return false;
        }

        private void RestoreSelectedItemIndex(int previousSelectedItemIndex)
        {
            if (previousSelectedItemIndex >= 0)
            {
                var restoredIndex = itemChoiceToIndex.IndexOf(previousSelectedItemIndex);
                CurrentSelectionIndex = restoredIndex >= 0 ? restoredIndex : 0;
            }
            else
            {
                CurrentSelectionIndex = 0;
            }
        }

        private bool EvaluateCanRepair(BlacksmithRepairItem entry)
        {
            var repairTarget = entry.ItemToRepair;

            if (repairTarget == null)
            {
                "BlacksmithUi.EvaluateCanRepair: ItemToRepair is null".LogWarning("BlacksmithUi");
                return false;
            }

            if (repairTarget.Template == null)
            {
                "BlacksmithUi.EvaluateCanRepair: Template is null".LogWarning("BlacksmithUi");
                return false;
            }

            try
            {
                return repairTarget.CanRepair(1, brain?.storehouseBrain);
            }
            catch (System.Exception ex)
            {
                $"BlacksmithUi.EvaluateCanRepair: threw on '{repairTarget.Template?.name ?? "<null>"}' currentUses={repairTarget.CurrentUses}, ex={ex.GetType().Name}:{ex.Message}".LogWarning(
                    "BlacksmithUi"
                );
                return false;
            }
        }
    }
}
