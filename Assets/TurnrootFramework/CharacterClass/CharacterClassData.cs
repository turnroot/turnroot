using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.GameSettings;
using Turnroot.Skills;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Criteria for determining class mastery progression.
    /// </summary>
    public enum MasteryCriteria
    {
        None,
        LevelBased,
        BattleBased,
    }

    /// <summary>
    /// ScriptableObject that defines a character class with stats, skills, requirements, and visual properties.
    /// </summary>
    [CreateAssetMenu(fileName = "New Character Class", menuName = "Turnroot/Characters/Class Data")]
    public partial class CharacterClassData : ScriptableObject
    {
        public string GetClassName() => Identity.ClassName;

        // Hidden field to cache the ClassSelectionMode for ShowIf evaluation
        [HideInInspector, SerializeField]
        private GameplayGeneralSettings.ClassSelectionMode _cachedClassSelectionMode;

        #region Facade Components

        [HorizontalLine(color: EColor.Yellow)]
        [Tooltip("Visual and identity properties for this class")]
        public ClassIdentity Identity = new();

        [Tooltip(
            "If true, this class provides its own outfit prefab and materials. If false, units will use their per-character default outfit and materials."
        )]
        public bool HasOutfit = true;

        [HorizontalLine(color: EColor.Green)]
        [Tooltip(
            "Walk animation for this class. If null, falls back to character's DefaultWalkingAnimation."
        )]
        public AnimationClip WalkAnimation;

        [Tooltip(
            "Run animation for this class. If null, falls back to character's DefaultRunningAnimation."
        )]
        public AnimationClip RunAnimation;

        [Tooltip(
            "Idle animations for this class. If empty, falls back to character's DefaultIdleAnimations. If multiple, one is chosen at random."
        )]
        public AnimationClip[] IdleAnimations = new AnimationClip[0];

        [HorizontalLine(color: EColor.Orange)]
        [Tooltip("Stat minimums, caps, bonuses, and growth rates")]
        public ClassStats Stats = new();

        [HorizontalLine(color: EColor.Violet)]
        [Tooltip("Requirements and restrictions for equipping this class")]
        public ClassRequirements Requirements = new();

        [HorizontalLine(color: EColor.Green)]
        [Tooltip("Innate skills and mastery progression")]
        public ClassMastery Mastery = new();

        #endregion

        #region Inspector Helpers

        [Tooltip(
            "Which pronoun sets are allowed for characters of this class (multi-select). Empty = allow all."
        )]
        [HideInInspector]
        public List<string> allowedPronounKeys = new();

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Initialize defaults when asset is loaded.
        /// </summary>
        private void OnEnable()
        {
            // Initialize cached mode when asset loads
            _cachedClassSelectionMode = GetProjectClassSelectionMode();

            // Ensure allowedPronounKeys defaults to all available keys when empty
            if (allowedPronounKeys == null || allowedPronounKeys.Count == 0)
            {
                var keys = Pronouns.GetAvailablePronounKeys();
                if (keys != null)
                {
                    allowedPronounKeys = new List<string>(keys);
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if a pronoun key is allowed for this class.
        /// </summary>
        public bool IsPronounAllowed(string pronounKey) =>
            string.IsNullOrEmpty(pronounKey)
            || allowedPronounKeys == null
            || allowedPronounKeys.Count == 0
            || allowedPronounKeys.Contains(pronounKey);

        /// <summary>
        /// Check if this class allows a specific weapon type.
        /// </summary>
        public bool AllowsWeaponType(WeaponType weaponType) =>
            // Empty list means no restrictions (can use any weapon)
            Requirements.AllowedWeaponTypes == null
            || Requirements.AllowedWeaponTypes.Count == 0
            || Requirements.AllowedWeaponTypes.Contains(weaponType);

        /// <summary>
        /// Get a list of weapon type names this class can use (for UI display).
        /// </summary>
        public string GetAllowedWeaponTypesString()
        {
            return
                Requirements.AllowedWeaponTypes == null
                || Requirements.AllowedWeaponTypes.Count == 0
                ? "Any"
                : string.Join(", ", Requirements.AllowedWeaponTypes);
        }

        /// <summary>
        /// Get the project's class selection mode from GameplayGeneralSettings.
        /// </summary>
        public static GameplayGeneralSettings.ClassSelectionMode GetProjectClassSelectionMode()
        {
            var settings = GameplayGeneralSettings.Instance;
            return settings != null
                ? settings.GetClassSelectionMode()
                : GameplayGeneralSettings.ClassSelectionMode.PromotionBased;
        }

        /// <summary>
        /// Convenience helper to get the class model prefab for a specific pronoun key.
        /// Falls back to the default class model if no pronoun override exists.
        /// </summary>
        public GameObject GetClassModelPrefabForPronoun(string pronounKey) =>
            Identity?.GetClassModelPrefabForPronoun(pronounKey);

        #endregion

        #region Editor Helpers

        /// <summary>
        /// Determine if promotion-based fields should be shown in the inspector.
        /// </summary>
        private bool ShowPromotionFields()
        {
            return _cachedClassSelectionMode
                == GameplayGeneralSettings.ClassSelectionMode.PromotionBased;
        }

        /// <summary>
        /// Determine if requirement-based fields should be shown in the inspector.
        /// </summary>
        private bool ShowRequirementFields()
        {
            return _cachedClassSelectionMode
                == GameplayGeneralSettings.ClassSelectionMode.RequirementBased;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Represents a character's proficiency rank (E-S) with a specific weapon type.
        /// </summary>
        [Serializable]
        public struct WeaponProficiency
        {
            public WeaponType weaponType;
            public LeveledLetteredField rank;

            public WeaponProficiency(WeaponType type, string rankValue)
            {
                weaponType = type;
                rank = new LeveledLetteredField(rankValue);
            }

            public override string ToString() => $"{weaponType}:{rank.Value}";
        }

        /// <summary>
        /// Defines minimum level and stat requirements for accessing a character class.
        /// </summary>
        [Serializable]
        public struct ClassRequirement
        {
            [Tooltip("Minimum level required to access this class")]
            public int minLevel;

            [Tooltip(
                "Minimum bounded stat requirements to change into this class; leave empty for none"
            )]
            public List<BoundedCharacterStat> minimumStats;
        }
    }

    /// <summary>
    /// Defines a minimum experience rank requirement for a specific skill or weapon type.
    /// </summary>
    [Serializable]
    public struct ExperienceRequirement
    {
        [Tooltip(
            "ID of the experience type this requirement applies to (e.g. 'sword' or 'riding')"
        )]
        public string experienceTypeId;

        [Tooltip("Minimum rank required (E, D, C, B, A, S)")]
        public LeveledLetteredField minimumRank;

        public ExperienceRequirement(string typeId, string rank)
        {
            experienceTypeId = typeId;
            minimumRank = new LeveledLetteredField(rank);
        }
    }

    /// <summary>
    /// Defines a skill mastery requirement with criteria (level or battle count) and target value.
    /// </summary>
    [Serializable]
    public struct Mastery
    {
        public Skill skill;
        public MasteryCriteria criteria;
        public int target;
    }

    /// <summary>
    /// Represents a modifier value applied to a character stat (bounded or unbounded).
    /// </summary>
    [Serializable]
    public struct UnboundedStatModifier
    {
        // growth may apply to either an unbounded stat (e.g. Strength) or a bounded stat
        // (currently only Health/HP).  "isBounded" indicates which field is active.
        public bool isBounded;

        // when isBounded == false, this field identifies the unbounded stat type.
        public UnboundedStatType unboundedStatType;

        // when isBounded == true, this field identifies the bounded stat type.
        public BoundedStatType boundedStatType;

        public float value;

        // constructor for unbounded stat growth
        public UnboundedStatModifier(UnboundedStatType type, float val)
        {
            isBounded = false;
            unboundedStatType = type;
            boundedStatType = default;
            value = val;
        }

        // constructor for bounded stat growth (HP)
        public UnboundedStatModifier(BoundedStatType type, float val)
        {
            isBounded = true;
            boundedStatType = type;
            unboundedStatType = default;
            value = val;
        }

        /// <summary>
        /// Helper to check if this modifier targets HP specifically.
        /// </summary>
        public bool IsHpGrowth => isBounded && boundedStatType == BoundedStatType.Health;
    }

        #endregion
}
