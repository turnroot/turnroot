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
                        if (itemInstance != null)
                        {
                            itemInstance.SetBrain(brain);
                        }

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
                    if (itemInstance != null)
                    {
                        itemInstance.SetBrain(brain);
                    }

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
            int previousSelectedItemIndex = -1;
            if (
                itemChoiceToIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToIndex.Count
            )
            {
                previousSelectedItemIndex = itemChoiceToIndex[CurrentSelectionIndex];
            }

            ClearInstantiatedItems();
            itemChoices = new List<UiChoice>();
            itemChoiceToIndex = new List<int>();

            var currentItemsCount =
                CurrentMode == BlacksmithMode.Repair
                    ? (repairableItems?.Length ?? 0)
                    : (forgeableItems?.Length ?? 0);

            for (var i = 0; i < currentItemsCount; i++)
            {
                if (ItemPrefab == null || ItemsParentContainer == null)
                {
                    "BlacksmithUi.BuildItemListForCurrentMode: ItemPrefab or ItemsParentContainer is null".LogWarning(
                        "BlacksmithUi"
                    );
                    continue;
                }

                var itemUiObject = Instantiate(ItemPrefab, ItemsParentContainer.transform);
                if (itemUiObject == null)
                {
                    $"BlacksmithUi.BuildItemListForCurrentMode: Instantiate returned null for index {i}".LogWarning(
                        "BlacksmithUi"
                    );
                    continue;
                }

                if (!itemUiObject.TryGetComponent<UiChoice>(out var uiChoice))
                {
                    uiChoice = itemUiObject.AddComponent<UiChoice>();
                }

                itemUiObject.TryGetComponent<BlacksmithItemRefs>(out var itemRefs);

                bool canSelect;
                if (
                    CurrentMode == BlacksmithMode.Repair
                    && repairableItems != null
                    && i < repairableItems.Length
                )
                {
                    ConfigureRepairItemUi(repairableItems[i], itemRefs, SelectionCountCache);
                    canSelect = EvaluateCanRepair(repairableItems[i]);
                }
                else if (
                    CurrentMode == BlacksmithMode.Forge
                    && forgeableItems != null
                    && i < forgeableItems.Length
                )
                {
                    ConfigureForgeItemUi(forgeableItems[i], itemRefs, SelectionCountCache);
                    canSelect = true;
                }
                else
                {
                    canSelect = false;
                }

                uiChoice.CanBeSelected = canSelect;

                if (!canSelect && itemRefs?.ItemNameText != null)
                {
                    itemRefs.ItemNameText.color = Color.grey;
                }

                itemChoices.Add(uiChoice);
                itemChoiceToIndex.Add(i);
            }

            totalPages = Mathf.CeilToInt((float)itemChoices.Count / ItemsPerPage);

            if (itemChoices.Count == 0)
            {
                CurrentPage = 0;
                CurrentSelectionIndex = 0;
                return;
            }

            if (previousSelectedItemIndex >= 0)
            {
                var restoredIndex = itemChoiceToIndex.IndexOf(previousSelectedItemIndex);
                CurrentSelectionIndex = restoredIndex >= 0 ? restoredIndex : 0;
            }
            else
            {
                CurrentSelectionIndex = 0;
            }

            CurrentPage = CurrentSelectionIndex / ItemsPerPage;
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
