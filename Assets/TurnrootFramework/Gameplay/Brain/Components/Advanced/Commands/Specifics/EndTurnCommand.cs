using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to end a turn.
    /// </summary>
    public class EndTurnCommand : CommandBase
    {
        public EndTurnCommand(int turn)
            : base(turn) { }

        public override bool Execute(BattleContext context)
        {
            // Notify that a unit's turn ended (unit is still the active one on the context)
            context.Brain?.PublishUnitTurnEnded(context.Unit.UnitInstance);

            // Also publish the round-level event
            context.Brain?.Publish(new Events.TurnEndedEvent(TurnNumber));
            return true;
        }

        public override bool Undo(BattleContext context)
        {
            // TODO: Turn undo command
            return false;
        }
    }
}
