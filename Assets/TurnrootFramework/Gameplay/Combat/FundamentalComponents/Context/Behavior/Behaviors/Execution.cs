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
                    _ =
                        context.MoveUnit(_context.UnitInstance, goal.Destination.CoordinatesInt)
                        == true
                            ? OperationResult.SuccessResult()
                            : OperationResult.Failure("Failed to move unit to destination");
                    _ = context.AttackTarget(_context.UnitInstance, goal.Target)
                        ? OperationResult.SuccessResult()
                        : OperationResult.Failure("Failed to execute attack on target");
                    break;

                case AIGoal.GoalType.HealAlly:
                    // TODO: Move to destination
                    // TODO: Execute heal on target
                    break;

                case AIGoal.GoalType.ProtectAlly:
                    // TODO: Move to destination
                    // TODO: Apply protective buff to target or attack enemies threatening them
                    break;

                case AIGoal.GoalType.KillEnemy:
                    // TODO: Move to destination
                    // TODO: Execute attack on target
                    break;

                case AIGoal.GoalType.HealSelf:
                    // TODO: Move to safety
                    // TODO: Execute self-heal
                    break;

                case AIGoal.GoalType.GainPosition:
                    // TODO: Move to strategic position
                    break;

                case AIGoal.GoalType.CollectTreasure:
                    // TODO: Move to treasure location
                    // TODO: Trigger treasure collection
                    break;

                case AIGoal.GoalType.DefensiveRetreat:
                    // TODO: Move to safe tile
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
