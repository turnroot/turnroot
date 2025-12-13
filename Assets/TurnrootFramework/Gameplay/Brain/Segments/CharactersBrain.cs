using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Services;
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
        private static class LtmKeys
        {
            public const string BattlesWon = "CharactersBrain.BattlesWon";
            public const string BattlesLost = "CharactersBrain.BattlesLost";
            public const string BattlesRetreated = "CharactersBrain.BattlesRetreated";
            public const string TotalBattles = "CharactersBrain.TotalBattles";
        }

        private GamewideContextBrain _gamewideContextBrain;
        private BattleBrain _battleBrain;
        private LongTermMemory _ltm;

        // Runtime battle statistics
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

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnStartBattle += HandleStartBattle;
            _brain.OnExitBattle += HandleExitBattle;
            _brain.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            _brain.OnEnemyTurnStarted += HandleEnemyTurnStarted;
            _brain.OnThirdPartyTurnStarted += HandleThirdPartyTurnStarted;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnStartBattle -= HandleStartBattle;
            _brain.OnExitBattle -= HandleExitBattle;
            _brain.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            _brain.OnEnemyTurnStarted -= HandleEnemyTurnStarted;
            _brain.OnThirdPartyTurnStarted -= HandleThirdPartyTurnStarted;
        }

        #region Battle Outcome Statistics

        private void LoadBattleOutcomeStatistics()
        {
            if (_ltm == null)
            {
                return;
            }

            _battlesWon = _ltm.RecallInt(LtmKeys.BattlesWon);
            if (_battlesWon < 0)
                _battlesWon = 0;

            _battlesLost = _ltm.RecallInt(LtmKeys.BattlesLost);
            if (_battlesLost < 0)
                _battlesLost = 0;

            _battlesRetreated = _ltm.RecallInt(LtmKeys.BattlesRetreated);
            if (_battlesRetreated < 0)
                _battlesRetreated = 0;

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

        #region Battle Lifecycle Handlers

        private void HandleStartBattle()
        {
            Debug.Log("CharactersBrain: Initializing battle statistics for all characters.");
            InitializeBattleStatistics();
        }

        private void HandleExitBattle(Combat.BattleExitType exitType)
        {
            Debug.Log($"CharactersBrain: Handling battle exit with type: {exitType}");

            // Record the battle outcome
            RecordBattleOutcome(exitType);

            if (
                exitType == Combat.BattleExitType.Victory
                || exitType == Combat.BattleExitType.Bookmark
            )
            {
                SaveBattleParticipantsProgress();
            }

            ResetBattleStatistics();
        }

        #endregion

        #region Turn Phase Handlers

        private void HandlePlayerTurnStarted() =>
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
                if (factionType == CharacterWhich.ALLY || factionType == CharacterWhich.AVATAR)
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

            Debug.Log("CharactersBrain: Reset battle statistics for all characters.");
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

                if (character.CurrentClass?.CheckMasteryConditions(character) == true)
                {
                    masteryCount++;
                }

                if (character.CharacterTemplate?.IsUnique == true)
                {
                    _gamewideContextBrain.SaveUniqueCharacterProgress(character);
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

        #region Public API

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
        /// Save a specific character's progress immediately.
        /// </summary>
        public void SaveCharacterProgress(CharacterInstance character)
        {
            if (character?.CharacterTemplate?.IsUnique == true && _gamewideContextBrain != null)
            {
                _gamewideContextBrain.SaveUniqueCharacterProgress(character);
                Debug.Log(
                    $"CharactersBrain: Manually saved {character.CharacterTemplate.DisplayName}"
                );
            }
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
        /// Change character's class and publish the class changed event.
        /// </summary>
        public bool ChangeCharacterClass(
            CharacterInstance character,
            CharacterClassData newClassData,
            MeshRenderer meshRenderer = null
        )
        {
            if (character == null || newClassData == null)
            {
                return false;
            }

            bool success = character.ChangeClass(newClassData, meshRenderer);
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
        /// Get all currently active character instances.
        /// </summary>
        public List<CharacterInstance> GetAllActiveCharacters() =>
            _gamewideContextBrain?.GetAllActiveInstances() ?? new List<CharacterInstance>();

        /// <summary>
        /// Find a character instance by template.
        /// </summary>
        public CharacterInstance FindCharacterByTemplate(CharacterData template) =>
            _gamewideContextBrain?.FindInstanceByTemplate(template);

        #endregion
    }
}
