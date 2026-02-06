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
        float visibleAlpha;
        CanvasGroup canvasGroup;

        public UnityEvent OnVisible;
        public UnityEvent OnHidden;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            visibleAlpha = canvasGroup.alpha;
        }

        bool _visible = true;
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value)
                {
                    return;
                }
                _visible = value;
                float targetAlpha = value ? visibleAlpha : 0f;
                StopAllCoroutines();

                if (!gameObject.activeInHierarchy || !enabled)
                {
                    TurnrootLogger.Log(
                        $"UIFade: Cannot start LerpAlpha coroutine because '{gameObject.name}' is inactive or disabled. Applying final alpha immediately.",
                        TurnrootLogger.LogLevel.Warning
                    );
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
