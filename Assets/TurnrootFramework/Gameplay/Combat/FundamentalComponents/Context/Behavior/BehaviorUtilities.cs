using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Gameplay.Combat.FundamentalComponents.Conditions.Specific;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using static MapGridPointFeature;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        /// <summary>
        /// Helper to add top N goals from a category to the main goals list
        /// </summary>
        private void AddTopGoals(List<AIGoal> goals, List<AIGoal> categoryGoals, int count = 3)
        {
            if (categoryGoals.Count == 0)
            {
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"Adding top {count} goals from category with {categoryGoals.Count} goals.");
#endif

            // Sort by utility descending
            categoryGoals.Sort((a, b) => b.UtilityScore.CompareTo(a.UtilityScore));

            // Add top N (or fewer if less than N exist)
            int toAdd = Mathf.Min(count, categoryGoals.Count);
            for (int i = 0; i < toAdd; i++)
            {
                goals.Add(categoryGoals[i]);
            }
        }

        #region Utility Calculation Methods
        private float CalculateHealUtility(CharacterInstance ally, CharacterBehavior behavior)
        {
            float healthPercentage = ally.GetHealthPercentage();

            // Base utility scales with how wounded the ally is
            float utility = 12f + ((1f - healthPercentage) * 15f);

            // Smart units prioritize critical allies
            if (healthPercentage < 0.3f && behavior.MindlessCunning > 0.5f)
            {
                utility += 5f;
            }

            // Selfless units heal more readily
            utility += behavior.SelfishSelfless * 5f; // 0-5

            // Formation units prefer healing nearby allies
            float distance = Vector2.Distance(
                _context.Unit.UnitInstance.MapGridPosition,
                ally.MapGridPosition
            );

            float distancePenalty = Mathf.Max(0, distance - 2f);

            if (behavior.SoldierLoneWolf < 0.5f) // Not a lone wolf
            {
                if (healthPercentage <= 0.3f)
                {
                    distancePenalty *= 0.5f; // Critical patients worth traveling for
                }
                else if (healthPercentage < 0.7f)
                {
                    distancePenalty *= 0.75f; // Less critical patients
                }
                utility -= distancePenalty;
            }
            else
            {
                utility -= distancePenalty * 2f; // Lone wolves reluctant to heal anyone
            }

            return Mathf.Max(0, utility);
        }

        private float CalculatePositionUtility(
            MapGridPoint tile,
            CharacterBehavior behavior,
            float distanceImprovement
        )
        {
            float utility = 12f; // Base utility

            // Smart units recognize strategic value of objectives
            utility += behavior.MindlessCunning * 3f; // 0-2.4 for cunning

            // Soldiers follow orders/objectives better (team players)
            utility += (1f - behavior.SoldierLoneWolf) * 2f; // 0-1.6 for soldiers

            // Selfless units less objective-focused (prioritize protecting allies)
            utility -= behavior.SelfishSelfless * 2f; // -0 to -1.6 penalty

            // Scale by how much closer this move gets us to objective
            utility += distanceImprovement * 3f; // Reward progress toward goal

            // Check if tile is dangerous (near player units = enemies to this AI)
            float closestPlayerDist = float.MaxValue;
            var allies = _context.Participants.Allies;
            for (int ai = 0; ai < (allies?.Count ?? 0); ai++) // _context.Participants.Allies are player units from enemy perspective
            {
                var ally = allies[ai];
                float dist = Vector2.Distance(tile.Coordinates(), ally.MapGridPosition);
                if (dist < closestPlayerDist)
                {
                    closestPlayerDist = dist;
                }
            }

            // Minor penalty for very dangerous tiles (but not as severe as defensive retreat)
            if (closestPlayerDist < 2f)
            {
                // Wary units more cautious about danger
                float dangerPenalty = (2f - closestPlayerDist) * behavior.BrashWary * 1.5f;
                utility -= dangerPenalty; // Max penalty: -3 for wary unit on adjacent tile
            }

            // Health factor: wounded units less aggressive about objectives
            float healthPercent = _context.Unit.UnitInstance.GetHealthPercentage();
            if (healthPercent < 0.5f)
            {
                utility *= (0.5f + healthPercent); // 50% HP = 100% utility, 0% HP = 50% utility
            }

            utility += CalculateTerrainBonusOrPenalty(tile, behavior);

            return Mathf.Max(0f, utility);
        }

        private float CalculateFeatureUtility(
            MapGridPoint featureLocation,
            CharacterBehavior behavior
        )
        {
            float utility = 12f;

            // High greed (high BloodthirstGreed) increases desire for treasure
            utility += (behavior.BloodthirstGreed) * 12f; // 0-9 based on greed

            // Cunning units assess risk vs reward
            var distanceToFeature = Vector2.Distance(
                _context.Unit.UnitInstance.MapGridPosition,
                featureLocation.Coordinates()
            );

            // Close features are more attractive
            float distancePenalty = Mathf.Max(0, distanceToFeature - 2f) * 1.5f;
            utility -= distancePenalty;

            // Check if enemies nearby make it dangerous
            float closestEnemyDist = float.MaxValue;
            foreach (var enemy in _context.Participants.Targets)
            {
                float dist = Vector2.Distance(featureLocation.Coordinates(), enemy.MapGridPosition);
                if (dist < closestEnemyDist)
                {
                    closestEnemyDist = dist;
                }
            }

            // Dangerous if enemies within 3 tiles
            if (closestEnemyDist < 3f)
            {
                // Wary units avoid dangerous treasure
                float dangerPenalty = (3f - closestEnemyDist) * behavior.BrashWary * 3f;

                // Smart units are even more cautious
                if (behavior.MindlessCunning > 0.6f)
                {
                    dangerPenalty *= 1.3f; // 30% more cautious
                }

                utility -= dangerPenalty;
            }
            utility += CalculateTerrainBonusOrPenalty(featureLocation, behavior);

            return Mathf.Max(0, utility);
        }

        private float CalculateDefensiveUtility(MapGridPoint safeTile, CharacterBehavior behavior)
        {
            float utility = 1f;
            utility += (1f - _context.Unit.UnitInstance.GetHealthPercentage()) * 10f;
            utility += behavior.SelfishSelfless * 3f; // Selfless units are less likely to retreat
            utility -= (1f - behavior.MindlessCunning) * 3f; // mindless units don't retreat as much
            // find the closest enemy to the safe tile
            float closestEnemyDist = float.MaxValue;
            foreach (var enemy in _context.Participants.Targets)
            {
                float dist = Vector2.Distance(safeTile.Coordinates(), enemy.MapGridPosition);
                if (dist < closestEnemyDist)
                {
                    closestEnemyDist = dist;
                }
            }

            // Penalty for proximity to enemies (closer is worse)
            float enemyPenalty = Mathf.Max(0f, 3f - closestEnemyDist);
            utility -= enemyPenalty;
            // More cunning units are slightly more sensitive to danger
            utility -= behavior.MindlessCunning * 0.5f;

            // Note: terrain is applied by retreat scoring helpers to avoid double-counting
            return Mathf.Max(0, utility);
        }

        private float CalculateTerrainBonusOrPenalty(MapGridPoint tile, CharacterBehavior behavior)
        {
            var MovementType = _context.Unit.UnitInstance.ToAIData().MovementType;
            var terrainType = tile.GetCachedTerrainType();
            var PersonalityBonus = behavior.MindlessCunning + (1f * behavior.BrashWary);
            var TerrainBonus = 0f;
            if (MovementType == MovementType.Infantry)
            {
                TerrainBonus += terrainType.AvoidBonusWalk * 0.1f;
                TerrainBonus += terrainType.DefenseBonusWalk * 0.1f;
                TerrainBonus += terrainType.HealthChangePerTurnWalk * 0.3f;
            }
            else if (MovementType == MovementType.Riding)
            {
                TerrainBonus += terrainType.AvoidBonusRiding * 0.1f;
                TerrainBonus += terrainType.DefenseBonusRiding * 0.1f;
                TerrainBonus += terrainType.HealthChangePerTurnRiding * 0.3f;
            }
            else if (MovementType == MovementType.Flying)
            {
                TerrainBonus += terrainType.AvoidBonusFlying * 0.1f;
                TerrainBonus += terrainType.DefenseBonusFlying * 0.1f;
                TerrainBonus += terrainType.HealthChangePerTurnFlying * 0.3f;
            }
            else if (MovementType == MovementType.Armored)
            {
                TerrainBonus += terrainType.AvoidBonusArmor * 0.1f;
                TerrainBonus += terrainType.DefenseBonusArmor * 0.1f;
                TerrainBonus += terrainType.HealthChangePerTurnArmor * 0.3f;
            }

            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            TerrainBonus *= settings.GetTerrainBonusMultiplier();
            TerrainBonus += PersonalityBonus;
            return TerrainBonus;
        }

        /// <summary>
        /// Compute a utility for moving to a retreat tile. Returns false if the tile does not
        /// increase distance from the nearest enemy (i.e., not a retreat candidate).
        /// </summary>
        private bool TryComputeRetreatTileUtility(
            MapGridPoint tile,
            CharacterBehavior behavior,
            Vector2 enemyClosest,
            float enemyClosestDist,
            out float utility
        )
        {
            utility = 0f;

            float newDistanceToClosest = Vector2.Distance(tile.Coordinates(), enemyClosest);
            float distanceImprovement = newDistanceToClosest - enemyClosestDist;
            if (distanceImprovement <= 0f)
            {
                return false;
            }

            // Use centralized defensive utility as base
            float baseUtility = CalculateDefensiveUtility(tile, behavior);
            utility = baseUtility + distanceImprovement * 2f;

            // Formation consideration: penalize tiles far from allies for soldiers
            if (behavior.SoldierLoneWolf < 0.5f)
            {
                float closestAllyDist = float.MaxValue;
                var allies = _context.Participants.Allies;
                for (int ai = 0; ai < (allies?.Count ?? 0); ai++)
                {
                    var ally = allies[ai];
                    if (ally == _context.Unit.UnitInstance)
                    {
                        continue;
                    }

                    float dist = Vector2.Distance(tile.Coordinates(), ally.MapGridPosition);
                    if (dist < closestAllyDist)
                    {
                        closestAllyDist = dist;
                    }
                }

                if (closestAllyDist > 3f)
                {
                    utility -= (closestAllyDist - 3f) * (1f - behavior.SoldierLoneWolf) * 2f;
                }
            }

            // Apply terrain consideration
            utility += CalculateTerrainBonusOrPenalty(tile, behavior);

            return true;
        }

        private (MapGridPoint tile, bool canAttack) GetAccessibleTile(
            MapGridPoint targetOccupiedPoint,
            CharacterBehavior modifiedBehaviorSettings
        )
        {
            var attackRanges = _context.Unit.UnitInstance.ToAIData().AttackRange;
            var minAttackRange = attackRanges.min;
            var maxAttackRange = attackRanges.max;

            using var potentialDestinations = PooledList<MapGridPoint>.Get();

            // Find all move tiles that would put us in attack range
            foreach (var moveTile in _reusableMoveTiles.Keys)
            {
                int distance =
                    Mathf.Abs(moveTile.Row - targetOccupiedPoint.Row)
                    + Mathf.Abs(moveTile.Col - targetOccupiedPoint.Col);

                if (distance >= minAttackRange && distance <= maxAttackRange)
                {
                    potentialDestinations.List.Add(moveTile);
                }
            }

            // If we found valid attack positions, score and return the best
            if (potentialDestinations.List.Count > 0)
            {
                MapGridPoint bestTile = ScoreTiles(
                    potentialDestinations.List,
                    targetOccupiedPoint,
                    modifiedBehaviorSettings
                );
                return (bestTile, canAttack: true);
            }

            // No attack positions available - return closest tile and signal we can't attack
            MapGridPoint closestTile = GetClosestTileInMoveTiles(targetOccupiedPoint);
            return (closestTile, canAttack: false);
        }

        private MapGridPoint ScoreTiles(
            List<MapGridPoint> tiles,
            MapGridPoint target,
            CharacterBehavior behavior
        )
        {
            var attackRanges = _context.Unit.UnitInstance.ToAIData().AttackRange;
            MapGridPoint bestTile = null;
            float bestScore = float.MinValue;

            foreach (var tile in tiles)
            {
                float score = 0f;

                // Terrain (smart units care more)
                score += CalculateTerrainBonusOrPenalty(tile, behavior) * behavior.MindlessCunning;

                // Distance preference
                int distanceToTarget =
                    Mathf.Abs(tile.Row - target.Row) + Mathf.Abs(tile.Col - target.Col);
                float distanceFactor =
                    behavior.BrashWary > 0.5f
                        ? distanceToTarget // Wary: further is better
                        : (attackRanges.max - distanceToTarget); // Brash: closer is better

                score += distanceFactor * 2f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }

            return bestTile;
        }

        // Helper: Find closest reachable tile when no attack positions available
        private MapGridPoint GetClosestTileInMoveTiles(MapGridPoint target)
        {
            MapGridPoint closest = null;
            int closestDist = int.MaxValue;

            foreach (var moveTile in _reusableMoveTiles.Keys)
            {
                int dist =
                    Mathf.Abs(moveTile.Row - target.Row) + Mathf.Abs(moveTile.Col - target.Col);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = moveTile;
                }
            }

            return closest;
        }
    }
        #endregion
}
