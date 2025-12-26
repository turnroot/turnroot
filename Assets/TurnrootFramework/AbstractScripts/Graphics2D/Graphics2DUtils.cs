using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.AbstractScripts.Graphics2D
{
    public static class Graphics2DUtils
    {
        // Kill tweens on images and reset alpha
        public static void KillImageTweens(params Image[] images)
        {
            foreach (var img in images)
            {
                if (img == null)
                {
                    continue;
                }

                img.DOKill();
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

        // Reset an image's color and alpha to default (white, fully opaque)
        public static void ResetImage(Image img)
        {
            if (img == null)
            {
                return;
            }

            img.color = Color.white;
            var c = img.color;
            c.a = 1f;
            img.color = c;
        }

        // Crossfade swap using overlays. Underlying sprites are swapped immediately,
        // then overlays fade out over crossfadeDuration.
        public static Tween CrossfadeSwap(
            Image a,
            Image b,
            float crossfadeDuration,
            Ease ease,
            int runId
        )
        {
            if (a == null || b == null)
            {
                return DOVirtual.DelayedCall(0f, () => { }).SetId(runId);
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
            var seq = DOTween.Sequence();
            seq.Append(imgA.DOFade(0f, crossfadeDuration).SetEase(ease));
            seq.Join(imgB.DOFade(0f, crossfadeDuration).SetEase(ease));
            seq.OnComplete(() =>
            {
                // cleanup overlays
                Object.Destroy(overlayA);
                Object.Destroy(overlayB);
            });
            return seq.SetId(runId);
        }

        public static Tween CreateTintSequence(
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
                return DOVirtual.DelayedCall(0f, () => { }).SetId(runId);
            }

            if (duration <= 0f)
            {
                return DOVirtual
                    .DelayedCall(
                        0f,
                        () =>
                        {
                            if (activeImg != null)
                            {
                                activeImg.color = activeColor;
                            }

                            if (inactiveImg != null)
                            {
                                inactiveImg.color = inactiveColor;
                            }
                        }
                    )
                    .SetId(runId);
            }

            var seq = DOTween.Sequence();
            seq.AppendCallback(() =>
            {
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
            });

            if (activeImg != null)
            {
                seq.Append(activeImg.DOColor(activeColor, duration).SetEase(ease));
            }

            if (inactiveImg != null && inactiveImg.enabled)
            {
                seq.Join(inactiveImg.DOColor(inactiveColor, duration).SetEase(ease));
            }

            return seq.SetId(runId);
        }

        public static Tween CreateHideTween(Image img, float duration, Ease ease, int runId)
        {
            return img == null
                ? DOVirtual.DelayedCall(0f, () => { }).SetId(runId)
                : duration <= 0f
                ? DOVirtual
                    .DelayedCall(
                        0f,
                        () =>
                        {
                            img.enabled = false;
                        }
                    )
                    .SetId(runId)
                : img.DOFade(0f, duration)
                .SetEase(ease)
                .OnComplete(() => img.enabled = false)
                .SetId(runId);
        }
    }
}
