using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Moves an adjacent ally to the tile directly behind the caster
    /// (i.e. the tile on the opposite side of where the ally stands), or swaps positions
    /// with the ally via the command system.
    /// All movements fire brain events so animation/SFX listeners can react.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Reposition")]
    [NodeLabel("Reposition: move ally behind caster, or swap")]
    public class RepositionNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

        [Tooltip(
            "Behind: ally vaults to the tile on the far side of the caster.\n"
                + "Swap: caster and ally exchange positions via SwapCommand."
        )]
        public RepositionDirection moveDirection = RepositionDirection.Behind;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var caster = context.Unit.UnitInstance;
            if (!ValidationHelper.ValidateNotNull(caster, nameof(caster)))
            {
                return;
            }

            if (context.Participants.AdjacentUnits == null)
            {
                "Reposition: No adjacent units data".LogWarning();
                return;
            }

            Direction allyDirection = context.GetCustomData("SelectedDirection", Direction.Center);
            var ally = context.Participants.AdjacentUnits.GetUnit(allyDirection);
            if (ally == null)
            {
                $"Reposition: No unit at {allyDirection}".LogWarning();
                return;
            }

            var casterPos = caster.MapGridPosition;
            var allyPos = ally.MapGridPosition;

            switch (moveDirection)
            {
                case RepositionDirection.Behind:
                    // targetPos = caster + (caster - ally) = 2*caster - ally
                    context.MoveUnitToPointInt(ally, 2 * casterPos - allyPos);
                    break;

                case RepositionDirection.Swap:
                    // Swap caster and ally using the command system (fires swap events for animation/SFX)
                    var turn = context.Brain?.battleBrain?.CurrentTurnNumber ?? 0;
                    context.Brain.ExecuteCommand(new SwapCommand(caster.Id, ally.Id, turn));
                    break;
            }

            $"Reposition: Moved ally from {allyDirection} ({allyPos}) via {moveDirection}".LogInfo();
        }
    }

    public enum RepositionDirection
    {
        Behind, // Ally vaults to the tile on the far side of the caster
        Swap, // Caster and ally exchange positions
    }
}
