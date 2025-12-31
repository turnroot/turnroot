using System;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// Common base for selectable radial menu items and minimal selectable sprite content.
    /// Provides selection events and a common API used by RadialMenu.
    /// </summary>
    public abstract class RadialMenuItemBase : MonoBehaviour
    {
        // Hover / click events
        public event Action OnHoverEnter;
        public event Action OnHoverExit;
        public event Action OnClick;

        public UnityEvent firedEvent;

        // Notify content when selection changes (true = selected, false = deselected)
        public event Action<bool> OnSelectedChanged;

        public UnityEvent OnSelected;
        public UnityEvent OnDeselected;

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

        protected void RaiseFiredEvent() => firedEvent?.Invoke();

        // Selection API
        public virtual void Select()
        {
            _isSelected = true;
            OnSelectedChanged?.Invoke(true);
            OnSelected?.Invoke();
        }

        public virtual void Deselect()
        {
            _isSelected = false;
            OnSelectedChanged?.Invoke(false);
            OnDeselected?.Invoke();
        }

        public virtual void SetIsCenter(bool center) => isCenter = center;

        public virtual void Activate() => RaiseFiredEvent();

        // Visual/layout helpers - default no-op for simple content
        public virtual void EnsureContentOnTop() { }

        public virtual void SetItemName(string name) { }

        public virtual void SetContentPrefab(GameObject prefab) { }

        public virtual void SetColors(Color normal, Color selected) { }

        public virtual void SetSegmentAngles(
            float startAngle,
            float endAngle,
            float innerRadius,
            float gapSize
        ) { }

        public virtual void PositionContent(
            float centerAngleDeg,
            float innerRadiusPct,
            float outerRadiusPct,
            float menuRadius,
            float radialOffsetPct = 0f
        ) { }
    }
}
