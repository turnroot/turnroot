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
    [CreateAssetMenu(fileName = "New Character Class", menuName = "Turnroot/Character/Class Data")]
    public class CharacterClassData : ScriptableObject
    {
        // Hidden field to cache the ClassSelectionMode for ShowIf evaluation
        [HideInInspector, SerializeField]
        private GameplayGeneralSettings.ClassSelectionMode _cachedClassSelectionMode;

        [Foldout("Visuals")]
        public Mesh ClassOutfit;

        [Foldout("Visuals")]
        public Shader ShaderGraph;

        [Foldout("Visuals")]
        public Texture2D Base;

        [Foldout("Visuals")]
        public Texture2D MSE;

        [Foldout("Visuals")]
        public Texture2D TintMask;

        [Foldout("Identity")]
        [Tooltip("Display name for this class")]
        public string className;

        [Tooltip("Short description or flavour text for the class")]
        [TextArea(2, 6)]
        public string description;

        [Tooltip("Optional icon for UI / inspector")]
        public Sprite icon;

        [Foldout("Identity")]
        public ProgressionLevel classTier = ProgressionLevel.Base;

        [ShowIf(nameof(ShowPromotionFields))]
        [Tooltip("List of classes this class can promote to (or from)")]
        public List<CharacterClassData> promotionPaths = new();

        [ShowIf(nameof(ShowPromotionFields))]
        [Tooltip("Minimum level or requirement to change into this class")]
        public int requiredLevelToChange = 0;

        [Tooltip(
            "Which pronoun sets are allowed for characters of this class (multi-select). Empty = allow all."
        )]
        public List<string> allowedPronounKeys = new List<string>();

        [Tooltip("If true, only a unique character can hold this class at a time")]
        public bool isUnique = false;

        [Tooltip(
            "Requirement-based: list of required experience types and minimum ranks needed to take this class"
        )]
        [ShowIf(nameof(ShowRequirementFields))]
        public List<ExperienceRequirement> experienceRequirements = new();

        [Tooltip(
            "Requirement-based: minimum overall character level required to change into this class (0 = no level requirement)"
        )]
        [ShowIf(nameof(ShowRequirementFields))]
        public int selectionMinimumLevel = 0;

        [Foldout("Mobility")]
        public MovementType movementType = MovementType.Infantry;

        [Tooltip("Tiles of movement")]
        public int movement = 5;

        [Foldout("Stats")]
        [Tooltip(
            "Base stat contribution provided by the class (flat) - e.g., core class base stats or additional class bonuses"
        )]
        public DefaultCharacterStats defaultStatsSource;

        [Tooltip(
            "Stat modifiers some classes apply when equipping / occupying class (flat modifiers)"
        )]
        public List<BoundedCharacterStat> boundedStatModifiers = new();

        [Tooltip(
            "Optional per-class growth rate modifiers (percent values 0-100) — useful to model class growths and teaching/mentor bonuses"
        )]
        public List<CharacterStat> unboundedStatModifiers = new();

        [Tooltip("Optional hard caps this class imposes on stats. Fill 0 to mean 'no special cap'")]
        public List<BoundedCharacterStat> statCaps = new();

        [Foldout("Combat & Weapon Proficiencies")]
        [Tooltip(
            "Weapon proficiencies + ranks this class has (E..S). If a weapon type is absent it's treated as None / unusable"
        )]
        [ReorderableList]
        public List<WeaponProficiency> weaponProficiencies = new();

        [Tooltip(
            "List of built-in class skills / abilities (names or asset references). These are the skills a character would get simply from being this class.)"
        )]
        public List<Skill> innateSkills = new();

        [Foldout("Combat & Weapon Proficiencies")]
        [Tooltip(
            "Skills learned by mastering this class (e.g., after X battles or reaching certain level)"
        )]
        public List<Skill> masterySkills = new();

        [Tooltip(
            "Item required to unlock/certify into this class (e.g., Intermediate Seal, Master Seal)"
        )]
        public ObjectItem certificationItem;

        [Foldout("Certification & Requirements")]
        [Tooltip(
            "Species restrictions - only characters of these species can enter this class. Empty = no restrictions."
        )]
        public List<SpeciesType> speciesRestrictions = new();

        [Foldout("Certification & Requirements")]
        [Tooltip("One-time stat bonuses applied when a character first enters this class")]
        public List<CharacterStat> classChangeBonuses = new();

        private void OnEnable()
        {
            // Initialize cached mode when asset loads
            var mode = GetProjectClassSelectionMode();
            Debug.Log(
                $"[{name}] OnEnable: Setting cached mode to {mode} (was {_cachedClassSelectionMode})"
            );
            _cachedClassSelectionMode = mode;

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update cached mode so ShowIf can see changes
            var mode = GetProjectClassSelectionMode();
            Debug.Log(
                $"[{name}] OnValidate: Setting cached mode to {mode} (was {_cachedClassSelectionMode})"
            );
            _cachedClassSelectionMode = mode;

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
#endif

        public bool IsPronounAllowed(string pronounKey)
        {
            if (string.IsNullOrEmpty(pronounKey))
                return true;
            if (allowedPronounKeys == null || allowedPronounKeys.Count == 0)
                return true;
            return allowedPronounKeys.Contains(pronounKey);
        }

        public static GameplayGeneralSettings.ClassSelectionMode GetProjectClassSelectionMode()
        {
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>();
            return settings != null
                ? settings.GetClassSelectionMode()
                : GameplayGeneralSettings.ClassSelectionMode.PromotionBased;
        }

        private bool ShowPromotionFields()
        {
            return _cachedClassSelectionMode
                == GameplayGeneralSettings.ClassSelectionMode.PromotionBased;
        }

        private bool ShowRequirementFields()
        {
            return _cachedClassSelectionMode
                == GameplayGeneralSettings.ClassSelectionMode.RequirementBased;
        }

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

        [Serializable]
        public struct ExperienceRequirement
        {
            [Tooltip(
                "ID of the experience type this requirement applies to (e.g. 'sword' or 'riding')"
            )]
            public string experienceTypeId;

            [Tooltip("Minimum rank/level required in the ExperienceType to meet this requirement")]
            public int minRank;
        }
    }
}
