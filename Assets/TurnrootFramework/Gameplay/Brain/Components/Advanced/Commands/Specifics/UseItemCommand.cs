using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to use an item.
    /// </summary>
    public class UseItemCommand : CommandBase
    {
        public string UserId { get; }
        public string ItemId { get; }
        public string TargetId { get; }

        public UseItemCommand(string userId, string itemId, string targetId, int turn)
            : base(turn)
        {
            UserId = userId;
            ItemId = itemId;
            TargetId = targetId;
        }

        public override bool Execute(BattleContext context)
        {
            TurnrootLogger.Log(
                $"[UseItemCommand] {UserId} used item {ItemId} on {TargetId ?? "self"}"
            );
            // TODO: Use item command
            return true;
        }

        public override bool Undo(BattleContext context)
        {
            TurnrootLogger.Log(
                "[UseItemCommand] Item use cannot be undone",
                TurnrootLogger.LogLevel.Warning
            );
            return false;
        }
    }
}
