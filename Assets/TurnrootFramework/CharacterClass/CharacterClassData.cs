using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.GameSettings;
using Turnroot.Skills;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    public enum MasteryCriteria
    {
        None,
        LevelBased,
        BattleBased,
    }

    [CreateAssetMenu(fileName = "New Character Class", menuName = "Turnroot/Characters/Class Data")]
    public partial class CharacterClassData : ScriptableObject
    {
        public string GetClassName() => Identity.ClassName;

        // Hidden field to cache the ClassSelectionMode for ShowIf evaluation
        [HideInInspector, SerializeField]
        private GameplayGeneralSettings.ClassSelectionMode _cachedClassSelectionMode;

        #region Facade Components

        [Foldout("Identity"), HorizontalLine(color: EColor.Yellow)]
        [Tooltip("Visual and identity properties for this class")]
        public ClassIdentity Identity = new();

        [Foldout("Identity")]
        [Tooltip("If true, attach per-character hair model")]
        public bool UseUnitHairOnModel = true;

        [Foldout("Identity")]
        [Tooltip(
            "If true, this class provides its own outfit prefab and materials. If false, units will use their per-character default outfit and materials."
        )]
        public bool HasOutfit = true;

        [Foldout("Stats"), HorizontalLine(color: EColor.Orange)]
        [Tooltip("Stat minimums, caps, bonuses, and growth rates")]
        public ClassStats Stats = new();

        [Foldout("Requirements"), HorizontalLine(color: EColor.Violet)]
        [Tooltip("Requirements and restrictions for equipping this class")]
        public ClassRequirements Requirements = new();

        [Foldout("Skills & Mastery"), HorizontalLine(color: EColor.Green)]
        [Tooltip("Innate skills and mastery progression")]
        public ClassMastery Mastery = new();

        #endregion

        #region Inspector Helpers

        [Foldout("Identity")]
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

    [Serializable]
    public struct Mastery
    {
        public Skill skill;
        public MasteryCriteria criteria;
        public int target;
    }

    [Serializable]
    public struct StatModifier
    {
        public BoundedStatType boundedStatType;
        public float value;

        public StatModifier(BoundedStatType type, float val)
        {
            boundedStatType = type;
            value = val;
        }
    }

    [Serializable]
    public struct UnboundedStatModifier
    {
        public UnboundedStatType unboundedStatType;
        public float value;

        public UnboundedStatModifier(UnboundedStatType type, float val)
        {
            unboundedStatType = type;
            value = val;
        }
    }

        #endregion
}
