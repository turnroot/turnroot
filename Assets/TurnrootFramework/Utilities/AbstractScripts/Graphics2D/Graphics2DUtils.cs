using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.AbstractScripts.Graphics2D
{
    /// <summary>
    /// Utility methods for 2D graphics operations including image manipulation, sprite swapping, and simple coroutine-based animations.
    /// </summary>
    public static class Graphics2DUtils
    {
        /// <summary>
        /// Reduced set of easing types that were previously provided by DOTween.
        /// </summary>
        public enum Ease
        {
            Linear,
            InOutSine,
            OutCubic,
        }

        private static float EvaluateEase(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.InOutSine:
                    // -(cos(pi*t) - 1) / 2
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case Ease.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                default:
                    return t;
            }
        }

        public static void KillImageTweens(params Image[] images)
        {
            foreach (var img in images)
            {
                if (img == null)
                {
                    continue;
                }

                var c = img.color;
                c.a = 1f;
                img.color = c;
            }
        }

        // Safely set a sprite on an Image, enable/disable it based on presence, and reset alpha
        public static void SetSprite(Image img, Sprite sprite)
        {
            if (img == null)
            {
                return;
            }

            img.sprite = sprite;
            img.enabled = sprite != null;
            var c = img.color;
            c.a = 1f;
            img.color = c;
        }

        // Reset an image to a blank state: clear the sprite, disable the component, and restore default color.
        public static void ResetImage(Image img)
        {
            if (img == null)
            {
                return;
            }

            img.sprite = null;
            img.enabled = false;
            img.color = Color.white;
        }

        // Crossfade swap using overlays. Underlying sprites are swapped immediately,
        // then overlays fade out over crossfadeDuration.  Caller should start the
        // returned IEnumerator via MonoBehaviour.StartCoroutine.
        public static IEnumerator CrossfadeSwapCoroutine(
            Image a,
            Image b,
            float crossfadeDuration,
            Ease ease,
            int runId
        )
        {
            if (a == null || b == null)
            {
                yield break;
            }

            // create overlays
            GameObject overlayA = new GameObject("swap_overlay_a");
            GameObject overlayB = new GameObject("swap_overlay_b");
            var ta = overlayA.AddComponent<RectTransform>();
            var tb = overlayB.AddComponent<RectTransform>();
            overlayA.transform.SetParent(a.transform.parent, false);
            overlayB.transform.SetParent(b.transform.parent, false);
            var imgA = overlayA.AddComponent<Image>();
            var imgB = overlayB.AddComponent<Image>();

            // copy rect transform properties
            ta.anchorMin = a.rectTransform.anchorMin;
            ta.anchorMax = a.rectTransform.anchorMax;
            ta.pivot = a.rectTransform.pivot;
            ta.anchoredPosition = a.rectTransform.anchoredPosition;
            ta.sizeDelta = a.rectTransform.sizeDelta;

            tb.anchorMin = b.rectTransform.anchorMin;
            tb.anchorMax = b.rectTransform.anchorMax;
            tb.pivot = b.rectTransform.pivot;
            tb.anchoredPosition = b.rectTransform.anchoredPosition;
            tb.sizeDelta = b.rectTransform.sizeDelta;

            // copy sprites and colors
            imgA.sprite = a.sprite;
            imgA.type = a.type;
            imgA.color = a.color;
            imgA.raycastTarget = false;

            imgB.sprite = b.sprite;
            imgB.type = b.type;
            imgB.color = b.color;
            imgB.raycastTarget = false;

            // ensure overlays render above their originals
            overlayA.transform.SetSiblingIndex(a.transform.GetSiblingIndex() + 1);
            overlayB.transform.SetSiblingIndex(b.transform.GetSiblingIndex() + 1);

            // swap underlying sprites immediately
            var tmp = a.sprite;
            a.sprite = b.sprite;
            b.sprite = tmp;

            // fade overlays out together
            float elapsed = 0f;
            Color cA = imgA.color;
            Color cB = imgB.color;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = EvaluateEase(ease, Mathf.Clamp01(elapsed / crossfadeDuration));
                float alpha = Mathf.Lerp(1f, 0f, t);
                cA.a = alpha;
                imgA.color = cA;
                cB.a = alpha;
                imgB.color = cB;
                yield return null;
            }

            // ensure final state
            cA.a = 0f;
            imgA.color = cA;
            cB.a = 0f;
            imgB.color = cB;

            Object.Destroy(overlayA);
            Object.Destroy(overlayB);
        }

        public static IEnumerator TintCoroutine(
            Image activeImg,
            Image inactiveImg,
            Color activeColor,
            Color inactiveColor,
            float duration,
            Ease ease,
            int runId
        )
        {
            if (activeImg == null && inactiveImg == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                if (activeImg != null)
                {
                    activeImg.color = activeColor;
                }

                if (inactiveImg != null)
                {
                    inactiveImg.color = inactiveColor;
                }

                yield break;
            }

            // ensure starting alpha is opaque
            if (activeImg != null)
            {
                var c = activeImg.color;
                c.a = 1f;
                activeImg.color = c;
            }
            if (inactiveImg != null)
            {
                var c2 = inactiveImg.color;
                c2.a = 1f;
                inactiveImg.color = c2;
            }

            float elapsed = 0f;
            Color startA = activeImg != null ? activeImg.color : Color.clear;
            Color startI = inactiveImg != null ? inactiveImg.color : Color.clear;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EvaluateEase(ease, Mathf.Clamp01(elapsed / duration));
                if (activeImg != null)
                {
                    activeImg.color = Color.Lerp(startA, activeColor, t);
                }
                if (inactiveImg != null && inactiveImg.enabled)
                {
                    inactiveImg.color = Color.Lerp(startI, inactiveColor, t);
                }
                yield return null;
            }

            if (activeImg != null)
            {
                activeImg.color = activeColor;
            }
            if (inactiveImg != null && inactiveImg.enabled)
            {
                inactiveImg.color = inactiveColor;
            }
        }

        public static IEnumerator HideCoroutine(Image img, float duration, Ease ease, int runId)
        {
            if (img == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                img.enabled = false;
                yield break;
            }

            float elapsed = 0f;
            Color c = img.color;
            float startAlpha = c.a;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EvaluateEase(ease, Mathf.Clamp01(elapsed / duration));
                c.a = Mathf.Lerp(startAlpha, 0f, t);
                img.color = c;
                yield return null;
            }

            c.a = 0f;
            img.color = c;
            img.enabled = false;
        }
    }
}
