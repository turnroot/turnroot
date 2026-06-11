using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    public class Blacksmith : HubVendor
    {
        public InventoryBrain _inventoryBrain { private get; set; }
        public StorehouseBrain _storehouseBrain { private get; set; }
        public CharactersBrain _charactersBrain { private get; set; }

        public void NotifyBlacksmithVisited()
        {
            NotifyVendorVisited(
                () => TryGetComponent<BlacksmithUi>(out var ui) ? ui : null,
                blacksmithUi => blacksmithUi.RefreshBlacksmithDisplay(),
                "Blacksmith"
            );
        }

        public void NotifyBlacksmithExited()
        {
            NotifyVendorExited(
                () => TryGetComponent<BlacksmithUi>(out var ui) ? ui : null,
                blacksmithUi => blacksmithUi.BlacksmithUiFade.Hide(),
                "Blacksmith"
            );
        }

        public override void HandleConfirmInput(string action)
        {
            var blacksmithUi = TryGetComponent<BlacksmithUi>(out var ui) ? ui : null;
            blacksmithUi?.HandleSelectInput(action);
        }

        public override void HandleBackInput(string action)
        {
            if (action is not "Back" and not InputActionConstants.Cancel)
            {
                return;
            }

            var blacksmithUi = TryGetComponent<BlacksmithUi>(out var ui) ? ui : null;
            if (blacksmithUi?.TryHandleBack(action) == true)
            {
                return;
            }

            NotifyBlacksmithExited();
        }
    }
}
