using Turnroot.Gameplay.Brain;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    public class Blacksmith : MonoBehaviour
    {
        // 1. select a character (playerteamroster) or storehouse
        // 2. show the character's inventory
        // 3. disable all items that are not weapons, shields, or accessories
        // 4. Navigable/selectable UIChoices for each
        // 4a. details
        // 4b. switch between repair mode and forge mode -  use sell button toggle from shopui
        // 5a. Repair mode - show durability and repair cost, confirm to repair. One option per
        // 5b. Forge mode many options per.
        // 6. Back flow- inventory select -> item select -> item action (forge/repair) -> confirm -> inventory select
        // back at any arrow point
        // 7. On forge/repair, update gold and storehouse (like shopui )
        // 7a. Replace item in inventory with new forged item or repaired item- InventoryBrain
        // 7b. On forge repair dialogue
        // 8. exit entry dialogue

        [HideInInspector]
        public Brain.Brain _brain;
        public InventoryBrain _inventoryBrain { private get; set; }
        public StorehouseBrain _storehouseBrain { private get; set; }
        public CharactersBrain _charactersBrain { private get; set; }
    }
}
