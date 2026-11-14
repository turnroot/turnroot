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
    {
        #region Command-Based Actions

        /// <summary>
        /// Moves a unit to a position using the command pattern.
        /// </summary>
        /// <param name="unit">The unit to move.</param>
        /// <param name="targetPosition">The target position.</param>
        /// <returns>True if the move succeeded.</returns>
        public OperationResult MoveUnitToPointInt(CharacterInstance unit, Vector2Int CoordinatesInt)
        {
            var command = new MoveCommand(
                unit.Id,
                CoordinatesInt,
                Brain?.battleBrain.CurrentTurnNumber ?? 0
            );
            return Brain.ExecuteCommand(command)
                ? OperationResult.Successful()
                : OperationResult.Failure("Move command failed to execute");
        }

        public bool SpawnAtPosition(CharacterInstance unit, Vector2Int spawnPosition)
        {
            if (unit == null)
            {
                this.LogWarning("SpawnAtPosition called with null unit");
                return false;
            }

            // make sure the unit is registered with the context so the
            // SpawnCommand can locate it by id during execution.  This also
            // establishes the unit's allegiance for targeting logic.
            EnsureUnitIsParticipant(unit);

            var command = new SpawnCommand(
                unit.Id,
                spawnPosition,
                Brain?.battleBrain.CurrentTurnNumber ?? 0
            );
            bool success = Brain.ExecuteCommand(command);
            if (success)
            {
                // Invalidate all tile caches when new unit spawns
                InvalidateAllTileCaches();
            }
            return success;
        }

        public OperationResult MoveUnitToPoint(CharacterInstance unit, MapGridPoint targetPoint)
        {
            var command = new MoveCommand(
                unit.Id,
                targetPoint.CoordinatesInt,
                Brain.battleBrain.CurrentTurnNumber
            );
            var t = Brain.ExecuteCommand(command)
                ? OperationResult.Successful()
                : OperationResult.Failure("Move command failed to execute");
            if (t.Success)
            {
                this.LogInfo(
                    $"Moved {unit.CharacterTemplate.DisplayName} to {targetPoint.CoordinatesInt}"
                );
            }
            return t;
        }

        public OperationResult AttackTarget(
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

        public OperationResult ExecuteCombatExchange(
            CharacterInstance attacker,
            CharacterInstance defender,
            ObjectItemInstance attackerWeapon = null
        )
        {
            if (attacker == null || defender == null)
            {
                return OperationResult.Failure(
                    "ExecuteCombatExchange: attacker or defender is null"
                );
            }

            // Store who initiated so IsInitiatingCombatNode and OnUnitAttacks can read it
            SetCustomData("CombatInitiatorId", attacker.Id);

            bool combatStarted = false;
            try
            {
                Brain?.PublishCombatStarted(attacker, defender);
                combatStarted = true;

                bool attackerFirstStrike = GetCustomData<bool>($"FirstStrike_{attacker.Id}");
                bool defenderVantage = GetCustomData<bool>($"Vantage_{defender.Id}");

                OperationResult result = defenderVantage && !attackerFirstStrike
                    ? ResolveVantageExchange(attacker, defender, attackerWeapon)
                    : attackerFirstStrike
                        ? ResolveFirstStrikeExchange(attacker, defender, attackerWeapon)
                        : ResolveStandardExchange(attacker, defender, attackerWeapon);

                // Track combat count for IsFirstCombatOfTurnNode.
                attacker.IncrementCombatCount();
                defender.IncrementCombatCount();
                return result;
            }
            finally
            {
                ClearCombatScopedData(attacker, defender);
                if (combatStarted)
                {
                    Brain?.PublishCombatEnded(attacker, defender);
                }
            }
        }

        private OperationResult ResolveVantageExchange(
            CharacterInstance attacker,
            CharacterInstance defender,
            ObjectItemInstance attackerWeapon
        )
        {
            // Vantage: defender counters before the attacker's first strike.
            AttackTarget(defender, attacker);
            if (attacker.IsDefeatedInCurrentBattle)
            {
                return OperationResult.Successful();
            }

            var result = AttackTarget(attacker, defender, attackerWeapon);
            if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
            {
                AttackTarget(attacker, defender, attackerWeapon);
            }

            bool defenderDisabled = GetCustomData<bool>($"DisableFollowup_{defender.Id}");
            if (
                !defender.IsDefeatedInCurrentBattle
                && !defenderDisabled
                && CanFollowUp(defender, attacker)
            )
            {
                AttackTarget(defender, attacker);
            }

            return result;
        }

        private OperationResult ResolveFirstStrikeExchange(
            CharacterInstance attacker,
            CharacterInstance defender,
            ObjectItemInstance attackerWeapon
        )
        {
            // First strike: attacker makes their initial and possible follow-up attacks first.
            var result = AttackTarget(attacker, defender, attackerWeapon);
            if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
            {
                AttackTarget(attacker, defender, attackerWeapon);
            }
            return result;
        }

        private OperationResult ResolveStandardExchange(
            CharacterInstance attacker,
            CharacterInstance defender,
            ObjectItemInstance attackerWeapon
        )
        {
            var result = AttackTarget(attacker, defender, attackerWeapon);
            bool defenderDisabled = GetCustomData<bool>($"DisableFollowup_{defender.Id}");

            if (!defender.IsDefeatedInCurrentBattle && !defenderDisabled)
            {
                AttackTarget(defender, attacker);
            }

            if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
            {
                AttackTarget(attacker, defender, attackerWeapon);
            }

            if (
                !defender.IsDefeatedInCurrentBattle
                && !defenderDisabled
                && CanFollowUp(defender, attacker)
            )
            {
                AttackTarget(defender, attacker);
            }

            return result;
        }

        private void ClearCombatScopedData(
            CharacterInstance attacker,
            CharacterInstance defender
        )
        {
            CustomData.Remove($"FirstStrike_{attacker.Id}");
            CustomData.Remove($"FirstStrike_{defender.Id}");
            CustomData.Remove($"Vantage_{attacker.Id}");
            CustomData.Remove($"Vantage_{defender.Id}");
            CustomData.Remove($"DisableFollowup_{attacker.Id}");
            CustomData.Remove($"DisableFollowup_{defender.Id}");
            CustomData.Remove($"GuaranteeFollowup_{attacker.Id}");
            CustomData.Remove($"GuaranteeFollowup_{defender.Id}");
            CustomData.Remove($"SpeedThresholdMod_{attacker.Id}");
            CustomData.Remove($"SpeedThresholdMod_{defender.Id}");
            CustomData.Remove($"NegateTerrainEffects_{attacker.Id}");
            CustomData.Remove($"NegateTerrainEffects_{defender.Id}");
            CustomData.Remove("CombatInitiatorId");
        }

        /// <summary>
        /// Returns true when <paramref name="attacker"/> is fast enough to follow up
        /// (attack twice in one exchange).  Uses SPD difference ≥ 4,
        /// modified by GuaranteeFollowup / DisableFollowup / SpeedThresholdMod CustomData
        /// keys set by skill nodes (ChangeBattleOrderNode, etc.).
        /// </summary>
        private bool CanFollowUp(CharacterInstance attacker, CharacterInstance defender)
        {
            // Skill explicitly guarantees a follow-up (e.g. Brash Assault)
            if (GetCustomData<bool>($"GuaranteeFollowup_{attacker.Id}"))
            {
                return true;
            }

            // Skill explicitly prevents attacker from following up
            if (GetCustomData<bool>($"DisableFollowup_{attacker.Id}"))
            {
                return false;
            }

            var atkSpd = attacker.GetUnboundedStat(UnboundedStatType.Speed)?.Current ?? 0f;
            var defSpd = defender.GetUnboundedStat(UnboundedStatType.Speed)?.Current ?? 0f;

            // SpeedThresholdMod adjusts the 4-SPD gap required (negative = easier, positive = harder)
            float threshold = 4f + GetCustomData<float>($"SpeedThresholdMod_{attacker.Id}");
            return (atkSpd - defSpd) >= threshold;
        }

        /// <summary>
        /// Deals damage to a target unit using the command pattern.
        /// </summary>
        /// <param name="attacker">The attacking unit.</param>
        /// <param name="target">The target unit.</param>
        /// <param name="damage">The damage amount to deal.</param>
        /// <returns>True if the damage was applied.</returns>
        public bool DealDamage(CharacterInstance attacker, CharacterInstance target, int damage)
        {
            var command = new DamageCommand(
                attacker.Id,
                target.Id,
                damage,
                Brain?.battleBrain.CurrentTurnNumber ?? 0
            );
            return Brain.ExecuteCommand(command);
        }

        /// <summary>
        /// Uses an item using the command pattern.
        /// </summary>
        /// <param name="user">The unit using the item.</param>
        /// <param name="item">The item to use.</param>
        /// <param name="target">Optional target for the item.</param>
        /// <returns>True if the item use succeeded.</returns>
        public bool UseItem(
            CharacterInstance user,
            ObjectItemInstance item,
            CharacterInstance target = null
        )
        {
            var command = new UseItemCommand(
                user.Id,
                item.InstanceID,
                target?.Id,
                Brain?.battleBrain.CurrentTurnNumber ?? 0
            );
            // UseItemLogicCompleted is published by the command on success
            return Brain.ExecuteCommand(command);
        }

        public bool HealUnit(
            CharacterInstance user,
            CharacterInstance target,
            ObjectItemInstance fromItem = null
        )
        {
            // If healing comes from an item, use UseItem
            if (fromItem != null)
            {
                UseItem(user, fromItem, target);
                Brain?.PublishHealLogicCompleted(user);
            }
            else
            {
                var command = new HealCommand(
                    user.Id,
                    target.Id,
                    Brain?.battleBrain.CurrentTurnNumber ?? 0
                );
                var success = Brain.ExecuteCommand(command);
                if (success)
                {
                    Brain?.PublishHealLogicCompleted(user);
                }
                return success;
            }
            return true;
        }

        /// <summary>
        /// Ends the current unit's turn using the command pattern.
        /// </summary>
        /// <returns>True if ending turn succeeded.</returns>
        public bool CompleteActiveUnitTurn()
        {
            var command = new EndUnitTurnCommand(Brain?.battleBrain.CurrentTurnNumber ?? 0);
            var success = Brain.ExecuteCommand(command);
            if (success)
            {
                var unit = Unit?.UnitInstance;
                Brain.PublishUnitTurnAdvanceRequested(unit);
            }
            return success;
        }

        /// <summary>
        /// Checks if a character is in the Allies roster (player-controlled units).
        /// For third-party allegiance-aware checks, use BattleContext.AreAllies() or CanAttack().
        /// </summary>
        public bool IsAlly(CharacterInstance character) =>
            Participants?.Allies?.Contains(character) ?? false;

        /// <summary>
        /// Checks if a character is in the Targets roster (enemy units).
        /// For third-party allegiance-aware checks, use BattleContext.AreAllies() or CanAttack().
        /// </summary>
        public bool IsTarget(CharacterInstance character) =>
            Participants?.Enemies?.Contains(character) ?? false;

        public int CalculatePotentialDamage(
            CharacterInstance unitInstance,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        ) => DamageCalculator.CalculatePotentialDamage(unitInstance, target, weaponItem, this);
        #endregion
    }
}
