using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Goal Execution


        /// <summary>
        /// Executes the chosen goal, performing movement and actions as needed.
        /// </summary>
        private void ExecuteGoal(AIGoal goal, BattleContext context)
        {
            switch (goal.Type)
            {
                case AIGoal.GoalType.AttackEnemy:

                    context.MoveUnitToPointInt(
                        _context.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    context.AttackTarget(_context.UnitInstance, goal.Target);

                    break;

                case AIGoal.GoalType.HealAlly:
                    context.MoveUnitToPointInt(
                        _context.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // TODO: Execute heal on target
                    break;

                case AIGoal.GoalType.ProtectAlly:
                    context.MoveUnitToPointInt(
                        _context.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // TODO: Apply protective buff to target or attack enemies threatening them
                    break;

                case AIGoal.GoalType.KillEnemy:
                    context.MoveUnitToPointInt(
                        _context.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // TODO: Execute attack on target
                    break;

                case AIGoal.GoalType.HealSelf:
                    context.MoveUnitToPointInt(
                        _context.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // TODO: Execute self-heal
                    break;

                case AIGoal.GoalType.GainPosition:
                    // TODO: Move to strategic position
                    break;

                case AIGoal.GoalType.CollectTreasure:
                    context.MoveUnitToPointInt(
                        _context.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // TODO: Trigger treasure collection
                    break;

                case AIGoal.GoalType.DefensiveRetreat:
                    context.MoveUnitToPointInt(
                        _context.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // TODO: End turn
                    break;

                case AIGoal.GoalType.HoldPosition:
                    // TODO: Just end turn without moving
                    break;
            }
        }

        #endregion
    }
}
