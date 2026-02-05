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
            _gamewideContextBrain = GetComponent<GamewideContextBrain>();
            _battleBrain = GetComponent<BattleBrain>();
        }

        private bool _migrationPerformed = false;

        private void Start()
        {
            // LongTermMemory is initialized in Brain.Awake(), safe to access here
            _ltm = GetComponent<LongTermMemory>();
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
        protected override void SubscribeToBrainEvents()
        {
            _brain.OnBattleStarted += HandleStartBattle;
            _brain.OnBattleCompleted += HandleExitBattle;
            _brain.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            _brain.OnEnemyTurnStarted += HandleEnemyTurnStarted;
            _brain.OnThirdPartyTurnStarted += HandleThirdPartyTurnStarted;
            _brain.OnSavePlayerRosterRequested += SavePlayerRosterProgress;
            _brain.OnLtmKeyCacheUpdated += HandleLtmKeyCacheUpdated;

            // Update weapon caches when inventory equipment changes
            _brain.OnItemEquipped += HandleItemEquipped;
            _brain.OnItemUnequipped += HandleItemUnequipped;
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

            _brain.OnItemEquipped -= HandleItemEquipped;
            _brain.OnItemUnequipped -= HandleItemUnequipped;
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
            TurnrootLogger.Log(
                $"CharactersBrain: Updated weapon cache for {character.Id} after equip."
            );
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
            TurnrootLogger.Log(
                $"CharactersBrain: Updated weapon cache for {character.Id} after unequip."
            );
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
                JsonUtility.ToJson(dto)
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
                TurnrootLogger.Log(
                    $"CharactersBrain: Migrated {migrated} DefaultStat keys to use FullName"
                );
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
    }
}
