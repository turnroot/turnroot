using System.Collections;
using NaughtyAttributes;
using TMPro;
using Turnroot.Characters;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Gameplay.NonCombatScenes.Hub.HubManager;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(Collider))]
    public class HubPoiUi : MonoBehaviour
    {
        #region Inspector Fields

        [Tooltip("Which hub sublocation this POI represents (Market, Cafe, Docks, etc.).")]
        public HubSublocationName Type;
        private Shop.Shop _shop;

        [InfoBox(
            "The root GameObject that contains all visual elements for the POI (icon, badge, etc.)."
        )]
        public GameObject poiVisual;

        [Tooltip("The target camera transform to move to when this POI is selected.")]
        public Transform CameraPoint;

        [Tooltip("If true, selecting this POI moves the camera to the CameraPoint.")]
        public bool MoveCameraOnSelect = true;

        [Tooltip("How long the fade in/out should take.")]
        public float fadeDuration = 0.25f;

        [Tooltip("AudioSource used to play UI sounds for this POI.")]
        public AudioSource UiFx;

        [Tooltip("Sound played when the POI becomes visible.")]
        public AudioClip PoiShowSound;

        [Tooltip("Sound played when this POI is selected.")]
        public AudioClip PoiSelectSound;
        private Renderer[] _renderers;
        private Material[] _materialInstances;
        private Camera _camera;
        private Coroutine _fadeCoroutine;

        [Tooltip("Text element used to show the POI label.")]
        public TextMeshPro Label;

        [Tooltip("Label text displayed on the POI (updated at runtime).")]
        public string LabelText;

        [Tooltip("If this POI represents a unit, the associated character instance.")]
        public CharacterInstance UnitCharacter;

        [Tooltip("Optional badge object used to display an icon or status on the POI.")]
        public GameObject Badge;

        [Tooltip(
            "Material used on the badge; an instance is created at runtime to avoid modifying the original."
        )]
        public Material BadgeMaterial;
        private Material _badgeMaterialInstance;

        [Tooltip("Texture used for the badge when enabled.")]
        public Texture BadgeTexture;

        [Tooltip("Texture used when selection is forbidden (e.g., shop closed).")]
        public Texture ForbiddenBadgeTexture;
        private Texture _currentBadgeTexture;

        [Tooltip("Optional particle effect GameObject to play when the POI is visible.")]
        public GameObject Particles;

        [Tooltip("Toggle whether the badge is shown on this POI.")]
        public bool ShowBadge = false;

        [HideInInspector]
        public bool CanSelect = true;

        private HubManager hubmanager;

        #endregion

        #region Public API

        public void SetLabel(string text)
        {
            LabelText = text;
            if (Label != null)
            {
                Label.text = text;
            }
        }

        public void SetUnitCharacter(CharacterInstance character)
        {
            UnitCharacter = character;
            if (character != null)
            {
                SetLabel(character.CharacterTemplate.DisplayName);
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
            if (UiFx != null && PoiShowSound != null)
            {
                UiFx.PlayOneShot(PoiShowSound);
            }
        }

        public void Hide()
        {
            StartFade(0f);
            if (Particles != null)
            {
                Particles.SetActive(false);
            }
        }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            hubmanager = FindFirstObjectByType<HubManager>();
            if (poiVisual == null)
            {
                $"HubPoiUi on {gameObject.name} has no poiVisual assigned, disabling.".LogWarning();
                return;
            }

            InitializeVisualMaterials();
            SetupUiState();
            HandleSubLocationType();
            Hide();
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

        #endregion

        #region Helpers

        private void SetupUiState()
        {
            if (poiVisual != null)
            {
                poiVisual.SetActive(false);
            }

            SetLabel(LabelText);
            SetBadgeVisible(ShowBadge);
            if (ShowBadge && BadgeMaterial != null && BadgeTexture != null)
            {
                _currentBadgeTexture = BadgeMaterial.mainTexture;
                SetBadgeTexture(BadgeTexture);
            }

            if (Particles != null)
            {
                Particles.SetActive(false);
            }
        }

        private void HandleSubLocationType()
        {
            switch (Type)
            {
                case HubSublocationName.Market:
                    _shop = TryGetComponent<Shop.Shop>(out var shop) ? shop : null;
                    if (_shop?.ShopOpen(hubmanager.gameDate) == false)
                    {
                        LabelText = "Not Open";
                        CanSelect = false;
                        SetBadgeTexture(ForbiddenBadgeTexture);
                        SetLabel(LabelText);
                    }
                    else
                    {
                        CanSelect = true;
                    }
                    break;

                default:
                    break;
            }
        }

        private void InitializeVisualMaterials()
        {
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

            return 1f;
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

        #endregion
        #region Selection
        public void Select()
        {
            UiFx?.PlayOneShot(PoiSelectSound);

            var subLocation = GetComponentInParent<HubSubLocation>();
            if (subLocation == null)
            {
                $"HubPoiUi: Failed to find parent HubSubLocation for {name}".LogWarning();
            }

            // Always log the selection, even if we don't immediately move the camera.
            hubmanager?.SpecificUiInputHandler?.SetCurrentSelection(subLocation, this);

            switch (Type)
            {
                case HubSublocationName.Market:
                    hubmanager.SetInputMode(HubInputMode.Chosen);
                    break;
                case HubSublocationName.Docks:
                    hubmanager.SetInputMode(HubInputMode.Chosen);
                    break;
                case HubSublocationName.Cafe:
                    hubmanager.SetInputMode(HubInputMode.Chosen);
                    break;
                case HubSublocationName.Training:
                    hubmanager.SetInputMode(HubInputMode.Chosen);
                    break;
                // battlefields don't have pois
                default:
                    break;
            }

            if (CameraPoint != null)
            {
                if (MoveCameraOnSelect)
                {
                    if (GameplayPlayerSettings.Instance.AnimatedCameraMovement)
                    {
                        hubmanager._brain.cameraBrain.StartCameraTransition(
                            hubmanager.GeneralCamera,
                            CameraPoint,
                            fadeDuration
                        );
                    }
                    else
                    {
                        hubmanager._brain.cameraBrain.MoveCameraInstant(
                            hubmanager.GeneralCamera,
                            CameraPoint
                        );
                    }
                }
                else
                {
                    // fade to black, move, fade back in (use the same structure as the sublocation transition)
                    if (
                        hubmanager?.HubFadeToBlack != null
                        && hubmanager.GeneralCamera != null
                        && CameraPoint != null
                    )
                    {
                        UnityEngine.Events.UnityAction onVisible = null;
                        UnityEngine.Events.UnityAction onHidden = null;

                        onVisible = () =>
                        {
                            // Move camera while screen is black
                            hubmanager.GeneralCamera.transform.SetPositionAndRotation(
                                CameraPoint.position,
                                CameraPoint.rotation
                            );

                            hubmanager.HubFadeToBlack.OnVisible.RemoveListener(onVisible);
                            hubmanager.HubFadeToBlack.Hide();
                        };

                        onHidden = () =>
                        {
                            hubmanager.HubFadeToBlack.OnHidden.RemoveListener(onHidden);
                        };

                        hubmanager.HubFadeToBlack.OnVisible.AddListener(onVisible);
                        hubmanager.HubFadeToBlack.OnHidden.AddListener(onHidden);
                        hubmanager.HubFadeToBlack.Show();
                    }
                }
            }
            Hide();
            // play confirmation sound
            // show sublocation UI
            // if sublocation has music, crossfade current music to sublocation music
        }
        #endregion
    }
}
