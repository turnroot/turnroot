using TMPro;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Manages the action selection UI during battle, populating a list of available actions for the current unit.
    /// </summary>
    public class BattleSelectAction : MonoBehaviour
    {
        public GameObject ActionButtonPrefab;
        public Transform ListMenuContainer;

        public void Initialize()
        {
            ListMenuContainer =
                ListMenuContainer != null ? ListMenuContainer : transform.Find("List Menu");
            ActionButtonPrefab =
                ActionButtonPrefab != null
                    ? ActionButtonPrefab
                    : Resources.Load<GameObject>("UI/BattleSelectActionButton");
        }

        private OperationResult SetTextOnInstance(GameObject instance, string text)
        {
            var validation = OperationResultGuards.RequireNotNull(instance, nameof(instance));
            if (!validation.Success)
            {
                return validation;
            }

            if (instance.TryGetComponent<ListMenu.ListMenuItem>(out var listMenuItem))
            {
                listMenuItem.SetItemName(text);
            }

            var tmp = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            validation = OperationResultGuards.RequireNotNull(tmp, "TextMeshProUGUI component");
            if (!validation.Success)
            {
                return validation;
            }

            tmp.text = text;
            return OperationResult.Successful();
        }

        public OperationResult PopulateList(string[] actions)
        {
            Initialize();

            var validation = OperationResultGuards.RequireNotNull(
                ListMenuContainer,
                nameof(ListMenuContainer)
            );
            if (!validation.Success)
            {
                return validation;
            }

            // MenuBase is on the ListMenuContainer GameObject, not on this GameObject
            var menuBase = ListMenuContainer.GetComponent<Menu.MenuBase>();
            if (menuBase != null)
            {
                menuBase.menuItems.Clear();
                menuBase.ResetSelection();
            }

            var childCount = ListMenuContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = ListMenuContainer.GetChild(i);
                DestroyImmediate(child.gameObject);
            }

            foreach (var action in actions)
            {
                var instance = Instantiate(ActionButtonPrefab, ListMenuContainer);
                var setTextResult = SetTextOnInstance(instance, action);
                if (!setTextResult.Success)
                {
                    TurnrootLogger.Log(setTextResult.ErrorMessage, TurnrootLogger.LogLevel.Warning);
                }
            }

            if (menuBase != null && ListMenuContainer != null)
            {
                menuBase.menuItems.Clear();
                var items = ListMenuContainer.GetComponentsInChildren<MenuItemBase>();
                foreach (var item in items)
                {
                    item.SetParentMenu(menuBase);
                    menuBase.menuItems.Add(item);
                }

                if (menuBase.menuItems.Count > 0)
                {
                    menuBase.SetSelection(0);
                }
            }

            return OperationResult.Successful();
        }
    }
}
