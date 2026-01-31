using System.Collections.Generic;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Gameplay.Maps.MapGridPointFeature;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        private void EvaluateExploreVillagesGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            float BestUtility = 0f; // This one just adds the best one instead of top 3
            var allVillageFeaturePoints = _context.MapGrid.GetAllGridPointsByFeatureType(
                FeatureType.Village
            );
            if (allVillageFeaturePoints == null)
            {
                return;
            }
            foreach (var villagePoint in allVillageFeaturePoints)
            {
                if (_reusableMoveTiles.ContainsKey(villagePoint))
                {
                    float utility = CalculateFeatureUtility(villagePoint, behavior);

                    utility += CalculateTerrainBonusOrPenalty(villagePoint, behavior);

                    if (utility > BestUtility)
                    {
                        BestUtility = utility;
                        goals.Add(
                            new AIGoal
                            {
                                Type = AIGoal.GoalType.ExploreVillages,
                                UtilityScore = utility,
                                Target = null,
                                Destination = villagePoint,
                                ActionToTake = AIGoal.Action.Feature,
                            }
                        );
                    }
                }
            }
        }

        private void EvaluateTreasureGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var treasureGoalsPooled = PooledList<AIGoal>.Get();
            var treasureGoals = treasureGoalsPooled.List;

            var allTreasureFeaturePoints = _context.MapGrid.GetAllGridPointsByFeatureType(
                FeatureType.Treasure
            );
            if (allTreasureFeaturePoints == null)
            {
                return;
            }
            foreach (var treasurePoint in allTreasureFeaturePoints)
            {
                if (_reusableMoveTiles.ContainsKey(treasurePoint))
                {
                    float utility = CalculateFeatureUtility(treasurePoint, behavior);

                    utility += CalculateTerrainBonusOrPenalty(treasurePoint, behavior);

                    treasureGoals.Add(
                        new AIGoal
                        {
                            Type = AIGoal.GoalType.CollectTreasure,
                            UtilityScore = utility,
                            Target = null,
                            Destination = treasurePoint,
                            ActionToTake = AIGoal.Action.Feature,
                        }
                    );
                }
            }
            AddTopGoals(goals, treasureGoals, behavior.BloodthirstGreed >= 0.5f ? 3 : 1);
        }

        private void EvaluateDefensiveGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var defensiveGoalsPooled = PooledList<AIGoal>.Get();
            var defensiveGoals = defensiveGoalsPooled.List;
            var enemyProximity = FindClosestAndFurthestEnemies(_context.Participants.Targets);
            foreach (var tile in _reusableMoveTiles.Keys)
            {
                if (
                    !TryComputeRetreatTileUtility(
                        tile,
                        behavior,
                        enemyProximity.closest,
                        enemyProximity.closestDist,
                        out float tileUtility
                    )
                )
                {
                    continue;
                }

                defensiveGoals.Add(
                    new AIGoal
                    {
                        Type = AIGoal.GoalType.DefensiveRetreat,
                        UtilityScore = tileUtility,
                        Target = null,
                        Destination = tile,
                        ActionToTake = AIGoal.Action.Move,
                    }
                );
            }

            // Sort and add top retreats (more for wary units, fewer for brash)
            int retreatsToConsider = behavior.BrashWary >= 0.5f ? 3 : 2;
            AddTopGoals(goals, defensiveGoals, retreatsToConsider);
        }

        private void EvaluatePositionGoals(
            List<AIGoal> goals,
            CharacterBehavior behavior,
            List<NoEnemiesCrossRowOrColumnBattleCondition> crossConditions,
            List<NoEnemyReachesTilesBattleCondition> reachConditions
        )
        {
            using var moveGoalsPooled = PooledList<AIGoal>.Get();
            var moveGoals = moveGoalsPooled.List;

            // === ROW/COLUMN CONDITIONS ===
            // Evaluate each cross condition into moveGoals and add the top candidates
            foreach (var condition in crossConditions)
            {
                moveGoals.Clear();
                EvaluateSingleCrossCondition(moveGoals, behavior, condition);
                AddTopGoals(goals, moveGoals, 2);
            }

            // === REACH TILE CONDITIONS ===
            // Evaluate moves that get closer to specific forbidden tiles
            foreach (var condition in reachConditions)
            {
                moveGoals.Clear();
                EvaluateSingleReachCondition(moveGoals, behavior, condition);
                AddTopGoals(goals, moveGoals, 2);
            }
        }

        private void EvaluateSingleCrossCondition(
            List<AIGoal> moveGoals,
            CharacterBehavior behavior,
            NoEnemiesCrossRowOrColumnBattleCondition condition
        )
        {
            int targetPoint = condition.RowOrColumnIndex;
            bool isRow = condition.IsRow;

            var currentPos = _context.Unit.UnitInstance.MapGridPosition;
            int currentPoint = isRow ? currentPos.y : currentPos.x;
            float currentDist = Mathf.Abs(currentPoint - targetPoint);

            bool alreadyCrossed = currentPoint >= targetPoint;

            foreach (var tile in _reusableMoveTiles.Keys)
            {
                int tilePoint = isRow ? tile.Row : tile.Col;
                float tileDist = Mathf.Abs(tilePoint - targetPoint);

                float distanceImprovement = 0f;
                bool shouldEvaluate = false;

                if (alreadyCrossed)
                {
                    if (tilePoint > currentPoint)
                    {
                        distanceImprovement = (tilePoint - currentPoint) * 0.5f;
                        shouldEvaluate = true;
                    }
                }
                else
                {
                    if (tileDist < currentDist)
                    {
                        distanceImprovement = currentDist - tileDist;
                        shouldEvaluate = true;

                        if (tilePoint >= targetPoint)
                        {
                            distanceImprovement += 2f; // Breakthrough bonus
                        }
                    }
                }

                if (!shouldEvaluate || distanceImprovement <= 0f)
                {
                    continue;
                }

                float utility = CalculatePositionUtility(tile, behavior, distanceImprovement);
                utility += CalculateTerrainBonusOrPenalty(tile, behavior);

                moveGoals.Add(
                    new AIGoal
                    {
                        Type = AIGoal.GoalType.GainPosition,
                        UtilityScore = utility,
                        Target = null,
                        Destination = tile,
                        ActionToTake = AIGoal.Action.Move,
                    }
                );
            }
        }

        private void EvaluateSingleReachCondition(
            List<AIGoal> moveGoals,
            CharacterBehavior behavior,
            NoEnemyReachesTilesBattleCondition condition
        )
        {
            var targetTiles = condition.TargetTiles;
            if (targetTiles == null || targetTiles.Count == 0)
            {
                return;
            }

            var currentPos = _context.Unit.UnitInstance.MapGridPosition;

            float currentMinDist = float.MaxValue;
            Vector2Int closestTarget = Vector2Int.zero;

            foreach (var target in targetTiles)
            {
                float dist = Vector2Int.Distance(currentPos, target);
                if (dist < currentMinDist)
                {
                    currentMinDist = dist;
                    closestTarget = target;
                }
            }

            foreach (var tile in _reusableMoveTiles.Keys)
            {
                Vector2Int tilePos = new Vector2Int(tile.Col, tile.Row);
                float tileDist = Vector2Int.Distance(tilePos, closestTarget);

                if (tileDist >= currentMinDist)
                {
                    continue;
                }

                float distanceImprovement = currentMinDist - tileDist;
                float utility = CalculatePositionUtility(tile, behavior, distanceImprovement);

                if (targetTiles.Contains(tilePos))
                {
                    utility += 5f; // Big bonus for reaching the objective
                }

                utility += CalculateTerrainBonusOrPenalty(tile, behavior);

                moveGoals.Add(
                    new AIGoal
                    {
                        Type = AIGoal.GoalType.GainPosition,
                        UtilityScore = utility,
                        Target = null,
                        Destination = tile,
                        ActionToTake = AIGoal.Action.Move,
                    }
                );
            }
        }

        private void EvaluateHealSelfGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            float healthPercentage = _context.Unit.UnitInstance.GetHealthPercentage();

            // Only evaluate if wounded
            if (!IsWounded)
            {
                return;
            }

            // Baseline desire to heal now (stay in place)
            float stayHealUtility = 14f;
            stayHealUtility += (1f - healthPercentage) * (1f - behavior.SelfishSelfless) * 10f;
            stayHealUtility += behavior.SoldierLoneWolf * 3f;

            goals.Add(
                new AIGoal
                {
                    Type = AIGoal.GoalType.HealSelf,
                    UtilityScore = stayHealUtility,
                    Target = _context.Unit.UnitInstance,
                    Destination = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                        _context.Unit.UnitInstance.MapGridPosition,
                        _context.MapGrid
                    ),
                    ActionToTake = AIGoal.Action.Heal,
                }
            );

            // Evaluate moving to a safer tile and then healing there
            using var healMoveGoalsPooled = PooledList<AIGoal>.Get();
            var healMoveGoals = healMoveGoalsPooled.List;

            var enemyProximity = FindClosestAndFurthestEnemies(_context.Participants.Targets);

            foreach (var tile in _reusableMoveTiles.Keys)
            {
                if (
                    !TryComputeRetreatTileUtility(
                        tile,
                        behavior,
                        enemyProximity.closest,
                        enemyProximity.closestDist,
                        out float tileUtility
                    )
                )
                {
                    continue;
                }

                // Combine with baseline heal desire so move+heal competes with heal-now
                tileUtility += stayHealUtility;

                healMoveGoals.Add(
                    new AIGoal
                    {
                        Type = AIGoal.GoalType.HealSelf,
                        UtilityScore = tileUtility,
                        Target = _context.Unit.UnitInstance,
                        Destination = tile,
                        ActionToTake = AIGoal.Action.Move,
                    }
                );
            }

            // Add the top move+heal goals alongside the stay-and-heal option
            AddTopGoals(goals, healMoveGoals, 2);
        }
    }
}
