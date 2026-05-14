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
            // If a weapon was not explicitly provided, use the currently equipped weapon
            weaponItem ??= attacker.GetEquippedWeapon();

            var canAttackWithoutWeapon = GameplayGeneralSettings
                .Instance
                .UnitCanAttackWithoutWeapons;

            if (weaponItem == null && !canAttackWithoutWeapon)
            {
                this.LogWarning(
                    $"{attacker.CharacterTemplate.DisplayName} has no weapon to attack with!"
                );
                return OperationResult.Failure("No weapon to attack with");
            }

            // Update Targets so skills that fire during this strike see the correct opponent
            var originalTargets = Participants.Targets;
            Participants.Targets = new System.Collections.Generic.List<CharacterInstance>
            {
                target,
            };

            bool hit;
            int damage = 0;
            if (weaponItem != null)
            {
                float hitChance = DamageCalculator.CalculateHitChance(
                    attacker,
                    target,
                    weaponItem,
                    this
                );
                hit = UnityEngine.Random.Range(0f, 100f) <= hitChance;
            }
            else
            {
                hit = true;
            }

            OperationResult result = OperationResult.Successful();
            if (hit)
            {
                // Check if this attack is negated by a skill (NegateNextAttackNode, etc.).
                // Value: -1 = all attacks this turn negated; positive = remaining shield count.
                var negateKey = $"NegateAttacks_{target.Id}";
                int negateCount = GetCustomData<int>(negateKey, 0);
                if (negateCount != 0)
                {
                    if (negateCount > 0)
                        SetCustomData(negateKey, negateCount - 1);
                    // Skip the rest of damage processing for this strike
                    Brain?.PublishAttackLogicCompleted(attacker);
                    Participants.Targets = originalTargets;
                    return OperationResult.Successful();
                }

                // If CriticalHitNode hasn't already forced a crit this strike,
                // roll against the attacker's crit chance stat. Applied before damage calculation
                // so CalculatePotentialDamage can read and consume the WillCriticalHit flag.
                if (weaponItem != null && !Flags.ActiveUnitFlags.WillCriticalHit)
                {
                    float critChance = DamageCalculator.CalculateCriticalChance(
                        attacker,
                        target,
                        weaponItem,
                        this
                    );
                    if (UnityEngine.Random.Range(0f, 100f) < critChance)
                    {
                        Flags.ActiveUnitFlags.WillCriticalHit = true;
                        Flags.ActiveUnitFlags.Unit = attacker;
                        Brain?.PublishCriticalHit(attacker);
                    }
                }

                damage = DamageCalculator.CalculatePotentialDamage(
                    attacker,
                    target,
                    weaponItem,
                    this
                );

                // Apply any damage reduction written by ReduceDamageNode (Aegis, Pavise, etc.).
                // The key persists for the whole exchange so every strike is reduced; it is
                // cleared at the start of the next exchange by OnCombatStartedHandler.
                var reductionKey = $"DamageReduction_{target.Id}";
                var reduction = GetCustomData<DamageReductionData>(reductionKey);
                if (reduction.Amount > 0f)
                {
                    damage = reduction.IsPercentage
                        ? Mathf.RoundToInt(damage * (1f - reduction.Amount / 100f))
                        : Mathf.Max(0, damage - Mathf.RoundToInt(reduction.Amount));
                }

                bool success = DealDamage(attacker, target, damage);
                if (!success)
                {
                    result = OperationResult.Failure("DealDamage command failed to execute");
                    damage = 0;
                }

                // Reflect damage back to the attacker if DamageReflectionNode is active on target
                if (damage > 0)
                {
                    var reflectData = GetCustomData<DamageReflectionData>(
                        $"ReflectDamage_{target.Id}"
                    );
                    if (reflectData.Percent > 0f)
                    {
                        int reflected = Mathf.Max(
                            1,
                            Mathf.RoundToInt(damage * reflectData.Percent / 100f)
                        );
                        DealDamage(target, attacker, reflected);
                    }
                }
            }

            SetCustomData($"LastDamageDealt_{attacker.Id}", (float)damage);

            // Fire skill trigger whether hit or miss — UnitAttacksNode fires on attempt
            Brain?.PublishAttackLogicCompleted(attacker);

            // Restore original target list so callers see the same state as before this strike
            Participants.Targets = originalTargets;

            return result;
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

            Brain?.PublishCombatStarted(attacker, defender);

            bool attackerFirstStrike = GetCustomData<bool>($"FirstStrike_{attacker.Id}");
            // Vantage: defender counterattacks before the attacker's first strike
            bool defenderVantage = GetCustomData<bool>($"Vantage_{defender.Id}");

            // Attacker's strike
            OperationResult result;
            if (defenderVantage && !attackerFirstStrike)
            {
                // Vantage order: defender counter first, then normal exchange without a second counter
                AttackTarget(defender, attacker);
                if (!attacker.IsDefeatedInCurrentBattle)
                {
                    result = AttackTarget(attacker, defender, attackerWeapon);

                    // Attacker follow-up (Vantage counter already spent)
                    if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
                        AttackTarget(attacker, defender, attackerWeapon);

                    // Defender follow-up if fast enough (Vantage counter was their first attack,
                    // so check follow-up speed threshold for a potential second strike)
                    bool defenderDisabledVantage = GetCustomData<bool>(
                        $"DisableFollowup_{defender.Id}"
                    );
                    if (
                        !defender.IsDefeatedInCurrentBattle
                        && !defenderDisabledVantage
                        && CanFollowUp(defender, attacker)
                    )
                        AttackTarget(defender, attacker);
                }
                else
                {
                    result = OperationResult.Successful();
                }
            }
            else if (attackerFirstStrike)
            {
                // First-strike: attacker hits twice before defender can respond
                result = AttackTarget(attacker, defender, attackerWeapon);
                if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
                    AttackTarget(attacker, defender, attackerWeapon);
            }
            else
            {
                result = AttackTarget(attacker, defender, attackerWeapon);

                // Defender counter-attack (if still alive and not blocked by DisableFollowup on defender)
                bool defenderDisabled = GetCustomData<bool>($"DisableFollowup_{defender.Id}");
                if (!defender.IsDefeatedInCurrentBattle && !defenderDisabled)
                    AttackTarget(defender, attacker);

                // Attacker follow-up (SPD diff >= 4, and defender hasn't disabled it)
                if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
                    AttackTarget(attacker, defender, attackerWeapon);

                // Defender follow-up (if defender is fast enough and not disabled)
                if (
                    !defender.IsDefeatedInCurrentBattle
                    && !defenderDisabled
                    && CanFollowUp(defender, attacker)
                )
                    AttackTarget(defender, attacker);
            }

            // Track combat count for IsFirstCombatOfTurnNode
            attacker.IncrementCombatCount();
            defender.IncrementCombatCount();

            // Clear all combat-scoped flags for both participants
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

            Brain?.PublishCombatEnded(attacker, defender);
            return result;
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
                return true;

            // Skill explicitly prevents attacker from following up
            if (GetCustomData<bool>($"DisableFollowup_{attacker.Id}"))
                return false;

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
            var success = Brain.ExecuteCommand(command);
            if (success)
            {
                Brain?.PublishUseItemLogicCompleted(user, item);
            }
            return success;
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
        public bool EndTurn()
        {
            var command = new EndTurnCommand(Brain?.battleBrain.CurrentTurnNumber ?? 0);
            var success = Brain.ExecuteCommand(command);
            if (success)
            {
                var unit = Unit?.UnitInstance;
                Brain.PublishEndTurnCompleted(unit);
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
            Participants?.Targets?.Contains(character) ?? false;

        public int CalculatePotentialDamage(
            CharacterInstance unitInstance,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        ) => DamageCalculator.CalculatePotentialDamage(unitInstance, target, weaponItem, this);
        #endregion
    }
}
