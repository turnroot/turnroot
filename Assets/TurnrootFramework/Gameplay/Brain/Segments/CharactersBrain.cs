using System;
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
    [RequireComponent(typeof(BattleBrain))]
    [RequireComponent(typeof(GamewideContextBrain))]
    public partial class CharactersBrain : BrainComponent
    {
        #region Dependencies
        private GamewideContextBrain _gamewideContextBrain;
        private BattleBrain _battleBrain;
        private LongTermMemory _ltm;
        private GameDate gameDate;
        #endregion

        #region Runtime State
        // results of birthday checks after the last scene change
        public bool[] BirthdayChecks { get; private set; }
        #endregion

        #region Initialization

        protected override void Awake()
        {
            base.Awake();
            _gamewideContextBrain = GetComponent<GamewideContextBrain>();
            _battleBrain = GetComponent<BattleBrain>();
        }

        private bool _migrationPerformed = false;

        private void Start() => _ltm = GetComponent<LongTermMemory>();

        private void InitializeLTMDependentData()
        {
            LoadBattleOutcomeStatistics();

            var keys = _ltm?.RecallKeysByPrefix("DefaultStat");
            if (keys != null && keys.Count > 0)
            {
                MigrateDefaultStatKeysIfNeeded();
            }
        }

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;
        #endregion

        #region Event Subscription
        partial void SubscribeBlacksmithItemEvents();

        partial void UnsubscribeBlacksmithItemEvents();

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnBattleStarted += HandleStartBattle;
            _brain.OnBattleCompleted += HandleExitBattle;
            _brain.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            _brain.OnEnemyTurnStarted += HandleEnemyTurnStarted;
            _brain.OnThirdPartyTurnStarted += HandleThirdPartyTurnStarted;
            _brain.OnSavePlayerRosterRequested += SavePlayerRosterProgress;
            _brain.OnLtmKeyCacheUpdated += HandleLtmKeyCacheUpdated;
            _brain.OnLongTermMemoryInitialized += InitializeLTMDependentData;
            _brain.OnItemEquipped += HandleItemEquipped;
            _brain.OnItemUnequipped += HandleItemUnequipped;
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnHubCharacterInteracted += HandleHubCharacterInteracted;
            _brain.OnHubCharacterTalked += HandleHubCharacterTalked;
            SubscribeBlacksmithItemEvents();
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnBattleStarted -= HandleStartBattle;
            _brain.OnBattleCompleted -= HandleExitBattle;
            _brain.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            _brain.OnEnemyTurnStarted -= HandleEnemyTurnStarted;
            _brain.OnThirdPartyTurnStarted -= HandleThirdPartyTurnStarted;
            _brain.OnSavePlayerRosterRequested -= SavePlayerRosterProgress;
            _brain.OnLtmKeyCacheUpdated -= HandleLtmKeyCacheUpdated;
            _brain.OnLongTermMemoryInitialized -= InitializeLTMDependentData;
            _brain.OnItemEquipped -= HandleItemEquipped;
            _brain.OnItemUnequipped -= HandleItemUnequipped;
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnHubCharacterInteracted -= HandleHubCharacterInteracted;
            _brain.OnHubCharacterTalked -= HandleHubCharacterTalked;
            UnsubscribeBlacksmithItemEvents();
        }
        #endregion

        #region Inventory Event Handlers
        private void HandleItemEquipped(
            CharacterInstance character,
            Objects.ObjectItemInstance item
        )
        {
            if (character == null)
            {
                return;
            }
            character.GetAvailableWeapons();

            $"CharactersBrain: Updated weapon cache for {character.Id} after equip.".LogInfo();
        }

        private void HandleItemUnequipped(
            CharacterInstance character,
            Objects.ObjectItemInstance item
        )
        {
            if (character == null)
            {
                return;
            }
            character.GetAvailableWeapons();

            $"CharactersBrain: Updated weapon cache for {character.Id} after unequip.".LogInfo();
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
            Brain.PublishCharacterKill(character);
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
                Brain.PublishCharacterLevelUp(character);
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
                Brain.PublishCharacterClassChanged(character);
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
            Brain.PublishExperienceGained(character, experienceTypeId, amount);

            $"{character.CharacterTemplate?.DisplayName} gained {amount} {experienceTypeId} XP".LogInfo();
        }
        #endregion

        #region Save/Load API
        public void SavePlayerRosterProgress()
        {
            if (_battleBrain.PlayerTeamRoster == null)
            {
                return;
            }

            foreach (var character in _battleBrain.PlayerTeamRoster.Instances)
            {
                if (character?.CharacterTemplate?.IsUnique == true)
                {
                    _battleBrain.SaveUniqueCharacterProgress(character);
                }
            }
        }
        #endregion

        #region LTM Template Defaults API
        [Serializable]
        private class StatDto
        {
            public float max,
                current,
                min;
        }

        public bool TryGetTemplateBoundedDefault(
            string templateFullName,
            Characters.Stats.BoundedStatType type,
            out (float max, float current, float min) values
        )
        {
            values = default;
            if (_ltm == null || string.IsNullOrEmpty(templateFullName))
            {
                return false;
            }

            var json = RecallWithFallback(
                $"DefaultStat/Template/{templateFullName}/Bounded/{type}",
                $"DefaultStat/Bounded/{type}"
            );
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            json = Brain.DecodeString(json);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            var dto = JsonUtility.FromJson<StatDto>(json);
            if (dto == null)
            {
                return false;
            }

            values = (dto.max, dto.current, dto.min);
            return true;
        }

        public bool TryGetTemplateUnboundedDefault(
            string templateFullName,
            Characters.Stats.UnboundedStatType type,
            out float value
        )
        {
            value = 0f;
            if (_ltm == null || string.IsNullOrEmpty(templateFullName))
            {
                return false;
            }

            var data = RecallWithFallback(
                $"DefaultStat/Template/{templateFullName}/Unbounded/{type}",
                $"DefaultStat/Unbounded/{type}"
            );
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            if (float.TryParse(data, out value))
            {
                return true;
            }

            var dto = JsonUtility.FromJson<StatDto>(data);
            if (dto != null)
            {
                value = dto.current;
                return true;
            }

            return false;
        }

        public void SaveTemplateBoundedDefault(
            string templateFullName,
            Characters.Stats.BoundedStatType type,
            (float max, float current, float min) values
        )
        {
            if (_ltm == null || string.IsNullOrEmpty(templateFullName))
            {
                return;
            }

            var dto = new StatDto
            {
                max = values.max,
                current = values.current,
                min = values.min,
            };
            _ltm.Remember(
                $"DefaultStat/Template/{templateFullName}/Bounded/{type}",
                Brain.EncodeString(JsonUtility.ToJson(dto))
            );
        }

        public void SaveTemplateUnboundedDefault(
            string templateFullName,
            Characters.Stats.UnboundedStatType type,
            float value
        )
        {
            if (_ltm == null || string.IsNullOrEmpty(templateFullName))
            {
                return;
            }

            _ltm.Remember(
                $"DefaultStat/Template/{templateFullName}/Unbounded/{type}",
                value.ToString()
            );
        }

        public List<string> GetDefaultStatKeys() =>
            _ltm?.RecallKeysByPrefix("DefaultStat") ?? new List<string>();

        private string RecallWithFallback(string primaryKey, string fallbackKey)
        {
            var result = _ltm?.Recall(primaryKey);
            return !string.IsNullOrEmpty(result) ? result : _ltm?.Recall(fallbackKey);
        }
        #endregion

        #region LTM Migration Helpers
        private void HandleLtmKeyCacheUpdated(int version) => MigrateDefaultStatKeysIfNeeded();

        private void MigrateDefaultStatKeysIfNeeded()
        {
            if (_ltm == null || _migrationPerformed)
            {
                return;
            }

            var keys = _ltm.RecallKeysByPrefix("DefaultStat");
            if (keys == null || keys.Count == 0)
            {
                _migrationPerformed = true;
                return;
            }

            var templates = Resources.FindObjectsOfTypeAll<CharacterData>();
            var migrated = 0;

            foreach (var key in keys)
            {
                var parts = key.Split('/');
                if (parts.Length < 5 || parts[0] != "DefaultStat" || parts[1] != "Template")
                {
                    continue;
                }

                var oldId = parts[2];
                foreach (var tmpl in templates)
                {
                    if (tmpl == null || tmpl.name != oldId || tmpl.FullName == oldId)
                    {
                        continue;
                    }

                    var newKey = $"DefaultStat/Template/{tmpl.FullName}/{parts[3]}/{parts[4]}";
                    if (string.IsNullOrEmpty(_ltm.Recall(newKey)))
                    {
                        var val = _ltm.Recall(key);
                        if (!string.IsNullOrEmpty(val))
                        {
                            _ltm.Remember(newKey, val);
                            migrated++;
                        }
                    }
                }
            }

            if (migrated > 0)
            {
                $"CharactersBrain: Migrated {migrated} DefaultStat keys to use FullName".LogInfo();
            }

            _migrationPerformed = true;
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

        #region State Change Handler
        private void HandleStateChanged(BrainState state) { }

        public void CheckBirthdays()
        {
            gameDate = _gamewideContextBrain.GetCurrentGameDate();
            var checkRoster = _gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            BirthdayChecks = new bool[checkRoster.characters.Length];
            for (int i = 0; i < checkRoster.characters.Length; i++)
            {
                var placement = checkRoster.characters[i];
                if (placement == null || placement.CharacterData == null)
                {
                    $"CharactersBrain: Skipping birthday check for null character data at index {i}.".LogWarning();
                    continue;
                }
                var data = placement.CharacterData;
                if (data.BirthdayMonth != gameDate.month)
                {
                    continue;
                }
                if (data.BirthdayDay < gameDate.day || data.BirthdayDay >= gameDate.day + 7)
                {
                    continue;
                }
                BirthdayChecks[i] = true;
                // try to find a matching runtime instance; if none exists we still
                // notify with null so listeners can handle it gracefully
                var inst = FindCharacterByTemplate(data);
                _brain.PublishCharacterBirthdayThisWeek(inst, gameDate);
                $"CharactersBrain: Published birthday event for character at index {i} with template {data.name}.".LogInfo();
            }
        }
        #endregion
    }
}
