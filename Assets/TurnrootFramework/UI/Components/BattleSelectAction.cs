using TMPro;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public class BattleSelectAction : MonoBehaviour
    {
        public GameObject ActionButtonPrefab;
        public Transform ListMenuContainer;

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
            if (ListMenuContainer == null)
            {
                return OperationResult.Failure("ListMenuContainer is not assigned");
            }

            foreach (Transform child in ListMenuContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var action in actions)
            {
                Instantiate(ActionButtonPrefab, ListMenuContainer);
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
