using System;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Damage calculation system for the battle context.
    /// Handles weapon triangle, effectiveness, stat calculations, and critical hits.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Calculate the potential damage that an attacker would deal to a target with a specific weapon.
        /// </summary>
        public static int CalculatePotentialDamage(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem,
            BattleContext context = null
        )
        {
            if (attacker == null || target == null || weaponItem?.Template == null)
            {
                Debug.LogWarning("CalculatePotentialDamage: null attacker, target, or weapon");
                return 0;
            }

            var weapon = weaponItem.Template;

            // Base damage calculation
            float baseDamage = CalculateBaseDamage(attacker, weapon);

            // Apply weapon/ magic triangle modifier (driven by GameplayGeneralSettings)
            float triangleModifier = CalculateWeaponTriangleModifier(attacker, target);
            baseDamage *= triangleModifier;

            // Apply effectiveness bonus (driven by settings)
            if (context != null && context.AttackIsEffective(attacker, target))
            {
                var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>(
                    "GameSettings"
                );
                float effMult = settings != null ? settings.GetEffectivenessMultiplier() : 1.5f;
                baseDamage *= effMult;
            }

            // Calculate defense reduction
            float defense = CalculateDefense(target, weapon);

            // Final damage (can't go below 0)
            float finalDamage = Mathf.Max(0, baseDamage - defense);

            // Apply critical hit if applicable (use settings multiplier)
            if (context != null && context.IsCriticalHit && context.CriticalHitUnit == attacker)
            {
                var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>(
                    "GameSettings"
                );
                float critMult = settings != null ? settings.GetCriticalHitMultiplier() : 3f;
                finalDamage *= critMult;
            }

            return Mathf.RoundToInt(finalDamage);
        }

        /// <summary>
        /// Calculate base damage: (Attacker's Attack Stat + Weapon Might) + Weapon Stat Bonuses
        /// </summary>
        private static float CalculateBaseDamage(CharacterInstance attacker, ObjectItem weapon)
        {
            // Determine which attack stat to use
            UnboundedStatType attackStatType =
                weapon.WeaponType?.IsMagic == true
                    ? UnboundedStatType.Magic
                    : UnboundedStatType.Strength;

            // Get attacker's base attack stat
            var attackStat = attacker.GetUnboundedStat(attackStatType);
            float attackPower = attackStat?.Get() ?? 0;

            // Add weapon might (use explicit Might property if present)
            float weaponMight = weapon.Might;
            // Add any additional stat bonuses from weapon (excluding Strength/might already added)
            float totalBonus = 0f;
            if (weapon.StatBonuses != null)
            {
                foreach (var bonus in weapon.StatBonuses.Dictionary)
                {
                    if (bonus.Key != UnboundedStatType.Strength)
                    {
                        totalBonus += bonus.Value;
                    }
                }
            }

            return attackPower + weaponMight + totalBonus;
        }

        /// <summary>
        /// Calculate the weapon/magic triangle modifier between attacker and target weapons.
        /// Uses values from GameplayGeneralSettings when available.
        /// </summary>
        private static float CalculateWeaponTriangleModifier(
            CharacterInstance attacker,
            CharacterInstance target
        )
        {
            var attackerWeapon = attacker.GetEquippedWeapon();
            var targetWeapon = target.GetEquippedWeapon();

            if (
                attackerWeapon?.Template?.WeaponType == null
                || targetWeapon?.Template?.WeaponType == null
            )
            {
                return 1.0f; // No triangle if either weapon is missing
            }

            var attackerTriangle = attackerWeapon.Template.WeaponType.TrianglePosition;
            var targetTriangle = targetWeapon.Template.WeaponType.TrianglePosition;

            // Load gameplay settings
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");

            // Decide whether to use weapon or magic triangle
            bool isMagic = attackerWeapon.Template.WeaponType?.IsMagic == true;
            if (isMagic && (settings == null || !settings.MagicTriangle))
            {
                return 1.0f;
            }
            if (!isMagic && (settings == null || !settings.WeaponTriangle))
            {
                return 1.0f;
            }

            // Determine advantage/disadvantage values
            int advantage = isMagic
                ? settings.GetMagicTriangleAdvantage()
                : settings.GetWeaponTriangleAdvantage();
            int disadvantage = isMagic
                ? settings.GetMagicTriangleDisadvantage()
                : settings.GetWeaponTriangleDisadvantage();

            if (attackerTriangle.WinsAgainst(targetTriangle))
            {
                return 1f + (advantage / 100f);
            }
            else if (attackerTriangle.LosesTo(targetTriangle))
            {
                return 1f + (disadvantage / 100f); // disadvantage is expected to be negative
            }

            return 1.0f; // Neutral matchup
        }

        /// <summary>
        /// Calculate target's defense stat.
        /// Uses Defense for physical weapons, Resistance for magical weapons.
        /// </summary>
        private static float CalculateDefense(CharacterInstance target, ObjectItem weapon)
        {
            UnboundedStatType defenseStatType =
                weapon.WeaponType?.IsMagic == true
                    ? UnboundedStatType.Resistance
                    : UnboundedStatType.Defense;

            var defenseStat = target.GetUnboundedStat(defenseStatType);
            return defenseStat?.Get() ?? 0;
        }

        /// <summary>
        /// Calculate hit chance percentage (0-100).
        /// Formula: (Weapon Hit + Attacker Skill/Dexterity) - (Target Speed/Luck)
        /// </summary>
        public static float CalculateHitChance(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        )
        {
            if (attacker == null || target == null || weaponItem?.Template == null)
            {
                return 0f;
            }

            var weapon = weaponItem.Template;

            // Base hit from weapon (fallback to 80 if not set on the template)
            float baseHit = weapon.Hit > 0f ? weapon.Hit : 80f; // Default hit rate

            // Attacker's skill/dexterity bonus
            var skillStat = attacker.GetUnboundedStat(UnboundedStatType.Skill);
            var dexStat = attacker.GetUnboundedStat(UnboundedStatType.Dexterity);
            float attackerBonus = (skillStat?.Get() ?? 0) + ((dexStat?.Get() ?? 0) * 0.5f);

            // Target's avoid (speed + luck)
            var speedStat = target.GetUnboundedStat(UnboundedStatType.Speed);
            var luckStat = target.GetUnboundedStat(UnboundedStatType.Luck);
            float targetAvoid = (speedStat?.Get() ?? 0) + ((luckStat?.Get() ?? 0) * 0.5f);

            // Apply weapon triangle bonus to hit using configured advantage/disadvantage
            float triangleModifier = CalculateWeaponTriangleModifier(attacker, target);
            float triangleHitBonus = (triangleModifier - 1.0f) * 15f; // keeps previous scaling (advantage 20 -> +3 hit)

            float finalHit = baseHit + attackerBonus - targetAvoid + triangleHitBonus;

            return Mathf.Clamp(finalHit, 0f, 100f);
        }

        /// <summary>
        /// Calculate critical hit chance percentage (0-100).
        /// Formula: (Weapon Crit + Attacker Skill) - Target's Critical Avoidance
        /// </summary>
        public static float CalculateCriticalChance(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        )
        {
            if (attacker == null || target == null || weaponItem?.Template == null)
            {
                return 0f;
            }

            var weapon = weaponItem.Template;

            // Base crit from weapon (fallback to 5 if not set on the template)
            float baseCrit = weapon.Critical > 0f ? weapon.Critical : 5f; // Default crit rate

            // Attacker's skill bonus
            var skillStat = attacker.GetUnboundedStat(UnboundedStatType.Skill);
            float attackerSkill = skillStat?.Get() ?? 0;

            // Target's critical avoidance
            var critAvoidStat = target.GetUnboundedStat(UnboundedStatType.CriticalAvoidance);
            float targetAvoid = critAvoidStat?.Get() ?? 0;

            float finalCrit = baseCrit + (attackerSkill * 0.5f) - targetAvoid;

            return Mathf.Clamp(finalCrit, 0f, 100f);
        }

        /// <summary>
        /// Calculate if an attack would kill the target (for AI decision making).
        /// </summary>
        public static bool WouldKill(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem,
            BattleContext context = null
        )
        {
            int damage = CalculatePotentialDamage(attacker, target, weaponItem, context);
            var targetHP = target.GetBoundedStat(BoundedStatType.Health);

            return targetHP != null && damage >= targetHP.Current;
        }

        /// <summary>
        /// Calculate the number of attacks in a round (double attacks for high speed difference).
        /// </summary>
        public static int CalculateAttackCount(CharacterInstance attacker, CharacterInstance target)
        {
            var attackerSpeed = attacker.GetUnboundedStat(UnboundedStatType.Speed);
            var targetSpeed = target.GetUnboundedStat(UnboundedStatType.Speed);

            if (attackerSpeed == null || targetSpeed == null)
            {
                return 1;
            }

            float speedDifference = attackerSpeed.Get() - targetSpeed.Get();

            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            int threshold = settings != null ? settings.GetDoubleAttackSpeedThreshold() : 4;
            // Double attack if speed advantage >= threshold
            return speedDifference >= threshold ? 2 : 1;
        }

        /// <summary>
        /// Calculate whether target can counter-attack.
        /// </summary>
        public static bool CanCounterAttack(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance attackerWeapon
        )
        {
            if (attacker == null || target == null || attackerWeapon?.Template == null)
            {
                return false;
            }

            var targetWeapon = target.GetEquippedWeapon();
            if (targetWeapon?.Template == null)
            {
                return false; // Can't counter without a weapon
            }

            // Check if target's weapon can reach the attacker
            // This would need distance calculation from BattleContext
            int attackRange = attackerWeapon.Template.LowerRange;
            int counterRange = targetWeapon.Template.UpperRange;

            // Simplified: if target weapon has range that overlaps with attack range
            return counterRange >= attackRange;
        }
    }
}
