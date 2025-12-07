using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages character lifecycle, progression, and statistics within the brain system.
    /// Handles battle statistics, mastery tracking, and character state management.
    /// In keeping with the farfalle architecture, events are propagated upwards to Brain,
    /// which then sends them out as needed.
    /// </summary>
    [RequireComponent(typeof(Brain))]
    [RequireComponent(typeof(GamewideContextBrain))]
    [RequireComponent(typeof(BattleBrain))]
    public class CharactersBrain : MonoBehaviour
    {
        private Brain _brain;
        private GamewideContextBrain _gamewideContextBrain;
        private BattleBrain _battleBrain;

        private void Awake()
        {
            _brain = GetComponent<Brain>();
            _gamewideContextBrain = GetComponent<GamewideContextBrain>();
            _battleBrain = GetComponent<BattleBrain>();

            Debug.Log("CharactersBrain Awake - subscribing to brain events.");
            SubscribeToBrainEvents();
        }

        public void SubscribeToBrainEvents()
        {
            _brain.OnStartBattle += HandleStartBattle;
            _brain.OnExitBattle += HandleExitBattle;
            _brain.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            _brain.OnEnemyTurnStarted += HandleEnemyTurnStarted;
            _brain.OnThirdPartyTurnStarted += HandleThirdPartyTurnStarted;
        }

        #region Battle Lifecycle Handlers

        private void HandleStartBattle()
        {
            Debug.Log("CharactersBrain: Initializing battle statistics for all characters.");
            InitializeBattleStatistics();
        }

        private void HandleExitBattle(Combat.BattleExitType exitType)
        {
            Debug.Log($"CharactersBrain: Handling battle exit with type: {exitType}");

            // Save character progression after battle (except on defeat)
            if (
                exitType == Combat.BattleExitType.Victory
                || exitType == Combat.BattleExitType.Bookmark
            )
            {
                SaveBattleParticipantsProgress();
            }

            // Reset per-battle statistics
            ResetBattleStatistics();
        }

        #endregion

        #region Turn Phase Handlers

        private void HandlePlayerTurnStarted()
        {
            IncrementTurnsAliveForFaction(CharacterWhich.ALLY, CharacterWhich.AVATAR);
        }

        private void HandleEnemyTurnStarted()
        {
            IncrementTurnsAliveForFaction(CharacterWhich.ENEMY);
        }

        private void HandleThirdPartyTurnStarted()
        {
            IncrementTurnsAliveForFaction(CharacterWhich.NPC);
        }

        private void IncrementTurnsAliveForFaction(params string[] factionTypes)
        {
            if (_battleBrain == null)
                return;

            // Get characters from appropriate battle rosters based on faction
            var characters = new List<CharacterInstance>();

            foreach (var factionType in factionTypes)
            {
                if (factionType == CharacterWhich.ALLY || factionType == CharacterWhich.AVATAR)
                {
                    if (_battleBrain.PlayerTeamRoster?.Instances != null)
                        characters.AddRange(_battleBrain.PlayerTeamRoster.Instances);
                }
                else if (factionType == CharacterWhich.ENEMY)
                {
                    if (_battleBrain.EnemyTeamRoster?.Instances != null)
                        characters.AddRange(_battleBrain.EnemyTeamRoster.Instances);
                }
                else if (factionType == CharacterWhich.NPC)
                {
                    if (_battleBrain.ThirdPartyTeamRoster?.Instances != null)
                        characters.AddRange(_battleBrain.ThirdPartyTeamRoster.Instances);
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

        /// <summary>
        /// Initialize battle statistics for all characters at battle start.
        /// </summary>
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
                if (instance != null)
                {
                    instance.RecordBattleStart();
                }
            }

            Debug.Log(
                $"CharactersBrain: Initialized battle statistics for {allCharacters.Count} characters."
            );
        }

        /// <summary>
        /// Reset per-battle statistics for all characters.
        /// </summary>
        private void ResetBattleStatistics()
        {
            if (_battleBrain == null)
                return;

            var allCharacters = GetAllBattleCharacters();
            foreach (var instance in allCharacters)
            {
                if (instance != null)
                {
                    instance.ResetBattleStats();
                }
            }

            Debug.Log("CharactersBrain: Reset battle statistics for all characters.");
        }

        /// <summary>
        /// Save character progression after battle completion.
        /// Persists unique characters to LongTermMemory and checks mastery conditions.
        /// </summary>
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
                    continue;

                // Increment battle count for current class
                character.CurrentClass?.IncrementBattleCount();

                // Check mastery conditions and learn skills
                if (character.CurrentClass?.CheckMasteryConditions(character) == true)
                {
                    masteryCount++;
                }

                // Save unique characters to LongTermMemory
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

        /// <summary>
        /// Get all characters currently in battle rosters.
        /// </summary>
        private List<CharacterInstance> GetAllBattleCharacters()
        {
            var characters = new List<CharacterInstance>();

            if (_battleBrain == null)
                return characters;

            if (_battleBrain.PlayerTeamRoster?.Instances != null)
                characters.AddRange(_battleBrain.PlayerTeamRoster.Instances);

            if (_battleBrain.EnemyTeamRoster?.Instances != null)
                characters.AddRange(_battleBrain.EnemyTeamRoster.Instances);

            if (_battleBrain.ThirdPartyTeamRoster?.Instances != null)
                characters.AddRange(_battleBrain.ThirdPartyTeamRoster.Instances);

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
                return;

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
                return;

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
                return;

            character.LevelUp();
            _brain?.PublishCharacterLevelUp(character);
        }

        /// <summary>
        /// Add a skill to a character and publish the learned skill event.
        /// </summary>
        public void LearnSkill(CharacterInstance character, Skill skill)
        {
            if (character == null || skill == null)
                return;

            character.AddSkill(skill);
            _brain?.PublishCharacterLearnedSkill(character, skill);

            Debug.Log(
                $"{character.CharacterTemplate?.DisplayName} learned skill: {skill.SkillName}"
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
                return false;

            bool success = character.ChangeClass(newClassData, meshRenderer);
            if (success)
            {
                _brain?.PublishCharacterClassChanged(character);
            }

            return success;
        }

        /// <summary>
        /// Get all currently active character instances.
        /// </summary>
        public List<CharacterInstance> GetAllActiveCharacters()
        {
            return _gamewideContextBrain?.GetAllActiveInstances() ?? new List<CharacterInstance>();
        }

        /// <summary>
        /// Find a character instance by template.
        /// </summary>
        public CharacterInstance FindCharacterByTemplate(CharacterData template)
        {
            return _gamewideContextBrain?.FindInstanceByTemplate(template);
        }

        #endregion

        private void OnDestroy()
        {
            if (_brain != null)
            {
                Debug.Log("CharactersBrain OnDestroy - unsubscribing from brain events.");
                _brain.OnStartBattle -= HandleStartBattle;
                _brain.OnExitBattle -= HandleExitBattle;
                _brain.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
                _brain.OnEnemyTurnStarted -= HandleEnemyTurnStarted;
                _brain.OnThirdPartyTurnStarted -= HandleThirdPartyTurnStarted;
            }
        }
    }
}
