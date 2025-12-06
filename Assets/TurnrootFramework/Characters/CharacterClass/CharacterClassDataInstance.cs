using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Stats;
using Turnroot.Serialization;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Runtime instance of a character class, handling visual representation and stat application.
    /// Manages material rendering and provides methods for applying class bonuses/modifiers to characters.
    /// </summary>
    [Serializable]
    public class CharacterClassDataInstance : IPostDeserialize
    {
        #region Fields

        [SerializeField]
        private CharacterData _characterData;

        [SerializeField]
        private CharacterClassData _classData;

        [SerializeField]
        private MeshRenderer _meshRenderer;

        [SerializeField]
        private bool _isFirstTimeEquipped = true;

        [SerializeField]
        private int _battlesCompleted = 0;

        [SerializeField]
        private int _levelWhenEquipped = 1;

        [SerializeField]
        private List<Skill> _masteredSkills = new();

        private Material _materialInstance;

        #endregion

        #region Properties

        public CharacterData CharacterData => _characterData;
        public CharacterClassData ClassData => _classData;
        public MeshRenderer MeshRenderer => _meshRenderer;
        public bool IsFirstTimeEquipped => _isFirstTimeEquipped;
        public int BattlesCompleted => _battlesCompleted;
        public int LevelWhenEquipped => _levelWhenEquipped;

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new runtime instance of a character class.
        /// </summary>
        public CharacterClassDataInstance(
            CharacterData characterData,
            CharacterClassData classData,
            MeshRenderer meshRenderer = null
        )
        {
            _characterData = characterData;
            _classData = classData;
            _meshRenderer = meshRenderer;
            _isFirstTimeEquipped = true;
            _battlesCompleted = 0;
            _levelWhenEquipped = characterData?.Level ?? 1;
            _masteredSkills = new List<Skill>();
        }

        // Parameterless constructor for serialization
        public CharacterClassDataInstance() { }

        /// <summary>
        /// Initialize visual representation by creating and configuring material.
        /// </summary>
        public bool Initialize()
        {
            // Validate required references
            if (_characterData == null)
            {
                Debug.LogWarning("CharacterClassDataInstance.Initialize: characterData is null");
                return false;
            }

            if (_meshRenderer == null)
            {
                Debug.LogWarning("CharacterClassDataInstance.Initialize: meshRenderer is null");
                return false;
            }

            if (_classData == null)
            {
                Debug.LogWarning("CharacterClassDataInstance.Initialize: classData is null");
                return false;
            }

            if (_classData.ShaderGraph == null)
            {
                Debug.LogWarning(
                    $"CharacterClassDataInstance.Initialize: ShaderGraph is null for class '{_classData.className}'"
                );
                return false;
            }

            // Cleanup existing material to prevent memory leak
            CleanupMaterial();

            // Create new material instance
            _materialInstance = new Material(_classData.ShaderGraph);
            _meshRenderer.material = _materialInstance;

            // Apply character colors
            _materialInstance.SetColor("_Skin_Color", _characterData.SkinColor);
            _materialInstance.SetColor("_Accent_Color_1", _characterData.AccentColor1);
            _materialInstance.SetColor("_Accent_Color_2", _characterData.AccentColor2);
            _materialInstance.SetColor("_Accent_Color_3", _characterData.AccentColor3);

            // Apply class textures
            _materialInstance.SetTexture("_Base", _classData.Base);
            _materialInstance.SetTexture("_MSE", _classData.MSE);
            _materialInstance.SetTexture("_Tint_Mask", _classData.TintMask);

            return true;
        }

        /// <summary>
        /// Handle post-deserialization initialization.
        /// Reinitializes material if needed after loading from save.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Re-initialize material after deserialization
            // This ensures materials are properly recreated when loading from save
            if (_characterData != null && _classData != null && _meshRenderer != null)
            {
                Initialize();
            }
        }

        #endregion

        #region Stat Application

        /// <summary>
        /// Apply class stat bonuses to a character instance.
        /// These are persistent bonuses while the class is equipped.
        /// </summary>
        public void ApplyClassBonuses(CharacterInstance character)
        {
            if (
                !StatApplicationHelper.ValidateReferences(
                    character,
                    _classData,
                    "CharacterClassDataInstance.ApplyClassBonuses"
                )
            )
            {
                return;
            }

            StatApplicationHelper.ApplyBoundedBonuses(_classData.statBonuses, character);
            StatApplicationHelper.ApplyUnboundedBonuses(_classData.unboundedStatBonuses, character);
        }

        /// <summary>
        /// Remove class stat bonuses from a character instance.
        /// Call when changing classes to remove old class bonuses.
        /// </summary>
        public void RemoveClassBonuses(CharacterInstance character)
        {
            if (
                !StatApplicationHelper.ValidateReferences(
                    character,
                    _classData,
                    "CharacterClassDataInstance.RemoveClassBonuses"
                )
            )
            {
                return;
            }

            StatApplicationHelper.RemoveBoundedBonuses(_classData.statBonuses, character);
            StatApplicationHelper.RemoveUnboundedBonuses(
                _classData.unboundedStatBonuses,
                character
            );
        }

        /// <summary>
        /// Apply one-time class change bonuses (permanent stat increases).
        /// Only applied the first time a character equips this class.
        /// </summary>
        public void ApplyClassChangeBonuses(CharacterInstance character)
        {
            if (!_isFirstTimeEquipped)
            {
                return; // Already applied
            }

            if (
                !StatApplicationHelper.ValidateReferences(
                    character,
                    _classData,
                    "CharacterClassDataInstance.ApplyClassChangeBonuses"
                )
            )
            {
                return;
            }

            StatApplicationHelper.ApplyBoundedPermanentBonuses(
                _classData.classChangeBonuses,
                character,
                logChanges: true
            );
            StatApplicationHelper.ApplyUnboundedPermanentBonuses(
                _classData.unboundedClassChangeBonuses,
                character,
                logChanges: true
            );

            _isFirstTimeEquipped = false;
        }

        /// <summary>
        /// Enforce stat minimums for this class.
        /// If character stats are below class minimums, raise them to the minimum.
        /// </summary>
        public void EnforceStatMinimums(CharacterInstance character)
        {
            if (
                !StatApplicationHelper.ValidateReferences(
                    character,
                    _classData,
                    "CharacterClassDataInstance.EnforceStatMinimums"
                )
            )
            {
                return;
            }

            StatApplicationHelper.EnforceBoundedMinimums(
                _classData.statMinimums,
                character,
                logChanges: true
            );
            StatApplicationHelper.EnforceUnboundedMinimums(
                _classData.unboundedStatMinimums,
                character,
                logChanges: true
            );
        }

        /// <summary>
        /// Apply stat caps for this class.
        /// Sets the maximum values for bounded stats based on class caps.
        /// </summary>
        public void ApplyStatCaps(CharacterInstance character)
        {
            if (
                !StatApplicationHelper.ValidateReferences(
                    character,
                    _classData,
                    "CharacterClassDataInstance.ApplyStatCaps"
                )
            )
            {
                return;
            }

            StatApplicationHelper.ApplyBoundedCaps(_classData.statCaps, character);

            // Note: Unbounded stat caps are enforced during level-up/stat modification
            // They don't set a hard max like bounded stats
        }

        /// <summary>
        /// Check if character stats are above class caps.
        /// Returns true if any stat exceeds the cap.
        /// </summary>
        public bool IsAboveCaps(CharacterInstance character)
        {
            if (!StatApplicationHelper.ValidateReferences(character, _classData, ""))
            {
                return false;
            }

            return StatApplicationHelper.IsAboveUnboundedCaps(
                _classData.unboundedStatCaps,
                character
            );
        }

        #endregion

        #region Mastery Tracking

        /// <summary>
        /// Increment battle count for mastery tracking.
        /// Call this after each battle where the character uses this class.
        /// </summary>
        public void IncrementBattleCount()
        {
            _battlesCompleted++;
        }

        /// <summary>
        /// Check if mastery conditions are met and learn skills if so.
        /// Returns true if any new skills were learned.
        /// </summary>
        public bool CheckMasteryConditions(CharacterInstance character)
        {
            if (character == null || _classData == null || _classData.masteries == null)
                return false;

            bool learnedNewSkill = false;

            foreach (var mastery in _classData.masteries)
            {
                if (mastery.skill == null)
                    continue;

                // Skip if already mastered (compare by reference, not name)
                if (_masteredSkills.Contains(mastery.skill))
                    continue;

                bool conditionMet = mastery.criteria switch
                {
                    CharacterClass.MasteryCriteria.LevelBased => character.CurrentLevel
                        >= _levelWhenEquipped + mastery.target,
                    CharacterClass.MasteryCriteria.BattleBased => _battlesCompleted
                        >= mastery.target,
                    _ => false,
                };

                if (conditionMet)
                {
                    character.AddSkill(mastery.skill);
                    _masteredSkills.Add(mastery.skill);
                    learnedNewSkill = true;

                    Debug.Log(
                        $"{character.CharacterTemplate?.DisplayName} mastered {mastery.skill.SkillName} "
                            + $"from class {_classData.className}!"
                    );
                }
            }

            return learnedNewSkill;
        }

        #endregion

        #region Material Management

        /// <summary>
        /// Cleanup dynamically created material to prevent memory leaks.
        /// Should be called before destroying this instance or re-initializing.
        /// </summary>
        public void CleanupMaterial()
        {
            if (_materialInstance != null)
            {
                // Use DestroyImmediate in editor, Destroy at runtime
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_materialInstance);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_materialInstance);
                }
                _materialInstance = null;
            }
        }

        /// <summary>
        /// Call this when the instance is being destroyed or no longer needed.
        /// Ensures proper cleanup of resources.
        /// </summary>
        public void Dispose()
        {
            CleanupMaterial();
        }

        #endregion
    }
}
