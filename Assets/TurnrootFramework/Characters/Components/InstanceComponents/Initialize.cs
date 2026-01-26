using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Skills;
using Turnroot.Utilities;

namespace Turnroot.Characters
{
    public partial class CharacterInstance : Serialization.IPostDeserialize, IHasStats
    {
        #region Initialization
        internal CharacterInstance(CharacterData template, bool useBattleModel = true)
        {
            _characterTemplate = template;
            _id = GenerateId(template);
            _useBattleModel = useBattleModel;
            Initialize();
        }

        private static string GenerateId(CharacterData template)
        {
            if (template == null)
            {
                return Guid.NewGuid().ToString();
            }

            return template.IsUnique ? $"unique_{template.name}" : Guid.NewGuid().ToString();
        }

        public static CharacterInstance Create(CharacterData template, bool useBattleModel = true)
        {
            if (template == null)
            {
                return null;
            }

            if (template.IsUnique)
            {
                var existing = UniqueInstanceRegistry.Get<CharacterInstance>(template);
                if (existing != null)
                {
                    return existing;
                }
            }

            var instance = new CharacterInstance(template, useBattleModel);
            if (template.IsUnique)
            {
                UniqueInstanceRegistry.Register(template, instance);
            }

            return instance;
        }

        private OperationResult Initialize()
        {
            if (_characterTemplate == null)
            {
                return OperationResult.Failure("CharacterTemplate is null.");
            }

            _currentLevel = _characterTemplate.Level;
            _currentExp = _characterTemplate.Exp;

            _runtimeBoundedStats = CharacterHelpers.CloneBoundedStats(
                _characterTemplate.BoundedStats
            );
            _runtimeUnboundedStats = CharacterHelpers.CloneUnboundedStats(
                _characterTemplate.UnboundedStats
            );

            // Repair missing stats (may create defaults and persist them to LTM)
            RepairMissingStats();

            var validation = ValidateRuntimeStatsComplete();
            if (!validation.Success)
            {
                return validation;
            }

#if UNITY_EDITOR
            ValidateStatsComplete();
#endif

            InitializeInventory();
            InitializeSupportRelationships();
            InitializeSkills();
            InitializeExperienceRanks();

            // Ensure any defaults we created are recorded in LTM so future loads reuse them
            EnsurePersistedInLtm();
            return InitializeClass();
        }

        private void InitializeInventory()
        {
            _inventoryInstance = new CharacterInventoryInstance();
            if (_characterTemplate.StartingInventory != null)
            {
                foreach (var slot in _characterTemplate.StartingInventory)
                {
                    _inventoryInstance.AddToInventory(new ObjectItemInstance(slot.Item));
                }
            }
        }

        private void InitializeSupportRelationships()
        {
            _supportRelationships = CharacterHelpers.CloneSupportRelationships(
                _characterTemplate.SupportRelationships,
                _characterTemplate
            );
        }

        private void InitializeSkills()
        {
            _skillInstances = new List<SkillInstance>();
            AddSkillsFromTemplates(_characterTemplate.Skills);
            AddSkillsFromTemplates(_characterTemplate.SpecialSkills);
        }

        private void AddSkillsFromTemplates(List<Skill> skillTemplates)
        {
            if (skillTemplates == null)
            {
                return;
            }

            foreach (var skill in skillTemplates.Where(s => s != null))
            {
                _skillInstances.Add(new SkillInstance(skill));
            }
        }

        private void InitializeExperienceRanks()
        {
            _experienceRanks = new List<ExperienceRankInstance>();
            if (_characterTemplate.ExperienceRanks != null)
            {
                foreach (
                    var expRank in _characterTemplate.ExperienceRanks.Where(e =>
                        e != null && !string.IsNullOrEmpty(e.ExperienceTypeId)
                    )
                )
                {
                    _experienceRanks.Add(new ExperienceRankInstance(expRank));
                }
            }
        }

        private OperationResult InitializeClass()
        {
            var classToApply = _characterTemplate.StartingClass ?? GetDefaultStartingClass();
            if (classToApply == null)
            {
                // Defer to deserialization-time handler to assign defaults if settings are not yet loaded.
                // Previously this returned Failure and caused higher-level recall/deserialize paths to abort
                // when GameSettings weren't available yet. Instead, log a warning and continue.
                TurnrootLogger.Log(
                    $"Character {Id} has no starting class and GameplayGeneralSettings.DefaultStartingClass is not set - deferring assignment",
                    TurnrootLogger.LogLevel.Warning
                );
                return OperationResult.Successful();
            }

            var result = ChangeClass(classToApply, applyClassChangeBonuses: false);
            if (result.Success)
            {
                TurnrootLogger.Log(
                    $"Character {Id} initialized with starting class {classToApply.Identity.ClassName}",
                    TurnrootLogger.LogLevel.Info
                );
            }
            return result;
        }

        private CharacterClassData GetDefaultStartingClass()
        {
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            return settings?.GetDefaultStartingClass();
        }
        #endregion

        #region Deserialization
        public void OnAfterDeserialize()
        {
            EnsureListsInitialized();
            RegisterUniqueInstance();
            HandleCurrentClass();
            RepairMissingStats();
            // Ensure instance/template defaults are recorded in LTM and LTM entries include any newly
            // repaired stats so future restores will be complete.
            EnsurePersistedInLtm();
        }

        private void EnsureListsInitialized()
        {
            _runtimeBoundedStats ??= new List<BoundedCharacterStat>();
            _runtimeUnboundedStats ??= new List<CharacterStat>();
            _supportRelationships ??= new List<SupportRelationshipInstance>();
            _skillInstances ??= new List<SkillInstance>();
            _inventoryInstance ??= new CharacterInventoryInstance();
            _experienceRanks ??= new List<ExperienceRankInstance>();
            _equippedClassHistory ??= new List<CharacterClassData>();
        }

        private void RegisterUniqueInstance()
        {
            if (_characterTemplate != null && _characterTemplate.IsUnique)
            {
                UniqueInstanceRegistry.Register(_characterTemplate, this);
            }
        }

        private void HandleCurrentClass()
        {
            if (_currentClass != null)
            {
                _currentClass.OnAfterDeserialize();
            }
            else if (_characterTemplate != null && _characterTemplate.StartingClass == null)
            {
                var defaultClass = GetDefaultStartingClass();
                if (defaultClass != null)
                {
                    var res = ChangeClass(defaultClass, applyClassChangeBonuses: false);
                    var logLevel = res.Success
                        ? TurnrootLogger.LogLevel.Info
                        : TurnrootLogger.LogLevel.Warning;
                    var message = res.Success
                        ? $"Character {Id} assigned default starting class {defaultClass.Identity.ClassName} after recall."
                        : $"CharacterInstance.OnAfterDeserialize: Failed to apply default starting class for {Id}: {res.ErrorMessage}";
                    TurnrootLogger.Log(message, logLevel);
                }
            }
        }
        #endregion
    }
}
