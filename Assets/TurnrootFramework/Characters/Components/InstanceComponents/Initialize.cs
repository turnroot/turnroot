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
        [NonSerialized]
        public GameplayGeneralSettings settings;

        [field: NonSerialized]
        public bool NeedsPersist { get; set; } = false;

        [field: NonSerialized]
        public bool ClassRecoveryHandled { get; set; } = false;

        internal CharacterInstance(CharacterData template, bool useBattleModel = true)
        {
            _characterTemplate = template;
            _id = GenerateId(template);
            _useBattleModel = useBattleModel;
            settings = GameplayGeneralSettings.Instance;
            Initialize();
            GetAvailableWeapons();
        }

        private static string GenerateId(CharacterData template)
        {
            return template == null ? Guid.NewGuid().ToString()
                : template.IsUnique ? $"unique_{template.name}"
                : Guid.NewGuid().ToString();
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
                return OperationResult.Successful();
            }

            var result = ChangeClass(classToApply, applyClassChangeBonuses: false);
            if (result.Success)
            {
                ClassRecoveryHandled = true;
            }
            return result;
        }

        private CharacterClassData GetDefaultStartingClass()
        {
            if (settings == null)
            {
                settings = GameplayGeneralSettings.Instance;
            }
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
            EnsurePersistedInLtm();
            if (RangeWeaponsCache == null)
            {
                GetAvailableWeapons();
            }
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
            // Avoid repeated recoveries
            if (ClassRecoveryHandled)
            {
                return;
            }

            if (_currentClass != null)
            {
                if (_currentClass.ClassData != null)
                {
                    _currentClass.OnAfterDeserialize();
                }
                else
                {
                    // Current class instance exists but ClassData failed to deserialize
                    var recoveredClass =
                        _characterTemplate?.StartingClass ?? GetDefaultStartingClass();
                    if (recoveredClass != null)
                    {
                        var res = ChangeClass(recoveredClass, applyClassChangeBonuses: false);
                        var logLevel = res.Success
                            ? TurnrootLogger.LogLevel.Info
                            : TurnrootLogger.LogLevel.Warning;
                        var message = res.Success
                            ? $"Character {Id} recovered missing class by assigning {recoveredClass.Identity.ClassName} after recall."
                            : $"CharacterInstance.OnAfterDeserialize: Failed to recover class for {Id}: {res.ErrorMessage}";
                        TurnrootLogger.Log(message, logLevel);

                        ClassRecoveryHandled = true;

                        if (res.Success)
                        {
                            NeedsPersist = true;
                        }
                    }
                    else
                    {
                        TurnrootLogger.Log(
                            $"CharacterInstance.OnAfterDeserialize: No starting/default class available to recover for {Id}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                }
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
