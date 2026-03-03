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
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Runtime instance of a character containing all state and behavior.
    /// This partial class contains initialization and deserialization logic.
    /// </summary>
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
            var validation = OperationResultGuards.RequireNotNull(
                _characterTemplate,
                nameof(_characterTemplate)
            );
            if (!validation.Success)
            {
                return validation;
            }

            // derive starting level/exp from the template's bounded stat entries
            _currentLevel = 1;
            _currentExp = 0;
            if (_characterTemplate.BoundedStats != null)
            {
                var lvlTemplate = _characterTemplate.BoundedStats.Find(s =>
                    s.StatType == BoundedStatType.Level
                );
                if (lvlTemplate != null)
                {
                    _currentLevel = Mathf.FloorToInt(lvlTemplate.Current);
                }
                var expTemplate = _characterTemplate.BoundedStats.Find(s =>
                    s.StatType == BoundedStatType.LevelExperience
                );
                if (expTemplate != null)
                {
                    _currentExp = Mathf.FloorToInt(expTemplate.Current);
                }
            }

            _runtimeBoundedStats = CharacterHelpers.CloneBoundedStats(
                _characterTemplate.BoundedStats
            );
            _runtimeUnboundedStats = CharacterHelpers.CloneUnboundedStats(
                _characterTemplate.UnboundedStats
            );

            // ensure the runtime movement stat matches the template value.  sometimes
            // downstream processing (LTM, class minimums) can reset it to the
            // hardcoded default of 5, so we enforce the template here.
            if (_runtimeUnboundedStats != null && _characterTemplate.UnboundedStats != null)
            {
                var templMov = _characterTemplate.UnboundedStats.Find(s =>
                    s != null && s.StatType == UnboundedStatType.Movement
                );
                var instMov = _runtimeUnboundedStats.Find(s =>
                    s != null && s.StatType == UnboundedStatType.Movement
                );
                if (templMov != null && instMov != null)
                {
                    int before = instMov.CurrentInt;
                    int after = templMov.CurrentInt;
                    if (before != after)
                    {
                        instMov.SetCurrent(after);
                        $"CharacterInstance.Initialize: movement corrected from {before} to {after} (template)".LogInfo();
                    }
                }
            }

            // make sure runtime copy reflects the derived level/exp values
            if (_runtimeBoundedStats != null)
            {
                var lvlStat = _runtimeBoundedStats.Find(s => s.StatType == BoundedStatType.Level);
                if (lvlStat != null)
                {
                    lvlStat.SetCurrent(_currentLevel);
                    lvlStat.SetMax(_currentLevel);
                }
                var expStat = _runtimeBoundedStats.Find(s =>
                    s.StatType == BoundedStatType.LevelExperience
                );
                if (expStat != null)
                {
                    expStat.SetCurrent(_currentExp);
                }
            }

            RepairMissingStats();

            var validation2 = ValidateRuntimeStatsComplete();
            if (!validation2.Success)
            {
                return validation;
            }

#if UNITY_EDITOR
            ValidateStatsComplete();
#endif

            InitializeInventory();
            EnsureWeaponInSlot0();
            InitializeSupportRelationships();
            InitializeSkills();
            InitializeExperienceRanks();
            EnsurePersistedInLtm();
            return InitializeClass();
        }

        private void InitializeInventory()
        {
            _inventoryInstance = new CharacterInventoryInstance();

            if (_characterTemplate.StartingInventory == null)
            {
                return;
            }

            foreach (var slot in _characterTemplate.StartingInventory)
            {
                var itemInstance = new ObjectItemInstance(slot.Item);
                _inventoryInstance.AddToInventory(itemInstance);
                itemInstance.Slot = slot.SlotIndex;

                // Equip weapons in slot 0 and shields in slot 1
                if (
                    slot.SlotIndex == 0
                    && slot.Item.Subtype == Gameplay.Objects.Components.ObjectSubtype.Weapon
                )
                {
                    _inventoryInstance.EquipItem(itemInstance.Slot);
                }
                else if (
                    slot.SlotIndex == 1
                    && slot.Item.Subtype == Gameplay.Objects.Components.ObjectSubtype.Shield
                )
                {
                    _inventoryInstance.EquipItem(itemInstance.Slot);
                }
            }
        }

        private void EnsureWeaponInSlot0()
        {
            var items = _inventoryInstance.Items();
            var equippedWeapon = items.FirstOrDefault(i =>
                i.IsEquipped
                && i.Template?.Subtype == Gameplay.Objects.Components.ObjectSubtype.Weapon
            );

            if (equippedWeapon != null)
            {
                return;
            }

            // No weapon equipped, equip the first one
            var firstWeapon = items.FirstOrDefault(i =>
                i.Template?.Subtype == Gameplay.Objects.Components.ObjectSubtype.Weapon
            );
            if (firstWeapon != null)
            {
                firstWeapon.IsEquipped = true;
                firstWeapon.Slot = 0; // Move to slot 0 for visual organization
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

            // Add the character's single PersonalSkill (if assigned). Personal skills are always equipped
            // for the character and cannot be unequipped at runtime.
            if (_characterTemplate.PersonalSkill != null)
            {
                _skillInstances.Add(new SkillInstance(_characterTemplate.PersonalSkill));
            }
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
            var classToApply =
                _characterTemplate.GetPreferredStartingClass() ?? GetDefaultStartingClass();
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

            // Safety: ensure the personal skill from the template is always present in the
            // skill list, even if the LTM entry pre-dates the skill being assigned or the
            // data was otherwise incomplete.
            if (_characterTemplate?.PersonalSkill != null)
            {
                bool hasPersonalSkill = _skillInstances.Exists(s =>
                    s?.SkillTemplate == _characterTemplate.PersonalSkill
                );
                if (!hasPersonalSkill)
                {
                    _skillInstances.Add(new SkillInstance(_characterTemplate.PersonalSkill));
                    NeedsPersist = true;
                }
            }

            RegisterUniqueInstance();
            HandleCurrentClass();
            RepairMissingStats();
            EnsureWeaponInSlot0();
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
                        // log according to chosen level
                        if (logLevel == TurnrootLogger.LogLevel.Error)
                        {
                            message.LogError();
                        }
                        else if (logLevel == TurnrootLogger.LogLevel.Warning)
                        {
                            message.LogWarning();
                        }
                        else
                        {
                            message.LogInfo();
                        }

                        ClassRecoveryHandled = true;

                        if (res.Success)
                        {
                            NeedsPersist = true;
                        }
                    }
                    else
                    {
                        $"CharacterInstance.OnAfterDeserialize: No starting/default class available to recover for {Id}".LogWarning();
                    }
                }
            }
            else if (_characterTemplate != null)
            {
                var classToApply =
                    _characterTemplate.GetPreferredStartingClass() ?? GetDefaultStartingClass();
                if (classToApply != null)
                {
                    var res = ChangeClass(classToApply, applyClassChangeBonuses: false);
                    var logLevel = res.Success
                        ? TurnrootLogger.LogLevel.Info
                        : TurnrootLogger.LogLevel.Warning;
                    var message = res.Success
                        ? $"Character {Id} assigned starting class {classToApply.Identity.ClassName} after recall."
                        : $"CharacterInstance.OnAfterDeserialize: Failed to apply starting class for {Id}: {res.ErrorMessage}";
                    if (logLevel == TurnrootLogger.LogLevel.Error)
                    {
                        message.LogError();
                    }
                    else if (logLevel == TurnrootLogger.LogLevel.Warning)
                    {
                        message.LogWarning();
                    }
                    else
                    {
                        message.LogInfo();
                    }

                    if (res.Success)
                    {
                        ClassRecoveryHandled = true;
                        NeedsPersist = true;
                    }
                }
            }
        }
        #endregion
    }
}
