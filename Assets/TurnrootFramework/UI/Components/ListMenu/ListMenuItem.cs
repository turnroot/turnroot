using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Turnroot.UI.Components.ListMenu
{
    public class ListMenuItem
        : MenuItemBase,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler
    {
        [HideInInspector]
        public ListMenu parentMenu;

        public override string ItemName => itemName;

        [SerializeField]
        private string itemName;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (parentMenu != null)
            {
                parentMenu.NavigateToItem(this);
            }
            // TODO: Add visual highlighting
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // TODO: Remove visual highlighting
        }

        public void OnPointerClick(PointerEventData eventData) => Select();

        public override void Select()
        {
            base.Select();
            if (parentMenu != null)
            {
                parentMenu.SelectItem(this);
            }
        }

        public override void SetItemName(string name)
        {
            itemName = name;
        }
    }
}
