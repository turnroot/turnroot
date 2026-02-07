using Turnroot.Characters;
using Turnroot.Gameplay.Objects;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Published when an item is used in battle.
    /// </summary>
    public class ItemUsedEvent : BattleEvent
    {
        public CharacterInstance User { get; }
        public ObjectItemInstance Item { get; }
        public CharacterInstance Target { get; }
        public int RemainingUses { get; }

        public ItemUsedEvent(
            CharacterInstance user,
            ObjectItemInstance item,
            CharacterInstance target,
            int remaining
        )
        {
            User = user;
            Item = item;
            Target = target;
            RemainingUses = remaining;
        }
    }

    /// <summary>
    /// Published when an item breaks (uses reach 0).
    /// </summary>
    public class ItemBrokenEvent : BattleEvent
    {
        public CharacterInstance Owner { get; }
        public ObjectItemInstance Item { get; }

        public ItemBrokenEvent(CharacterInstance owner, ObjectItemInstance item)
        {
            Owner = owner;
            Item = item;
        }
    }
}
