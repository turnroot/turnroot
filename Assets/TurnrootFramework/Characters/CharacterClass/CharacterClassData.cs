using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Objects;
using Turnroot.Gameplay.Objects.Components;
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

    /// <summary>
    /// Character class definition - now acts as a facade over focused sub-components.
    /// Decomposed from 568-line monolith into Identity, Stats, Requirements, and Mastery classes.
    /// </summary>
    [CreateAssetMenu(fileName = "New Character Class", menuName = "Turnroot/Character/Class Data")]
    public class CharacterClassData : ScriptableObject
    {
        // Hidden field to cache the ClassSelectionMode for ShowIf evaluation
        [HideInInspector, SerializeField]
        private GameplayGeneralSettings.ClassSelectionMode _cachedClassSelectionMode;

        #region Facade Components

        [Foldout("Identity"), HorizontalLine(color: EColor.Yellow)]
        [Tooltip("Visual and identity properties for this class")]
        public ClassIdentity Identity = new();

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

        #region Legacy Property Accessors (For Backward Compatibility)

        // Identity accessors
        [Obsolete("Use Identity.ClassName instead")]
        public string className => Identity.ClassName;

        [Obsolete("Use Identity.Description instead")]
        public string description => Identity.Description;

        [Obsolete("Use Identity.Icon instead")]
        public Sprite icon => Identity.Icon;

        [Obsolete("Use Identity.ClassTier instead")]
        public ProgressionLevel classTier => Identity.ClassTier;

        [Obsolete("Use Identity.IsMagic instead")]
        public bool IsMagic => Identity.IsMagic;

        [Obsolete("Use Identity.IsUnique instead")]
        public bool isUnique => Identity.IsUnique;

        [Obsolete("Use Identity.MovementType instead")]
        public MovementType movementType => Identity.MovementType;

        [Obsolete("Use Identity.ClassOutfit instead")]
        public Mesh ClassOutfit => Identity.ClassOutfit;

        [Obsolete("Use Identity.ShaderGraph instead")]
        public Shader ShaderGraph => Identity.ShaderGraph;

        [Obsolete("Use Identity.Base instead")]
        public Texture2D Base => Identity.Base;

        [Obsolete("Use Identity.MSE instead")]
        public Texture2D MSE => Identity.MSE;

        [Obsolete("Use Identity.TintMask instead")]
        public Texture2D TintMask => Identity.TintMask;

        // Stats accessors
        [Obsolete("Use Stats.StatMinimums instead")]
        public List<StatModifier> statMinimums => Stats.StatMinimums;

        [Obsolete("Use Stats.UnboundedStatMinimums instead")]
        public List<UnboundedStatModifier> unboundedStatMinimums => Stats.UnboundedStatMinimums;

        [Obsolete("Use Stats.StatCaps instead")]
        public List<StatModifier> statCaps => Stats.StatCaps;

        [Obsolete("Use Stats.UnboundedStatCaps instead")]
        public List<UnboundedStatModifier> unboundedStatCaps => Stats.UnboundedStatCaps;

        [Obsolete("Use Stats.StatBonuses instead")]
        public List<StatModifier> statBonuses => Stats.StatBonuses;

        [Obsolete("Use Stats.UnboundedStatBonuses instead")]
        public List<UnboundedStatModifier> unboundedStatBonuses => Stats.UnboundedStatBonuses;

        [Obsolete("Use Stats.GrowthRateModifiers instead")]
        public List<UnboundedStatModifier> growthRateModifiers => Stats.GrowthRateModifiers;

        [Obsolete("Use Stats.ClassChangeBonuses instead")]
        public List<StatModifier> classChangeBonuses => Stats.ClassChangeBonuses;

        [Obsolete("Use Stats.UnboundedClassChangeBonuses instead")]
        public List<UnboundedStatModifier> unboundedClassChangeBonuses =>
            Stats.UnboundedClassChangeBonuses;

        // Requirements accessors
        [Obsolete("Use Requirements.CertificationItem instead")]
        public ObjectItem certificationItem => Requirements.CertificationItem as ObjectItem;

        [Obsolete("Use Requirements.AllowedWeaponTypes instead")]
        public List<WeaponType> allowedWeaponTypes => Requirements.AllowedWeaponTypes;

        [Obsolete("Use Requirements.MinimumLevelRequirement instead")]
        public int requiredLevelToChange => Requirements.MinimumLevelRequirement;

        [Obsolete("Use Requirements.PromotionPaths instead")]
        public List<CharacterClassData> promotionPaths => Requirements.PromotionPaths;

        // Mastery accessors
        [Obsolete("Use Mastery.InnateSkills instead")]
        public List<Skill> innateSkills => Mastery.InnateSkills;

        #endregion

        #region Inspector Helpers

        [Foldout("Identity")]
        [ShowIf(nameof(ShowPromotionFields))]
        [Tooltip("List of classes this class can promote to (or from)")]
        public List<CharacterClassData> _legacyPromotionPaths
        {
            get => Requirements.PromotionPaths;
            set => Requirements.PromotionPaths = value;
        }

        [Foldout("Identity")]
        [ShowIf(nameof(ShowPromotionFields))]
        [Tooltip("Minimum level to change into this class")]
        public int _legacyRequiredLevelToChange
        {
            get => Requirements.MinimumLevelRequirement;
            set => Requirements.MinimumLevelRequirement = value;
        }

        [Foldout("Identity")]
        [Tooltip(
            "Which pronoun sets are allowed for characters of this class (multi-select). Empty = allow all."
        )]
        [HideInInspector]
        public List<string> allowedPronounKeys = new();

        [Foldout("Identity")]
        [Tooltip("Optional icon for UI / inspector")]
        public Sprite icon;

        [Foldout("Identity")]
        public ProgressionLevel classTier = ProgressionLevel.Base;

        [Foldout("Identity")]
        public bool IsMagic;

        [Foldout("Identity")]
        [ShowIf(nameof(ShowPromotionFields))]
        [Tooltip("List of classes this class can promote to (or from)")]
        public List<CharacterClassData> promotionPaths = new();

        [Foldout("Identity")]
        [ShowIf(nameof(ShowPromotionFields))]
        [Tooltip("Minimum level to change into this class")]
        public int requiredLevelToChange = 0;

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

#if UNITY_EDITOR

        #region Validation

        /// <summary>
        /// Validate class data when modified in editor.
        /// </summary>
        private void OnValidate()
        {
            // Update cached mode so ShowIf can see changes
            _cachedClassSelectionMode = GetProjectClassSelectionMode();

            // Auto-populate and validate stat lists
            ValidateStatLists();

            // Validate experience requirements
            ValidateExperienceRequirements();

            // Validate promotion paths
            ValidatePromotionPaths();

            // Force inspector refresh
            ForceInspectorRefresh();
        }

        private void ValidateStatLists()
        {
            var defaultStats =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<DefaultCharacterStats>(
                    "GameSettings"
                );
            if (defaultStats == null)
            {
                return;
            }

            // Define all bounded stat lists to validate in one place
            var boundedStatLists = new[]
            {
                (list: statMinimums, name: nameof(statMinimums)),
                (list: statCaps, name: nameof(statCaps)),
                (list: statBonuses, name: nameof(statBonuses)),
                (list: classChangeBonuses, name: nameof(classChangeBonuses)),
            };

            // Validate all bounded lists with single loop
            foreach (var (list, name) in boundedStatLists)
            {
                ValidateBoundedStatList(
                    list,
                    defaultStats.DefaultBoundedStats,
                    name,
                    (stat) => new StatModifier(stat.StatType, 0)
                );
            }

            // Define all unbounded stat lists
            var unboundedStatLists = new[]
            {
                (list: unboundedStatMinimums, name: nameof(unboundedStatMinimums)),
                (list: unboundedStatCaps, name: nameof(unboundedStatCaps)),
                (list: unboundedStatBonuses, name: nameof(unboundedStatBonuses)),
                (list: growthRateModifiers, name: nameof(growthRateModifiers)),
                (list: unboundedClassChangeBonuses, name: nameof(unboundedClassChangeBonuses)),
            };

            // Validate all unbounded lists
            foreach (var (list, name) in unboundedStatLists)
            {
                ValidateUnboundedStatList(
                    list,
                    defaultStats.DefaultUnboundedStats,
                    name,
                    (stat) => new UnboundedStatModifier(stat.StatType, 0)
                );
            }
        }

        private void ValidateBoundedStatList(
            List<StatModifier> list,
            List<DefaultCharacterStats.DefaultBoundedStat> defaults,
            string listName,
            System.Func<DefaultCharacterStats.DefaultBoundedStat, StatModifier> creator
        )
        {
            if (list.Count == 0)
            {
                foreach (var stat in defaults)
                {
                    list.Add(creator(stat));
                }
            }
            else if (list.Count != defaults.Count)
            {
                Debug.LogWarning(
                    $"{name}: {listName} count ({list.Count}) doesn't match DefaultCharacterStats count ({defaults.Count}). This may cause issues."
                );
            }
        }

        private void ValidateUnboundedStatList(
            List<UnboundedStatModifier> list,
            List<DefaultCharacterStats.DefaultUnboundedStat> defaults,
            string listName,
            System.Func<DefaultCharacterStats.DefaultUnboundedStat, UnboundedStatModifier> creator
        )
        {
            if (list.Count == 0)
            {
                foreach (var stat in defaults)
                {
                    list.Add(creator(stat));
                }
            }
            else if (list.Count != defaults.Count)
            {
                Debug.LogWarning(
                    $"{name}: {listName} count ({list.Count}) doesn't match DefaultCharacterStats count ({defaults.Count}). This may cause issues."
                );
            }
        }

        private void ValidateExperienceRequirements()
        {
            if (experienceRequirements == null || experienceRequirements.Count == 0)
            {
                return;
            }

            var validRanks = new[]
            {
                Turnroot.CommonAncestors.LeveledLetteredField.E,
                Turnroot.CommonAncestors.LeveledLetteredField.D,
                Turnroot.CommonAncestors.LeveledLetteredField.C,
                Turnroot.CommonAncestors.LeveledLetteredField.B,
                Turnroot.CommonAncestors.LeveledLetteredField.A,
                Turnroot.CommonAncestors.LeveledLetteredField.S,
            };

            foreach (var req in experienceRequirements)
            {
                if (string.IsNullOrEmpty(req.experienceTypeId))
                {
                    Debug.LogWarning(
                        $"{name}: ExperienceRequirement has empty experienceTypeId. This will not work at runtime."
                    );
                }

                if (
                    req.minimumRank != null
                    && !System.Array.Exists(validRanks, r => r == req.minimumRank.Value)
                )
                {
                    Debug.LogWarning(
                        $"{name}: ExperienceRequirement '{req.experienceTypeId}' has invalid rank '{req.minimumRank.Value}'. Valid ranks: E, D, C, B, A, S."
                    );
                }
            }
        }

        private void ValidatePromotionPaths()
        {
            if (promotionPaths == null || promotionPaths.Count == 0)
            {
                return;
            }

            if (promotionPaths.Contains(this))
            {
                Debug.LogError(
                    $"{name}: Class cannot have itself in its promotion paths. This creates a cycle."
                );
            }

            // Check for simple 2-step cycles (A -> B -> A)
            foreach (var promotion in promotionPaths)
            {
                if (
                    promotion != null
                    && promotion.promotionPaths != null
                    && promotion.promotionPaths.Contains(this)
                )
                {
                    Debug.LogWarning(
                        $"{name}: Detected circular promotion path with {promotion.className}. This may cause issues."
                    );
                }
            }
        }

        /// <summary>
        /// Force the inspector to refresh to update ShowIf conditions.
        /// </summary>
        private void ForceInspectorRefresh()
        {
            // Force inspector refresh when validating to update ShowIf conditions
            // This ensures promotion/requirement fields show/hide correctly based on GameplayGeneralSettings
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            };
        }

        #endregion

#endif

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
            allowedWeaponTypes == null
            || allowedWeaponTypes.Count == 0
            || allowedWeaponTypes.Contains(weaponType);

        /// <summary>
        /// Get a list of weapon type names this class can use (for UI display).
        /// </summary>
        public string GetAllowedWeaponTypesString()
        {
            return allowedWeaponTypes == null || allowedWeaponTypes.Count == 0
                ? "Any"
                : string.Join(", ", allowedWeaponTypes);
        }

        /// <summary>
        /// Get the project's class selection mode from GameplayGeneralSettings.
        /// </summary>
        public static GameplayGeneralSettings.ClassSelectionMode GetProjectClassSelectionMode()
        {
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>();
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

        /// <summary>
        /// Weapon proficiency rank for a specific weapon type.
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
        /// Requirements for characters to access this class.
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

        /// <summary>
        /// Experience/aptitude rank requirement for class access.
        /// </summary>
        [Serializable]
        public struct ExperienceRequirement
        {
            [Tooltip(
                "ID of the experience type this requirement applies to (e.g. 'sword' or 'riding')"
            )]
            public string experienceTypeId;

            [Tooltip("Minimum rank required (E, D, C, B, A, S)")]
            public Turnroot.CommonAncestors.LeveledLetteredField minimumRank;

            public ExperienceRequirement(string typeId, string rank)
            {
                experienceTypeId = typeId;
                minimumRank = new Turnroot.CommonAncestors.LeveledLetteredField(rank);
            }
        }

        #endregion
    }
}
