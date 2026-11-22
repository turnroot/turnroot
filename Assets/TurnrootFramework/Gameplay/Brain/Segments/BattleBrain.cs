using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// The battle brain manages one battle at a time.
    /// Responsible for initializing battles and managing turn order.
    /// </summary>
    [RequireComponent(typeof(TurnRotisserie))]
    [RequireComponent(typeof(PlayerTurnFlow))]
    public partial class BattleBrain : BrainComponent
    {
        #region Dependencies

        [SerializeField, HideInInspector]
        private PlayerTeamRoster _playerTeamRoster;

        [HideInInspector]
        public TurnRotisserie turnRotisserie;

        [HideInInspector]
        public PlayerTurnFlow playerTurnFlow;

        private BattleContextAIHelper _aiHelper;
        private SkillTriggerRouter _skillExecutor;
        #endregion

        #region State

        [HideInInspector]
        public bool IsInputEnabled = true;

        /// <summary>
        /// True during battle initialization (before precompute completes).
        /// Used to prevent premature snapshots during initial unit spawn.
        /// </summary>
        public bool IsInitializing { get; private set; } = true;

        public BattleGameObject BattleObject { get; private set; }
        public Combat.PreBattle.BattlePreparationObject PreparationObject { get; private set; }

        public CharacterInstance ActiveUnit => turnRotisserie.GetActiveUnit();

        public int CurrentTurnNumber { get; private set; } = 0;

        public void ResetTurnNumber() => CurrentTurnNumber = 0;

        public void IncrementTurnNumber()
        {
            CurrentTurnNumber++;
            BattleObject.Context.InvalidateAllTileCaches();
        }

        #endregion

        #region Roster Accessors
        public PlayerTeamRosterInstance PlayerTeamRoster => BattleObject?.PlayerTeamRoster;

        public GenericRosterInstance ThirdPartyTeamRoster => BattleObject?.ThirdPartyTeamRoster;

        #endregion


        #region Initialization

        protected override void Awake()
        {
            base.Awake();

            turnRotisserie = GetComponent<TurnRotisserie>();
            turnRotisserie?.BindToBattleBrain(this);
            playerTurnFlow = GetComponent<PlayerTurnFlow>();
            playerTurnFlow.Intialize();

            _skillExecutor = new SkillTriggerRouter(this);
            _skillExecutor.SubscribeToEvents();

            Brain.OnPrecomputeCompleted += HandlePrecomputeCompleted;
        }

        private void HandlePrecomputeCompleted()
        {
            "BattleBrain: Precompute completed, taking initial snapshot".LogInfo();

            IsInitializing = false;
            _brain.TakeSnapshot();
        }

        private void InitializeLTMDependentData()
        {
            // Called after LongTermMemory is initialized
            if (Brain.gamewideContextBrain == null)
            {
                "BattleBrain: gamewideContextBrain is null during InitializeLTMDependentData!".LogError();
                return;
            }

            if (Brain.gamewideContextBrain.GamewidePersistentPlayerRoster == null)
            {
                Brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            }

            _playerTeamRoster = Brain.gamewideContextBrain.GamewidePersistentPlayerRoster;

            if (_playerTeamRoster == null)
            {
                "BattleBrain: No PlayerTeamRoster available! Make sure PersistentPlayerRoster.asset is configured and assigned.".LogError();
                return;
            }

            var rosterInstance = Brain.gamewideContextBrain.GetOrCreatePlayerTeamRoster(
                _playerTeamRoster
            );

            if (rosterInstance == null)
            {
                $"BattleBrain: Failed to get/create runtime instance for roster '{_playerTeamRoster.name}'".LogError();
                return;
            }

            if (rosterInstance.Instances == null || rosterInstance.Instances.Count == 0)
            {
                $"BattleBrain: Roster '{_playerTeamRoster.name}' has no character instances. Check roster template configuration.".LogWarning();
            }
        }

        protected override void OnDestroy()
        {
            _skillExecutor?.UnsubscribeFromEvents();
            Brain.OnPrecomputeCompleted -= HandlePrecomputeCompleted;
            base.OnDestroy();
        }

        #endregion





    }
}
