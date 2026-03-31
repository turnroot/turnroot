using NaughtyAttributes;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Gameplay.Objects;
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

            NotifyBlacksmithExited();
        }

        /* --------------------------------- Testing -------------------------------- */
        public ObjectItem TestRepairItem;

        [Button("Add Test Repair Item to Storehouse")]
        public void AddTestRepairItemInstanceToStorehouse()
        {
            if (_storehouseBrain == null)
            {
                $"Blacksmith '{name}': Cannot add test repair item instance to storehouse because StorehouseBrain reference is null.".LogWarning();
                return;
            }

            if (TestRepairItem == null)
            {
                $"Blacksmith '{name}': TestRepairItem is null, cannot add item.".LogWarning();
                return;
            }

            var newItemInstance = new ObjectItemInstance(TestRepairItem);
            _inventoryBrain.UseItem(newItemInstance);
            _inventoryBrain.UseItem(newItemInstance);
            _inventoryBrain.UseItem(newItemInstance);
            _inventoryBrain.UseItem(newItemInstance);
            _inventoryBrain.UseItem(newItemInstance);
            _inventoryBrain.UseItem(newItemInstance);
            _inventoryBrain.UseItem(newItemInstance);
            var result = _storehouseBrain.DepositItem(newItemInstance);
            if (!result.Success)
            {
                $"Blacksmith '{name}': Failed to deposit item instance: {result.ErrorMessage}".LogWarning();
                return;
            }
        }
    }
}
