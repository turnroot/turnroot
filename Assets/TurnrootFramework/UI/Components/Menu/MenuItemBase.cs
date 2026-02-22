using System;
using Turnroot.UI.Components.Menu;
using UnityEngine;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Common base for selectable menu items.
    /// Provides selection events and a common API used by both RadialMenu and ListMenu.
    /// </summary>
    public abstract class MenuItemBase : MonoBehaviour
    {
        // Hover / click events
        public event Action OnHoverEnter;
        public event Action OnHoverExit;
        public event Action OnClick;

        // Notify content when selection changes (true = selected, false = deselected)
        public event Action<bool> OnSelectedChanged;

        protected bool _isSelected = false;
        protected bool _isHovered = false;

        [SerializeField]
        protected bool isCenter = false;

        public virtual string ItemName => (this as Component)?.name ?? gameObject.name;
        public virtual bool IsCenter => isCenter;

        // Helper methods so derived classes can raise the public events
        protected void RaiseHoverEnter() => OnHoverEnter?.Invoke();

        protected void RaiseHoverExit() => OnHoverExit?.Invoke();

        protected void RaiseClick() => OnClick?.Invoke();

        protected void RaiseSelectedChanged(bool s) => OnSelectedChanged?.Invoke(s);

        // Selection API
        public virtual void Select()
        {
            _isSelected = true;
            OnSelectedChanged?.Invoke(true);
        }

        public virtual void Deselect()
        {
            _isSelected = false;
            OnSelectedChanged?.Invoke(false);
        }

        public virtual void SetIsCenter(bool center) => isCenter = center;

        // Visual/layout helpers - default no-op for simple content
        public virtual void EnsureContentOnTop() { }

        public virtual void SetItemName(string name) { }

        // Parent menu reference
        public MenuBase ParentMenu { get; private set; }

        // Menu parent setter used by MenuBase to wire up child items generically
        public virtual void SetParentMenu(MenuBase parent) => ParentMenu = parent;

        public virtual void SetContentPrefab(GameObject prefab) { }

        public virtual void SetColors(Color normal, Color selected) { }

        public virtual void SetSegmentAngles(
            float startAngle,
            float endAngle,
            float innerRadius,
            float gapSize
        )
        { }

        public virtual void PositionContent(
            float centerAngleDeg,
            float innerRadiusPct,
            float outerRadiusPct,
            float menuRadius,
            float radialOffsetPct = 0f
        )
        { }
    }
}
