using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    public partial class BlacksmithUi : MonoBehaviour
    {
        public void HandleItemChangeInput(string action)
        {
            if (_inForgeOptionSelection)
            {
                NavigateForgeOptions(action);
                return;
            }

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

        public void HandleSpecialInput(string action)
        {
            if (_inForgeOptionSelection)
            {
                return;
            }

            if (CurrentMode == BlacksmithMode.Repair)
            {
                SetMode(BlacksmithMode.Forge);
            }
            else if (CurrentMode == BlacksmithMode.Forge)
            {
                SetMode(BlacksmithMode.Repair);
            }
        }

        public void HandleNavigateLeftInput(string action)
        {
            if (action != InputActionConstants.NavigateLeft)
            {
                return;
            }

            if (_inForgeOptionSelection || CurrentMode != BlacksmithMode.Repair)
            {
                return;
            }

            if (paginationHelper == null || itemChoices == null || itemChoices.Count == 0)
            {
                "BlacksmithUi: No item choices available to change quantity".LogWarning();
                return;
            }

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

            if (_inForgeOptionSelection || CurrentMode != BlacksmithMode.Repair)
            {
                return;
            }

            if (paginationHelper == null || itemChoices == null || itemChoices.Count == 0)
            {
                "BlacksmithUi: No item choices available to change quantity".LogWarning();
                return;
            }

            int maxSelection = GetSelectedRepairMaxCount();
            if (maxSelection <= 0)
            {
                "BlacksmithUi: Cannot increase repair count — insufficient gold, materials, or uses remaining".LogWarning();
                return;
            }

            SelectionCountCache = Mathf.Clamp(SelectionCountCache + 1, 1, maxSelection);
            UpdateCurrentItemUiWithSelectionCount();
            AudioPlayer?.PlayOneShot(NavigateAudioClip);
        }

        public int GetSelectedRepairIndex()
        {
            return
                itemChoiceToIndex != null
                && CurrentSelectionIndex >= 0
                && CurrentSelectionIndex < itemChoiceToIndex.Count
                ? itemChoiceToIndex[CurrentSelectionIndex]
                : CurrentSelectionIndex;
        }

        public void HandleSelectInput(string action)
        {
            if (
                action
                is not InputActionConstants.Submit
                    and not InputActionConstants.Select
                    and not InputActionConstants.Confirm
            )
            {
                return;
            }

            if (_inForgeOptionSelection)
            {
                ExecuteSelectedForgeOption();
                return;
            }

            if (itemChoices == null || itemChoices.Count == 0)
            {
                "BlacksmithUi.HandleSelectInput: No item choices available".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            if (CurrentMode == BlacksmithMode.Forge)
            {
                HandleForgeSelect();
            }
            else
            {
                HandleRepairSelect();
            }
        }

        private void HandleRepairSelect()
        {
            if (repairableItems == null || repairableItems.Length == 0)
            {
                "BlacksmithUi.HandleRepairSelect: No repairable items are available".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            int chosenIndex = GetSelectedRepairIndex();
            if (chosenIndex < 0 || chosenIndex >= repairableItems.Length)
            {
                "BlacksmithUi.HandleRepairSelect: Selected index is invalid".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var entry = repairableItems[chosenIndex];
            var itemInstance = entry.ItemToRepair;
            if (itemInstance == null)
            {
                "BlacksmithUi.HandleRepairSelect: No item instance available for selected entry".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var storehouse = brain?.storehouseBrain;
            if (storehouse == null)
            {
                "BlacksmithUi.HandleRepairSelect: Missing StorehouseBrain".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            if (!itemInstance.CanRepair(SelectionCountCache, storehouse))
            {
                "BlacksmithUi.HandleRepairSelect: Selected item cannot be repaired with current resources".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            BeginGoldScroll(CostCache);

            var repairResult = itemInstance.Repair(SelectionCountCache);
            if (!repairResult.Success)
            {
                $"BlacksmithUi.HandleRepairSelect: Repair failed: {repairResult.ErrorMessage}".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            FinalizeTransaction(storehouse);
            RefreshBlacksmithDisplay();
        }

        private void HandleForgeSelect()
        {
            if (forgeableItems == null || forgeableItems.Length == 0)
            {
                "BlacksmithUi.HandleForgeSelect: No forgeable items are available".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            int chosenIndex = GetSelectedRepairIndex();
            if (chosenIndex < 0 || chosenIndex >= forgeableItems.Length)
            {
                "BlacksmithUi.HandleForgeSelect: Selected index is invalid".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            EnterForgeOptionSelection(forgeableItems[chosenIndex]);
        }

        private void BeginGoldScroll(int cost)
        {
            if (TotalGoldScroll == null)
            {
                return;
            }

            int currentGold = brain?.storehouseBrain?.PlayerGold ?? 0;
            TotalGoldScroll.StartNumber = currentGold;
            TotalGoldScroll.EndNumber = Mathf.Max(0, currentGold - cost);
            TotalGoldScroll.StartScroll();
        }

        private void FinalizeTransaction(StorehouseBrain storehouse)
        {
            storehouse.SaveGoldToLTM();
            storehouse.SaveCurrentStorehouse();
            AudioPlayer?.PlayOneShot(NavigateAudioClip);
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
            else if (
                CurrentMode == BlacksmithMode.Forge
                && forgeableItems != null
                && CurrentSelectionIndex < forgeableItems.Length
            )
            {
                ConfigureForgeItemUi(
                    forgeableItems[CurrentSelectionIndex],
                    refs,
                    SelectionCountCache
                );
            }
        }
    }
}
