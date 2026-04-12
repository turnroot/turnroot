using System.Collections.Generic;
using Turnroot.Characters.Components.Behavior;
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

            var targets = _context.Participants.Targets;
            for (int ti = 0; ti < (targets?.Count ?? 0); ti++)
            {
                var target = targets[ti];
                var targetGridPoint = target.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    _context.MapGrid
                );

                var (destination, canAttack) = GetAccessibleTile(targetGridPoint, behavior);

                var (utility, chosenWeapon) = CalculateAttackUtility(target, behavior);
                utility += canAttack ? 2f : 0f; // Bonus for being able to attack immediately

                // Check cached weapon summaries for attacker & target (precomputed during prebattle)
                var attackerInfo = _context.GetCachedWeaponInfo(_context.Unit.UnitInstance);
                var targetInfo = _context.GetCachedWeaponInfo(target);

                bool attackerHasEffectOverTarget = false;
                bool targetHasEffectOverAttacker = false;

                if (attackerInfo != null)
                {
                    // If attacker has ANY weapon effective against the target's species or weapon type
                    var targetSpecies = target.CharacterTemplate?.Species;
                    if (
                        targetSpecies != null
                        && attackerInfo.HasAnyWeaponEffectiveAgainstSpecies(targetSpecies)
                    )
                    {
                        attackerHasEffectOverTarget = true;
                    }

                    var targetEquippedWeaponType =
                        target.InventoryInstance?.GetEquippedWeaponIndex() >= 0
                            ? target.GetEquippedWeapon()?.Template?.WeaponType
                            : null;
                    if (
                        targetEquippedWeaponType != null
                        && attackerInfo.HasAnyWeaponEffectiveAgainstWeaponType(
                            targetEquippedWeaponType
                        )
                    )
                    {
                        attackerHasEffectOverTarget = true;
                    }
                }

                if (targetInfo != null)
                {
                    var mySpecies = _context.Unit.UnitInstance.CharacterTemplate?.Species;
                    if (
                        mySpecies != null
                        && targetInfo.HasAnyWeaponEffectiveAgainstSpecies(mySpecies)
                    )
                    {
                        targetHasEffectOverAttacker = true;
                    }

                    var myEquippedWeaponType = _context
                        .Unit.UnitInstance.GetEquippedWeapon()
                        ?.Template?.WeaponType;
                    if (
                        myEquippedWeaponType != null
                        && targetInfo.HasAnyWeaponEffectiveAgainstWeaponType(myEquippedWeaponType)
                    )
                    {
                        targetHasEffectOverAttacker = true;
                    }
                }

                // Apply modifiers: cautious/strategic units avoid attacking targets that threaten them
                if (targetHasEffectOverAttacker)
                {
                    // Stronger penalty for more cautious units; cunning units reduce avoidance
                    float penalty = 3f * behavior.BrashWary + 2f * (1f - behavior.MindlessCunning);
                    utility -= penalty;
                }

                // Cunning or bloodthirsty units gain a boost when attacking targets they are effective against
                if (attackerHasEffectOverTarget)
                {
                    float bonus = 2f * behavior.MindlessCunning + 3f * behavior.BloodthirstGreed;
                    utility += bonus;
                }

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
                    _context.MapGrid
                );

                // Check if target is in attack range

                var (utility, chosenWeapon) = CalculateAttackUtility(target, behavior);
                utility -= behavior.SelfishSelfless * 3f; // Selfless units are less kill-focused
                utility *= IsAttackable(targetGridPoint) ? 5f : 3f;

                // Weapon-effectiveness adjustments (use cached summaries)
                var attackerInfo = _context.GetCachedWeaponInfo(_context.Unit.UnitInstance);
                var targetInfo = _context.GetCachedWeaponInfo(target);

                bool attackerHasEffectOverTarget = false;
                bool targetHasEffectOverAttacker = false;

                if (attackerInfo != null)
                {
                    var targetSpecies = target.CharacterTemplate?.Species;
                    if (
                        targetSpecies != null
                        && attackerInfo.HasAnyWeaponEffectiveAgainstSpecies(targetSpecies)
                    )
                    {
                        attackerHasEffectOverTarget = true;
                    }

                    var targetEquippedWeaponType = target.GetEquippedWeapon()?.Template?.WeaponType;
                    if (
                        targetEquippedWeaponType != null
                        && attackerInfo.HasAnyWeaponEffectiveAgainstWeaponType(
                            targetEquippedWeaponType
                        )
                    )
                    {
                        attackerHasEffectOverTarget = true;
                    }
                }

                if (targetInfo != null)
                {
                    var mySpecies = _context.Unit.UnitInstance.CharacterTemplate?.Species;
                    if (
                        mySpecies != null
                        && targetInfo.HasAnyWeaponEffectiveAgainstSpecies(mySpecies)
                    )
                    {
                        targetHasEffectOverAttacker = true;
                    }

                    var myEquippedWeaponType = _context
                        .Unit.UnitInstance.GetEquippedWeapon()
                        ?.Template?.WeaponType;
                    if (
                        myEquippedWeaponType != null
                        && targetInfo.HasAnyWeaponEffectiveAgainstWeaponType(myEquippedWeaponType)
                    )
                    {
                        targetHasEffectOverAttacker = true;
                    }
                }

                if (targetHasEffectOverAttacker)
                {
                    float penalty = 3f * behavior.BrashWary + 2f * (1f - behavior.MindlessCunning);
                    utility -= penalty;
                }

                if (attackerHasEffectOverTarget)
                {
                    float bonus = 2f * behavior.MindlessCunning + 3f * behavior.BloodthirstGreed;
                    utility += bonus;
                }

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
                        Type = IsAttackable(targetGridPoint)
                            ? AIGoal.GoalType.KillEnemy
                            : AIGoal.GoalType.GainPosition,
                        UtilityScore = utility,
                        Target = target,
                        Destination = DestinationFromTargetGridPoint(targetGridPoint),
                        ActionToTake = IsAttackable(targetGridPoint)
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
            float utility = 0f;
            var targetsForSimple = _context.Participants.Targets;
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
                    (1f - behavior.BloodthirstGreed)
                    * (IsAttackable(targetGridPoint) ? 5f : 3f);

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
                        utility += 2f * behavior.MindlessCunning + 3f * behavior.BloodthirstGreed;
                    }

                    var mySpecies = _context.Unit.UnitInstance.CharacterTemplate?.Species;
                    if (
                        mySpecies != null
                        && targetInfo.HasAnyWeaponEffectiveAgainstSpecies(mySpecies)
                    )
                    {
                        utility -= 3f * behavior.BrashWary + 2f * (1f - behavior.MindlessCunning);
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
