using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.UI.Components.GridMenu
{
    /// <summary>
    /// Specialized grid menu item for displaying character units with battle selection and explorer assignment states.
    /// </summary>
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

        public CharacterInstance CharacterInstanceData;
    }
}
