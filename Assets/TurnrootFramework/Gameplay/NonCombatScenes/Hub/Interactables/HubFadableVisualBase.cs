using System.Collections;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public abstract class HubFadableVisualBase : MonoBehaviour, IHubVisualFadable
    {
        [Tooltip(
            "The root GameObject that contains all visual elements for this hub interaction (icon, badge, etc.)."
        )]
        public GameObject poiVisual;

        [Tooltip("How long the fade in/out should take.")]
        public float fadeDuration = 0.25f;

        [Tooltip("AudioSource used to play UI sounds for this hub interaction.")]
        public AudioSource UiFx;

        [Tooltip("Sound played when the POI becomes visible.")]
        public AudioClip PoiShowSound;

        [Tooltip("Sound played when this POI is selected.")]
        public AudioClip PoiSelectSound;

        protected Renderer[] _renderers;
        protected Material[] _materialInstances;
        private Camera _camera;
        private Coroutine _fadeCoroutine;
        private float _activeFadeTarget = float.NaN;

        public GameObject PoiVisual => poiVisual;

        public float FadeDuration
        {
            get => fadeDuration;
            set => fadeDuration = Mathf.Max(0f, value);
        }

        public virtual void Show()
        {
            if (poiVisual)
            {
                poiVisual.SetActive(true);
            }

            StartFade(1f);
            PlayPoiShowSound();
        }

        public virtual void Hide() => StartFade(0f);

        protected void PlayPoiSelectSound()
        {
            if (UiFx != null && PoiSelectSound != null)
            {
                UiFx.PlayOneShot(PoiSelectSound);
            }
        }

        protected void PlayPoiShowSound()
        {
            if (UiFx != null && PoiShowSound != null)
            {
                UiFx.PlayOneShot(PoiShowSound);
            }
        }

        public void FaceCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (poiVisual == null || _camera == null)
            {
                return;
            }

            poiVisual.transform.rotation = Quaternion.LookRotation(
                poiVisual.transform.position - _camera.transform.position
            );
        }

        public void InitializeVisualMaterials()
        {
            if (poiVisual == null)
            {
                return;
            }

            _renderers = poiVisual.GetComponentsInChildren<Renderer>();
            if (_renderers == null || _renderers.Length == 0)
            {
                return;
            }

            _materialInstances = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material baseMat = _renderers[i].sharedMaterial;
                Material inst = baseMat != null ? Instantiate(baseMat) : null;
                _materialInstances[i] = inst;
                if (inst != null)
                {
                    _renderers[i].material = inst;
                }
            }

            SetAlpha(0f);
        }

        protected void StartFade(float target)
        {
            target = Mathf.Clamp01(target);

            if (_fadeCoroutine != null && Mathf.Approximately(_activeFadeTarget, target))
            {
                return;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (_materialInstances == null || _materialInstances.Length == 0)
            {
                if (Mathf.Approximately(target, 0f) && poiVisual != null)
                {
                    poiVisual.SetActive(false);
                }
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                SetAlpha(target);
                if (Mathf.Approximately(target, 0f) && poiVisual != null)
                {
                    poiVisual.SetActive(false);
                }
                return;
            }

            _activeFadeTarget = target;
            float start = GetAlpha(_materialInstances[0]);
            _fadeCoroutine = StartCoroutine(FadeRoutine(start, target));
        }

        public float GetAlpha(Material mat)
        {
            return mat == null ? 0f
                : mat.HasProperty("_Color") ? mat.color.a
                : mat.HasProperty("_FaceColor") ? mat.GetColor("_FaceColor").a
                : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor").a
                : 1f;
        }

        public IEnumerator FadeRoutine(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(from, to, elapsed / fadeDuration);
                SetAlpha(a);
                yield return null;
            }

            SetAlpha(to);

            if (Mathf.Approximately(to, 0f) && poiVisual != null)
            {
                poiVisual.SetActive(false);
            }

            _fadeCoroutine = null;
            _activeFadeTarget = float.NaN;
        }

        public void SetAlpha(float a)
        {
            if (_materialInstances == null)
            {
                return;
            }

            foreach (var mat in _materialInstances)
            {
                if (mat == null)
                {
                    continue;
                }

                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = a;
                    mat.color = c;
                }
                else if (mat.HasProperty("_FaceColor"))
                {
                    Color c = mat.GetColor("_FaceColor");
                    c.a = a;
                    mat.SetColor("_FaceColor", c);
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = a;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }
}
