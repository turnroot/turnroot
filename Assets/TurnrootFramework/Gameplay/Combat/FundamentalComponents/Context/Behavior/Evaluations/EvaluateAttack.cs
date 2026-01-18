using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
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
    }
}
