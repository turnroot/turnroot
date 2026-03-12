using System.Collections;
using TMPro;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(Collider))]
    public class HubPoiUi : MonoBehaviour
    {
        public GameObject poiVisual;

        [Tooltip("How long the fade in/out should take.")]
        public float fadeDuration = 0.25f;

        private Renderer[] _renderers;
        private Material[] _materialInstances;
        private Camera _camera;
        private Coroutine _fadeCoroutine;

        public TextMeshPro Label;

        public string LabelText;

        public GameObject Badge;
        public Material BadgeMaterial;
        private Material _badgeMaterialInstance;
        public Texture BadgeTexture;

        public GameObject Particles;

        public bool ShowBadge = false;

        public void SetLabel(string text)
        {
            if (Label != null)
            {
                Label.text = text;
            }
        }

        public void SetBadgeVisible(bool visible)
        {
            if (Badge != null)
            {
                Badge.SetActive(visible);
            }
        }

        public void SetBadgeTexture(Texture texture)
        {
            if (BadgeMaterial != null)
            {
                if (_badgeMaterialInstance == null)
                {
                    _badgeMaterialInstance = Instantiate(BadgeMaterial);
                    if (Badge != null)
                    {
                        Renderer badgeRenderer = Badge.GetComponent<Renderer>();
                        if (badgeRenderer != null)
                        {
                            badgeRenderer.material = _badgeMaterialInstance;
                        }
                    }
                }

                if (_badgeMaterialInstance != null)
                {
                    _badgeMaterialInstance.mainTexture = texture;
                }
            }
        }

        private void Awake()
        {
            if (poiVisual == null)
            {
                $"HubPoiUi on {gameObject.name} has no poiVisual assigned, disabling.".LogWarning();
                return;
            }

            _renderers = poiVisual.GetComponentsInChildren<Renderer>();
            if (_renderers != null && _renderers.Length > 0)
            {
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

                // bump textmeshpro materials above other POI visuals so they always render
                int overlayQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;
                for (int i = 0; i < _materialInstances.Length; i++)
                {
                    var mat = _materialInstances[i];
                    if (mat == null)
                        continue;
                    mat.renderQueue =
                        mat.shader != null && mat.shader.name.Contains("TextMeshPro")
                            ? overlayQueue + 1
                            : overlayQueue - 10;
                }
            }

            if (poiVisual != null)
            {
                poiVisual.SetActive(false);
            }

            SetLabel(LabelText);
            SetBadgeVisible(ShowBadge);
            if (ShowBadge && BadgeMaterial != null && BadgeTexture != null)
            {
                SetBadgeTexture(BadgeTexture);
            }
            if (Particles != null)
            {
                Particles.SetActive(false);
            }
        }

        private void Update()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null || poiVisual == null)
            {
                return;
            }

            poiVisual.transform.rotation = Quaternion.LookRotation(
                poiVisual.transform.position - _camera.transform.position
            );
        }

        public void Show()
        {
            if (poiVisual)
            {
                poiVisual.SetActive(true);
            }
            if (Particles != null)
            {
                Particles.SetActive(true);
                Particles.GetComponent<ParticleSystem>()?.Play();
            }

            StartFade(1f);
        }

        public void Hide()
        {
            StartFade(0f);
            if (Particles != null)
            {
                Particles.SetActive(false);
            }
        }

        private void StartFade(float target)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            if (_materialInstances == null || _materialInstances.Length == 0)
            {
                return;
            }

            float start = GetAlpha(_materialInstances[0]);
            _fadeCoroutine = StartCoroutine(FadeRoutine(start, target));
        }

        private float GetAlpha(Material mat)
        {
            if (mat == null)
            {
                return 0f;
            }

            if (mat.HasProperty("_Color"))
            {
                return mat.color.a;
            }

            if (mat.HasProperty("_FaceColor"))
            {
                return mat.GetColor("_FaceColor").a;
            }

            if (mat.HasProperty("_BaseColor"))
            {
                return mat.GetColor("_BaseColor").a;
            }

            return 1f; // default opaque
        }

        private IEnumerator FadeRoutine(float from, float to)
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
        }

        private void SetAlpha(float a)
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
