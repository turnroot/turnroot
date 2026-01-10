using Turnroot.Gameplay.Combat.PreBattle;
using UnityEngine;
using UnityEngine.UI;

public class PopulateBattleMapPreview : MonoBehaviour
{
    public Image BattleMapImage;

    public void Initialize(BattlePreparationObject battlePreparationObject)
    {
        var mapGrid = battlePreparationObject.MapGrid;
        if (mapGrid != null && BattleMapImage != null)
        {
            BattleMapImage.sprite = mapGrid.StandardMapImage;
        }
    }
}
