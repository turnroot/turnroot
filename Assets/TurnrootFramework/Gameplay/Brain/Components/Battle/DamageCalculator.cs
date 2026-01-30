using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Damage calculation system for the battle context.
    /// Handles weapon triangle, effectiveness, stat calculations, and critical hits.
    /// All formulas are configurable via GameplayGeneralSettings.
    /// </summary>
    public static class DamageCalculator
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
            float finalHit = baseHit + attackerHit - targetAvoid + triangleHitBonus;

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
            return targetWeapon?.Template == null
                ? false
                : targetWeapon.Template.UpperRange >= attackerWeapon.Template.LowerRange;
        }
        #endregion

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

        #region Support Bonuses
        private static (
            float attackerHit,
            float attackerCrit,
            float targetAvoid,
            float targetDodge
        ) CalculateSupportBonuses(
            BattleContext context,
            CharacterInstance attacker,
            CharacterInstance target
        )
        {
            var settings = LoadSettings();
            if (settings == null)
            {
                return (0f, 0f, 0f, 0f);
            }

            var attackerBonus = AccumulateAdjacentSupport(context, attacker, settings);
            var targetBonus = AccumulateAdjacentSupport(context, target, settings);

            return (attackerBonus.Hit, attackerBonus.Crit, targetBonus.Avoid, targetBonus.Dodge);
        }

        private static GameplayGeneralSettings.SupportBonus AccumulateAdjacentSupport(
            BattleContext context,
            CharacterInstance unit,
            GameplayGeneralSettings settings
        )
        {
            var total = new GameplayGeneralSettings.SupportBonus();
            if (context == null || unit == null || settings == null)
            {
                return total;
            }

            var adjacency =
                (context.Participants.AdjacentUnits?.Center == unit)
                    ? context.Participants.AdjacentUnits
                    : new Locations.Adjacency(unit);

            using var allyIds = PooledHashSet<string>.Get();
            if (context.Participants.Allies != null)
            {
                foreach (var ally in context.Participants.Allies)
                {
                    if (ally != null)
                    {
                        allyIds.HashSet.Add(ally.Id);
                    }
                }
            }

            var adjacentList = ListPool<CharacterInstance>.Get();
            adjacency.GetAllAdjacentNonAlloc(adjacentList);

            foreach (var adjacent in adjacentList)
            {
                if (adjacent == null || adjacent == unit || !allyIds.HashSet.Contains(adjacent.Id))
                {
                    continue;
                }

                var bonus = GetSupportBonusForPair(unit, adjacent, settings);
                total.Hit += bonus.Hit;
                total.Avoid += bonus.Avoid;
                total.Crit += bonus.Crit;
                total.Dodge += bonus.Dodge;
            }

            ListPool<CharacterInstance>.Return(adjacentList);
            return total;
        }

        private static GameplayGeneralSettings.SupportBonus GetSupportBonusForPair(
            CharacterInstance unit,
            CharacterInstance adjacent,
            GameplayGeneralSettings settings
        )
        {
            var rel1 = unit.GetSupportRelationship(adjacent.CharacterTemplate);
            var rel2 = adjacent.GetSupportRelationship(unit.CharacterTemplate);

            int val1 = rel1 != null ? RankValue(rel1.CurrentLevel) : 0;
            int val2 = rel2 != null ? RankValue(rel2.CurrentLevel) : 0;

            int chosenValue = System.Math.Max(val1, val2);
            string rankLetter = RankLetter(chosenValue);

            SupportRelationshipInstance chosenRel = (rel1 != null && val1 >= val2) ? rel1 : rel2;

            return chosenRel?.HasSupportBonusOverride() == true
                ? chosenRel.GetSupportBonusOverride()
                : settings.GetSupportBonusForRank(rankLetter);
        }

        private static int RankValue(string rankLetter) =>
            rankLetter switch
            {
                LeveledLetteredField.S => 5,
                LeveledLetteredField.A => 4,
                LeveledLetteredField.B => 3,
                LeveledLetteredField.C => 2,
                LeveledLetteredField.D => 1,
                _ => 0,
            };

        private static string RankLetter(int rankValue) =>
            rankValue switch
            {
                5 => LeveledLetteredField.S,
                4 => LeveledLetteredField.A,
                3 => LeveledLetteredField.B,
                2 => LeveledLetteredField.C,
                1 => LeveledLetteredField.D,
                _ => LeveledLetteredField.E,
            };
        #endregion

        #region Helper Methods
        private static bool ValidateBasicInputs(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        )
        {
            if (attacker == null || target == null || weaponItem?.Template == null)
            {
                TurnrootLogger.Log(
                    "CalculatePotentialDamage: null attacker, target, or weapon",
                    TurnrootLogger.LogLevel.Warning
                );
                return false;
            }
            return true;
        }

        private static GameplayGeneralSettings LoadSettings() =>
            GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");

        private static void GetHitFormulaMultipliers(
            GameplayGeneralSettings settings,
            out float skillMult,
            out float dexMult,
            out float luckMult
        )
        {
            if (settings != null)
            {
                settings.GetHitFormulaMultipliers(out skillMult, out dexMult, out luckMult);
            }
            else
            {
                skillMult = 2f;
                dexMult = 1f;
                luckMult = 0.5f;
            }
        }

        private static void GetCritFormulaMultipliers(
            GameplayGeneralSettings settings,
            out float skillMult,
            out float luckMult
        )
        {
            if (settings != null)
            {
                settings.GetCritFormulaMultipliers(out skillMult, out luckMult);
            }
            else
            {
                skillMult = 0.5f;
                luckMult = 0f;
            }
        }

        private static void GetAvoidFormulaMultipliers(
            GameplayGeneralSettings settings,
            out float speedMult,
            out float luckMult
        )
        {
            if (settings != null)
            {
                settings.GetAvoidFormulaMultipliers(out speedMult, out luckMult);
            }
            else
            {
                speedMult = 2f;
                luckMult = 1f;
            }
        }
        #endregion
    }
}
