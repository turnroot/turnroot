using Turnroot.UI;
using Turnroot.UI.Components.Menu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Turnroot.UI.Components.GridMenu
{
    /// <summary>
    /// A menu item designed for grid-based menus with row/column positioning and UiChoice visual feedback.
    /// </summary>
    [RequireComponent(typeof(UiChoice))]
    public class GridMenuItem
        : MenuItemBase,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler
    {
        [HideInInspector]
        public MenuBase parentMenu;

        [SerializeField]
        public int Row;

        [SerializeField]
        public int Column;

        public override string ItemName => itemName;

        public void SetItemNamePublic(string name) => itemName = name;

        [SerializeField]
        private string itemName;

        private UiChoice _uiChoice;

        private void Awake()
        {
            _uiChoice = GetComponent<UiChoice>() ?? gameObject.AddComponent<UiChoice>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _uiChoice.Select();
            parentMenu?.NavigateToItem(this);
            RaiseHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _uiChoice.Deselect();
            RaiseHoverExit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
            Select();
        }

        public override void Select()
        {
            base.Select();
            parentMenu?.SelectItem(this);
        }

        public override void Deselect()
        {
            base.Deselect();
            _uiChoice?.Deselect();
        }

        public void HighlightVisual() => _uiChoice?.Select();

        public void ClearHighlightVisual() => _uiChoice?.Deselect();

        public override void SetParentMenu(MenuBase parent)
        {
            base.SetParentMenu(parent);
            parentMenu = parent;
        }

        public override void SetItemName(string name) => itemName = name;
    }
}
