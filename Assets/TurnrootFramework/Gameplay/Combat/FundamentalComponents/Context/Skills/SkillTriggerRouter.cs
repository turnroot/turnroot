using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class SkillTriggerRouter
    {
        private readonly BattleBrain _battleBrain;
        private readonly Stack<TriggerExecutionScope> _triggerExecutionScopes = new();
        private Brain Brain => _battleBrain?.Brain;
        private BattleGameObject BattleObject => _battleBrain?.BattleObject;

        public SkillTriggerRouter(BattleBrain battleBrain)
        {
            _battleBrain = battleBrain;
        }

        // BattleStartsNode — continuously re-evaluated passive auras
        private readonly Dictionary<CharacterInstance, List<Skill>> _battleStartSkills = new();

        // One-shot trigger skills, keyed by the unit that owns the skill
        // TurnEnds / UnitMoves - non-combat, full enemy list in Targets
        private readonly Dictionary<CharacterInstance, List<Skill>> _turnEndsSkills = new();

        // AnyTurnStarts - fires for ALL skill owners when any unit's turn starts
        private readonly Dictionary<CharacterInstance, List<Skill>> _anyTurnStartsSkills = new();

        // UnitTurnStarts - fires only for the unit whose turn it is
        private readonly Dictionary<CharacterInstance, List<Skill>> _unitTurnStartsSkills = new();
        private readonly Dictionary<CharacterInstance, List<Skill>> _unitMovesSkills = new();

        // UnitAttacks — combat, Targets already contains the combat target(s)
        private readonly Dictionary<CharacterInstance, List<Skill>> _unitAttacksSkills = new();

        // EnemyAttacks — fired for the DEFENDER; Targets set to [attacker] during execution
        private readonly Dictionary<CharacterInstance, List<Skill>> _enemyAttacksSkills = new();

        // EnemyDefeated — fired for the KILLER; Targets set to [defeated unit] during execution
        private readonly Dictionary<CharacterInstance, List<Skill>> _enemyDefeatedSkills = new();

        // CombatStarts — fired before the first strike of a combat exchange, for both attacker and defender
        private readonly Dictionary<CharacterInstance, List<Skill>> _combatStartSkills = new();

        // PostCombat — fired after all strikes in a combat exchange resolve, for both attacker and defender
        private readonly Dictionary<CharacterInstance, List<Skill>> _postCombatSkills = new();

        #region Event Subscriptions
        public void SubscribeToEvents()
        {
            if (Brain == null)
            {
                return;
            }

            Brain.OnBattleStarted += HandleBattleStartSkills;
            Brain.OnSkillExecutionCompleted += OnSkillExecutionCompletedHandler;
            Brain.OnCharacterLearnedSkill += OnCharacterLearnedSkillHandler;
            Brain.OnCharacterRemovedSkill += OnCharacterRemovedSkillHandler;
            Brain.OnUnitTurnEnded += OnUnitTurnEndedHandler;
            Brain.OnUnitTurnStarted += OnUnitTurnStartedHandler;
            Brain.OnUnitMoved += OnUnitMovedHandler;
            Brain.OnAttackLogicCompleted += OnUnitAttacksHandler;
            Brain.OnLastAttackerSet += OnLastAttackerSetHandler;
            Brain.OnCombatStarted += OnCombatStartedHandler;
            Brain.OnCombatEnded += OnCombatEndedHandler;
            Brain.Subscribe<UnitDefeatedEvent>(OnUnitDefeatedHandler, EventPriority.Normal);
        }

        public void UnsubscribeFromEvents()
        {
            if (Brain == null)
            {
                return;
            }

            Brain.OnBattleStarted -= HandleBattleStartSkills;
            Brain.OnSkillExecutionCompleted -= OnSkillExecutionCompletedHandler;
            Brain.OnCharacterLearnedSkill -= OnCharacterLearnedSkillHandler;
            Brain.OnCharacterRemovedSkill -= OnCharacterRemovedSkillHandler;
            Brain.OnUnitTurnEnded -= OnUnitTurnEndedHandler;
            Brain.OnUnitTurnStarted -= OnUnitTurnStartedHandler;
            Brain.OnUnitMoved -= OnUnitMovedHandler;
            Brain.OnAttackLogicCompleted -= OnUnitAttacksHandler;
            Brain.OnLastAttackerSet -= OnLastAttackerSetHandler;
            Brain.OnCombatStarted -= OnCombatStartedHandler;
            Brain.OnCombatEnded -= OnCombatEndedHandler;
            Brain.Unsubscribe<UnitDefeatedEvent>(OnUnitDefeatedHandler);
        }
        #endregion
    }
}
