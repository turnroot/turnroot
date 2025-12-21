using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Attack
        private float CalculateAttackUtility(CharacterInstance target, CharacterBehavior behavior)
        {
            var baseWeight = 12f;
            // add bloodthirst
            baseWeight += (1 - behavior.BloodthirstGreed) * 2f;
            // Get effective (i.e. archer against flyer)
            var effective = _context.AttackIsEffective(_context.UnitInstance, target);
            if (effective)
            {
                baseWeight += 1f + (behavior.MindlessCunning * 3f);
            }
            // TODO: Set up effective weapons
            // Bonus for attacking same target as allies (focus fire) if solider
            foreach (var ally in _context.Allies)
            {
                if (ally.LastAttackedTarget == target)
                {
                    baseWeight += 2f * (1f - behavior.SoldierLoneWolf);
                }
            }
            // Get weapon triangle advantage or disadvantage
            float bestWeaponTriangleModifier = 1f;
            float weaponTriangleModifier = 1f;
            var availableWeapons = _context.UnitInstance.GetAvailableWeapons();
            var bestWeapon = null as ObjectItemInstance;
            var potentialDamage = 0;
            var targetEquippedWeapon = target.GetEquippedWeapon();
            foreach (var weaponItem in availableWeapons)
            {
                var advantageCheck = weaponItem.Template.WeaponType.TrianglePosition;
                var targetWeaponAdvantageCheck = targetEquippedWeapon
                    .Template
                    .WeaponType
                    .TrianglePosition;
                if (advantageCheck.WinsAgainst(targetWeaponAdvantageCheck))
                {
                    float advantageBonus = 1f + behavior.MindlessCunning * 10f;
                    baseWeight += advantageBonus;
                    var checkDamage = _context.CalculatePotentialDamage(
                        _context.UnitInstance,
                        target,
                        weaponItem
                    ); // TODO: Make this
                    if (checkDamage > potentialDamage)
                    {
                        potentialDamage = checkDamage;
                        bestWeapon = weaponItem;
                    }
                }
                else
                {
                    if (advantageCheck.LosesTo(targetWeaponAdvantageCheck))
                    {
                        float disadvantagePenalty = 3f + (behavior.MindlessCunning * 7f);
                        baseWeight -= disadvantagePenalty;
                    }
                }
                if (weaponTriangleModifier > bestWeaponTriangleModifier)
                {
                    bestWeaponTriangleModifier = weaponTriangleModifier;
                }
            }
            // add some based on if the target was the last attacked target
            if (_context.UnitInstance.LastAttackedTarget == target)
            {
                // higher weight if the unit is more mindless
                baseWeight += 2f + ((1 - behavior.MindlessCunning) * 2f);
            }
            // Next, check our own health and the target health to see which is higher
            var healthFactor =
                _context
                    .UnitInstance.GetBoundedStat(Characters.Stats.BoundedStatType.Health)
                    .Current
                - target.GetBoundedStat(Characters.Stats.BoundedStatType.Health).Current;
            healthFactor += BrashWary; // more wary units care more about health differences
            float healthAdvantage =
                healthFactor
                / Mathf.Max(
                    _context
                        .UnitInstance.GetBoundedStat(Characters.Stats.BoundedStatType.Health)
                        .Max,
                    target.GetBoundedStat(Characters.Stats.BoundedStatType.Health).Max
                );
            float scaledBonus = healthAdvantage * 10f * Mathf.Max(0.3f, MindlessCunning);
            baseWeight += Mathf.Clamp(scaledBonus, -10f, +20f);

            return Mathf.Max(0f, baseWeight);
        }
        #endregion
    }
}
