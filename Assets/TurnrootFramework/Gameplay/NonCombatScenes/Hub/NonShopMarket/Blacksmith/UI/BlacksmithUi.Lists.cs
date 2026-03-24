using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using Turnroot.UI;
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
            if (rosterInstance != null)
            {
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
                        if (!filter(itemInstance))
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
            if (storehouseItems != null)
            {
                foreach (var itemInstance in storehouseItems)
                {
                    if (!filter(itemInstance))
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
            // Keep previous selection index mapping before rebuilding
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
                    continue;
                }

                var itemUiObject = Instantiate(ItemPrefab, ItemsParentContainer.transform);
                var uiChoice = itemUiObject.GetComponent<UiChoice>();
                if (uiChoice == null)
                {
                    uiChoice = itemUiObject.AddComponent<UiChoice>();
                }

                var itemRefs = itemUiObject.GetComponent<BlacksmithItemRefs>();

                if (
                    CurrentMode == BlacksmithMode.Repair
                    && repairableItems != null
                    && i < repairableItems.Length
                )
                {
                    ConfigureRepairItemUi(repairableItems[i], itemRefs, SelectionCountCache);
                }
                else if (
                    CurrentMode == BlacksmithMode.Forge
                    && forgeableItems != null
                    && i < forgeableItems.Length
                )
                {
                    ConfigureForgeItemUi(forgeableItems[i], itemRefs, SelectionCountCache);
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
                if (restoredIndex >= 0)
                {
                    CurrentSelectionIndex = restoredIndex;
                    CurrentPage = restoredIndex / ItemsPerPage;
                }
                else
                {
                    CurrentSelectionIndex = 0;
                    CurrentPage = 0;
                }
            }
            else
            {
                CurrentSelectionIndex = 0;
                CurrentPage = 0;
            }
        }
    }
}
