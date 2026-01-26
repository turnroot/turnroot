using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages character lifecycle, progression, and statistics within the brain system.
    /// Handles battle statistics, mastery tracking, and character state management.
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        #region Dependencies

        private GamewideContextBrain _gamewideContextBrain;
        private BattleBrain _battleBrain;
        private LongTermMemory _ltm;

        #endregion

        #region Initialization

        public void Initialize(GamewideContextBrain gamewideContextBrain, BattleBrain battleBrain)
        {
            _gamewideContextBrain = gamewideContextBrain;
            _battleBrain = battleBrain;
        }

        protected override void Awake()
        {
            base.Awake();
            _gamewideContextBrain ??= GetComponent<GamewideContextBrain>();
            _battleBrain ??= GetComponent<BattleBrain>();
        }

        private void Start()
        {
            _ltm = GetComponent<LongTermMemory>();
            LoadBattleOutcomeStatistics();
        }

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        #endregion

        #region Event Subscription

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnBattleStarted += HandleStartBattle;
            _brain.OnBattleCompleted += HandleExitBattle;
            _brain.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            _brain.OnEnemyTurnStarted += HandleEnemyTurnStarted;
            _brain.OnThirdPartyTurnStarted += HandleThirdPartyTurnStarted;
            _brain.OnSavePlayerRosterRequested += SavePlayerRosterProgress;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnBattleStarted -= HandleStartBattle;
            _brain.OnBattleCompleted -= HandleExitBattle;
            _brain.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            _brain.OnEnemyTurnStarted -= HandleEnemyTurnStarted;
            _brain.OnThirdPartyTurnStarted -= HandleThirdPartyTurnStarted;
            _brain.OnSavePlayerRosterRequested -= SavePlayerRosterProgress;
        }

        #endregion

        #region Character Progression API

        public void RecordKill(CharacterInstance character)
        {
            if (!Validate(character))
            {
                return;
            }

            character.RecordKill();
            _brain?.PublishCharacterKill(character);
        }

        public void IncrementCombatCount(CharacterInstance character)
        {
            if (!Validate(character))
            {
                return;
            }

            character.IncrementCombatCount();
        }

        public void LevelUpCharacter(CharacterInstance character)
        {
            if (!Validate(character))
            {
                return;
            }

            var res = character.LevelUp();
            if (res.Success)
            {
                _brain?.PublishCharacterLevelUp(character);
            }
        }

        public bool ChangeCharacterClass(
            CharacterInstance character,
            CharacterClassData newClassData
        )
        {
            if (!Validate(character, newClassData))
            {
                return false;
            }

            bool success = character.ChangeClass(newClassData).Success;
            if (success)
            {
                _brain?.PublishCharacterClassChanged(character);
            }
            return success;
        }

        public void AddExperience(CharacterInstance character, string experienceTypeId, int amount)
        {
            if (!Validate(character) || string.IsNullOrEmpty(experienceTypeId))
            {
                return;
            }

            character.AddExperience(experienceTypeId, amount);
            _brain?.PublishExperienceGained(character, experienceTypeId, amount);
            TurnrootLogger.Log(
                $"{character.CharacterTemplate?.DisplayName} gained {amount} {experienceTypeId} XP"
            );
        }

        #endregion

        #region Save/Load API

        public void SavePlayerRosterProgress()
        {
            if (_battleBrain?.PlayerTeamRoster == null)
            {
                return;
            }

            int savedCount = 0;
            foreach (var character in _battleBrain.PlayerTeamRoster.Instances)
            {
                if (character?.CharacterTemplate?.IsUnique == true)
                {
                    _battleBrain.SaveUniqueCharacterProgress(character);
                    savedCount++;
                }
            }
        }

        #endregion

        #region Character Query API

        public List<CharacterInstance> GetAllActiveCharacters() =>
            _battleBrain?.GetAllActiveInstances() ?? new List<CharacterInstance>();

        public CharacterInstance FindCharacterByTemplate(CharacterData template) =>
            _battleBrain?.FindInstanceByTemplate(template);

        #endregion

        #region Validation Helpers

        private bool Validate(params object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj == null)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
