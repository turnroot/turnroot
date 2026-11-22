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
    {        public bool DealDamage(CharacterInstance attacker, CharacterInstance target, int damage)
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
    }
}
