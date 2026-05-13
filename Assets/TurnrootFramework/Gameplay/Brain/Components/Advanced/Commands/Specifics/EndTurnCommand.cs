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
            // Clear dynamic participant data when turn ends
            // (Targets in range and adjacent units are specific to the active unit)
            context.ClearParticipantDynamicData();

            // Publish the round-level turn-ended event.
            // NOTE: PublishUnitTurnEnded is intentionally NOT called here.
            // TurnRotisserie.SetActiveUnitInContext fires it for ALL unit types
            // (player, enemy, third-party) when the next unit is activated, ensuring
            // exactly one firing regardless of whether EndTurnCommand was called.
            context.Brain.Publish(new Events.TurnEndedEvent(TurnNumber));
            return true;
        }

        public override bool Undo(BattleContext context) =>
            // TODO: Turn undo command
            false;
    }
}
