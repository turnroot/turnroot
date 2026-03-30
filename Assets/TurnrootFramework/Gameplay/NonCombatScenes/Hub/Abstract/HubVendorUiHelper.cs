using System.Collections.Generic;
using TMPro;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.Ui;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Abstract
{
    public static class HubVendorUiHelper
    {
        public static void UpdateGoldDisplay(
            TextMeshProUGUI totalGoldText,
            ScrollDownNumber totalGoldScroll,
            Brain.Brain brain
        )
        {
            if (totalGoldText == null)
            {
                return;
            }

            if (brain.storehouseBrain != null)
            {
                totalGoldText.text = $"Gold: {brain.storehouseBrain.PlayerGold}G";
                if (totalGoldScroll != null && !totalGoldScroll.IsScrolling)
                {
                    totalGoldScroll.StartNumber = brain.storehouseBrain.PlayerGold;
                }
            }
            else
            {
                totalGoldText.text = "Gold: ???";
                if (totalGoldScroll != null)
                {
                    totalGoldScroll.StartNumber =
                        brain.storehouseBrain != null ? brain.storehouseBrain.PlayerGold : 0;
                }
            }
        }

        public static void EnsurePagination(
            ref PaginationHelper paginationHelper,
            int itemsPerPage,
            Transform pageIndicatorContainer,
            Sprite activePageIndicatorSprite,
            Sprite inactivePageIndicatorSprite,
            float pageIndicatorSize,
            AudioSource audioPlayer,
            AudioClip pageChangeAudioClip,
            List<UiChoice> itemChoices,
            int currentSelectionIndex,
            out int currentPage,
            out int resultingSelectionIndex
        )
        {
            paginationHelper ??= new PaginationHelper(
                itemsPerPage,
                pageIndicatorContainer,
                activePageIndicatorSprite,
                inactivePageIndicatorSprite,
                pageIndicatorSize,
                audioPlayer,
                pageChangeAudioClip
            );

            paginationHelper.ItemsPerPage = itemsPerPage;
            paginationHelper.SetItemChoices(itemChoices, currentSelectionIndex);

            currentPage = paginationHelper.CurrentPage;
            resultingSelectionIndex = paginationHelper.CurrentSelectionIndex;
        }

        public static bool HandleItemNavigationInput(
            string action,
            ref PaginationHelper paginationHelper,
            List<UiChoice> itemChoices,
            ref int selectionCountCache,
            ref int costCache,
            AudioSource audioPlayer,
            AudioClip navigateAudioClip,
            out int currentPage,
            out int currentSelectionIndex
        )
        {
            // Reset selection count when moving to another item, but keep costCache until the selected item is reconfigured.
            selectionCountCache = 1;

            currentPage = 0;
            currentSelectionIndex = 0;

            if (paginationHelper == null || itemChoices == null || itemChoices.Count == 0)
            {
                return false;
            }

            if (action == InputActionConstants.NavigateDown)
            {
                paginationHelper.ChangeSelectionByOffset(1);
            }
            else if (action == InputActionConstants.NavigateUp)
            {
                paginationHelper.ChangeSelectionByOffset(-1);
            }
            else
            {
                return false;
            }

            currentPage = paginationHelper.CurrentPage;
            currentSelectionIndex = paginationHelper.CurrentSelectionIndex;

            audioPlayer?.PlayOneShot(navigateAudioClip);
            return true;
        }

        public static void HandlePageInput(
            string action,
            ref PaginationHelper paginationHelper,
            out int currentPage,
            out int currentSelectionIndex
        )
        {
            currentPage = 0;
            currentSelectionIndex = 0;

            paginationHelper?.HandleScrollInput(action);
            if (paginationHelper != null)
            {
                currentPage = paginationHelper.CurrentPage;
                currentSelectionIndex = paginationHelper.CurrentSelectionIndex;
            }
        }

        public static void ClearInstantiatedItems(
            GameObject itemsParentContainer,
            GameObject pageIndicatorContainer,
            List<GameObject> pageIndicatorObjects,
            ref List<UiChoice> itemChoices,
            ref List<int> itemChoiceToIndex
        )
        {
            if (itemsParentContainer != null)
            {
                for (int i = itemsParentContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Object.Destroy(itemsParentContainer.transform.GetChild(i).gameObject);
                }
            }

            if (pageIndicatorContainer != null)
            {
                for (int i = pageIndicatorContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Object.Destroy(pageIndicatorContainer.transform.GetChild(i).gameObject);
                }
            }

            pageIndicatorObjects?.Clear();
            itemChoices = null;
            itemChoiceToIndex = null;
        }
    }
}
