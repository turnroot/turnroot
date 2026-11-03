using Turnroot.GameSettings;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// A segment item in a radial menu with material-based highlighting and optional content prefab support.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class RadialMenuItem : MenuItemBase
    {
        [Header("Visual Settings")]
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private bool showIcon = false;
        private Color normalColor;
        private Color selectedColor;

        [Header("Item Data")]
        [SerializeField]
        private string itemName;

        [SerializeField]
        private GameObject contentPrefab;

        private Material _material;

        // optional content prefab instance interface
        private IRadialMenuContent _contentComponent;
        private RectTransform _contentRect;

        public override string ItemName => itemName;

        private void Awake()
        {
            InitializeColorsAndMaterial();
            var existingContent = InitializeExistingContent();
            if (existingContent == null && contentPrefab != null)
            {
                InitializePrefabContent();
            }
            UpdateVisuals();
            ConfigureContentCanvas();
        }

        private void InitializeColorsAndMaterial()
        {
            var uiSettings = GamewideUiSettings.Instance;
            normalColor = uiSettings != null ? uiSettings.RadialMenuNormalColor : Color.white;
            selectedColor = uiSettings != null
                ? uiSettings.RadialMenuSelectedColor
                : new Color(1f, 0.8f, 0f);

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (backgroundImage == null || backgroundImage.material == null)
            {
                return;
            }

            _material = new Material(backgroundImage.material);
            backgroundImage.material = _material;
            backgroundImage.raycastTarget = false;
        }

        private IRadialMenuContent InitializeExistingContent()
        {
            var existingContent = GetComponentInChildren<IRadialMenuContent>(includeInactive: true);
            if (existingContent == null)
            {
                return null;
            }

            _contentComponent = existingContent;
            _contentRect = (existingContent as Component)?.GetComponent<RectTransform>();
            _contentComponent.SetLabel(itemName);
            ApplyContentVisibility(showIcon);

            var contentTransform = _contentRect as Transform;
            if (contentTransform != null)
            {
                contentTransform.SetAsLastSibling();
                var position = contentTransform.localPosition;
                contentTransform.localPosition = new Vector3(position.x, position.y, 0f);
            }

            return existingContent;
        }

        private void InitializePrefabContent()
        {
            var instance = Instantiate(contentPrefab, transform);
            if (instance == null)
            {
                return;
            }

            CenterPrefabContent(instance);
            instance.transform.SetAsLastSibling();

            var contentTransform = instance.transform as RectTransform;
            _contentComponent = instance.GetComponentInChildren<IRadialMenuContent>();
            if (_contentComponent != null)
            {
                _contentComponent.SetLabel(itemName);
                ApplyContentVisibility(includeItemIcon: false);
            }

            _contentRect = contentTransform ?? instance.GetComponent<RectTransform>();
        }

        private static void CenterPrefabContent(GameObject instance)
        {
            if (!instance.TryGetComponent<RectTransform>(out var rectTransform))
            {
                return;
            }

            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = new Vector3(
                rectTransform.localPosition.x,
                rectTransform.localPosition.y,
                0f
            );
        }

        private void ApplyContentVisibility(bool includeItemIcon)
        {
            var settings = GamewideUiSettings.Instance;
            if (settings == null)
            {
                return;
            }

            _contentComponent.ApplyVisibility(
                settings.RadialMenuHaveIcons && (!includeItemIcon || showIcon),
                settings.RadialMenuHaveLabels
            );
        }

        private void ConfigureContentCanvas()
        {
            if (_contentRect == null)
            {
                return;
            }

            var contentObject = _contentRect.gameObject;
            if (!contentObject.TryGetComponent<Canvas>(out var contentCanvas))
            {
                contentCanvas = contentObject.AddComponent<Canvas>();
            }

            contentCanvas.overrideSorting = true;
            contentCanvas.sortingOrder = 1;
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        public override void Select()
        {
            base.Select();
            UpdateVisuals();
        }

        public override void Deselect()
        {
            base.Deselect();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (backgroundImage == null)
            {
                return;
            }

            // Use only a selected color vs normal color. Hover no longer changes color to avoid visual ambiguity.
            backgroundImage.color = _isSelected ? selectedColor : normalColor;
        }

        public override void SetItemName(string name)
        {
            itemName = name;
            _contentComponent?.SetLabel(name);
            if (_contentRect != null)
            {
                _contentRect.anchoredPosition = Vector2.zero; // ensure centered
                _contentRect.localRotation = Quaternion.identity; // ensure upright
            }
        }

        /// <summary>
        /// Set the content prefab at runtime (optional). If provided it will be instantiated and centered.
        /// </summary>
        public override void SetContentPrefab(GameObject prefab)
        {
            contentPrefab = prefab;
            // Remove existing children created from previous prefab
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }

            if (contentPrefab != null)
            {
                var instance = Instantiate(contentPrefab, transform);
                if (instance.TryGetComponent<RectTransform>(out var rt))
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }

                // Wire up content interface
                _contentComponent = instance.GetComponentInChildren<IRadialMenuContent>();
                if (_contentComponent != null)
                {
                    _contentComponent.SetLabel(itemName);
                    var gw = GamewideUiSettings.Instance;
                    if (gw != null)
                    {
                        _contentComponent.ApplyVisibility(
                            gw.RadialMenuHaveIcons,
                            gw.RadialMenuHaveLabels
                        );
                    }
                }

                _contentRect = instance.GetComponent<RectTransform>();
            }
        }

        public override void SetColors(Color normal, Color selected)
        {
            normalColor = normal;
            selectedColor = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Ensure instantiated/assigned content is placed above the segment visuals and z is reset.
        /// Useful to fix inconsistent prefab ordering in the scene.
        /// </summary>
        public override void EnsureContentOnTop()
        {
            if (_contentRect == null)
            {
                return;
            }

            var t = _contentRect.transform;
            t.SetAsLastSibling();
            t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, 0f);
        }

        public override void SetSegmentAngles(
            float startAngle,
            float endAngle,
            float innerRadius,
            float gapSize
        )
        {
            if (_material != null)
            {
                _material.SetFloat("_StartAngle", startAngle);
                _material.SetFloat("_EndAngle", endAngle);
                _material.SetFloat("_InnerRadius", innerRadius);
                _material.SetFloat("_GapSize", gapSize);
                _material.SetFloat("_IsCenter", isCenter ? 1f : 0f);
            }
        }

        /// <summary>
        /// Position content prefab inside the segment at the given center angle (deg) and radius.
        /// Keeps content upright and centered in the segment.
        /// </summary>
        /// <param name="radialOffsetPct">Additional radial offset applied to the computed radial percent (fraction of menu radius). Can be negative.</param>
        public override void PositionContent(
            float centerAngleDeg,
            float innerRadiusPct,
            float outerRadiusPct,
            float menuRadius,
            float radialOffsetPct = 0f
        )
        {
            if (_contentRect == null)
            {
                return;
            }

            // TODO: Warning- unsolvable bug!
            // I've spent two days trying to figure out why the first segment's content is always
            // in the wrong place. I've asked professional developers. I've asked chatbots. I've iterated
            // over every possible trigonometric combination. There is no logical or rational behavior
            // for this behavior. There is no way to explain it mathematically, programatically, or
            // algorithmically. There is no solving this issue- it is a permanent bug
            // and this issue will remain open as a load-bearing pillar of malicious, precarious,
            // entropic hate and rage. Don't try and solve this.
            // If you make a PR that "fixes" this, I promise you I've already tried whatever you
            // are trying. It doesn't work. It will never work. Run away.

            bool isFirstSegment = transform.GetSiblingIndex() == 1;

            float angleRad = centerAngleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));

            // WORKAROUND: Flip both X and Y for first segment only (180° rotation)
            if (isFirstSegment)
            {
                dir = -dir;
            }

            float radialPct = innerRadiusPct + (outerRadiusPct - innerRadiusPct) * 0.5f;
            radialPct += radialOffsetPct;
            radialPct = Mathf.Clamp01(radialPct);

            float radialDist = radialPct * menuRadius;

            Vector2 targetPos = dir * radialDist;

            // If this item is the center item, offset its content downward by half the menu radius
            if (isCenter)
            {
                targetPos += Vector2.down * (menuRadius * 0.5f);
            }

            _contentRect.anchoredPosition = targetPos;
            _contentRect.rotation = Quaternion.identity;
        }

        public override void SetIsCenter(bool center)
        {
            isCenter = center;
            _material?.SetFloat("_IsCenter", center ? 1f : 0f);
        }
    }
}
