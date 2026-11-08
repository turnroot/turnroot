using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to end a turn.
    /// </summary>
    public class EndUnitTurnCommand : CommandBase
    {
        public EndUnitTurnCommand(int turn)
            : base(turn) { }

        public override bool Execute(BattleContext context)
        {
            // Clear dynamic participant data when turn ends
            // (Targets in range and adjacent units are specific to the active unit)
            context.ClearParticipantDynamicData();

            // TurnRotisserie publishes unit-turn lifecycle events when it activates
            // the next unit, and round events only when the full round ends.
            return true;
        }

        public override bool Undo(BattleContext context) =>
            // TODO: Turn undo command
            false;
    }
}
