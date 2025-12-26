using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.CommonAncestors;
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

#if UNITY_EDITOR

        #region Validation

        /// <summary>
        /// Validate class data when modified in editor.
        /// </summary>
        private void OnValidate()
        {
            // Update cached mode so ShowIf can see changes
            _cachedClassSelectionMode = GetProjectClassSelectionMode();

            ValidateStatLists();
            ValidatePromotionPaths();
            ForceInspectorRefresh();
        }

        private void ValidateStatLists()
        {
            var defaultStats =
                GameSettingsLoader.LoadFirst<DefaultCharacterStats>(
                    "GameSettings"
                );
            if (defaultStats == null)
            {
                return;
            }

            // Define all bounded stat lists to validate in one place
            var boundedStatLists = new[]
            {
                (list: Stats.StatMinimums, name: nameof(Stats.StatMinimums)),
                (list: Stats.StatCaps, name: nameof(Stats.StatCaps)),
                (list: Stats.StatBonuses, name: nameof(Stats.StatBonuses)),
                (list: Stats.ClassChangeBonuses, name: nameof(Stats.ClassChangeBonuses)),
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
                (list: Stats.UnboundedStatMinimums, name: nameof(Stats.UnboundedStatMinimums)),
                (list: Stats.UnboundedStatCaps, name: nameof(Stats.UnboundedStatCaps)),
                (list: Stats.UnboundedStatBonuses, name: nameof(Stats.UnboundedStatBonuses)),
                (list: Stats.GrowthRateModifiers, name: nameof(Stats.GrowthRateModifiers)),
                (
                    list: Stats.UnboundedClassChangeBonuses,
                    name: nameof(Stats.UnboundedClassChangeBonuses)
                ),
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
            Func<DefaultCharacterStats.DefaultBoundedStat, StatModifier> creator
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
            Func<DefaultCharacterStats.DefaultUnboundedStat, UnboundedStatModifier> creator
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

        private void ValidatePromotionPaths()
        {
            if (Requirements.PromotionPaths == null || Requirements.PromotionPaths.Count == 0)
            {
                return;
            }

            if (Requirements.PromotionPaths.Contains(this))
            {
                Debug.LogError(
                    $"{name}: Class cannot have itself in its promotion paths. This creates a cycle."
                );
            }

            // Check for simple 2-step cycles (A -> B -> A)
            foreach (var promotion in Requirements.PromotionPaths)
            {
                if (
                    promotion != null
                    && promotion.Requirements.PromotionPaths != null
                    && promotion.Requirements.PromotionPaths.Contains(this)
                )
                {
                    Debug.LogWarning(
                        $"{name}: Detected circular promotion path with {promotion.Identity.ClassName}. This may cause issues."
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
            public LeveledLetteredField minimumRank;

            public ExperienceRequirement(string typeId, string rank)
            {
                experienceTypeId = typeId;
                minimumRank = new LeveledLetteredField(rank);
            }
        }

        #endregion
    }
}
