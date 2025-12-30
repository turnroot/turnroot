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

        #region Goal Evaluation Methods


        #region Attack Goals

        private void EvaluateAttackGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var attackGoalsPooled = PooledList<AIGoal>.Get();
            var attackGoals = attackGoalsPooled.List;

            var targets = _context.Participants.Targets;
            for (int ti = 0; ti < (targets?.Count ?? 0); ti++)
            {
                var target = targets[ti];
                var targetGridPoint = target.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    _context.mapGrid
                );

                var (destination, canAttack) = GetAccessibleTile(targetGridPoint, behavior);

                var (utility, chosenWeapon) = CalculateAttackUtility(target, behavior);
                utility += canAttack ? 2f : 0f; // Bonus for being able to attack immediately

                // If the unit is cunning or wary, check for possible counterattacks
                // reduce weight if the target can counterattack
                // extra reduction if the counterattack would do major damage

                if (behavior.MindlessCunning > 0.5f || behavior.BrashWary > 0.5f)
                {
                    bool canCounter = _context.TargetCanCounterattack(
                        _context.Unit.UnitInstance,
                        target,
                        destination
                    );
                    if (canCounter)
                    {
                        utility -= 3f * behavior.MindlessCunning;
                        // check if the counterattack would do major damage
                        int counterDamage = DamageCalculator.CalculatePotentialDamage(
                            target,
                            _context.Unit.UnitInstance,
                            chosenWeapon,
                            _context
                        );
                        int myHealth = (int)
                            _context
                                .Unit.UnitInstance.GetBoundedStat(
                                    Characters.Stats.BoundedStatType.Health
                                )
                                .Current;
                        if (counterDamage >= myHealth * 0.5f)
                        {
                            utility -= 3f * behavior.BrashWary;
                        }
                    }
                    else
                    {
                        // small bonus if they can't counterattack
                        utility += behavior.MindlessCunning;
                    }
                }

                attackGoals.Add(
                    new AIGoal
                    {
                        Type = canAttack
                            ? AIGoal.GoalType.AttackEnemy
                            : AIGoal.GoalType.GainPosition,
                        UtilityScore = utility,
                        Target = target,
                        Destination = destination,
                        ActionToTake = canAttack ? AIGoal.Action.Attack : AIGoal.Action.Move,
                        ChosenWeapon = chosenWeapon,
                    }
                );
            }

            AddTopGoals(goals, attackGoals, 3);
        }

        private void EvaluateKillEnemyGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var killGoalsPooled = PooledList<AIGoal>.Get();
            var killGoals = killGoalsPooled.List;
            // here, get the enemies and do a normal attack calculation- but then,
            // if an attack would kill an an enemy, add a massive boost
            var targets = _context.Participants.Targets;
            for (int ti = 0; ti < (targets?.Count ?? 0); ti++)
            {
                var target = targets[ti];
                var targetGridPoint = target.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    _context.mapGrid
                );

                // Check if target is in attack range

                var (utility, chosenWeapon) = CalculateAttackUtility(target, behavior);
                utility -= behavior.SelfishSelfless * 3f; // Selfless units are less kill-focused
                utility *= _reusableAttackTiles.ContainsKey(targetGridPoint) ? 5f : 3f;

                // If any weapon would kill the target, add kill bonus (CalculateAttackUtility already adds bonuses, but ensure kill detection if attacking now)
                if (
                    DamageCalculator.WouldKill(
                        _context.Unit.UnitInstance,
                        target,
                        chosenWeapon ?? _context.Unit.UnitInstance.GetEquippedWeapon(),
                        _context
                    )
                )
                {
                    float killBonus = 5f + ((1f - behavior.BloodthirstGreed) * 5f);
                    utility += killBonus;
                }

                killGoals.Add(
                    new AIGoal
                    {
                        Type = _reusableAttackTiles.ContainsKey(targetGridPoint)
                            ? AIGoal.GoalType.KillEnemy
                            : AIGoal.GoalType.GainPosition,
                        UtilityScore = utility,
                        Target = target,
                        Destination = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                            targetGridPoint.CoordinatesInt,
                            _context.mapGrid
                        ),
                        ActionToTake = _reusableAttackTiles.ContainsKey(targetGridPoint)
                            ? AIGoal.Action.Attack
                            : AIGoal.Action.Move,
                        ChosenWeapon = chosenWeapon,
                    }
                );
            }
            AddTopGoals(goals, killGoals, 3);
        }

        private void EvaluateSimpleAttackGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var attackGoalsPooled = PooledList<AIGoal>.Get();
            var attackGoals = attackGoalsPooled.List;
            // Attack the closest enemy without regard for strategy
            CharacterInstance closestEnemy = null;
            float closestDistance = float.MaxValue;
            float utility = 0f;
            var targetsForSimple = _context.Participants.Targets;
            for (int ti = 0; ti < (targetsForSimple?.Count ?? 0); ti++)
            {
                var target = targetsForSimple[ti];
                utility = 6f;

                if (target == _context.Unit.UnitInstance.LastAttackedTarget)
                {
                    utility += 3f * (1f - MindlessCunning); // Mindless units more consistent
                }
                var distance = Vector2.Distance(
                    _context.Unit.UnitInstance.MapGridPosition,
                    target.MapGridPosition
                );
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = target;
                }
            }
            if (closestEnemy != null)
            {
                var targetGridPoint = closestEnemy.UnitPositionToMapGridPoint(
                    closestEnemy.MapGridPosition,
                    _context.mapGrid
                );

                // Check if target is in attack range
                utility +=
                    (1f - behavior.BloodthirstGreed)
                    * (_reusableAttackTiles.ContainsKey(targetGridPoint) ? 5f : 3f);

                // Use CalculateAttackUtility to pick a preferred weapon
                var (_, chosenWeapon) = CalculateAttackUtility(closestEnemy, behavior);

                attackGoals.Add(
                    new AIGoal
                    {
                        Type = _reusableAttackTiles.ContainsKey(targetGridPoint)
                            ? AIGoal.GoalType.AttackEnemy
                            : AIGoal.GoalType.GainPosition,
                        UtilityScore = utility,
                        Target = closestEnemy,
                        Destination = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                            targetGridPoint.CoordinatesInt,
                            _context.mapGrid
                        ),
                        ActionToTake = _reusableAttackTiles.ContainsKey(targetGridPoint)
                            ? AIGoal.Action.Attack
                            : AIGoal.Action.Move,
                        ChosenWeapon = chosenWeapon,
                    }
                );
            }
            AddTopGoals(goals, attackGoals, 3);
        }

        #endregion

        #region Ally Goals

        private void EvaluateHealAlliesGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var healGoalsPooled = PooledList<AIGoal>.Get();
            var healGoals = healGoalsPooled.List;
            var allies = _context.Participants.Allies;
            for (int ai = 0; ai < (allies?.Count ?? 0); ai++)
            {
                var ally = allies[ai];
                var allyGridPoint = ally.UnitPositionToMapGridPoint(
                    ally.MapGridPosition,
                    _context.mapGrid
                );

                // Check if ally is in heal range
                if (_reusableHealTiles.ContainsKey(allyGridPoint))
                {
                    float utility = CalculateHealUtility(ally, behavior);

                    goals.Add(
                        new AIGoal
                        {
                            Type = AIGoal.GoalType.HealAlly,
                            UtilityScore = utility,
                            Target = ally,
                            Destination = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                                allyGridPoint.CoordinatesInt,
                                _context.mapGrid
                            ),
                            ActionToTake = AIGoal.Action.Heal,
                        }
                    );
                }
            }
            AddTopGoals(goals, healGoals, 3);
        }

        private void EvaluateProtectAllyGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            // This is a more complex one. We look at ally health and position, as well as our health,
            // and whoever attacked the ally last.  We look at this last attacker to see how dangerous
            // they are both to the ally and to ourself.

            using var protectGoalsPooled = PooledList<AIGoal>.Get();
            var protectGoals = protectGoalsPooled.List;

            using var allyLastAttackers =
                new PooledDictionary<CharacterInstance, CharacterInstance>();
            var allies = _context.Participants.Allies;
            for (int ai = 0; ai < (allies?.Count ?? 0); ai++)
            {
                var ally = allies[ai];
                // get: distance to ally, last attacker, ally health, last attacker health,
                // we also get how many squares around the ally
                // are occupied by enemies, how many are occupied by allies
                var distanceToAlly = Vector2.Distance(
                    _context.Unit.UnitInstance.MapGridPosition,
                    ally.MapGridPosition
                );
                var lastAttacker = ally.LastAttacker;
                var lastAttackerHealth = 1f;
                if (lastAttacker != null)
                {
                    lastAttackerHealth = lastAttacker.GetHealthPercentage();
                }

                var adjacency = new Adjacency(ally);
                var allySurroundingEnemies = adjacency.GetAdjacentEnemyCount(_context);
                var allySurroundingAllies = adjacency.GetAdjacentAllyCount(_context);

                // We know everything we need now.
                float utility = 5f;
                utility += 3f * allySurroundingEnemies * behavior.SelfishSelfless;
                utility += 3f * (1f - behavior.SoldierLoneWolf) * allySurroundingAllies; // Lone Wolf doesn't want a crowd
                utility += 3f * (behavior.BrashWary * (1f - lastAttackerHealth)); // Lower attacker health makes Wary happy >:)
                utility += 2f * (behavior.MindlessCunning * (3f - distanceToAlly)); // Cunning prefers enemies closer to the ally
                utility += (1f - behavior.SoldierLoneWolf) * 4F; // Soldiers are far more likely to protect allies

                utility += CalculateTerrainBonusOrPenalty(
                    ally.UnitPositionToMapGridPoint(ally.MapGridPosition, _context.mapGrid),
                    behavior
                );

                protectGoals.Add(
                    new AIGoal
                    {
                        Type = AIGoal.GoalType.ProtectAlly,
                        UtilityScore = utility,
                        Target = ally,
                        Destination = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                            ally.UnitPositionToMapGridPoint(
                                ally.MapGridPosition,
                                _context.mapGrid
                            ).CoordinatesInt,
                            _context.mapGrid
                        ),
                    }
                );
            }
            AddTopGoals(goals, protectGoals, 3);
        }

        #endregion

        #region Strategic Goals

        private void EvaluateExploreVillagesGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            float BestUtility = 0f; // This one just adds the best one instead of top 3
            var allVillageFeaturePoints = _context.mapGrid.GetAllGridPointsByFeatureType(
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

            var allTreasureFeaturePoints = _context.mapGrid.GetAllGridPointsByFeatureType(
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

            // === ASSESS CURRENT THREAT LEVEL ===
            var enemyProximity = FindClosestAndFurthestEnemies(_context.Participants.Targets);
            float healthPercent = _context.Unit.UnitInstance.GetHealthPercentage();

            // Base danger increases as:
            // - Enemies get closer (10 - distance gives 0-10 scale)
            // - Health decreases
            // - Surrounded by multiple enemies
            float baseDanger = 0f;
            baseDanger += Mathf.Max(0, 10f - enemyProximity.closestDist); // 0-10: closer = more danger
            baseDanger += (1f - healthPercent) * 2.5f; // 0-5: wounded = more danger
            baseDanger += IsSurrounded ? 2.5f : 0f; // Big spike when surrounded

            // === PERSONALITY MODIFIERS ===
            // Wary units feel threatened earlier, but only if there's actual danger
            if (enemyProximity.closestDist < 6f)
            {
                baseDanger += behavior.BrashWary * 3f; // 0-4 based on wariness
            }

            // Lone wolves more willing to retreat (soldiers hate abandoning formation)
            float personalityMultiplier = 1f + (behavior.SoldierLoneWolf * 0.3f); // 1.0-1.3x
            baseDanger *= personalityMultiplier;

            // === DEBUG DIAGNOSTICS ===
            Debug.Log(
                $"[Defensive Eval] HP: {healthPercent:P0}, Surrounded: {IsSurrounded}, "
                    + $"ClosestEnemy: {enemyProximity.closestDist:F1}, BaseDanger: {baseDanger:F1}"
            );

            // === EVALUATE EACH RETREAT TILE ===
            foreach (var tile in _reusableMoveTiles.Keys)
            {
                if (
                    !TryComputeRetreatTileUtility(
                        tile,
                        behavior,
                        baseDanger,
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
            // Enemy wants to cross the forbidden line
            foreach (var condition in crossConditions)
            {
                int targetPoint = condition.RowOrColumnIndex;
                bool isRow = condition.IsRow;

                var currentPos = _context.Unit.UnitInstance.MapGridPosition;
                int currentPoint = isRow ? currentPos.y : currentPos.x;
                float currentDist = Mathf.Abs(currentPoint - targetPoint);

                // Check if we've already crossed the objective line
                bool alreadyCrossed = currentPoint >= targetPoint;

                foreach (var tile in _reusableMoveTiles.Keys)
                {
                    int tilePoint = isRow ? tile.Row : tile.Col;
                    float tileDist = Mathf.Abs(tilePoint - targetPoint);

                    float distanceImprovement = 0f;
                    bool shouldEvaluate = false;

                    if (alreadyCrossed)
                    {
                        // Already crossed - reward pushing deeper into enemy territory
                        if (tilePoint > currentPoint)
                        {
                            distanceImprovement = (tilePoint - currentPoint) * 0.5f;
                            shouldEvaluate = true;
                        }
                    }
                    else
                    {
                        // Haven't crossed yet - reward approaching the objective line
                        if (tileDist < currentDist)
                        {
                            distanceImprovement = currentDist - tileDist;
                            shouldEvaluate = true;

                            // Extra bonus if this move crosses the line
                            if (tilePoint >= targetPoint)
                            {
                                distanceImprovement += 2f; // Breakthrough bonus!
                            }
                        }
                    }

                    if (shouldEvaluate && distanceImprovement > 0)
                    {
                        float utility = CalculatePositionUtility(
                            tile,
                            behavior,
                            distanceImprovement
                        );

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
                AddTopGoals(goals, moveGoals, 2);
            }

            // === REACH TILE CONDITIONS ===
            // Enemy wants to reach specific forbidden tiles
            foreach (var condition in reachConditions)
            {
                var targetTiles = condition.TargetTiles;
                if (targetTiles == null || targetTiles.Count == 0)
                {
                    continue;
                }

                var currentPos = _context.Unit.UnitInstance.MapGridPosition;

                // Find closest target tile from current position
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

                // Evaluate moves that get closer to ANY target tile
                foreach (var tile in _reusableMoveTiles.Keys)
                {
                    Vector2Int tilePos = new Vector2Int(tile.Col, tile.Row);
                    float tileDist = Vector2Int.Distance(tilePos, closestTarget);

                    // Only consider moves that reduce distance to closest target
                    if (tileDist < currentMinDist)
                    {
                        float distanceImprovement = currentMinDist - tileDist;
                        float utility = CalculatePositionUtility(
                            tile,
                            behavior,
                            distanceImprovement
                        );

                        // Extra bonus if this tile IS one of the target objectives
                        if (targetTiles.Contains(tilePos))
                        {
                            utility += 5f; // Big bonus for reaching the objective!
                        }

                        utility += CalculateTerrainBonusOrPenalty(tile, behavior);

                        goals.Add(
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
                AddTopGoals(goals, moveGoals, 2);
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
                        _context.mapGrid
                    ),
                    ActionToTake = AIGoal.Action.Heal,
                }
            );

            // Evaluate moving to a safer tile and then healing there
            using var healMoveGoalsPooled = PooledList<AIGoal>.Get();
            var healMoveGoals = healMoveGoalsPooled.List;

            var enemyProximity = FindClosestAndFurthestEnemies(_context.Participants.Targets);

            // Build a base danger metric similar to defensive goals
            float baseDanger = 0f;
            baseDanger += Mathf.Max(0, 10f - enemyProximity.closestDist);
            baseDanger += (1f - healthPercentage) * 2.5f;
            baseDanger += IsSurrounded ? 2.5f : 0f;
            if (enemyProximity.closestDist < 6f)
            {
                baseDanger += behavior.BrashWary * 3f;
            }
            float personalityMultiplier = 1f + (behavior.SoldierLoneWolf * 0.3f);
            baseDanger *= personalityMultiplier;

            foreach (var tile in _reusableMoveTiles.Keys)
            {
                if (
                    !TryComputeRetreatTileUtility(
                        tile,
                        behavior,
                        baseDanger,
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

        #endregion

        #endregion

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
            // If close to allies and soldier, penalty for retreating
            var adjacency = new Adjacency(_context.Unit.UnitInstance);
            var nearbyAllies = adjacency.GetAdjacentAllyCount(_context);
            if (behavior.SoldierLoneWolf <= 0.5f)
            {
                utility -= (3f * (1f - behavior.SoldierLoneWolf)) * nearbyAllies;
            }
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
            utility -= 3f - closestEnemyDist + behavior.MindlessCunning;
            utility += CalculateTerrainBonusOrPenalty(safeTile, behavior);
            return Mathf.Max(0, utility);
        }

        private float CalculateTerrainBonusOrPenalty(MapGridPoint tile, CharacterBehavior behavior)
        {
            var MovementType = _context.Unit.UnitInstance.ToAIData().MovementType;
            var terrainType = tile.GetCachedTerrainType();
            var PersonalityBonus = behavior.MindlessCunning + 1f * behavior.BrashWary;
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
            float baseDanger,
            Vector2 enemyClosest,
            float enemyClosestDist,
            out float utility
        )
        {
            utility = 0f;

            float newDistanceToClosest = Vector2.Distance(tile.Coordinates(), enemyClosest);
            float distanceImprovement = newDistanceToClosest - enemyClosestDist;
            if (distanceImprovement <= 0f)
                return false;

            utility = baseDanger + distanceImprovement * 2f;

            // Formation consideration: penalize tiles far from allies for soldiers
            if (behavior.SoldierLoneWolf < 0.5f)
            {
                float closestAllyDist = float.MaxValue;
                var allies = _context.Participants.Allies;
                for (int ai = 0; ai < (allies?.Count ?? 0); ai++)
                {
                    var ally = allies[ai];
                    if (ally == _context.Unit.UnitInstance)
                        continue;

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
