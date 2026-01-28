using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Attack
        private (float utility, ObjectItemInstance chosenWeapon) CalculateAttackUtility(
            CharacterInstance target,
            CharacterBehavior behavior
        )
        {
            var baseWeight = 12f;
            // add bloodthirst
            baseWeight += (1 - behavior.BloodthirstGreed) * 2f;
            // Get effective (i.e. archer against flyer)
            var effective = _context.AttackIsEffective(_context.Unit.UnitInstance, target);
            if (effective)
            {
                baseWeight += 1f + (behavior.MindlessCunning * 3f);
            }
            // Bonus for attacking same target as allies (focus fire) if solider
            foreach (var ally in _context.Participants.Allies)
            {
                if (ally.LastAttackedTarget == target)
                {
                    baseWeight += 2f * (1f - behavior.SoldierLoneWolf);
                }
            }
            // Evaluate available weapons using centralized DamageCalculator
            var availableWeapons = _context.Unit.UnitInstance.RangeWeaponsCache;
            var bestWeapon = null as ObjectItemInstance;
            float bestTotalPotential = 0f;
            int potentialDamage = 0;

            foreach (var weaponItem in availableWeapons)
            {
                int perHit = DamageCalculator.CalculatePotentialDamage(
                    _context.Unit.UnitInstance,
                    target,
                    weaponItem,
                    _context
                );
                int attackCount = DamageCalculator.CalculateAttackCount(
                    _context.Unit.UnitInstance,
                    target
                );
                float totalPotential = perHit * attackCount;

                // Slight preference for equipped weapon
                if (weaponItem == _context.Unit.UnitInstance.GetEquippedWeapon())
                {
                    totalPotential *= 1.05f; // small bonus for equipped weapon
                }

                if (totalPotential > bestTotalPotential)
                {
                    bestTotalPotential = totalPotential;
                    potentialDamage = perHit;
                    bestWeapon = weaponItem;
                }

                // Large bonus for kill opportunities
                if (
                    DamageCalculator.WouldKill(
                        _context.Unit.UnitInstance,
                        target,
                        weaponItem,
                        _context
                    )
                )
                {
                    baseWeight += 6f + (behavior.MindlessCunning * 4f);
                }
            }

            // Add a scaled weight based on best potential damage
            baseWeight += Mathf.Clamp(
                bestTotalPotential / 10f * (1f + behavior.MindlessCunning),
                0f,
                20f
            );

            // Small bonus if the best weapon is currently equipped
            if (bestWeapon == _context.Unit.UnitInstance.GetEquippedWeapon() && bestWeapon != null)
            {
                baseWeight += 0.6f;
            }

            // add some based on if the target was the last attacked target
            if (_context.Unit.UnitInstance.LastAttackedTarget == target)
            {
                // higher weight if the unit is more mindless
                baseWeight += 2f + ((1 - behavior.MindlessCunning) * 2f);
            }
            // Next, check our own health and the target health to see which is higher
            var healthFactor =
                _context
                    .Unit.UnitInstance.GetBoundedStat(Characters.Stats.BoundedStatType.Health)
                    .Current
                - target.GetBoundedStat(Characters.Stats.BoundedStatType.Health).Current;
            healthFactor += BrashWary; // more wary units care more about health differences
            float healthAdvantage =
                healthFactor
                / Mathf.Max(
                    _context
                        .Unit.UnitInstance.GetBoundedStat(Characters.Stats.BoundedStatType.Health)
                        .Max,
                    target.GetBoundedStat(Characters.Stats.BoundedStatType.Health).Max
                );
            float scaledBonus = healthAdvantage * 10f * Mathf.Max(0.3f, MindlessCunning);
            baseWeight += Mathf.Clamp(scaledBonus, -10f, +20f);

            return (Mathf.Max(0f, baseWeight), bestWeapon);
        }
        #endregion
    }
}
