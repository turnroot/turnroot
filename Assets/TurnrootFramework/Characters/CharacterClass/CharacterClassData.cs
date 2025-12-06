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

    [CreateAssetMenu(fileName = "New Character Class", menuName = "Turnroot/Character/Class Data")]
    public class CharacterClassData : ScriptableObject
    {
        // Hidden field to cache the ClassSelectionMode for ShowIf evaluation
        [HideInInspector, SerializeField]
        private GameplayGeneralSettings.ClassSelectionMode _cachedClassSelectionMode;

        [Foldout("Visuals"), HorizontalLine(color: EColor.Blue)]
        public Mesh ClassOutfit;

        [Foldout("Visuals")]
        public Shader ShaderGraph;

        [Foldout("Visuals")]
        public Texture2D Base;

        [Foldout("Visuals")]
        public Texture2D MSE;

        [Foldout("Visuals")]
        public Texture2D TintMask;

        [Foldout("Identity"), HorizontalLine(color: EColor.Yellow)]
        [Tooltip("Display name for this class")]
        public string className;

        [Foldout("Identity")]
        [Tooltip("Short description or flavour text for the class")]
        [TextArea(2, 6)]
        public string description;

        [Foldout("Identity")]
        [Tooltip("Optional icon for UI / inspector")]
        public Sprite icon;

        [Foldout("Identity")]
        public ProgressionLevel classTier = ProgressionLevel.Base;

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
        public List<string> allowedPronounKeys = new List<string>();

        [Foldout("Identity")]
        [Tooltip("If true, only a unique character can hold this class at a time")]
        public bool isUnique = false;

        [Foldout("Mobility"), HorizontalLine(color: EColor.Blue)]
        [Tooltip("Movement type for this class")]
        public MovementType movementType = MovementType.Infantry;

        [Foldout("Stats"), HorizontalLine(color: EColor.Orange)]
        [InfoBox("Leave any of these at zero and they will be ignored")]
        public string _;

        [Foldout("Stats")]
        [Tooltip("Minimum bounded stat values this class enforces (0 = no minimum)")]
        [ReorderableList]
        public List<StatModifier> statMinimums = new();

        [Foldout("Stats")]
        [Tooltip("Minimum unbounded stat values this class enforces (0 = no minimum)")]
        [ReorderableList]
        public List<UnboundedStatModifier> unboundedStatMinimums = new();

        [Foldout("Stats")]
        [Tooltip("Maximum bounded stat caps this class imposes (0 = no cap)")]
        [ReorderableList]
        public List<StatModifier> statCaps = new();

        [Foldout("Stats")]
        [Tooltip("Maximum unbounded stat caps this class imposes (0 = no cap)")]
        [ReorderableList]
        public List<UnboundedStatModifier> unboundedStatCaps = new();

        [Foldout("Stats")]
        [Tooltip("Flat bounded stat bonuses applied when equipping/occupying this class")]
        [ReorderableList]
        public List<StatModifier> statBonuses = new();

        [Foldout("Stats")]
        [Tooltip("Flat unbounded stat bonuses applied when equipping/occupying this class")]
        [ReorderableList]
        public List<UnboundedStatModifier> unboundedStatBonuses = new();

        [Foldout("Stats")]
        [Tooltip("Growth rate modifiers (percentage 0-100) for stat increases on level up")]
        [ReorderableList]
        public List<UnboundedStatModifier> growthRateModifiers = new();

        [Foldout("Skills & Abilities"), HorizontalLine(color: EColor.Green)]
        [Tooltip(
            "Built-in class skills/abilities that characters get simply from being this class"
        )]
        public List<Skill> innateSkills = new();

        [Foldout("Skills & Abilities")]
        [Tooltip(
            "Skills learned by mastering this class (e.g., after X battles or reaching certain level)"
        )]
        public Mastery[] masteries = new Mastery[0];

        [Foldout("Requirements & Certification"), HorizontalLine(color: EColor.Violet)]
        [Tooltip(
            "Item required to unlock/certify into this class (e.g., Intermediate Seal, Master Seal)"
        )]
        public ObjectItem certificationItem;

        [Foldout("Requirements & Certification")]
        [Tooltip(
            "Weapon types this class can equip. Empty = no restrictions (can equip any weapon)"
        )]
        public List<WeaponType> allowedWeaponTypes = new();

        [Foldout("Requirements & Certification")]
        [Tooltip(
            "Requirement-based: list of required experience types and minimum ranks needed to take this class"
        )]
        [ShowIf(nameof(ShowRequirementFields))]
        public List<ExperienceRequirement> experienceRequirements = new();

        [Foldout("Requirements & Certification")]
        [Tooltip(
            "Species restrictions - only characters of these species can enter this class. Empty = no restrictions."
        )]
        public List<SpeciesType> speciesRestrictions = new();

        [Foldout("Requirements & Certification")]
        [Tooltip(
            "One-time bounded stat bonuses applied when a character first changes into this class"
        )]
        [ReorderableList]
        public List<StatModifier> classChangeBonuses = new();

        [Foldout("Requirements & Certification")]
        [Tooltip(
            "One-time unbounded stat bonuses applied when a character first changes into this class"
        )]
        [ReorderableList]
        public List<UnboundedStatModifier> unboundedClassChangeBonuses = new();

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
                return;

            // Bounded stat lists
            ValidateBoundedStatList(
                statMinimums,
                defaultStats.DefaultBoundedStats,
                nameof(statMinimums),
                (stat) => new StatModifier(stat.StatType, 0)
            );
            ValidateBoundedStatList(
                statCaps,
                defaultStats.DefaultBoundedStats,
                nameof(statCaps),
                (stat) => new StatModifier(stat.StatType, 0)
            );
            ValidateBoundedStatList(
                statBonuses,
                defaultStats.DefaultBoundedStats,
                nameof(statBonuses),
                (stat) => new StatModifier(stat.StatType, 0)
            );
            ValidateBoundedStatList(
                classChangeBonuses,
                defaultStats.DefaultBoundedStats,
                nameof(classChangeBonuses),
                (stat) => new StatModifier(stat.StatType, 0)
            );

            // Unbounded stat lists
            ValidateUnboundedStatList(
                unboundedStatMinimums,
                defaultStats.DefaultUnboundedStats,
                nameof(unboundedStatMinimums),
                (stat) => new UnboundedStatModifier(stat.StatType, 0)
            );
            ValidateUnboundedStatList(
                unboundedStatCaps,
                defaultStats.DefaultUnboundedStats,
                nameof(unboundedStatCaps),
                (stat) => new UnboundedStatModifier(stat.StatType, 0)
            );
            ValidateUnboundedStatList(
                unboundedStatBonuses,
                defaultStats.DefaultUnboundedStats,
                nameof(unboundedStatBonuses),
                (stat) => new UnboundedStatModifier(stat.StatType, 0)
            );
            ValidateUnboundedStatList(
                growthRateModifiers,
                defaultStats.DefaultUnboundedStats,
                nameof(growthRateModifiers),
                (stat) => new UnboundedStatModifier(stat.StatType, 0)
            );
            ValidateUnboundedStatList(
                unboundedClassChangeBonuses,
                defaultStats.DefaultUnboundedStats,
                nameof(unboundedClassChangeBonuses),
                (stat) => new UnboundedStatModifier(stat.StatType, 0)
            );
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
                return;

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
                return;

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
        public bool IsPronounAllowed(string pronounKey)
        {
            if (string.IsNullOrEmpty(pronounKey))
                return true;
            if (allowedPronounKeys == null || allowedPronounKeys.Count == 0)
                return true;
            return allowedPronounKeys.Contains(pronounKey);
        }

        /// <summary>
        /// Check if this class allows a specific weapon type.
        /// </summary>
        public bool AllowsWeaponType(WeaponType weaponType)
        {
            // Empty list means no restrictions (can use any weapon)
            if (allowedWeaponTypes == null || allowedWeaponTypes.Count == 0)
                return true;

            return allowedWeaponTypes.Contains(weaponType);
        }

        /// <summary>
        /// Get a list of weapon type names this class can use (for UI display).
        /// </summary>
        public string GetAllowedWeaponTypesString()
        {
            if (allowedWeaponTypes == null || allowedWeaponTypes.Count == 0)
                return "Any";

            return string.Join(", ", allowedWeaponTypes);
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
