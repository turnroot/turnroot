using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Skills;
using Turnroot.Utilities;

namespace Turnroot.Characters
{
    public partial class CharacterInstance : Serialization.IPostDeserialize, IHasStats
    {
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

