using System.Collections;
using UnityEngine;

namespace Turnroot.UI.Components.RadialMenu
{
    public partial class RadialMenu
    {
        private void ArrangeItemsInCircle()
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            _rotStep = 360f / menuItems.Count;

            float segmentSize = menuRadiusPixels * 2f;

            for (int i = 0; i < menuItems.Count; i++)
            {
                RectTransform itemRect = menuItems[i].GetComponent<RectTransform>();
                itemRect.localRotation = Quaternion.identity;
                itemRect.anchorMin = new Vector2(0.5f, 0.5f);
                itemRect.anchorMax = new Vector2(0.5f, 0.5f);
                itemRect.pivot = new Vector2(0.5f, 0.5f);

                itemRect.sizeDelta = new Vector2(segmentSize, segmentSize);

                // Start at top (0°) and go clockwise. Subtract from 360 to reverse the order
                // so menu items match visual layout (index 0 at top, increasing clockwise)
                float startAngle = (360f - (i * _rotStep)) % 360f;
                float endAngle = (360f - ((i + 1) * _rotStep)) % 360f;

                // Swap start/end since we reversed the direction
                float temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;

                menuItems[i].SetSegmentAngles(startAngle, endAngle, innerRadiusPercent, segmentGap);

                float centerAngle = (startAngle + endAngle) * 0.5f;
                menuItems[i]
                    .PositionContent(
                        centerAngle,
                        innerRadiusPercent,
                        1f,
                        menuRadiusPixels,
                        contentRadialOffset
                    );
            }
        }

        private void SetupCenterItem()
        {
            // Do not resize or reposition a minimal sprite-only center item
            if (centerItem is null or RadialMenuItemSprite)
            {
                return;
            }

            RectTransform centerRect = centerItem.GetComponent<RectTransform>();
            centerRect.localRotation = Quaternion.identity;
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);

            float segmentSize = menuRadiusPixels * 2f;
            centerRect.sizeDelta = new Vector2(segmentSize, segmentSize);

            centerItem.SetSegmentAngles(0, 360, innerRadiusPercent, 0);
            centerItem.PositionContent(0f, 0f, 1f, menuRadiusPixels);
        }

        public void RefreshLayout()
        {
            ArrangeItemsInCircle();
            if (centerItem != null)
            {
                SetupCenterItem();
            }

            for (int i = 0; i < menuItems.Count; i++)
            {
                menuItems[i].EnsureContentOnTop();
            }
            centerItem?.EnsureContentOnTop();
        }

        public void SetContentRadialOffset(float offset)
        {
            contentRadialOffset = Mathf.Clamp(offset, -0.5f, 0.5f);
            RefreshLayout();
        }

        public void SetMenuRadius(float radiusPixels)
        {
            menuRadiusPixels = radiusPixels;
            RefreshLayout();
        }

        private void ShowMenu()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            // If we're not configured to hide the menu until ready, just notify immediately.
            if (!hideUntilReady)
            {
                NotifyMenuReady();
                return;
            }

            // Stop any existing reveal coroutine
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            if (showFadeTime <= 0f)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                NotifyMenuReady();
            }
            else
            {
                _showCoroutine = StartCoroutine(FadeIn(_canvasGroup, showFadeTime));
            }
        }

        private IEnumerator FadeIn(CanvasGroup cg, float duration)
        {
            float t = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            _showCoroutine = null;
            NotifyMenuReady();
        }

        private void NotifyMenuReady() => OnMenuReady?.Invoke(this);
    }
}
