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
                        _context.Unit.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    context.AttackTarget(
                        _context.Unit.UnitInstance,
                        goal.Target,
                        goal.ChosenWeapon
                    );
                    context.EndTurn();
                    break;

                case AIGoal.GoalType.HealAlly:
                    context.MoveUnitToPointInt(
                        _context.Unit.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    context.HealUnit(_context.Unit.UnitInstance, goal.Target); // TODO: Specify healing item if using
                    context.EndTurn();
                    break;

                case AIGoal.GoalType.ProtectAlly:
                    context.MoveUnitToPointInt(
                        _context.Unit.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // TODO: Apply protective buff to target or attack enemies threatening them
                    context.EndTurn();
                    break;

                case AIGoal.GoalType.KillEnemy:
                    context.MoveUnitToPointInt(
                        _context.Unit.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    context.EndTurn();
                    context.AttackTarget(
                        _context.Unit.UnitInstance,
                        goal.Target,
                        goal.ChosenWeapon
                    );
                    context.EndTurn();
                    break;

                case AIGoal.GoalType.HealSelf:
                    context.MoveUnitToPointInt(
                        _context.Unit.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    context.HealUnit(_context.Unit.UnitInstance, _context.Unit.UnitInstance); // TODO: Specify healing item if using
                    context.EndTurn();
                    break;

                case AIGoal.GoalType.GainPosition:
                    context.MoveUnitToPointInt(
                        _context.Unit.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    context.EndTurn();
                    break;

                case AIGoal.GoalType.CollectTreasure:
                    context.MoveUnitToPointInt(
                        _context.Unit.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // TODO: Trigger treasure collection
                    context.EndTurn();
                    break;

                case AIGoal.GoalType.DefensiveRetreat:
                    context.MoveUnitToPointInt(
                        _context.Unit.UnitInstance,
                        goal.Destination.CoordinatesInt
                    );
                    // End turn after retreating to a safe tile
                    context.EndTurn();
                    break;

                case AIGoal.GoalType.HoldPosition:
                    // Hold position — end turn immediately
                    context.EndTurn();
                    break;
            }
        }

        #endregion
    }
}
