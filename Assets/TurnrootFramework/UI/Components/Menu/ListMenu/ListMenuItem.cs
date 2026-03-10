using Turnroot.UI.Components.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using SimpleButtonComponent = Turnroot.UI.Components.SimpleButton.SimpleButton;

namespace Turnroot.UI.Components.ListMenu
{
    /// <summary>
    /// A selectable list menu item that handles pointer interactions and integrates with SimpleButton for visual feedback.
    /// </summary>
    [RequireComponent(typeof(SimpleButtonComponent))]
    public class ListMenuItem
        : MenuItemBase,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler
    {
        [HideInInspector]
        public MenuBase parentMenu;

        public override string ItemName => itemName;

        [SerializeField]
        private string itemName;

        private SimpleButtonComponent _simpleButton;

        private void Awake()
        {
            // Get or add SimpleButton component for visual feedback
            _simpleButton = GetComponent<SimpleButtonComponent>() ?? gameObject.AddComponent<SimpleButtonComponent>();

            // Subscribe to SimpleButton's OnSelected event to trigger menu selection
            _simpleButton.OnSelected += HandleSimpleButtonSelection;
        }

        private void OnDestroy()
        {
            // Clean up event subscription
            if (_simpleButton != null)
            {
                _simpleButton.OnSelected -= HandleSimpleButtonSelection;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Delegate to SimpleButton for visual feedback
            _simpleButton.OnPointerEnter(eventData);

            // Handle menu navigation
            parentMenu?.NavigateToItem(this);

            RaiseHoverEnter();
        }

        public override void SetParentMenu(MenuBase parent)
        {
            base.SetParentMenu(parent);
            parentMenu = parent;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _simpleButton.OnPointerExit(eventData);

            RaiseHoverExit();
        }

        public void OnPointerClick(PointerEventData eventData) =>
            _simpleButton.OnPointerClick(eventData);

        public override void Select()
        {
            base.Select();
            if (parentMenu != null)
            {
                parentMenu.SelectItem(this);
            }
        }

        private void HandleSimpleButtonSelection() => Select();

        public override void SetItemName(string name) => itemName = name;
    }
}
