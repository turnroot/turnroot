using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Damage calculation system for the battle context.
    /// Handles weapon triangle, effectiveness, stat calculations, and critical hits.
    /// All formulas are now configurable via GameplayGeneralSettings.
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
#if UNITY_EDITOR
                Debug.LogWarning("CalculatePotentialDamage: null attacker, target, or weapon");
#endif
                return 0;
            }

            var weapon = weaponItem.Template;

            // Base damage calculation
            float baseDamage = CalculateBaseDamage(attacker, weapon);

            // Apply weapon/magic triangle modifier to damage if enabled
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            if (settings == null || settings.GetWeaponTriangleAffectsDamage())
            {
                float triangleModifier = CalculateWeaponTriangleModifier(attacker, target);
                baseDamage *= triangleModifier;
            }

            // Apply effectiveness bonus
            if (context != null && context.AttackIsEffective(attacker, target))
            {
                float effMult = settings != null ? settings.GetEffectivenessMultiplier() : 1.5f;
                baseDamage *= effMult;
            }

            // Calculate defense reduction
            float defense = CalculateDefense(target, weapon);

            // Final damage (can't go below 0)
            float finalDamage = Mathf.Max(0, baseDamage - defense);

            // Apply critical hit if applicable
            if (
                context != null
                && context.Flags.IsCriticalHit
                && context.Flags.CriticalHitUnit == attacker
            )
            {
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

            // Add weapon might
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
                return 1.0f;
            }

            var attackerTriangle = attackerWeapon.Template.WeaponType.TrianglePosition;
            var targetTriangle = targetWeapon.Template.WeaponType.TrianglePosition;

            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");

            // Check if triangle is enabled
            bool isMagic = attackerWeapon.Template.WeaponType?.IsMagic == true;
            if (isMagic && (settings == null || !settings.MagicTriangle))
            {
                return 1.0f;
            }
            if (!isMagic && (settings == null || !settings.WeaponTriangle))
            {
                return 1.0f;
            }

            // Get advantage/disadvantage values
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

        /// <summary>
        /// Calculate target's defense stat.
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
        /// Formula is configurable via GameplayGeneralSettings.HitFormula:
        /// - ClassicDouble: Skill*2 + Dex + Luck/2
        /// - RadiantDouble: Skill*2.5 + Dex + Luck/2
        /// - Modern: Skill + Dex + Luck/2
        /// - WeaponOnly: Just weapon hit (no stat bonuses)
        /// - Custom: User-defined multipliers
        /// </summary>
        public static float CalculateHitChance(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem,
            BattleContext context = null
        )
        {
            if (attacker == null || target == null || weaponItem?.Template == null)
            {
                return 0f;
            }

            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            var weapon = weaponItem.Template;

            // Base hit from weapon
            float baseHit = weapon.Hit > 0f ? weapon.Hit : 80f;

            // Get formula multipliers from settings
            float skillMult,
                dexMult,
                luckMult;
            if (settings != null)
            {
                settings.GetHitFormulaMultipliers(out skillMult, out dexMult, out luckMult);
            }
            else
            {
                // Fallback to classic formula
                skillMult = 2f;
                dexMult = 1f;
                luckMult = 0.5f;
            }

            // Calculate attacker hit based on formula
            float attackerHit = 0f;

            if (skillMult != 0f)
            {
                var skillStat = attacker.GetUnboundedStat(UnboundedStatType.Skill);
                attackerHit += (skillStat?.Get() ?? 0) * skillMult;
            }

            if (dexMult != 0f)
            {
                var dexStat = attacker.GetUnboundedStat(UnboundedStatType.Dexterity);
                attackerHit += (dexStat?.Get() ?? 0) * dexMult;
            }

            // Add luck bonus if enabled and multiplier is set
            if (luckMult != 0f && settings != null && settings.UseLuck)
            {
                var luckStat = attacker.GetUnboundedStat(UnboundedStatType.Luck);
                attackerHit += (luckStat?.Get() ?? 0) * luckMult;
            }

            // Target avoid
            float targetAvoid = CalculateAvoid(target, context, settings);

            // Apply support bonuses if context available
            if (context != null)
            {
                var supportBonus = CalculateSupportBonuses(context, attacker, target);
                attackerHit += supportBonus.attackerHit;
                targetAvoid += supportBonus.targetAvoid;
            }

            // Apply weapon triangle bonus to hit if enabled
            float triangleHitBonus = 0f;

            // Check if triangle affects hit and if triangle is actually active
            bool isMagic = weaponItem.Template.WeaponType?.IsMagic == true;
            bool triangleActive =
                (isMagic && settings != null && settings.MagicTriangle)
                || (!isMagic && settings != null && settings.WeaponTriangle);
            bool triangleAffectsHit = settings == null || settings.GetWeaponTriangleAffectsHit();

            if (triangleActive && triangleAffectsHit)
            {
                float triangleModifier = CalculateWeaponTriangleModifier(attacker, target);
                if (triangleModifier != 1.0f)
                {
                    float triangleHitBonusValue = settings?.GetWeaponTriangleHitBonus() ?? 15f;
                    triangleHitBonus = (triangleModifier - 1.0f) * triangleHitBonusValue;
                }
            }

            float finalHit = baseHit + attackerHit - targetAvoid + triangleHitBonus;

            return Mathf.Clamp(finalHit, 0f, 100f);
        }

        /// <summary>
        /// Calculate critical hit chance percentage (0-100).
        /// Formula is configurable via GameplayGeneralSettings.CritFormula:
        /// - SkillHalf: Weapon Crit + Skill/2
        /// - SkillAndLuck: Weapon Crit + (Skill + Luck)/2
        /// - WeaponOnly: Just weapon crit
        /// - Custom: User-defined multipliers
        /// </summary>
        public static float CalculateCriticalChance(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem,
            BattleContext context = null
        )
        {
            if (attacker == null || target == null || weaponItem?.Template == null)
            {
                return 0f;
            }

            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            var weapon = weaponItem.Template;

            // Base crit from weapon
            float baseCrit = weapon.Critical > 0f ? weapon.Critical : 0f;

            // Get formula multipliers from settings
            float skillMult,
                luckMult;
            if (settings != null)
            {
                settings.GetCritFormulaMultipliers(out skillMult, out luckMult);
            }
            else
            {
                // Fallback to classic formula
                skillMult = 0.5f;
                luckMult = 0f;
            }

            // Calculate attacker's crit bonus based on formula
            float attackerCritBonus = 0f;

            if (skillMult != 0f)
            {
                var skillStat = attacker.GetUnboundedStat(UnboundedStatType.Skill);
                attackerCritBonus += (skillStat?.Get() ?? 0) * skillMult;
            }

            if (luckMult != 0f && settings != null && settings.UseLuck)
            {
                var luckStat = attacker.GetUnboundedStat(UnboundedStatType.Luck);
                attackerCritBonus += (luckStat?.Get() ?? 0) * luckMult;
            }

            // Target's critical avoidance
            float targetCritAvoid;
            if (settings != null && settings.UseSeparateCriticalAvoidance)
            {
                // Use dedicated CriticalAvoidance stat if enabled
                var critAvoidStat = target.GetUnboundedStat(UnboundedStatType.CriticalAvoidance);
                targetCritAvoid = critAvoidStat?.Get() ?? 0;
            }
            else if (settings != null && settings.UseLuck)
            {
                // Fall back to Luck as crit avoid
                var luckStat = target.GetUnboundedStat(UnboundedStatType.Luck);
                targetCritAvoid = luckStat?.Get() ?? 0;
            }
            else
            {
                // No crit avoidance if neither stat is enabled
                targetCritAvoid = 0;
            }

            // Apply support bonuses if context available
            if (context != null)
            {
                var supportBonus = CalculateSupportBonuses(context, attacker, target);
                baseCrit += supportBonus.attackerCrit;
                targetCritAvoid += supportBonus.targetDodge;
            }

            float finalCrit = baseCrit + attackerCritBonus - targetCritAvoid;

            return Mathf.Clamp(finalCrit, 0f, 100f);
        }

        /// <summary>
        /// Calculate support bonuses from adjacent allies for both attacker and target.
        /// Returns a tuple with separate bonuses for attacker and target.
        /// </summary>
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
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            if (settings == null)
            {
                return (0f, 0f, 0f, 0f);
            }

            // Calculate attacker's support bonuses from adjacent allies
            var attackerBonus = AccumulateAdjacentSupport(context, attacker, settings);

            // Calculate target's support bonuses from adjacent allies
            var targetBonus = AccumulateAdjacentSupport(context, target, settings);

            return (attackerBonus.Hit, attackerBonus.Crit, targetBonus.Avoid, targetBonus.Dodge);
        }

        /// <summary>
        /// Calculate avoid stat for a character.
        /// Formula is configurable via GameplayGeneralSettings.AvoidFormula:
        /// - ClassicDouble: Speed*2 + Luck + Terrain
        /// - Modern: Speed + Luck + Terrain
        /// - SpeedOnly: Speed + Terrain
        /// - Custom: User-defined multipliers + Terrain
        /// </summary>
        private static float CalculateAvoid(
            CharacterInstance target,
            BattleContext context,
            GameplayGeneralSettings settings
        )
        {
            // Get formula multipliers from settings
            float speedMult,
                luckMult;
            if (settings != null)
            {
                settings.GetAvoidFormulaMultipliers(out speedMult, out luckMult);
            }
            else
            {
                // Fallback to classic formula
                speedMult = 2f;
                luckMult = 1f;
            }

            // Calculate avoid based on formula
            float avoid = 0f;

            if (speedMult != 0f)
            {
                var speedStat = target.GetUnboundedStat(UnboundedStatType.Speed);
                avoid += (speedStat?.Get() ?? 0) * speedMult;
            }

            // Add Luck if enabled and multiplier is set
            if (luckMult != 0f && settings != null && settings.UseLuck)
            {
                var luckStat = target.GetUnboundedStat(UnboundedStatType.Luck);
                avoid += (luckStat?.Get() ?? 0) * luckMult;
            }

            // Add terrain avoid bonus
            if (context?.mapGrid != null && target != null)
            {
                var targetGridPoint = target.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    context.mapGrid
                );

                if (targetGridPoint != null)
                {
                    avoid += CalculateTerrainAvoidBonus(target, targetGridPoint, settings);
                }
            }

            return avoid;
        }

        /// <summary>
        /// Calculate terrain avoid bonus based on unit's movement type and terrain.
        /// Applies terrain bonus multiplier from settings.
        /// </summary>
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

            // Apply terrain bonus multiplier from settings
            if (settings != null)
            {
                terrainAvoid *= settings.GetTerrainBonusMultiplier();
            }

            return terrainAvoid;
        }

        /// <summary>
        /// Accumulate support bonuses from adjacent allied units.
        /// Uses the BattleContext's Adjacency system to find nearby allies efficiently.
        /// </summary>
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

            // Use context's adjacency if centered on this unit, otherwise create temporary
            var adjacency =
                (
                    context.Participants.AdjacentUnits != null
                    && context.Participants.AdjacentUnits.Center == unit
                )
                    ? context.Participants.AdjacentUnits
                    : new Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations.Adjacency(
                        unit
                    );

            // Build fast lookup of ally IDs
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

            // Iterate adjacent units and apply support for allied neighbors (non-alloc)
            var adjacentList = ListPool<CharacterInstance>.Get();
            adjacency.GetAllAdjacentNonAlloc(adjacentList);
            foreach (var adjacent in adjacentList)
            {
                if (adjacent == null || adjacent == unit || !allyIds.HashSet.Contains(adjacent.Id))
                {
                    continue;
                }

                // Get support bonus for this pair
                var bonus = GetSupportBonusForPair(unit, adjacent, settings);

                total.Hit += bonus.Hit;
                total.Avoid += bonus.Avoid;
                total.Crit += bonus.Crit;
                total.Dodge += bonus.Dodge;
            }
            ListPool<CharacterInstance>.Return(adjacentList);

            return total;
        }

        /// <summary>
        /// Get the support bonus between two units, checking for relationship-specific overrides.
        /// </summary>
        private static GameplayGeneralSettings.SupportBonus GetSupportBonusForPair(
            CharacterInstance unit,
            CharacterInstance adjacent,
            GameplayGeneralSettings settings
        )
        {
            // Get support relationships from both perspectives
            var rel1 = unit.GetSupportRelationship(adjacent.CharacterTemplate);
            var rel2 = adjacent.GetSupportRelationship(unit.CharacterTemplate);

            // Convert rank letters to numeric values
            int val1 = rel1 != null ? RankValue(rel1.CurrentLevel) : 0;
            int val2 = rel2 != null ? RankValue(rel2.CurrentLevel) : 0;

            // Use the higher support rank
            int chosenValue = System.Math.Max(val1, val2);
            string rankLetter = RankLetter(chosenValue);

            // Determine which relationship instance to use
            SupportRelationshipInstance chosenRelInstance =
                (rel1 != null && val1 >= val2) ? rel1 : rel2;

            // Check for per-relationship override, otherwise use global settings
            if (chosenRelInstance != null && chosenRelInstance.HasSupportBonusOverride())
            {
                return chosenRelInstance.GetSupportBonusOverride();
            }

            return settings.GetSupportBonusForRank(rankLetter);
        }

        private static int RankValue(string rankLetter)
        {
            return rankLetter switch
            {
                LeveledLetteredField.S => 5,
                LeveledLetteredField.A => 4,
                LeveledLetteredField.B => 3,
                LeveledLetteredField.C => 2,
                LeveledLetteredField.D => 1,
                LeveledLetteredField.E => 0,
                _ => 0,
            };
        }

        private static string RankLetter(int rankValue)
        {
            return rankValue switch
            {
                5 => LeveledLetteredField.S,
                4 => LeveledLetteredField.A,
                3 => LeveledLetteredField.B,
                2 => LeveledLetteredField.C,
                1 => LeveledLetteredField.D,
                _ => LeveledLetteredField.E,
            };
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
                return false;
            }

            int attackRange = attackerWeapon.Template.LowerRange;
            int counterRange = targetWeapon.Template.UpperRange;

            return counterRange >= attackRange;
        }
    }
}
