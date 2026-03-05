using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Smoothly fades UI elements by controlling CanvasGroup alpha with events for visibility changes.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIFade : MonoBehaviour
    {
        [SerializeField]
        public float lerpTime = 0.3f;
        private float visibleAlpha;
        private CanvasGroup canvasGroup;

        public UnityEvent OnVisible;
        public UnityEvent OnHidden;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            // If alpha starts at 0, assume visible alpha should be 1
            visibleAlpha = canvasGroup.alpha > 0f ? canvasGroup.alpha : 1f;
            _visible = canvasGroup.alpha > 0f;
        }

        private bool _visible = true;
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value)
                {
                    $"UIFade: Visible set to {value} but already in that state. No action taken.".LogInfo();
                    return;
                }
                _visible = value;
                float targetAlpha = value ? visibleAlpha : 0f;
                StopAllCoroutines();

                if (!gameObject.activeInHierarchy || !enabled)
                {
                    $"UIFade: Cannot start LerpAlpha coroutine because '{gameObject.name}' is inactive or disabled. Applying final alpha immediately.".LogWarning();
                    canvasGroup.alpha = targetAlpha;
                    if (Mathf.Approximately(targetAlpha, visibleAlpha))
                    {
                        OnVisible?.Invoke();
                    }
                    else
                    {
                        OnHidden?.Invoke();
                    }
                    return;
                }

                StartCoroutine(LerpAlpha(canvasGroup.alpha, targetAlpha));
            }
        }

        public void Show() => Visible = true;

        public void Hide() => Visible = false;

        private IEnumerator LerpAlpha(float startAlpha, float targetAlpha)
        {
            float time = 0f;

            while (time < lerpTime)
            {
                time += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / lerpTime);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            if (Mathf.Approximately(targetAlpha, visibleAlpha))
            {
                OnVisible?.Invoke();
            }
            else
            {
                OnHidden?.Invoke();
            }
        }
    }
}
