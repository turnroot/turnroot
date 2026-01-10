using Turnroot.UI.Components.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using SimpleButtonComponent = Turnroot.UI.Components.SimpleButton.SimpleButton;

namespace Turnroot.UI.Components.GridMenu
{
    [RequireComponent(typeof(SimpleButtonComponent))]
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

        private SimpleButtonComponent _simpleButton;

        private void Awake()
        {
            _simpleButton = GetComponent<SimpleButtonComponent>();
            if (_simpleButton == null)
            {
                _simpleButton = gameObject.AddComponent<SimpleButtonComponent>();
            }
            _simpleButton.OnSelected += HandleSimpleButtonSelection;
        }

        private void OnDestroy()
        {
            if (_simpleButton != null)
            {
                _simpleButton.OnSelected -= HandleSimpleButtonSelection;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _simpleButton.OnPointerEnter(eventData);
            parentMenu?.NavigateToItem(this);
            RaiseHoverEnter();
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
#if UNITY_EDITOR
            Debug.Log(
                $"GridMenuItem: Select called for {ItemName} parentMenu={(parentMenu == null ? "null" : parentMenu.name)} Row={Row} Col={Column}"
            );
#endif
            base.Select();
            parentMenu?.SelectItem(this);
        }

        private void HandleSimpleButtonSelection() => Select();

        public override void SetParentMenu(MenuBase parent)
        {
            base.SetParentMenu(parent);
            parentMenu = parent;
        }

        public override void SetItemName(string name) => itemName = name;
    }
}
