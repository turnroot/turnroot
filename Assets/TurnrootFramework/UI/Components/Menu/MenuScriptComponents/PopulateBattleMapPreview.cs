using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Populates an image with the battle map preview sprite from a BattlePreparationObject.
    /// </summary>
    public class PopulateBattleMapPreview : MonoBehaviour
    {
        public Image BattleMapImage;

        public OperationResult Initialize(BattlePreparationObject battlePreparationObject)
        {
            var mapGrid = battlePreparationObject.MapGrid;
            if (mapGrid != null && BattleMapImage != null)
            {
                BattleMapImage.sprite = mapGrid.StandardMapImage;
            }
            else
            {
                return OperationResult.Failure("Invalid parameters for PopulateBattleMapPreview");
            }
            return OperationResult.Successful();
        }
    }
}
