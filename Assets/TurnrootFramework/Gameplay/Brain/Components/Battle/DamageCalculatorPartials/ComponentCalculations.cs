using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public static partial class DamageCalculator
    {
        #region Component Calculations
        private static float CalculateBaseDamage(CharacterInstance attacker, ObjectItem weapon)
        {
            UnboundedStatType attackStatType =
                weapon.WeaponType?.IsMagic == true
                    ? UnboundedStatType.Magic
                    : UnboundedStatType.Strength;

            float attackPower = attacker.GetUnboundedStat(attackStatType)?.Get() ?? 0;
            float weaponMight = weapon.Might;

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

        private static float CalculateDefense(CharacterInstance target, ObjectItem weapon)
        {
            UnboundedStatType defenseStatType =
                weapon.WeaponType?.IsMagic == true
                    ? UnboundedStatType.Resistance
                    : UnboundedStatType.Defense;

            return target.GetUnboundedStat(defenseStatType)?.Get() ?? 0;
        }

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
                return 1.0f;
            }

            var settings = LoadSettings();
            bool isMagic = attackerWeapon.Template.WeaponType?.IsMagic == true;

            // Check if triangle is enabled
            if (
                (isMagic && !(settings?.MagicTriangle ?? false))
                || (!isMagic && !(settings?.WeaponTriangle ?? false))
            )
            {
                return 1.0f;
            }

            var attackerTriangle = attackerWeapon.Template.WeaponType.TrianglePosition;
            var targetTriangle = targetWeapon.Template.WeaponType.TrianglePosition;

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
                return 1f + (disadvantage / 100f);
            }

            return 1.0f;
        }

        private static float CalculateTriangleHitBonus(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem,
            GameplayGeneralSettings settings
        )
        {
            bool isMagic = weaponItem.Template.WeaponType?.IsMagic == true;
            bool triangleActive =
                (isMagic && (settings?.MagicTriangle ?? false))
                || (!isMagic && (settings?.WeaponTriangle ?? false));
            bool triangleAffectsHit = settings?.GetWeaponTriangleAffectsHit() ?? true;

            if (!triangleActive || !triangleAffectsHit)
            {
                return 0f;
            }

            float triangleModifier = CalculateWeaponTriangleModifier(attacker, target);
            if (Mathf.Approximately(triangleModifier, 1.0f))
            {
                return 0f;
            }

            float triangleHitBonusValue = settings?.GetWeaponTriangleHitBonus() ?? 15f;
            return (triangleModifier - 1.0f) * triangleHitBonusValue;
        }

        private static float CalculateAvoid(
            CharacterInstance target,
            BattleContext context,
            GameplayGeneralSettings settings
        )
        {
            GetAvoidFormulaMultipliers(settings, out float speedMult, out float luckMult);

            float avoid = 0f;

            if (!Mathf.Approximately(speedMult, 0f))
            {
                avoid += (target.GetUnboundedStat(UnboundedStatType.Speed)?.Get() ?? 0) * speedMult;
            }

            if (!Mathf.Approximately(luckMult, 0f) && (settings?.UseLuck ?? false))
            {
                avoid += (target.GetUnboundedStat(UnboundedStatType.Luck)?.Get() ?? 0) * luckMult;
            }

            if (context?.MapGrid != null && target != null)
            {
                var targetGridPoint = target.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    context.MapGrid
                );
                if (targetGridPoint != null)
                {
                    avoid += CalculateTerrainAvoidBonus(target, targetGridPoint, settings);
                }
            }

            return avoid;
        }

        private static float CalculateTerrainAvoidBonus(
            CharacterInstance unit,
            MapGridPoint gridPoint,
            GameplayGeneralSettings settings
        )
        {
            if (gridPoint == null || unit?.CurrentClass?.ClassData?.Identity == null)
            {
                return 0f;
            }

            var terrain = gridPoint.GetCachedTerrainType();
            if (terrain == null)
            {
                return 0f;
            }

            var movementType = unit.CurrentClass.ClassData.Identity.MovementType;
            float terrainAvoid = movementType switch
            {
                MovementType.Infantry => terrain.AvoidBonusWalk,
                MovementType.Riding => terrain.AvoidBonusRiding,
                MovementType.Flying => terrain.AvoidBonusFlying,
                MovementType.Armored => terrain.AvoidBonusArmor,
                _ => 0f,
            };

            return terrainAvoid * (settings?.GetTerrainBonusMultiplier() ?? 1f);
        }

        private static float CalculateCritAvoid(
            CharacterInstance target,
            GameplayGeneralSettings settings
        )
        {
            if (settings?.UseSeparateCriticalAvoidance == true)
            {
                return target.GetUnboundedStat(UnboundedStatType.CriticalAvoidance)?.Get() ?? 0;
            }
            else if (settings?.UseLuck == true)
            {
                return target.GetUnboundedStat(UnboundedStatType.Luck)?.Get() ?? 0;
            }

            return 0;
        }

        private static float CalculateStatContribution(
            CharacterInstance unit,
            float skillMult,
            float dexMult,
            float luckMult,
            GameplayGeneralSettings settings
        )
        {
            float total = 0f;

            if (!Mathf.Approximately(skillMult, 0f))
            {
                total += (unit.GetUnboundedStat(UnboundedStatType.Skill)?.Get() ?? 0) * skillMult;
            }

            if (!Mathf.Approximately(dexMult, 0f))
            {
                total += (unit.GetUnboundedStat(UnboundedStatType.Dexterity)?.Get() ?? 0) * dexMult;
            }

            if (!Mathf.Approximately(luckMult, 0f) && (settings?.UseLuck ?? false))
            {
                total += (unit.GetUnboundedStat(UnboundedStatType.Luck)?.Get() ?? 0) * luckMult;
            }

            return total;
        }
        #endregion
    }
}
