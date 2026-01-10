using TMPro;
using Turnroot.Gameplay.Combat.PreBattle;
using UnityEngine;

public class PopulateBattleMapName : MonoBehaviour
{
    public TextMeshProUGUI[] BattleMapNameObjects;

    public void Initialize(BattlePreparationObject battlePreparationObject)
    {
        var mapGrid = battlePreparationObject.MapGrid;
        if (mapGrid != null && BattleMapNameObjects != null)
        {
            foreach (var textObj in BattleMapNameObjects)
            {
                textObj.text = mapGrid.MapName;
            }
        }
    }
}
