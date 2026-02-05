using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Utility-based AI system for battle decision-making.
    /// Evaluates possible goals, calculates utility scores, and executes the most valuable action.
    /// </summary>
    public partial class BattleContextAIHelper
    {
        private readonly BattleContext _context;
        BattleCondition[] BattleConditions;
        private AStarModified _aStarModified = new();

        // Some basic behavioral bools
        private bool CanMove => BehaviorSettings.MovementDisabled == false;
        private bool CanAttack => BehaviorSettings.AttackDisabled == false;
        private bool CanHeal => _context.Unit.UnitInstance.CurrentClass.ClassData.Identity.CanHeal;

        // Context dependent bools
        private bool WasDamagedThisTurn;
        private bool AllyDiedLastTurn;
        private CharacterInstance LastAttackerThisTurn;

        // Computed situational states
        private bool IsWounded =>
            _context
                .Unit.UnitInstance.GetBoundedStat(Characters.Stats.BoundedStatType.Health)
                .Current < ((MindlessCunning / 2.5f) + .1f);

        private bool IsSurrounded =>
            _context.Participants.AdjacentUnits.GetAdjacentEnemyCount(_context)
            >= (SoldierLoneWolf >= .6f ? 2 : 3);

        private Vector2Int LastTurnPosition;

        private Vector2Int CurrentTurnPosition => _context.Unit.UnitInstance.MapGridPosition;
        private bool HasMovedSinceLastTurn => LastTurnPosition != CurrentTurnPosition;

        // Behavior weights
        private CharacterBehavior BehaviorSettings =>
            _context.Unit.UnitInstance.CharacterTemplate.BehaviorSettings;
        private float BloodthirstGreed => BehaviorSettings.BloodthirstGreed;
        private float BrashWary => BehaviorSettings.BrashWary;
        private float MindlessCunning => BehaviorSettings.MindlessCunning;
        private float SelfishSelfless => BehaviorSettings.SelfishSelfless;
        private float SoldierLoneWolf =>
            BehaviorSettings.SoldierLoneWolf < .75f
                ? BehaviorSettings.SoldierLoneWolf / 3f
                : BehaviorSettings.SoldierLoneWolf;

        private CharacterBehavior modifiedBehaviorSettings;

        // Reusable dictionaries to avoid allocations during AI decision-making
        private readonly Dictionary<MapGridPoint, float> _reusableMoveTiles = new();
        private readonly Dictionary<MapGridPoint, float> _reusableAttackTiles = new();
        private readonly Dictionary<MapGridPoint, float> _reusableHealTiles = new();

        public BattleContextAIHelper(BattleContext context)
        {
            _context = context;
            // Subscribe to unit turn ended so we can capture last-turn position and reset per-turn flags
            if (_context?.Brain != null)
            {
                _context.Brain.OnUnitTurnEnded += HandleUnitTurnEnded;
            }
        }

        private void HandleUnitTurnEnded(CharacterInstance unit)
        {
            // If the unit ending its turn is the one this helper is currently tracking, record position
            if (unit != null && unit == _context.Unit.UnitInstance)
            {
                LastTurnPosition = CurrentTurnPosition;

                // Reset per-turn transient flags
                WasDamagedThisTurn = false;
                AllyDiedLastTurn = false;
                LastAttackerThisTurn = null;
            }
        }

        public OperationResult InitializeAIControlledUnit(CharacterInstance unitInstance)
        {
            if (unitInstance == null)
            {
                return OperationResult.Failure("Cannot initialize AI for null unit");
            }

            _aStarModified ??= new AStarModified();

            if (_context.Brain?.battleBrain?.BattleObject != null)
            {
                BattleConditions = _context.Brain.battleBrain.BattleObject.BattleConditions;
            }
            return OperationResult.Successful();
        }

        #region AI Goal System

        /// <summary>
        /// Represents a potential action the AI can take with its evaluated utility score
        /// </summary>
        public struct AIGoal
        {
            public enum GoalType
            {
                AttackEnemy,
                KillEnemy,
                HealAlly,
                ProtectAlly,
                CollectTreasure,
                ExploreVillages,
                DefensiveRetreat,
                HoldPosition,
                GainPosition,
                HealSelf,
            }

            public GoalType Type;
            public float UtilityScore;
            public CharacterInstance Target; // Enemy or ally, if applicable
            public MapGridPoint Destination; // Store any extra info needed for execution
            public Objects.ObjectItemInstance ChosenWeapon; // Optional chosen weapon for attack goals

            public enum Action
            {
                Attack,
                Heal,
                Move,
                Feature,
            }

            public Action ActionToTake;
        }

        #endregion
    }
}
