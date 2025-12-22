using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Gameplay.Objects;
using Turnroot.Skills.Nodes;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Runtime context for the entire battle.
    /// Contains all the dynamic data that skills and other systems need at runtime.
    /// </summary>
    public class BattleContext
    {
        /// <summary>
        /// Reference to the Brain for publishing events.
        /// Set this when creating the BattleContext.
        /// </summary>
        public Brain.Brain Brain { get; set; }

        /// <summary>
        /// Active map graph for this battle.
        /// </summary>
        public MapGrid mapGrid { get; set; }

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
        public bool MoveUnit(CharacterInstance unit, Vector2Int targetPosition)
        {
            RequireBrain();
            var command = new MoveCommand(unit.Id, targetPosition, Brain.CurrentTurnNumber);
            return Brain.ExecuteCommand(command);
        }

        public bool SpawnAtPosition(CharacterInstance unit, Vector2Int spawnPosition)
        {
            RequireBrain();
            var command = new SpawnCommand(unit.Id, spawnPosition, Brain.CurrentTurnNumber);
            return Brain.ExecuteCommand(command);
        }

        public bool MoveUnitToPoint(CharacterInstance unit, MapGridPoint targetPoint)
        {
            RequireBrain();
            var command = new MoveCommand(
                unit.Id,
                targetPoint.CoordinatesInt(),
                Brain.CurrentTurnNumber
            );
            return Brain.ExecuteCommand(command);
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
            RequireBrain();
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
            RequireBrain();
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
            RequireBrain();
            var command = new EndTurnCommand(Brain.CurrentTurnNumber);
            return Brain.ExecuteCommand(command);
        }

        /// <summary>
        /// Throws if Brain is not set. All command operations require a Brain.
        /// </summary>
        private void RequireBrain()
        {
            if (Brain == null)
            {
                throw new System.InvalidOperationException(
                    "BattleContext.Brain must be set before performing command-based actions. "
                        + "Ensure BattleGameObject.InitializeContextWithBrain() was called."
                );
            }
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
