using System;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// Minimal radial menu content that swaps a sprite on selection.
    /// - Exposes Unselected / Selected sprites
    /// - Optional Image target (defaults to Image on same GameObject)
    /// - Optional IsCenter checkbox which will set the parent RadialMenuItem's center flag
    /// Implements IRadialMenuContent so it can be used as the content prefab for RadialMenuItem.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class RadialMenuItemSprite : RadialMenuItemBase, IRadialMenuContent
    {
        [Header("Sprite Settings")]
        [SerializeField]
        private Sprite unselectedSprite;

        [SerializeField]
        private Sprite selectedSprite;

        [Header("Behavior")]
        [SerializeField]
        [Tooltip("If checked, mark this item as the center item in the parent RadialMenuItem")]
        // uses base 'isCenter' field (serialized in RadialMenuItemBase)
        private bool _editor_isCenterProxy = false;

        [SerializeField]
        [Tooltip(
            "Optional target Image to update when selected/unselected. If null, the Image on this GameObject is used."
        )]
        private Image targetImage;

        private RadialMenuItemBase _ownerItem;

        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();

            // If we're inside a RadialMenuItem, wire up selection events
            _ownerItem = GetComponentInParent<RadialMenuItemBase>();
            if (_ownerItem != null && _ownerItem != this)
            {
                // reflect isCenter to owner
                _ownerItem.SetIsCenter(isCenter);

                // Subscribe to selection changes if available
                _ownerItem.OnSelectedChanged += HandleOwnerSelectedChanged;
            }

            // Initialize sprite to unselected
            ApplyUnselected();
        }

        private void OnDestroy()
        {
            if (_ownerItem != null && _ownerItem != this)
            {
                _ownerItem.OnSelectedChanged -= HandleOwnerSelectedChanged;
            }
        }

        private void HandleOwnerSelectedChanged(bool selected)
        {
            if (selected)
                ApplySelected();
            else
                ApplyUnselected();
        }

        private void ApplySelected()
        {
            if (targetImage != null && selectedSprite != null)
                targetImage.sprite = selectedSprite;
        }

        private void ApplyUnselected()
        {
            if (targetImage != null && unselectedSprite != null)
                targetImage.sprite = unselectedSprite;
        }

        public override void Select()
        {
            base.Select();
            ApplySelected();
        }

        public override void Deselect()
        {
            base.Deselect();
            ApplyUnselected();
        }

        public override void SetIsCenter(bool center)
        {
            isCenter = center;
            // If we sit inside another item, propagate to it
            if (_ownerItem != null && _ownerItem != this)
                _ownerItem.SetIsCenter(center);
        }

        public override void EnsureContentOnTop()
        {
            // Ensure our own image sits on top if used as a standalone item
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.SetAsLastSibling();
                rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
            }
        }

        // IRadialMenuContent implementation
        public void SetLabel(string text)
        {
            // no-op for sprite-only content
        }

        public void SetIcon(Sprite icon)
        {
            // Treat provided icon as the unselected sprite if none assigned
            if (unselectedSprite == null)
            {
                unselectedSprite = icon;
                ApplyUnselected();
            }
        }

        public void ApplyVisibility(bool showIcon, bool showLabel)
        {
            // showIcon toggles the target Image's enabled state
            if (targetImage != null)
            {
                targetImage.enabled = showIcon;
            }

            // no labels supported here
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // keep owner isCenter in sync in editor
            if (!Application.isPlaying)
            {
                _ownerItem = GetComponentInParent<RadialMenuItemBase>();
                if (_ownerItem != null && _ownerItem != this)
                {
                    _ownerItem.SetIsCenter(isCenter);

                    // update preview image
                    if (targetImage == null)
                        targetImage = GetComponent<Image>();
                    if (targetImage != null && unselectedSprite != null)
                        targetImage.sprite = unselectedSprite;
                }
            }
        }
#endif
    }
}
