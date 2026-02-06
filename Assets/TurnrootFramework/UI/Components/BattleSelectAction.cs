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

        public OperationResult SetTextActionButtonPrefab(string text)
        {
            if (ActionButtonPrefab.TryGetComponent<ListMenu.ListMenuItem>(out var listMenuItem))
            {
                var l = listMenuItem.ItemName;
                l = text;
                var tmp = ActionButtonPrefab.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = text;
                    return OperationResult.Successful();
                }
            }
            return OperationResult.Failure(
                "BattleSelectAction: Failed to set text on ActionButtonPrefab"
            );
        }

        public OperationResult PopulateList(string[] actions)
        {
            Initialize();

            foreach (Transform child in ListMenuContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var action in actions)
            {
                _ = Instantiate(ActionButtonPrefab, ListMenuContainer);
                var setTextResult = SetTextActionButtonPrefab(action);
                if (!setTextResult.Success)
                {
                    TurnrootLogger.Log(
                        $"BattleSelectAction: Failed to set text for action '{action}' - {setTextResult.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }

            return OperationResult.Successful();
        }
    }
}
