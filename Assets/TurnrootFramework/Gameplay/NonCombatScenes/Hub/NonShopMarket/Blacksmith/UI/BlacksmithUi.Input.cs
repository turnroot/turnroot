using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    public partial class BlacksmithUi : MonoBehaviour
    {
        public void HandleItemChangeInput(string action)
        {
            if (
                HubVendorUiHelper.HandleItemNavigationInput(
                    action,
                    ref paginationHelper,
                    itemChoices,
                    ref SelectionCountCache,
                    ref CostCache,
                    AudioPlayer,
                    NavigateAudioClip,
                    out int newPage,
                    out int newSelection
                )
            )
            {
                CurrentPage = newPage;
                CurrentSelectionIndex = newSelection;
                UpdateCurrentItemUiWithSelectionCount();
            }
        }

        public void ChangePageInput(string action)
        {
            HubVendorUiHelper.HandlePageInput(
                action,
                ref paginationHelper,
                out int newPage,
                out int newSelection
            );

            CurrentPage = newPage;
            CurrentSelectionIndex = newSelection;
        }

        public void HandleNavigateLeftInput(string action)
        {
            if (action != InputActionConstants.NavigateLeft)
            {
                return;
            }

            if (paginationHelper == null || itemChoices == null || itemChoices.Count == 0)
            {
                "BlacksmithUi: No item choices available to change quantity".LogWarning();
                return;
            }

            // Decrease repair quantity, min 1.
            SelectionCountCache = Mathf.Max(1, SelectionCountCache - 1);
            UpdateCurrentItemUiWithSelectionCount();
            AudioPlayer?.PlayOneShot(NavigateAudioClip);
        }

        public void HandleNavigateRightInput(string action)
        {
            if (action != InputActionConstants.NavigateRight)
            {
                return;
            }

            if (paginationHelper == null || itemChoices == null || itemChoices.Count == 0)
            {
                "BlacksmithUi: No item choices available to change quantity".LogWarning();
                return;
            }

            int maxSelection = 1;
            if (
                CurrentMode == BlacksmithMode.Repair
                && repairableItems != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < repairableItems.Length
            )
            {
                maxSelection = GetSelectedRepairMaxCount();
            }

            if (maxSelection <= 0)
            {
                "BlacksmithUi: selected item cannot be repaired because shortage of gold/materials/durability".LogWarning();
                return;
            }

            SelectionCountCache = Mathf.Clamp(SelectionCountCache + 1, 1, maxSelection);
            UpdateCurrentItemUiWithSelectionCount();
            AudioPlayer?.PlayOneShot(NavigateAudioClip);
        }

        public int GetSelectedRepairIndex()
        {
            return itemChoiceToIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToIndex.Count
                ? itemChoiceToIndex[CurrentSelectionIndex]
                : CurrentSelectionIndex;
        }

        public void HandleSelectInput(string action)
        {
            if (
                action != InputActionConstants.Submit
                && action != InputActionConstants.Select
                && action != InputActionConstants.Confirm
            )
            {
                return;
            }

            if (
                repairableItems == null
                || repairableItems.Length == 0
                || itemChoices == null
                || itemChoices.Count == 0
            )
            {
                "BlacksmithUi.HandleSelectInput: No repairable items are available".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            int chosenIndex = GetSelectedRepairIndex();
            if (chosenIndex < 0 || chosenIndex >= repairableItems.Length)
            {
                "BlacksmithUi.HandleSelectInput: Selected index is invalid".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var entry = repairableItems[chosenIndex];
            var itemInstance = entry.ItemToRepair;
            if (itemInstance == null)
            {
                "BlacksmithUi.HandleSelectInput: No item instance available for selected entry".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var storehouse = brain?.storehouseBrain;
            if (storehouse == null)
            {
                "BlacksmithUi.HandleSelectInput: Missing StorehouseBrain".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            if (!itemInstance.CanRepair(SelectionCountCache, storehouse))
            {
                "BlacksmithUi.HandleSelectInput: Selected item cannot be repaired with current resources".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            int currentGold = storehouse.PlayerGold;
            if (TotalGoldScroll != null)
            {
                TotalGoldScroll.StartNumber = currentGold;
                TotalGoldScroll.EndNumber = Mathf.Max(0, currentGold - CostCache);
                TotalGoldScroll.StartScroll();
            }

            var repairResult = itemInstance.Repair(SelectionCountCache);
            if (!repairResult.Success)
            {
                $"BlacksmithUi.HandleSelectInput: Repair failed: {repairResult.ErrorMessage}".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            // Ensure gold and storehouse material state is saved.
            storehouse.SaveGoldToLTM();
            storehouse.SaveCurrentStorehouse();

            AudioPlayer?.PlayOneShot(NavigateAudioClip);

            // Refresh to remove fully repaired entries from the list, and update costs.
            RefreshBlacksmithDisplay();
        }

        private void UpdateCurrentItemUiWithSelectionCount()
        {
            if (
                itemChoices == null
                || CurrentSelectionIndex < 0
                || CurrentSelectionIndex >= itemChoices.Count
            )
            {
                return;
            }

            var chosen = itemChoices[CurrentSelectionIndex];
            if (chosen == null)
            {
                return;
            }

            var refs = chosen.gameObject.GetComponent<BlacksmithItemRefs>();
            if (refs == null)
            {
                "BlacksmithUi.UpdateCurrentItemUiWithSelectionCount: BlacksmithItemRefs missing on selected UiChoice".LogWarning();
                return;
            }

            if (
                CurrentMode == BlacksmithMode.Repair
                && repairableItems != null
                && CurrentSelectionIndex < repairableItems.Length
            )
            {
                ConfigureRepairItemUi(
                    repairableItems[CurrentSelectionIndex],
                    refs,
                    SelectionCountCache
                );
            }
        }
    }
}
