using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;

    public class Blacksmith : HubVendor
    {
        [HideInInspector]
        public Brain.Brain _brain;
        public InventoryBrain _inventoryBrain { private get; set; }
        public StorehouseBrain _storehouseBrain { private get; set; }
        public CharactersBrain _charactersBrain { private get; set; }

        public void NotifyBlacksmithVisited()
        {
            NotifyVisited(
                () =>
                {
                    var blacksmithUi = TryGetComponent<BlacksmithUi>(out var ui) ? ui : null;
                    if (blacksmithUi == null)
                    {
                        $"Blacksmith '{name}': No BlacksmithUi component found for dialogue playback.".LogWarning();
                    }
                    else
                    {
                        blacksmithUi.RefreshBlacksmithDisplay();
                    }
                },
                "Blacksmith"
            );
        }

        public void NotifyBlacksmithExited()
        {
            NotifyExited(
                () =>
                {
                    var blacksmithUi = TryGetComponent<BlacksmithUi>(out var ui) ? ui : null;
                    blacksmithUi?.BlacksmithUiFade.Hide();
                },
                "Blacksmith"
            );
        }
    }
}
