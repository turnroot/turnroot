using Turnroot.UI.Components.Menu;
using UnityEngine;

namespace Turnroot.UI.Components.ListMenu
{
    /// <summary>
    /// A selectable list menu item that handles pointer interactions and uses UiChoice for visual feedback.
    /// </summary>
    [RequireComponent(typeof(UiChoice))]
    public class ListMenuItem
        : MenuItemBase
    {
        [HideInInspector]
        public MenuBase parentMenu;

        public override string ItemName => itemName;

        [SerializeField]
        private string itemName;

        private UiChoice _uiChoice;

        private void Awake() => _uiChoice = GetComponent<UiChoice>() ?? gameObject.AddComponent<UiChoice>();

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
