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

        /// <summary>
        /// Total battles won this playthrough.
        /// </summary>
        public int BattlesWon => _battlesWon;

        /// <summary>
        /// Total battles lost this playthrough.
        /// </summary>
        public int BattlesLost => _battlesLost;

        /// <summary>
        /// Total battles retreated from this playthrough.
        /// </summary>
        public int BattlesRetreated => _battlesRetreated;

        /// <summary>
        /// Total battles fought this playthrough.
        /// </summary>
        public int TotalBattles => _battlesWon + _battlesLost + _battlesRetreated;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor for dependency injection (used in tests).
        /// </summary>
        public void Initialize(GamewideContextBrain gamewideContextBrain, BattleBrain battleBrain)
        {
            _gamewideContextBrain = gamewideContextBrain;
            _battleBrain = battleBrain;
        }

        protected override void Awake()
        {
            base.Awake(); // Calls parent Awake which gets Brain and subscribes

            // Use injected dependencies if available, otherwise get from components (Unity default)
            _gamewideContextBrain ??= GetComponent<GamewideContextBrain>();
            _battleBrain ??= GetComponent<BattleBrain>();
            _ltm = GetComponent<LongTermMemory>();

            // Load battle statistics from LTM
            LoadBattleOutcomeStatistics();
        }

        /// <summary>
        /// CharactersBrain uses Highest priority because it manages critical character state.
        /// This ensures character data is saved before UI tries to read it.
        /// </summary>
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

            // Respond to save requests triggered by roster mutations
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

            Debug.Log(
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
            Debug.Log(
                $"CharactersBrain: Recorded battle outcome {exitType}. Total: W{_battlesWon}/L{_battlesLost}/R{_battlesRetreated}"
            );
        }

        #endregion

        #region Battle Lifecycle Event Handlers

        private void HandleStartBattle()
        {
#if UNITY_EDITOR
            Debug.Log("CharactersBrain: Initializing battle statistics for all characters.");
#endif
            InitializeBattleStatistics();
        }

        private void HandleExitBattle(Combat.BattleExitType exitType)
        {
#if UNITY_EDITOR
            Debug.Log($"CharactersBrain: Handling battle exit with type: {exitType}");
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
                Debug.LogWarning(
                    "CharactersBrain: BattleBrain not found, cannot initialize battle statistics."
                );
                return;
            }

            var allCharacters = GetAllBattleCharacters();
            foreach (var instance in allCharacters)
            {
                instance?.RecordBattleStart();
            }

            Debug.Log(
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
            Debug.Log("CharactersBrain: Reset battle statistics for all characters.");
#endif
        }

        private void SaveBattleParticipantsProgress()
        {
            if (_gamewideContextBrain == null || _battleBrain == null)
            {
                Debug.LogWarning(
                    "CharactersBrain: Cannot save battle participants - required components not found."
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

            Debug.Log(
                $"CharactersBrain: Saved {savedCount} unique characters. {masteryCount} new mastery skills learned."
            );
        }

        private List<CharacterInstance> GetAllBattleCharacters()
        {
            var characters = new List<CharacterInstance>();

            if (_battleBrain == null)
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

        /// <summary>
        /// Record a kill for a character and publish event.
        /// </summary>
        public void RecordKill(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            character.RecordKill();
            _brain?.PublishCharacterKill(character);

            Debug.Log(
                $"{character.CharacterTemplate?.DisplayName} recorded a kill. Total kills: {character.TotalKills}"
            );
        }

        /// <summary>
        /// Increment combat count for a character (used for skill conditions).
        /// </summary>
        public void IncrementCombatCount(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            character.IncrementCombatCount();
        }

        /// <summary>
        /// Level up a character and publish the level up event.
        /// </summary>
        public void LevelUpCharacter(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            character.LevelUp();

            // Publish through Brain (centralized event system)
            _brain?.PublishCharacterLevelUp(character);
        }

        /// <summary>
        /// Change character's class and publish the class changed event.
        /// </summary>
        public bool ChangeCharacterClass(
            CharacterInstance character,
            CharacterClassData newClassData
        )
        {
            if (character == null || newClassData == null)
            {
                return false;
            }

            bool success = character.ChangeClass(newClassData);
            if (success)
            {
                // Publish through Brain (centralized event system)
                _brain?.PublishCharacterClassChanged(character);
            }

            return success;
        }

        /// <summary>
        /// Add experience to a character's experience rank and publish event.
        /// </summary>
        public void AddExperience(CharacterInstance character, string experienceTypeId, int amount)
        {
            if (character == null || string.IsNullOrEmpty(experienceTypeId))
            {
                return;
            }

            character.AddExperience(experienceTypeId, amount);
            _brain?.PublishExperienceGained(character, experienceTypeId, amount);

            Debug.Log(
                $"{character.CharacterTemplate?.DisplayName} gained {amount} {experienceTypeId} experience"
            );
        }

        #endregion

        #region Skill Management API

        /// <summary>
        /// Add a skill to a character and publish the learned skill event.
        /// </summary>
        public void LearnSkill(CharacterInstance character, Skill skill)
        {
            if (character == null || skill == null)
            {
                return;
            }

            character.AddSkill(skill);

            // Publish through Brain (centralized event system)
            _brain?.PublishCharacterLearnedSkill(character, skill);

            Debug.Log(
                $"{character.CharacterTemplate?.DisplayName} learned skill: {skill.SkillName}"
            );
        }

        /// <summary>
        /// Remove a skill from a character and publish the removed skill event.
        /// </summary>
        public void RemoveSkill(CharacterInstance character, SkillInstance skill)
        {
            if (character == null || skill == null)
            {
                return;
            }

            character.RemoveSkill(skill);
            _brain?.PublishCharacterRemovedSkill(character, skill.SkillTemplate);

            Debug.Log(
                $"{character.CharacterTemplate?.DisplayName} removed skill: {skill.SkillTemplate?.SkillName}"
            );
        }

        /// <summary>
        /// Equip a skill on a character and publish the equip event via Brain.
        /// This operation is not battle-specific and should go through CharactersBrain.
        /// </summary>
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

            Debug.Log($"CharactersBrain: Equipped skill {skill.SkillName} on {character.Id}");
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Unequip a skill on a character and publish the unequip event via Brain.
        /// </summary>
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
            Debug.Log($"CharactersBrain: Unequipped skill {skill.SkillName} on {character.Id}");
            return OperationResult.SuccessResult();
        }

        #endregion

        #region Support System API

        /// <summary>
        /// Increase support level between two characters and publish event.
        /// </summary>
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

            Debug.Log(
                $"Support increased between {character.CharacterTemplate?.DisplayName} and {targetCharacter.DisplayName}"
            );
        }

        /// <summary>
        /// Add a support relationship to a character and publish event.
        /// </summary>
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

            Debug.Log(
                $"CharactersBrain: Added support relationship for {template.Character.DisplayName} on {character.Id}"
            );
        }

        /// <summary>
        /// Remove a support relationship from a character and publish event.
        /// </summary>
        public void RemoveSupportRelationship(CharacterInstance character, CharacterData target)
        {
            if (character == null || target == null)
            {
                return;
            }

            character.RemoveSupportRelationship(target);
            _brain?.PublishSupportRelationshipRemoved(character, target);

            Debug.Log(
                $"CharactersBrain: Removed support relationship for {target.DisplayName} on {character.Id}"
            );
        }

        #endregion

        #region Recruitment System API

        /// <summary>
        /// Set whether a target character is recruitable by the source character.
        /// </summary>
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

            Debug.Log(
                $"CharactersBrain: Set recruitable override for {targetCharacter.DisplayName} to {isRecruitable} on {character.Id}"
            );
        }

        /// <summary>
        /// Set the base recruitment chance for a target character.
        /// </summary>
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

            Debug.Log(
                $"CharactersBrain: Set recruitment chance override for {targetCharacter.DisplayName} to {chance} on {character.Id}"
            );
        }

        /// <summary>
        /// Set the recruitment chance increase per conversation for a target character.
        /// </summary>
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

            Debug.Log(
                $"CharactersBrain: Set recruitment increase override for {targetCharacter.DisplayName} to {increase} on {character.Id}"
            );
        }

        /// <summary>
        /// Set whether a target character requires minimum support level for recruitment.
        /// </summary>
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

            Debug.Log(
                $"CharactersBrain: Set requires-min-support override for {targetCharacter.DisplayName} to {requiresMinSupportLevel} on {character.Id}"
            );
        }

        /// <summary>
        /// Clear all recruitment overrides for a target character.
        /// </summary>
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

            Debug.Log(
                $"CharactersBrain: Cleared recruitment overrides for {targetCharacter.DisplayName} on {character.Id}"
            );
        }

        /// <summary>
        /// Check if a target character is recruitable by the source character.
        /// </summary>
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

        /// <summary>
        /// Get the recruitment chance for a target character.
        /// </summary>
        public float GetCharacterRecruitmentChance(
            CharacterInstance character,
            CharacterData targetCharacter
        )
        {
            return character == null || targetCharacter == null
                ? 0f
                : character.GetCharacterRecruitmentChance(targetCharacter);
        }

        /// <summary>
        /// Get the recruitment chance increase per conversation for a target character.
        /// </summary>
        public float GetCharacterRecruitmentChanceIncreasePerConversation(
            CharacterInstance character,
            CharacterData targetCharacter
        )
        {
            return character == null || targetCharacter == null
                ? 0f
                : character.GetCharacterRecruitmentChanceIncreasePerConversation(targetCharacter);
        }

        /// <summary>
        /// Check if a target character requires minimum support level for recruitment.
        /// </summary>
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

        /// <summary>
        /// Save a specific character's progress immediately.
        /// </summary>
        public void SaveCharacterProgress(CharacterInstance character)
        {
            if (character?.CharacterTemplate?.IsUnique == true && _gamewideContextBrain != null)
            {
                _battleBrain.SaveUniqueCharacterProgress(character);
                Debug.Log(
                    $"CharactersBrain: Manually saved {character.CharacterTemplate.DisplayName}"
                );
            }
        }

        /// <summary>
        /// Saves all unique characters in the player roster.
        /// Call this whenever roster state changes outside of battle.
        /// </summary>
        public void SavePlayerRosterProgress()
        {
            if (_battleBrain?.PlayerTeamRoster == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("CharactersBrain: No player roster to save");
#endif
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

#if UNITY_EDITOR
            Debug.Log($"CharactersBrain: Saved {savedCount} unique characters from player roster");
#endif
        }

        #endregion

        #region Character Query API

        /// <summary>
        /// Get all currently active character instances.
        /// </summary>
        public List<CharacterInstance> GetAllActiveCharacters() =>
            _battleBrain?.GetAllActiveInstances() ?? new List<CharacterInstance>();

        /// <summary>
        /// Find a character instance by template.
        /// </summary>
        public CharacterInstance FindCharacterByTemplate(CharacterData template) =>
            _battleBrain?.FindInstanceByTemplate(template);

        #endregion
    }
}
