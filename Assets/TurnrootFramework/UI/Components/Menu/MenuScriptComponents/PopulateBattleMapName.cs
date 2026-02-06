using TMPro;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Populates text objects with the battle map name from a BattlePreparationObject.
    /// </summary>
    public class PopulateBattleMapName : MonoBehaviour
    {
        public TextMeshProUGUI[] BattleMapNameObjects;

        public OperationResult Initialize(BattlePreparationObject battlePreparationObject)
        {
            var mapGrid = battlePreparationObject.MapGrid;
            if (mapGrid != null && BattleMapNameObjects != null)
            {
                foreach (var textObj in BattleMapNameObjects)
                {
                    textObj.text = mapGrid.MapName;
                }
            }
            else
            {
                return OperationResult.Failure("Invalid parameters for PopulateBattleMapName");
            }
            return OperationResult.Successful();
        }
    }
}
