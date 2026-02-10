using System;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Objects;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Item Events

        public event Action<ObjectItemInstance, int> OnItemUsed;
        public event Action<ObjectItemInstance> OnItemBroken;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemTransferred;
        public event Action<ObjectItemInstance> OnItemDiscarded;
        public event Action<ObjectItemInstance> OnItemSold;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemBought;
        public event Action<ObjectItemInstance, int> OnItemRepaired;
        public event Action<ObjectItemInstance, ObjectItem> OnItemForged;
        public event Action<ObjectItemInstance> OnItemDeposited;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemWithdrawn;
        public event Action<CharacterInstance, ObjectItemInstance> OnItemEquipped;
        public event Action<CharacterInstance, ObjectItemInstance> OnItemUnequipped;

        public void PublishItemUsed(ObjectItemInstance item, int remainingUses) =>
            OnItemUsed?.Invoke(item, remainingUses);

        public void PublishItemBroken(ObjectItemInstance item) => OnItemBroken?.Invoke(item);

        public void PublishItemTransferred(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        ) => OnItemTransferred?.Invoke(item, targetInventory);

        public void PublishItemDiscarded(ObjectItemInstance item) => OnItemDiscarded?.Invoke(item);

        public void PublishItemSold(ObjectItemInstance item) => OnItemSold?.Invoke(item);

        public void PublishItemBought(
            ObjectItemInstance item,
            CharacterInventoryInstance buyerInventory
        ) => OnItemBought?.Invoke(item, buyerInventory);

        public void PublishItemRepaired(ObjectItemInstance item, int repairUses) =>
            OnItemRepaired?.Invoke(item, repairUses);

        public void PublishItemForged(ObjectItemInstance item, ObjectItem targetItem) =>
            OnItemForged?.Invoke(item, targetItem);

        public void PublishItemDeposited(ObjectItemInstance item) => OnItemDeposited?.Invoke(item);

        public void PublishItemWithdrawn(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        ) => OnItemWithdrawn?.Invoke(item, targetInventory);

        public void PublishItemEquipped(CharacterInstance character, ObjectItemInstance item) =>
            OnItemEquipped?.Invoke(character, item);

        public void PublishItemUnequipped(CharacterInstance character, ObjectItemInstance item) =>
            OnItemUnequipped?.Invoke(character, item);

        #endregion

        #region Gold Events

        public event Action<int> OnGoldGained;
        public event Action<int> OnGoldSpent;

        public void PublishGoldGained(int amount) => OnGoldGained?.Invoke(amount);

        public void PublishGoldSpent(int amount) => OnGoldSpent?.Invoke(amount);

        #endregion
    }
}
