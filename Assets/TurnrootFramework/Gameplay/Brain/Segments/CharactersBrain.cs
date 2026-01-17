using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages character lifecycle, progression, and statistics within the brain system.
    /// Handles battle statistics, mastery tracking, and character state management.
    /// In keeping with the farfalle architecture, events are propagated upwards to Brain,
    /// which then sends them out as needed.
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    public class CharactersBrain : BrainComponent
    {
        #region Dependencies

        private GamewideContextBrain _gamewideContextBrain;
        private BattleBrain _battleBrain;
        private LongTermMemory _ltm;

        #endregion

        #region Battle Outcome Statistics

        private int _battlesWon;
        private int _battlesLost;
        private int _battlesRetreated;
        public int BattlesWon => _battlesWon;
        public int BattlesLost => _battlesLost;
        public int BattlesRetreated => _battlesRetreated;
        public int TotalBattles => _battlesWon + _battlesLost + _battlesRetreated;

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
            _ltm = GetComponent<LongTermMemory>();

            // Load battle statistics from LTM
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

        #region Battle Outcome Statistics Management

        private void LoadBattleOutcomeStatistics()
        {
            if (_ltm == null)
            {
                return;
            }

            _battlesWon = _ltm.RecallInt(LtmKeys.BattlesWon);
            if (_battlesWon < 0)
            {
                _battlesWon = 0;
            }

            _battlesLost = _ltm.RecallInt(LtmKeys.BattlesLost);
            if (_battlesLost < 0)
            {
                _battlesLost = 0;
            }

            _battlesRetreated = _ltm.RecallInt(LtmKeys.BattlesRetreated);
            if (_battlesRetreated < 0)
            {
                _battlesRetreated = 0;
            }

            TurnrootLogger.Log(
                $"CharactersBrain: Loaded battle statistics - Won: {_battlesWon}, Lost: {_battlesLost}, Retreated: {_battlesRetreated}"
            );
        }

        private void SaveBattleOutcomeStatistics()
        {
            if (_ltm == null)
            {
                return;
            }

            _ltm.RememberInt(LtmKeys.BattlesWon, _battlesWon);
            _ltm.RememberInt(LtmKeys.BattlesLost, _battlesLost);
            _ltm.RememberInt(LtmKeys.BattlesRetreated, _battlesRetreated);
            _ltm.RememberInt(LtmKeys.TotalBattles, TotalBattles);
        }

        private void RecordBattleOutcome(Combat.BattleExitType exitType)
        {
            switch (exitType)
            {
                case Combat.BattleExitType.Victory:
                    _battlesWon++;
                    break;
                case Combat.BattleExitType.Defeat:
                    _battlesLost++;
                    break;
                case Combat.BattleExitType.Retreat:
                    _battlesRetreated++;
                    break;
                // Bookmark doesn't count as a completed battle outcome
            }

            SaveBattleOutcomeStatistics();
            TurnrootLogger.Log(
                $"CharactersBrain: Recorded battle outcome {exitType}. Total: W{_battlesWon}/L{_battlesLost}/R{_battlesRetreated}"
            );
        }

        #endregion

        #region Battle Lifecycle Event Handlers

        private void HandleStartBattle()
        {
#if UNITY_EDITOR
            TurnrootLogger.Log(
                "CharactersBrain: Initializing battle statistics for all characters."
            );
#endif
            InitializeBattleStatistics();
        }

        private void HandleExitBattle(Combat.BattleExitType exitType)
        {
#if UNITY_EDITOR
            TurnrootLogger.Log($"CharactersBrain: Handling battle exit with type: {exitType}");
#endif

            // Record the battle outcome
            RecordBattleOutcome(exitType);

            if (exitType is Combat.BattleExitType.Victory or Combat.BattleExitType.Bookmark)
            {
                SaveBattleParticipantsProgress();
            }

            ResetBattleStatistics();
        }

        #endregion

        #region Turn Phase Event Handlers

        private void HandlePlayerTurnStarted(CharacterInstance character) =>
            IncrementTurnsAliveForFaction(CharacterWhich.ALLY, CharacterWhich.AVATAR);

        private void HandleEnemyTurnStarted() =>
            IncrementTurnsAliveForFaction(CharacterWhich.ENEMY);

        private void HandleThirdPartyTurnStarted() =>
            IncrementTurnsAliveForFaction(CharacterWhich.NPC);

        private void IncrementTurnsAliveForFaction(params string[] factionTypes)
        {
            if (_battleBrain == null)
            {
                return;
            }

            var characters = new List<CharacterInstance>();

            foreach (var factionType in factionTypes)
            {
                if (factionType is CharacterWhich.ALLY or CharacterWhich.AVATAR)
                {
                    if (_battleBrain.PlayerTeamRoster?.Instances != null)
                    {
                        characters.AddRange(_battleBrain.PlayerTeamRoster.Instances);
                    }
                }
                else if (factionType == CharacterWhich.ENEMY)
                {
                    if (_battleBrain.EnemyTeamRoster?.Instances != null)
                    {
                        characters.AddRange(_battleBrain.EnemyTeamRoster.Instances);
                    }
                }
                else if (factionType == CharacterWhich.NPC)
                {
                    if (_battleBrain.ThirdPartyTeamRoster?.Instances != null)
                    {
                        characters.AddRange(_battleBrain.ThirdPartyTeamRoster.Instances);
                    }
                }
            }

            foreach (var instance in characters)
            {
                if (instance != null)
                {
                    instance.IncrementTurnsAlive();
                    instance.ResetTurnStats();
                }
            }
        }

        #endregion

        #region Battle Statistics Management

        private void InitializeBattleStatistics()
        {
            if (_battleBrain == null)
            {
                TurnrootLogger.Log(
                    "CharactersBrain: BattleBrain not found, cannot initialize battle statistics.",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var allCharacters = GetAllBattleCharacters();
            foreach (var instance in allCharacters)
            {
                instance?.RecordBattleStart();
            }

            TurnrootLogger.Log(
                $"CharactersBrain: Initialized battle statistics for {allCharacters.Count} characters."
            );
        }

        private void ResetBattleStatistics()
        {
            if (_battleBrain == null)
            {
                return;
            }

            var allCharacters = GetAllBattleCharacters();
            foreach (var instance in allCharacters)
            {
                instance?.ResetBattleStats();
            }

#if UNITY_EDITOR
            TurnrootLogger.Log("CharactersBrain: Reset battle statistics for all characters.");
#endif
        }

        private void SaveBattleParticipantsProgress()
        {
            if (_gamewideContextBrain == null || _battleBrain == null)
            {
                TurnrootLogger.Log(
                    "CharactersBrain: Cannot save battle participants - required components not found.",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var allCharacters = GetAllBattleCharacters();
            int savedCount = 0;
            int masteryCount = 0;

            foreach (var character in allCharacters)
            {
                if (character == null)
                {
                    continue;
                }

                character.CurrentClass?.IncrementBattleCount();

                if (character.CharacterTemplate?.IsUnique == true)
                {
                    _battleBrain.SaveUniqueCharacterProgress(character);
                    savedCount++;
                }
            }

            TurnrootLogger.Log(
                $"CharactersBrain: Saved {savedCount} unique characters. {masteryCount} new mastery skills learned."
            );
        }

        private OperationResult ValidateBattleBrainPresence()
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharactersBrain",
                out var missing,
                (_battleBrain, nameof(_battleBrain))
            );

            if (!ok)
            {
                var msg =
                    $"CharactersBrain validation failed: missing {string.Join(", ", missing)}";
                return OperationResult.Failure(msg);
            }

            return OperationResult.SuccessResult();
        }

        private List<CharacterInstance> GetAllBattleCharacters()
        {
            var characters = new List<CharacterInstance>();

            var validateRes = ValidateBattleBrainPresence();
            if (!validateRes.Success)
            {
                return characters;
            }

            if (_battleBrain.PlayerTeamRoster?.Instances != null)
            {
                characters.AddRange(_battleBrain.PlayerTeamRoster.Instances);
            }

            if (_battleBrain.EnemyTeamRoster?.Instances != null)
            {
                characters.AddRange(_battleBrain.EnemyTeamRoster.Instances);
            }

            if (_battleBrain.ThirdPartyTeamRoster?.Instances != null)
            {
                characters.AddRange(_battleBrain.ThirdPartyTeamRoster.Instances);
            }

            return characters;
        }

        #endregion

        #region Character Progression API

        public void RecordKill(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            character.RecordKill();
            _brain?.PublishCharacterKill(character);
        }

        public void IncrementCombatCount(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            character.IncrementCombatCount();
        }

        public void LevelUpCharacter(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            character.LevelUp();
            _brain?.PublishCharacterLevelUp(character);
        }

        public bool ChangeCharacterClass(
            CharacterInstance character,
            CharacterClassData newClassData
        )
        {
            if (character == null || newClassData == null)
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
            if (character == null || string.IsNullOrEmpty(experienceTypeId))
            {
                return;
            }

            character.AddExperience(experienceTypeId, amount);
            _brain?.PublishExperienceGained(character, experienceTypeId, amount);

            TurnrootLogger.Log(
                $"{character.CharacterTemplate?.DisplayName} gained {amount} {experienceTypeId} experience"
            );
        }

        #endregion

        #region Skill Management API
        public void LearnSkill(CharacterInstance character, Skill skill)
        {
            if (character == null || skill == null)
            {
                return;
            }

            character.AddSkill(skill);

            _brain?.PublishCharacterLearnedSkill(character, skill);

            TurnrootLogger.Log(
                $"{character.CharacterTemplate?.DisplayName} learned skill: {skill.SkillName}"
            );
        }

        public void RemoveSkill(CharacterInstance character, SkillInstance skill)
        {
            if (character == null || skill == null)
            {
                return;
            }

            character.RemoveSkill(skill);
            _brain?.PublishCharacterRemovedSkill(character, skill.SkillTemplate);

            TurnrootLogger.Log(
                $"{character.CharacterTemplate?.DisplayName} removed skill: {skill.SkillTemplate?.SkillName}"
            );
        }

        public OperationResult EquipSkill(CharacterInstance character, Skill skill)
        {
            if (character == null || skill == null)
            {
                return OperationResult.Failure("Invalid character or skill.");
            }

            var instance = character.SkillInstances?.Find(s => s.SkillTemplate == skill);
            if (instance == null)
            {
                return OperationResult.Failure("Skill not found on character.");
            }

            instance.SetEquipped(true, character);
            _brain?.PublishSkillEquipped(character, skill);

            TurnrootLogger.Log(
                $"CharactersBrain: Equipped skill {skill.SkillName} on {character.Id}"
            );
            return OperationResult.SuccessResult();
        }

        public OperationResult UnequipSkill(CharacterInstance character, Skill skill)
        {
            if (character == null || skill == null)
            {
                return OperationResult.Failure("Invalid character or skill.");
            }

            var instance = character.SkillInstances?.Find(s => s.SkillTemplate == skill);
            if (instance == null)
            {
                return OperationResult.Failure("Skill not found on character.");
            }

            instance.SetEquipped(false, character);
            _brain?.PublishSkillUnequipped(character, skill);
            TurnrootLogger.Log(
                $"CharactersBrain: Unequipped skill {skill.SkillName} on {character.Id}"
            );
            return OperationResult.SuccessResult();
        }

        #endregion

        #region Support System API

        public void IncreaseSupport(
            CharacterInstance character,
            CharacterData targetCharacter,
            int amount
        )
        {
            if (character == null || targetCharacter == null)
            {
                return;
            }

            character.IncreaseSupport(targetCharacter, amount);
            _brain?.PublishSupportIncreased(character, targetCharacter, amount);

            TurnrootLogger.Log(
                $"Support increased between {character.CharacterTemplate?.DisplayName} and {targetCharacter.DisplayName}"
            );
        }

        public void AddSupportRelationship(
            CharacterInstance character,
            Turnroot.Characters.Components.Support.SupportRelationship template
        )
        {
            if (character == null || template == null || template.Character == null)
            {
                return;
            }

            character.AddSupportRelationship(template);
            var added = character.GetSupportRelationship(template.Character);
            if (added != null)
            {
                _brain?.PublishSupportRelationshipAdded(character, added);
            }

            TurnrootLogger.Log(
                $"CharactersBrain: Added support relationship for {template.Character.DisplayName} on {character.Id}"
            );
        }

        public void RemoveSupportRelationship(CharacterInstance character, CharacterData target)
        {
            if (character == null || target == null)
            {
                return;
            }

            character.RemoveSupportRelationship(target);
            _brain?.PublishSupportRelationshipRemoved(character, target);

            TurnrootLogger.Log(
                $"CharactersBrain: Removed support relationship for {target.DisplayName} on {character.Id}"
            );
        }

        #endregion

        #region Recruitment System API
        public void SetCharacterRecruitableOverride(
            CharacterInstance character,
            CharacterData targetCharacter,
            bool isRecruitable
        )
        {
            if (character == null || targetCharacter == null)
            {
                return;
            }

            character.SetCharacterRecruitable(targetCharacter, isRecruitable);
            _brain?.PublishCharacterRecruitableChanged(character, targetCharacter, isRecruitable);

            TurnrootLogger.Log(
                $"CharactersBrain: Set recruitable override for {targetCharacter.DisplayName} to {isRecruitable} on {character.Id}"
            );
        }

        public void SetCharacterRecruitmentChanceOverride(
            CharacterInstance character,
            CharacterData targetCharacter,
            float chance
        )
        {
            if (character == null || targetCharacter == null)
            {
                return;
            }

            character.SetCharacterRecruitmentChance(targetCharacter, chance);
            _brain?.PublishCharacterRecruitmentChanceChanged(character, targetCharacter, chance);

            TurnrootLogger.Log(
                $"CharactersBrain: Set recruitment chance override for {targetCharacter.DisplayName} to {chance} on {character.Id}"
            );
        }

        public void SetCharacterRecruitmentChanceIncreaseOverride(
            CharacterInstance character,
            CharacterData targetCharacter,
            float increase
        )
        {
            if (character == null || targetCharacter == null)
            {
                return;
            }

            character.SetCharacterRecruitmentChanceIncreasePerConversation(
                targetCharacter,
                increase
            );
            _brain?.PublishCharacterRecruitmentChanceIncreaseChanged(
                character,
                targetCharacter,
                increase
            );

            TurnrootLogger.Log(
                $"CharactersBrain: Set recruitment increase override for {targetCharacter.DisplayName} to {increase} on {character.Id}"
            );
        }

        public void SetCharacterRequiresMinSupportLevelOverride(
            CharacterInstance character,
            CharacterData targetCharacter,
            bool requiresMinSupportLevel
        )
        {
            if (character == null || targetCharacter == null)
            {
                return;
            }

            character.SetCharacterRequiresMinSupportLevel(targetCharacter, requiresMinSupportLevel);
            _brain?.PublishCharacterRequiresMinSupportLevelChanged(
                character,
                targetCharacter,
                requiresMinSupportLevel
            );

            TurnrootLogger.Log(
                $"CharactersBrain: Set requires-min-support override for {targetCharacter.DisplayName} to {requiresMinSupportLevel} on {character.Id}"
            );
        }

        public void ClearCharacterRecruitmentOverrides(
            CharacterInstance character,
            CharacterData targetCharacter
        )
        {
            if (character == null || targetCharacter == null)
            {
                return;
            }

            character.ClearRecruitmentOverrides(targetCharacter);
            _brain?.PublishCharacterRecruitmentOverridesCleared(character, targetCharacter);

            TurnrootLogger.Log(
                $"CharactersBrain: Cleared recruitment overrides for {targetCharacter.DisplayName} on {character.Id}"
            );
        }

        public bool IsCharacterRecruitable(
            CharacterInstance character,
            CharacterData targetCharacter
        )
        {
            if (character == null || targetCharacter == null)
            {
                return false;
            }

            var result = character.IsCharacterRecruitable(targetCharacter);
            return result;
        }

        public float GetCharacterRecruitmentChance(
            CharacterInstance character,
            CharacterData targetCharacter
        )
        {
            return character == null || targetCharacter == null
                ? 0f
                : character.GetCharacterRecruitmentChance(targetCharacter);
        }

        public float GetCharacterRecruitmentChanceIncreasePerConversation(
            CharacterInstance character,
            CharacterData targetCharacter
        )
        {
            return character == null || targetCharacter == null
                ? 0f
                : character.GetCharacterRecruitmentChanceIncreasePerConversation(targetCharacter);
        }

        public bool GetCharacterRequiresMinSupportLevel(
            CharacterInstance character,
            CharacterData targetCharacter
        )
        {
            return character == null || targetCharacter == null
                ? false
                : character.GetCharacterRequiresMinSupportLevel(targetCharacter);
        }

        #endregion

        #region Save/Load API

        public void SaveCharacterProgress(CharacterInstance character)
        {
            if (character?.CharacterTemplate?.IsUnique == true && _gamewideContextBrain != null)
            {
                _battleBrain.SaveUniqueCharacterProgress(character);
                TurnrootLogger.Log(
                    $"CharactersBrain: Manually saved {character.CharacterTemplate.DisplayName}"
                );
            }
        }

        private OperationResult TrySavePlayerRosterProgress()
        {
            if (_battleBrain?.PlayerTeamRoster == null)
            {
                return OperationResult.Failure("No player roster to save");
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
            return OperationResult.SuccessResult();
        }

        public void SavePlayerRosterProgress() => TrySavePlayerRosterProgress();

        #endregion

        #region Character Query API
        public List<CharacterInstance> GetAllActiveCharacters() =>
            _battleBrain?.GetAllActiveInstances() ?? new List<CharacterInstance>();

        public CharacterInstance FindCharacterByTemplate(CharacterData template) =>
            _battleBrain?.FindInstanceByTemplate(template);

        #endregion
    }
}
