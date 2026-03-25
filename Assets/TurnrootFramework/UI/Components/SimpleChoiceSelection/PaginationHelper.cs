using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI
{
    public class PaginationHelper
    {
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; private set; } = 0;
        public int CurrentSelectionIndex { get; private set; } = 0;

        private readonly List<GameObject> pageIndicatorObjects = new();

        private List<UiChoice> itemChoices;
        private Transform pageIndicatorContainer;
        private Sprite activePageSprite;
        private Sprite inactivePageSprite;
        private float pageIndicatorSize;
        private AudioSource audioPlayer;
        private AudioClip pageChangeClip;

        public int TotalPages =>
            itemChoices == null || ItemsPerPage <= 0
                ? 0
                : Mathf.CeilToInt((float)itemChoices.Count / ItemsPerPage);

        public PaginationHelper(
            int itemsPerPage,
            Transform pageIndicatorContainer,
            Sprite activePageSprite,
            Sprite inactivePageSprite,
            float pageIndicatorSize,
            AudioSource audioPlayer = null,
            AudioClip pageChangeClip = null
        )
        {
            ItemsPerPage = Mathf.Max(1, itemsPerPage);
            this.pageIndicatorContainer = pageIndicatorContainer;
            this.activePageSprite = activePageSprite;
            this.inactivePageSprite = inactivePageSprite;
            this.pageIndicatorSize = pageIndicatorSize;
            this.audioPlayer = audioPlayer;
            this.pageChangeClip = pageChangeClip;
        }

        public void SetItemChoices(List<UiChoice> choices, int selectedIndex = 0)
        {
            itemChoices = choices ?? new List<UiChoice>();
            CurrentSelectionIndex = Mathf.Clamp(selectedIndex, 0, itemChoices.Count - 1);
            CurrentPage = itemChoices.Count == 0 ? 0 : CurrentSelectionIndex / ItemsPerPage;
            InitializePageIndicators();
            UpdateVisiblePageItems();
            RefreshSelection();
            UpdatePaginationIndicators();
        }

        public void ClearChoices()
        {
            itemChoices = null;
            CurrentSelectionIndex = 0;
            CurrentPage = 0;
            ClearPageIndicators();
        }

        private void ClearPageIndicators()
        {
            foreach (var indicator in pageIndicatorObjects)
            {
                if (indicator != null)
                {
                    Object.Destroy(indicator);
                }
            }
            pageIndicatorObjects.Clear();
        }

        public void InitializePageIndicators()
        {
            ClearPageIndicators();
            if (pageIndicatorContainer == null || TotalPages <= 0)
            {
                return;
            }

            for (var i = 0; i < TotalPages; i++)
            {
                var pageIndicatorObj = new GameObject(
                    $"PageIndicator_{i}",
                    typeof(UnityEngine.UI.Image)
                );
                pageIndicatorObj.transform.SetParent(pageIndicatorContainer, false);
                var image = pageIndicatorObj.GetComponent<UnityEngine.UI.Image>();
                image.sprite = i == CurrentPage ? activePageSprite : inactivePageSprite;
                var rectTransform = pageIndicatorObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(pageIndicatorSize, pageIndicatorSize);
                pageIndicatorObjects.Add(pageIndicatorObj);
            }
        }

        public void UpdatePaginationIndicators()
        {
            for (var i = 0; i < pageIndicatorObjects.Count; i++)
            {
                var image = pageIndicatorObjects[i]?.GetComponent<UnityEngine.UI.Image>();
                if (image == null)
                {
                    continue;
                }
                image.sprite = i == CurrentPage ? activePageSprite : inactivePageSprite;
            }
        }

        public void UpdateVisiblePageItems()
        {
            if (itemChoices == null || itemChoices.Count == 0)
            {
                return;
            }

            var startIndex = CurrentPage * ItemsPerPage;
            var endIndex = Mathf.Min(startIndex + ItemsPerPage, itemChoices.Count);

            for (var i = 0; i < itemChoices.Count; i++)
            {
                var choice = itemChoices[i];
                if (choice == null || choice.gameObject == null)
                {
                    continue;
                }
                choice.gameObject.SetActive(i >= startIndex && i < endIndex);
            }
        }

        public void RefreshSelection()
        {
            if (itemChoices == null || itemChoices.Count == 0)
            {
                return;
            }

            CurrentSelectionIndex = Mathf.Clamp(CurrentSelectionIndex, 0, itemChoices.Count - 1);

            for (var i = 0; i < itemChoices.Count; i++)
            {
                if (itemChoices[i] == null)
                {
                    continue;
                }

                if (i == CurrentSelectionIndex)
                {
                    itemChoices[i].Select();
                }
                else
                {
                    itemChoices[i].Deselect();
                }
            }
        }

        public void ChangePage(int? page = null)
        {
            if (itemChoices == null || itemChoices.Count == 0)
            {
                CurrentPage = 0;
                CurrentSelectionIndex = 0;
                ClearPageIndicators();
                return;
            }

            var totalPages = TotalPages;
            if (totalPages <= 0)
            {
                CurrentPage = 0;
                CurrentSelectionIndex = 0;
                return;
            }

            int targetPage;
            if (page == null)
            {
                targetPage = CurrentPage;
            }
            else if (page == -1)
            {
                targetPage = Mathf.Max(0, totalPages - 1);
            }
            else
            {
                targetPage = Mathf.Clamp(page.Value, 0, Mathf.Max(0, totalPages - 1));
            }

            CurrentPage = targetPage;
            CurrentSelectionIndex = Mathf.Clamp(
                CurrentPage * ItemsPerPage,
                0,
                itemChoices.Count - 1
            );

            if (audioPlayer != null && pageChangeClip != null)
            {
                audioPlayer.PlayOneShot(pageChangeClip);
            }

            UpdateVisiblePageItems();
            UpdatePaginationIndicators();
            RefreshSelection();
        }

        public void ChangeSelectionByOffset(int offset)
        {
            if (itemChoices == null || itemChoices.Count == 0)
            {
                return;
            }

            int itemCount = itemChoices.Count;
            int candidateIndex = CurrentSelectionIndex + offset;

            if (candidateIndex >= itemCount)
            {
                candidateIndex = 0;
            }
            else if (candidateIndex < 0)
            {
                candidateIndex = itemCount - 1;
            }

            int targetPage = candidateIndex / ItemsPerPage;
            bool pageChanged = false;

            if (targetPage != CurrentPage)
            {
                ChangePage(targetPage);
                pageChanged = true;
            }

            CurrentSelectionIndex = candidateIndex;
            RefreshSelection();

            if (!pageChanged && audioPlayer != null && pageChangeClip != null)
            {
                audioPlayer.PlayOneShot(pageChangeClip);
            }
        }

        public void HandleScrollInput(string action)
        {
            if (action == InputActionConstants.ScrollLeft)
            {
                ChangePage(CurrentPage - 1);
            }
            else if (action == InputActionConstants.ScrollRight)
            {
                ChangePage(CurrentPage + 1);
            }
        }
    }
}
