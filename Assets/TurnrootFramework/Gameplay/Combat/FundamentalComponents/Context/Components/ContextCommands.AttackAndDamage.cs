using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Skills.Nodes.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext : MonoBehaviour
    {        public OperationResult AttackTarget(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem = null
        )
        {
            attacker?.RecordTargetAttackedThisTurn(target);

            // If a weapon was not explicitly provided, use the currently equipped weapon
            weaponItem ??= attacker.GetEquippedWeapon();

            var canAttackWithoutWeapon =
                GameplayGeneralSettings.Instance?.UnitCanAttackWithoutWeapons ?? false;

            if (weaponItem == null && !canAttackWithoutWeapon)
            {
                this.LogWarning(
                    $"{attacker.CharacterTemplate.DisplayName} has no weapon to attack with!"
                );
                return OperationResult.Failure("No weapon to attack with");
            }

            // Update action targets so skills that fire during this strike see the correct opponent.
            var originalTargets = GetCurrentTargets();
            try
            {
                SetCurrentTargets(new[] { target });
                return ResolveAttackStrike(attacker, target, weaponItem);
            }
            finally
            {
                SetCurrentTargets(originalTargets);
            }
        }

        private OperationResult ResolveAttackStrike(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        )
        {
            bool hit = weaponItem == null || RollAttackHit(attacker, target, weaponItem);
            int damage = 0;

            OperationResult result = OperationResult.Successful();
            if (hit)
            {
                if (TryConsumeAttackNegation(target))
                {
                    Brain?.PublishAttackLogicCompleted(attacker);
                    return OperationResult.Successful();
                }

                ApplyCriticalHit(attacker, target, weaponItem);
                damage = CalculateStrikeDamage(attacker, target, weaponItem);
                result = ApplyStrikeDamage(attacker, target, ref damage);
            }

            SetCustomData($"LastDamageDealt_{attacker.Id}", (float)damage);

            // Fire skill trigger whether hit or miss — UnitAttacksNode fires on attempt
            Brain?.PublishAttackLogicCompleted(attacker);

            return result;
        }

        private bool RollAttackHit(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        )
        {
            float hitChance = DamageCalculator.CalculateHitChance(
                attacker,
                target,
                weaponItem,
                this
            );
            return Random.Range(0f, 100f) <= hitChance;
        }

        private bool TryConsumeAttackNegation(CharacterInstance target)
        {
            // -1 negates all attacks this turn; positive values count remaining negations.
            var negateKey = $"NegateAttacks_{target.Id}";
            int negateCount = GetCustomData(negateKey, 0);
            if (negateCount == 0)
            {
                return false;
            }

            if (negateCount > 0)
            {
                SetCustomData(negateKey, negateCount - 1);
            }
            return true;
        }

        private void ApplyCriticalHit(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        )
        {
            // CriticalHitNode may already have forced a critical hit for this strike.
            if (weaponItem == null || Flags.ActiveUnitFlags.WillCriticalHit)
            {
                return;
            }

            float critChance = DamageCalculator.CalculateCriticalChance(
                attacker,
                target,
                weaponItem,
                this
            );
            if (Random.Range(0f, 100f) < critChance)
            {
                Flags.ActiveUnitFlags.WillCriticalHit = true;
                Flags.ActiveUnitFlags.Unit = attacker;
                Brain?.PublishCriticalHit(attacker);
            }
        }

        private int CalculateStrikeDamage(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        )
        {
            int damage = DamageCalculator.CalculatePotentialDamage(
                attacker,
                target,
                weaponItem,
                this
            );

            // Damage-reduction skills remain active for every strike in the exchange.
            var reduction = GetCustomData<DamageReductionData>($"DamageReduction_{target.Id}");
            return reduction.Amount <= 0f
                ? damage
                : reduction.IsPercentage
                ? Mathf.RoundToInt(damage * (1f - reduction.Amount / 100f))
                : Mathf.Max(0, damage - Mathf.RoundToInt(reduction.Amount));
        }

        private OperationResult ApplyStrikeDamage(
            CharacterInstance attacker,
            CharacterInstance target,
            ref int damage
        )
        {
            if (!DealDamage(attacker, target, damage))
            {
                damage = 0;
                return OperationResult.Failure("DealDamage command failed to execute");
            }

            if (damage > 0)
            {
                ApplyDamageReflection(attacker, target, damage);
            }

            return OperationResult.Successful();
        }

        private void ApplyDamageReflection(
            CharacterInstance attacker,
            CharacterInstance target,
            int damage
        )
        {
            var reflectData = GetCustomData<DamageReflectionData>($"ReflectDamage_{target.Id}");
            if (reflectData.Percent <= 0f)
            {
                return;
            }

            int reflected = Mathf.Max(1, Mathf.RoundToInt(damage * reflectData.Percent / 100f));
            DealDamage(target, attacker, reflected);
        }

    }
}
