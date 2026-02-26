using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Damage calculation system for the battle context.
    /// Handles weapon triangle, effectiveness, stat calculations, and critical hits.
    /// All formulas are configurable via GameplayGeneralSettings.
    /// Component calculations, support bonuses, and helpers are in DamageCalculatorPartials/.
    /// </summary>
    public static partial class DamageCalculator
    {
        #region Main Calculations
        public static int CalculatePotentialDamage(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem,
            BattleContext context = null
        )
        {
            if (!ValidateBasicInputs(attacker, target, weaponItem))
            {
                return 0;
            }

            var weapon = weaponItem.Template;
            var settings = LoadSettings();

            float baseDamage = CalculateBaseDamage(attacker, weapon);

            // Apply weapon triangle modifier to damage if enabled
            if (settings?.GetWeaponTriangleAffectsDamage() ?? true)
            {
                baseDamage *= CalculateWeaponTriangleModifier(attacker, target);
            }

            // Apply effectiveness bonus
            if (context?.AttackIsEffective(attacker, target) == true)
            {
                baseDamage *= settings?.GetEffectivenessMultiplier() ?? 1.5f;
            }

            float defense = CalculateDefense(target, weapon);
            float finalDamage = Mathf.Max(0, baseDamage - defense);

            // Apply critical hit if applicable
            if (
                context?.Flags.ActiveUnitFlags.WillCriticalHit == true
                && context.Flags.ActiveUnitFlags.Unit == attacker
            )
            {
                finalDamage *= settings?.GetCriticalHitMultiplier() ?? 3f;
            }

            return Mathf.RoundToInt(finalDamage);
        }

        public static float CalculateHitChance(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem,
            BattleContext context = null
        )
        {
            if (!ValidateBasicInputs(attacker, target, weaponItem))
            {
                return 0f;
            }

            var settings = LoadSettings();
            var weapon = weaponItem.Template;

            float baseHit = weapon.Hit > 0f ? weapon.Hit : 80f;
            GetHitFormulaMultipliers(
                settings,
                out float skillMult,
                out float dexMult,
                out float luckMult
            );

            float attackerHit = CalculateStatContribution(
                attacker,
                skillMult,
                dexMult,
                luckMult,
                settings
            );
            float targetAvoid = CalculateAvoid(target, context, settings);

            if (context != null)
            {
                var supportBonus = CalculateSupportBonuses(context, attacker, target);
                attackerHit += supportBonus.attackerHit;
                targetAvoid += supportBonus.targetAvoid;
            }

            float triangleHitBonus = CalculateTriangleHitBonus(
                attacker,
                target,
                weaponItem,
                settings
            );

            // average weapon and stat contributions before subtracting avoid
            float combinedHit = (baseHit + attackerHit) * 0.5f;
            float finalHit = combinedHit - targetAvoid + triangleHitBonus;

            return Mathf.Clamp(finalHit, 0f, 100f);
        }

        public static float CalculateCriticalChance(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem,
            BattleContext context = null
        )
        {
            if (!ValidateBasicInputs(attacker, target, weaponItem))
            {
                return 0f;
            }

            var settings = LoadSettings();
            var weapon = weaponItem.Template;

            float baseCrit = weapon.Critical > 0f ? weapon.Critical : 0f;
            GetCritFormulaMultipliers(settings, out float skillMult, out float luckMult);

            float attackerCritBonus = 0f;
            if (!Mathf.Approximately(skillMult, 0f))
            {
                attackerCritBonus +=
                    (attacker.GetUnboundedStat(UnboundedStatType.Skill)?.Get() ?? 0) * skillMult;
            }

            if (!Mathf.Approximately(luckMult, 0f) && (settings?.UseLuck ?? false))
            {
                attackerCritBonus +=
                    (attacker.GetUnboundedStat(UnboundedStatType.Luck)?.Get() ?? 0) * luckMult;
            }

            float targetCritAvoid = CalculateCritAvoid(target, settings);

            if (context != null)
            {
                var supportBonus = CalculateSupportBonuses(context, attacker, target);
                baseCrit += supportBonus.attackerCrit;
                targetCritAvoid += supportBonus.targetDodge;
            }

            return Mathf.Clamp(baseCrit + attackerCritBonus - targetCritAvoid, 0f, 100f);
        }

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

        public static int CalculateAttackCount(CharacterInstance attacker, CharacterInstance target)
        {
            var attackerSpeed = attacker.GetUnboundedStat(UnboundedStatType.Speed);
            var targetSpeed = target.GetUnboundedStat(UnboundedStatType.Speed);

            if (attackerSpeed == null || targetSpeed == null)
            {
                return 1;
            }

            float speedDiff = attackerSpeed.Get() - targetSpeed.Get();
            int threshold = LoadSettings()?.GetDoubleAttackSpeedThreshold() ?? 4;

            return speedDiff >= threshold ? 2 : 1;
        }

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
            return (targetWeapon?.Template) != null
                && targetWeapon.Template.UpperRange >= attackerWeapon.Template.LowerRange;
        }
        #endregion

        // Component calculations (damage, defense, weapon triangle, avoid, terrain) are in
        // DamageCalculatorPartials/ComponentCalculations.cs
        //
        // Support bonus calculations are in DamageCalculatorPartials/SupportBonuses.cs
        //
        // Helper methods (validation, settings loading, formula multipliers) are in
        // DamageCalculatorPartials/Helpers.cs
    }
}
