using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Turnroot.UI.Components.RadialMenu
{
    [RequireComponent(typeof(Image))]
    public class RadialMenuItem
        : RadialMenuItemBase,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler
    {
        [Header("Visual Settings")]
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private bool showIcon = false;

        [SerializeField]
        private Color normalColor = Color.white;

        [SerializeField]
        private Color selectedColor = new Color(1f, 0.8f, 0f);

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
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            // Create material instance for this item
            if (backgroundImage != null && backgroundImage.material != null)
            {
                _material = new Material(backgroundImage.material);
                backgroundImage.material = _material;

                // Enable raycast target so mouse works
                backgroundImage.raycastTarget = true;
            }

            /*             // Initialize default colors from global settings if available
                        var settings = Turnroot.GameSettings.GamewideUiSettings.Instance;
                        if (settings != null)
                        {
                            normalColor = settings.RadialMenuNormalColor;
                            selectedColor = settings.RadialMenuSelectedColor;
                        } */

            // Prefer existing content in scene (assigned instance) if present
            var existingContent = GetComponentInChildren<IRadialMenuContent>(includeInactive: true);
            if (existingContent != null)
            {
                _contentComponent = existingContent;
                _contentRect = (existingContent as Component)?.GetComponent<RectTransform>();
                _contentComponent.SetLabel(itemName);
                var gw = Turnroot.GameSettings.GamewideUiSettings.Instance;
                if (gw != null)
                {
                    _contentComponent.ApplyVisibility(
                        gw.RadialMenuHaveIcons && showIcon,
                        gw.RadialMenuHaveLabels
                    );
                }

                // Ensure the content is on top of the segment's visuals
                var contentTransformComp = (_contentRect as Transform);
                if (contentTransformComp != null)
                {
                    contentTransformComp.SetAsLastSibling();
                    var lp = contentTransformComp.localPosition;
                    contentTransformComp.localPosition = new Vector3(lp.x, lp.y, 0f);
                }
            }

            // Instantiate content prefab (icon + label) and center it inside this segment only if no existing content
            if (existingContent == null && contentPrefab != null)
            {
                var instance = Instantiate(contentPrefab, transform);
                if (instance != null)
                {
                    var rt = instance.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = Vector2.zero;
                        rt.localRotation = Quaternion.identity; // ensure it doesn't rotate with the segment
                        rt.localScale = Vector3.one;
                        // Ensure z is zero to avoid unexpected layering
                        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
                    }

                    // Place content above the background by moving it to the end of sibling list
                    instance.transform.SetAsLastSibling();

                    // Wire up content interface if present
                    var contentTransform = instance.transform as RectTransform;
                    _contentComponent = instance.GetComponentInChildren<IRadialMenuContent>();
                    if (_contentComponent != null)
                    {
                        _contentComponent.SetLabel(itemName);
                        // No per-item icon is stored here; prefab can include its own default icon.
                        var gw = Turnroot.GameSettings.GamewideUiSettings.Instance;
                        if (gw != null)
                        {
                            _contentComponent.ApplyVisibility(
                                gw.RadialMenuHaveIcons,
                                gw.RadialMenuHaveLabels
                            );
                        }
                    }

                    // keep a reference to the content rect if found
                    _contentRect = contentTransform ?? instance.GetComponent<RectTransform>();

                    // Warn if prefab contains a Canvas which can break draw order
                    var prefabCanvas = instance.GetComponentInChildren<Canvas>();
                    if (prefabCanvas != null)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning(
                            $"RadialMenuItem '{name}' instantiated content prefab contains a Canvas. This may override draw order; consider removing it."
                        );
#endif
                    }
                }
            }

            UpdateVisuals();

            if (_contentRect != null)
            {
                Canvas contentCanvas = (_contentRect.gameObject).GetComponent<Canvas>();
                if (contentCanvas == null)
                {
                    contentCanvas = _contentRect.gameObject.AddComponent<Canvas>();
                }
                contentCanvas.overrideSorting = true;
                contentCanvas.sortingOrder = 1; // Render above parent

                // Also add GraphicRaycaster so it still receives input
                if (_contentRect.GetComponent<GraphicRaycaster>() == null)
                {
                    _contentRect.gameObject.AddComponent<GraphicRaycaster>();
                }
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            RaiseHoverEnter();
            UpdateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            RaiseHoverExit();
            UpdateVisuals();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            RaiseClick();
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

        public override void Activate()
        {
#if UNITY_EDITOR
            Debug.Log($"Activated menu item: {itemName}");
#endif
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
            if (_contentComponent != null)
            {
                _contentComponent.SetLabel(name);
            }
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
                var rt = instance.GetComponent<RectTransform>();
                if (rt != null)
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
                    var gw = Turnroot.GameSettings.GamewideUiSettings.Instance;
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
            if (_material != null)
            {
                _material.SetFloat("_IsCenter", center ? 1f : 0f);
            }
        }
    }
}
