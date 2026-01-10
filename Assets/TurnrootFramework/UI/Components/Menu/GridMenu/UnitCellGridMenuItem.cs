using UnityEngine;

namespace Turnroot.UI.Components.GridMenu
{
    public class UnitCellGridMenuItem : GridMenuItem
    {
        // These need extra data
        [HideInInspector]
        public bool IsSelectedForBattle = false;

        [HideInInspector]
        public bool CanBeSelectedForBattle = true;

        [HideInInspector]
        public bool IsGettingDetailsShown = false;

        [HideInInspector]
        public bool IsSelectedToBeExplorer = false;
    }
}
