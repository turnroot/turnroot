using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Gameplay.Objects;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Runtime context for the entire battle.
    /// Contains all the dynamic data that skills and other systems need at runtime.
    /// </summary>
    public class BattleContext : MonoBehaviour
    {
        /// <summary>
        /// Reference to the Brain for publishing events.
        /// Set this when creating the BattleContext. Use Initialize() to assign.
        /// </summary>
        public Brain.Brain Brain { get; private set; }

        /// <summary>
        /// Active map graph for this battle.
        /// </summary>
        public MapGrid mapGrid { get; private set; }

        /// <summary>
        /// Initialize the BattleContext with required dependencies. Throws if brain is null.
        /// </summary>
        public void Initialize(Brain.Brain brain, MapGrid mapGrid)
        {
            if (brain == null)
                throw new System.ArgumentNullException(nameof(brain));
            Brain = brain;
            this.mapGrid = mapGrid;
        }

        // Currently executing skill (if any)
        public Skill CurrentSkill { get; set; }

        // All skills and their graphs that can be executed in this battle
        public List<Skill> ActiveSkills { get; set; }
        public List<SkillGraph> ActiveSkillGraphs { get; set; }

        public Dictionary<Skill, int> SkillUseCount { get; set; }
        public CharacterInstance UnitInstance { get; set; }
        public List<CharacterInstance> Targets { get; set; }
        public List<CharacterInstance> Allies { get; set; }
        public List<CharacterInstance> ThirdParty { get; set; }
        public Adjacency AdjacentUnits { get; set; }

        // Currently executing skill graph (if any)
        public SkillGraph CurrentSkillGraph { get; set; }

        public EnvironmentalConditions EnvironmentalConditions { get; set; }
        public Dictionary<string, object> CustomData { get; private set; }

        public bool IsInterrupted { get; set; }

        // Combat state flags
        public bool IsCriticalHit { get; set; }
        public CharacterInstance CriticalHitUnit { get; set; }
        public bool AnotherTurnGranted { get; set; }
        public CharacterInstance UnitTakingAnotherTurn { get; set; }

        public bool AttackIsEffective(CharacterInstance unit, CharacterInstance target) => false; //TODO: set this up properly

        public bool AttackWouldKill(CharacterInstance target) => false; // TODO: Implement this

        public BattleContext()
        {
            CustomData = new Dictionary<string, object>();
            Targets = new List<CharacterInstance>();
            Allies = new List<CharacterInstance>();
            ThirdParty = new List<CharacterInstance>();
            AdjacentUnits = new Adjacency(null);
            ActiveSkills = new List<Skill>();
            ActiveSkillGraphs = new List<SkillGraph>();
            SkillUseCount = new Dictionary<Skill, int>();
        }

        // Get a custom data value, or default if not found
        public T GetCustomData<T>(string key, T defaultValue = default) =>
            CustomData.TryGetValue(key, out object value) && value is T typedValue
                ? typedValue
                : defaultValue;

        // Set a custom data value
        public void SetCustomData(string key, object value) => CustomData[key] = value;

        #region Command-Based Actions

        /// <summary>
        /// Moves a unit to a position using the command pattern.
        /// </summary>
        /// <param name="unit">The unit to move.</param>
        /// <param name="targetPosition">The target position.</param>
        /// <returns>True if the move succeeded.</returns>
        public OperationResult MoveUnitToPointInt(CharacterInstance unit, Vector2Int CoordinatesInt)
        {
            var command = new MoveCommand(unit.Id, CoordinatesInt, Brain.CurrentTurnNumber);
            return Brain.ExecuteCommand(command)
                ? OperationResult.SuccessResult()
                : OperationResult.Failure("Move command failed to execute");
        }

        public bool SpawnAtPosition(CharacterInstance unit, Vector2Int spawnPosition)
        {
            var command = new SpawnCommand(unit.Id, spawnPosition, Brain.CurrentTurnNumber);
            return Brain.ExecuteCommand(command);
        }

        public OperationResult MoveUnitToPoint(CharacterInstance unit, MapGridPoint targetPoint)
        {
            var command = new MoveCommand(
                unit.Id,
                targetPoint.CoordinatesInt,
                Brain.CurrentTurnNumber
            );
            if (Brain.ExecuteCommand(command))
            {
                return OperationResult.SuccessResult();
            }
            else
            {
                return OperationResult.Failure("Move command failed to execute");
            }
        }

        public OperationResult AttackTarget(CharacterInstance attacker, CharacterInstance target)
        {
            var weaponItem = attacker.GetEquippedWeapon();
            if (weaponItem == null)
            {
                Debug.LogWarning(
                    $"BattleContext: {attacker.CharacterTemplate.DisplayName} has no equipped weapon to attack with!"
                );
                return OperationResult.Failure("No equipped weapon to attack with");
            }
            return DealDamage(
                attacker,
                target,
                CalculatePotentialDamage(attacker, target, weaponItem) // TODO: CalculatePotentialDamage needs redone
            )
                ? OperationResult.SuccessResult()
                : OperationResult.Failure("Attack command failed to execute");
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
                Brain.CurrentTurnNumber
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
                Brain.CurrentTurnNumber
            );
            return Brain.ExecuteCommand(command);
        }

        /// <summary>
        /// Ends the current unit's turn using the command pattern.
        /// </summary>
        /// <returns>True if ending turn succeeded.</returns>
        public bool EndTurn()
        {
            var command = new EndTurnCommand(Brain.CurrentTurnNumber);
            return Brain.ExecuteCommand(command);
        }

        /// <summary>
        /// Checks if a character is an ally in this context.
        /// </summary>
        public bool IsAlly(CharacterInstance character) => Allies?.Contains(character) ?? false;

        /// <summary>
        /// Checks if a character is a target (potential enemy) in this context.
        /// </summary>
        public bool IsTarget(CharacterInstance character) => Targets?.Contains(character) ?? false;

        public int CalculatePotentialDamage(
            CharacterInstance unitInstance,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        ) => throw new NotImplementedException(); // TODO: Implement potential damage calculation
        #endregion
    }
}
