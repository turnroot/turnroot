using System.Collections.Generic;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Attack Goals
        private void EvaluateAttackGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var attackGoalsPooled = PooledList<AIGoal>.Get();
            var attackGoals = attackGoalsPooled.List;

            var targets = GetPotentialTargetsForCurrentUnit(BattleTargetIntent.Offensive);
            for (int ti = 0; ti < (targets?.Count ?? 0); ti++)
            {
                attackGoals.Add(CreateAttackGoal(targets[ti], behavior));
            }

            AddTopGoals(goals, attackGoals, 3);
        }

        private AIGoal CreateAttackGoal(
            Characters.CharacterInstance target,
            CharacterBehavior behavior
        )
        {
            var targetGridPoint = target.UnitPositionToMapGridPoint(
                target.MapGridPosition,
                _context.MapGrid
            );
            var (destination, canAttack) = GetAccessibleTile(targetGridPoint, behavior);
            var (utility, chosenWeapon) = CalculateAttackUtility(target, behavior);
            utility += canAttack ? 2f : 0f;

            var (attackerEffective, targetEffective) = GetWeaponEffectiveness(
                target,
                requireTargetEquippedWeapon: true
            );
            utility = ApplyEffectivenessModifiers(
                utility,
                behavior,
                attackerEffective,
                targetEffective
            );
            utility = ApplyCounterattackModifiers(
                utility,
                target,
                destination,
                chosenWeapon,
                behavior
            );

            return new AIGoal
            {
                Type = canAttack ? AIGoal.GoalType.AttackEnemy : AIGoal.GoalType.GainPosition,
                UtilityScore = utility,
                Target = target,
                Destination = destination,
                ActionToTake = canAttack ? AIGoal.Action.Attack : AIGoal.Action.Move,
                ChosenWeapon = chosenWeapon,
            };
        }

        private void EvaluateKillEnemyGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var killGoalsPooled = PooledList<AIGoal>.Get();
            var killGoals = killGoalsPooled.List;
            var targets = GetPotentialTargetsForCurrentUnit(BattleTargetIntent.Offensive);
            for (int ti = 0; ti < (targets?.Count ?? 0); ti++)
            {
                killGoals.Add(CreateKillEnemyGoal(targets[ti], behavior));
            }
            AddTopGoals(goals, killGoals, 3);
        }

        private AIGoal CreateKillEnemyGoal(
            Characters.CharacterInstance target,
            CharacterBehavior behavior
        )
        {
            var targetGridPoint = target.UnitPositionToMapGridPoint(
                target.MapGridPosition,
                _context.MapGrid
            );
            var (utility, chosenWeapon) = CalculateAttackUtility(target, behavior);
            utility -= behavior.SelfishSelfless * 3f;
            utility *= IsAttackable(targetGridPoint) ? 5f : 3f;

            var (attackerEffective, targetEffective) = GetWeaponEffectiveness(
                target,
                requireTargetEquippedWeapon: false
            );
            utility = ApplyEffectivenessModifiers(
                utility,
                behavior,
                attackerEffective,
                targetEffective
            );

            if (
                DamageCalculator.WouldKill(
                    _context.Unit.UnitInstance,
                    target,
                    chosenWeapon ?? _context.Unit.UnitInstance.GetEquippedWeapon(),
                    _context
                )
            )
            {
                utility += 5f + ((1f - behavior.BloodthirstGreed) * 5f);
            }

            bool canAttack = IsAttackable(targetGridPoint);
            return new AIGoal
            {
                Type = canAttack ? AIGoal.GoalType.KillEnemy : AIGoal.GoalType.GainPosition,
                UtilityScore = utility,
                Target = target,
                Destination = DestinationFromTargetGridPoint(targetGridPoint),
                ActionToTake = canAttack ? AIGoal.Action.Attack : AIGoal.Action.Move,
                ChosenWeapon = chosenWeapon,
            };
        }

        private (bool attackerEffective, bool targetEffective) GetWeaponEffectiveness(
            Characters.CharacterInstance target,
            bool requireTargetEquippedWeapon
        )
        {
            var attackerInfo = _context.GetCachedWeaponInfo(_context.Unit.UnitInstance);
            var targetInfo = _context.GetCachedWeaponInfo(target);
            bool attackerEffective = false;
            bool targetEffective = false;

            var targetSpecies = target.CharacterTemplate?.Species;
            if (
                attackerInfo != null
                && targetSpecies != null
                && attackerInfo.HasAnyWeaponEffectiveAgainstSpecies(targetSpecies)
            )
            {
                attackerEffective = true;
            }

            var targetWeaponType = requireTargetEquippedWeapon
                ? target.InventoryInstance?.GetEquippedWeaponIndex() >= 0
                    ? target.GetEquippedWeapon()?.Template?.WeaponType
                    : null
                : target.GetEquippedWeapon()?.Template?.WeaponType;
            if (
                attackerInfo != null
                && targetWeaponType != null
                && attackerInfo.HasAnyWeaponEffectiveAgainstWeaponType(targetWeaponType)
            )
            {
                attackerEffective = true;
            }

            var attacker = _context.Unit.UnitInstance;
            var attackerSpecies = attacker.CharacterTemplate?.Species;
            if (
                targetInfo != null
                && attackerSpecies != null
                && targetInfo.HasAnyWeaponEffectiveAgainstSpecies(attackerSpecies)
            )
            {
                targetEffective = true;
            }

            var attackerWeaponType = attacker.GetEquippedWeapon()?.Template?.WeaponType;
            if (
                targetInfo != null
                && attackerWeaponType != null
                && targetInfo.HasAnyWeaponEffectiveAgainstWeaponType(attackerWeaponType)
            )
            {
                targetEffective = true;
            }

            return (attackerEffective, targetEffective);
        }

        private static float ApplyEffectivenessModifiers(
            float utility,
            CharacterBehavior behavior,
            bool attackerEffective,
            bool targetEffective
        )
        {
            if (targetEffective)
            {
                utility -= (3f * behavior.BrashWary) + (2f * (1f - behavior.MindlessCunning));
            }

            if (attackerEffective)
            {
                utility += (2f * behavior.MindlessCunning) + (3f * behavior.BloodthirstGreed);
            }

            return utility;
        }

        private float ApplyCounterattackModifiers(
            float utility,
            Characters.CharacterInstance target,
            MapGridPoint destination,
            ObjectItemInstance chosenWeapon,
            CharacterBehavior behavior
        )
        {
            if (behavior.MindlessCunning <= 0.5f && behavior.BrashWary <= 0.5f)
            {
                return utility;
            }

            var attacker = _context.Unit.UnitInstance;
            if (!_context.TargetCanCounterattack(attacker, target, destination))
            {
                return utility + behavior.MindlessCunning;
            }

            utility -= 3f * behavior.MindlessCunning;
            int counterDamage = DamageCalculator.CalculatePotentialDamage(
                target,
                attacker,
                chosenWeapon,
                _context
            );
            int attackerHealth = (int)
                attacker.GetBoundedStat(Characters.Stats.BoundedStatType.Health).Current;
            if (counterDamage >= attackerHealth * 0.5f)
            {
                utility -= 3f * behavior.BrashWary;
            }

            return utility;
        }

        private void EvaluateSimpleAttackGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var attackGoalsPooled = PooledList<AIGoal>.Get();
            var attackGoals = attackGoalsPooled.List;
            // Attack the closest enemy without regard for strategy
            float utility = 0f;
            var targetsForSimple = GetPotentialTargetsForCurrentUnit(
                BattleTargetIntent.Offensive
            );
            var closestEnemy = PathfinderHelpers.FindClosestUnit(
                _context.Unit.UnitInstance.MapGridPosition,
                targetsForSimple
            );
            if (closestEnemy != null)
            {
                utility = 6f;
                if (closestEnemy == _context.Unit.UnitInstance.LastAttackedTarget)
                {
                    utility += 3f * (1f - MindlessCunning); // Mindless units more consistent
                }
                var targetGridPoint = closestEnemy.UnitPositionToMapGridPoint(
                    closestEnemy.MapGridPosition,
                    _context.MapGrid
                );

                // Check if target is in attack range
                utility +=
                    (1f - behavior.BloodthirstGreed) * (IsAttackable(targetGridPoint) ? 5f : 3f);

                // Use CalculateAttackUtility to pick a preferred weapon
                var (_, chosenWeapon) = CalculateAttackUtility(closestEnemy, behavior);

                // Consider weapon-effectiveness cache for quick heuristics (bonus/penalty)
                var attackerInfo = _context.GetCachedWeaponInfo(_context.Unit.UnitInstance);
                var targetInfo = _context.GetCachedWeaponInfo(closestEnemy);

                if (attackerInfo != null && targetInfo != null)
                {
                    var targetSpecies = closestEnemy.CharacterTemplate?.Species;
                    if (
                        targetSpecies != null
                        && attackerInfo.HasAnyWeaponEffectiveAgainstSpecies(targetSpecies)
                    )
                    {
                        utility +=
                            (2f * behavior.MindlessCunning) + (3f * behavior.BloodthirstGreed);
                    }

                    var mySpecies = _context.Unit.UnitInstance.CharacterTemplate?.Species;
                    if (
                        mySpecies != null
                        && targetInfo.HasAnyWeaponEffectiveAgainstSpecies(mySpecies)
                    )
                    {
                        utility -=
                            (3f * behavior.BrashWary) + (2f * (1f - behavior.MindlessCunning));
                    }
                }

                attackGoals.Add(
                    new AIGoal
                    {
                        Type = IsAttackable(targetGridPoint)
                            ? AIGoal.GoalType.AttackEnemy
                            : AIGoal.GoalType.GainPosition,
                        UtilityScore = utility,
                        Target = closestEnemy,
                        Destination = DestinationFromTargetGridPoint(targetGridPoint),
                        ActionToTake = IsAttackable(targetGridPoint)
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
