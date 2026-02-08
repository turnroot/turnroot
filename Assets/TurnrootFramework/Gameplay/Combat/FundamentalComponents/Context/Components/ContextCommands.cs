using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
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
                Brain?.battleBrain?.CurrentTurnNumber ?? 0
            );
            return Brain.ExecuteCommand(command)
                ? OperationResult.Successful()
                : OperationResult.Failure("Move command failed to execute");
        }

        public bool SpawnAtPosition(CharacterInstance unit, Vector2Int spawnPosition)
        {
            var command = new SpawnCommand(
                unit.Id,
                spawnPosition,
                Brain?.battleBrain?.CurrentTurnNumber ?? 0
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
                TurnrootLogger.Log(
                    $"BattleContext: Moved {unit.CharacterTemplate.DisplayName} to {targetPoint.CoordinatesInt}",
                    TurnrootLogger.LogLevel.Info
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

            if (weaponItem == null)
            {
                Debug.LogWarning(
                    $"BattleContext: {attacker.CharacterTemplate.DisplayName} has no weapon to attack with!"
                );
                return OperationResult.Failure("No weapon to attack with");
            }

            var success = DealDamage(
                attacker,
                target,
                DamageCalculator.CalculatePotentialDamage(attacker, target, weaponItem, this)
            );
            if (success)
            {
                Brain?.PublishAttackLogicCompleted(attacker);
                return OperationResult.Successful();
            }
            return OperationResult.Failure("Attack command failed to execute");
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
                Brain?.battleBrain?.CurrentTurnNumber ?? 0
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
                Brain?.battleBrain?.CurrentTurnNumber ?? 0
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
                    Brain?.battleBrain?.CurrentTurnNumber ?? 0
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
            var command = new EndTurnCommand(Brain?.battleBrain?.CurrentTurnNumber ?? 0);
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
