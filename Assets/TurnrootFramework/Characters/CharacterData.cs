using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.CommonAncestors;
using Turnroot.Gameplay.Objects;
using Turnroot.Skills;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "NewCharacterConfiguration",
        menuName = "Turnroot/Characters/CharacterData"
    )]
    public partial class CharacterData : ScriptableObject, IHasStats
    {
#if UNITY_EDITOR
        [
            InfoBox(
                "This is pre-runtime data. Use this editor to define the character's base stats, skills, inventory, and relationships- anything that should be in place before the game starts."
            ),
            SerializeField
        ]
        private string _;
#endif

        [field: Foldout("Identity"), HorizontalLine(color: EColor.White), SerializeField]
        public CharacterWhich Which { get; private set; } = new("Enemy");

        [field: Foldout("Identity"), SerializeField]
        public string DisplayName { get; private set; } = "New Unit";

        [field: Foldout("Identity"), SerializeField]
        public string FullName { get; private set; } = "Newly Created Unit";

        [field: Foldout("Identity")]
        public string Team { get; private set; }

        [field: Foldout("Demographics"), HorizontalLine(color: EColor.Black), SerializeField]
        public Pronouns CharacterPronouns { get; private set; } = new();

        [field: Foldout("Demographics"), SerializeField, Range(100f, 250f)]
        public float Height { get; private set; } = 166f;

        [field: Foldout("Demographics"), SerializeField, Range(1, 31)]
        public int BirthdayDay { get; private set; } = 1;

        [field: Foldout("Demographics"), SerializeField, Range(1, 12)]
        public int BirthdayMonth { get; private set; } = 1;

        [field: Foldout("Demographics"), SerializeField]
        [Tooltip("The species/race of this character (e.g., Human, Beast, Dragon, Manakete)")]
        public SpeciesType Species { get; private set; }

        [field:
            Foldout("Description"),
            SerializeField,
            ResizableTextArea,
            HorizontalLine(color: EColor.Gray)
        ]
        public string ShortDescription { get; private set; } = "A new unit";

        [field: Foldout("Description"), SerializeField]
        public string[] Likes { get; private set; } = new string[0];

        [field: Foldout("Description"), SerializeField]
        public string[] Dislikes { get; private set; } = new string[0];

        [field: Foldout("Description"), SerializeField, ResizableTextArea]
        public string Notes { get; private set; } =
            "Take private notes (only in the editor) about this unit";

        [field: Foldout("Character Flags"), SerializeField, HorizontalLine(color: EColor.Red)]
        public bool CanSSupport { get; private set; } = true;

        [field: Foldout("Character Flags"), SerializeField, ShowIf(nameof(CanShowSSupportAvatar))]
        public bool CanSSupportAvatar { get; private set; } = false;
#if TURNROOT_BLOODLINES_MODULE

        [field: Foldout("Character Flags"), SerializeField, ShowIf(nameof(IsAllyOrRecruitable))]
        public bool CanHaveChildren { get; private set; } = false;
#endif

        [field: Foldout("Character Flags"), SerializeField, ShowIf(nameof(CanShowRecruitable))]
        public bool IsRecruitable { get; private set; } = false;

        [field: Foldout("Character Flags"), SerializeField, ShowIf(nameof(IsRecruitable))]
        public bool RequiresMinSupportLevel { get; private set; } = true;

        [field:
            Foldout("Character Flags"),
            SerializeField,
            ShowIf(nameof(IsRecruitableRequiresMinSupportLevel))
        ]
        public LeveledLetteredField SupportRelationshipMinRank { get; private set; } =
            new LeveledLetteredField(LeveledLetteredField.E);

        [field: Foldout("Character Flags"), SerializeField, ShowIf(nameof(IsRecruitable))]
        public bool UseRecruitmentChance { get; private set; } = true;

        [field:
            Foldout("Character Flags"),
            SerializeField,
            Range(0f, 100f),
            ShowIf(nameof(IsRecruitableUseRecruitmentChance))
        ]
        public float RecruitmentChance { get; private set; } = 25f;

        [field:
            Foldout("Character Flags"),
            SerializeField,
            Range(0f, 100f),
            ShowIf(nameof(IsRecruitableUseRecruitmentChance))
        ]
        public float RecruitmentChanceIncreasePerConversation { get; private set; } = 15f;

        [field: Foldout("Character Flags"), SerializeField, ShowIf(nameof(CanShowUnique))]
        public bool IsUnique { get; private set; } = false;

        [field: Foldout("Class and Skills"), HorizontalLine(color: EColor.Yellow), SerializeField]
        public CharacterClassData StartingClass { get; private set; }

        [field: Foldout("Visual"), SerializeField, HorizontalLine(color: EColor.Pink)]
        public string BadgeText { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        public Sprite BadgeIcon { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        public CharacterModelBlendshapeSet Blendshapes { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        public Color SkinColor { get; private set; }

#if TURNROOT_BLOODLINES_MODULE
        [Foldout("Visual"), SerializeField]
        private Color _hairColor;

        [Foldout("Visual"), SerializeField]
        private Color _eyeColor;
#endif

        [field: Foldout("Visual"), SerializeField]
        public Color AccentColor1 { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        public Color AccentColor2 { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        public Color AccentColor3 { get; private set; }

        [field: Foldout("Visual"), SerializeField, HideInInspector]
        public SerializableDictionary<string, Portrait> Portraits { get; private set; }

        public Portrait DefaultPortrait => CharacterHelpers.GetDefaultPortrait(Portraits);

        [field: Foldout("Visual"), HideInInspector]
        public SerializableDictionary<string, TaggedLayerDefault> TaggedLayerDefaults
        {
            get;
            private set;
        } = new();
        private Portrait[] _portraitArrayCache;

        [field: Foldout("Visual"), SerializeField]
        [Tooltip(
            "Prefab containing head/hands mesh(s). Should contain a SkinnedMeshRenderer to be used for head/hands"
        )]
        public GameObject HeadAndHandsPrefab { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        [Tooltip(
            "Prefab containing hair mesh (SkinnedMeshRenderer). Used when classes opt to attach unit hair"
        )]
        public GameObject HairPrefab { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        [Tooltip(
            "Optional per-character non-battle outfit prefab used when UseBattleModel is false"
        )]
        public GameObject NonBattleOutfitPrefab { get; private set; }

        [field:
            Foldout("Animations"),
            SerializeField,
            HorizontalLine(color: EColor.Green),
            InfoBox(
                "If true, this character will always use the default animations assigned here, regardless of class-specific animations."
            )
        ]
        public bool UseDefaultAnimationsAlways { get; private set; } = false;

        [field:
            Foldout("Animations"),
            SerializeField,
            InfoBox(
                "Animations don't need to be unique- characters should all use the same underlying bone structure, with extra bones animated via a separate layer if needed. See the Rigging section below"
            )
        ]
        public AnimationClip DefaultWalkingAnimation { get; private set; }

        [field:
            Foldout("Animations"),
            SerializeField,
            InfoBox("Used if no running animation is assigned in class.")
        ]
        public AnimationClip DefaultRunningAnimation { get; private set; }

        [field:
            Foldout("Animations"),
            SerializeField,
            InfoBox("If multiple idle animations are assigned, one will be chosen at random.")
        ]
        public AnimationClip[] DefaultIdleAnimations { get; private set; }

        [field:
            Foldout("Rigging"),
            SerializeField,
            Tooltip("Enable if this character has an additional bone layer (+X)"),
            HorizontalLine(color: EColor.Blue)
        ]
        public bool HasExtraBoneLayer { get; private set; } = false;

        [field:
            Foldout("Rigging"),
            SerializeField,
            Tooltip(
                "Optionally assign a custom Avatar (skeleton) for characters whose skeleton differs from the default base Avatar."
            )
        ]
        public Avatar CustomAvatar { get; private set; }

        [field:
            Foldout("Rigging"),
            SerializeField,
            Tooltip(
                "AvatarMask that marks bones belonging to the +X layer. Use this mask to animate only extra bones on a separate Animator layer."
            )
        ]
        public AvatarMask AdditionalBonesMask { get; private set; }

        [field:
            Foldout("Rigging"),
            SerializeField,
            Tooltip(
                "A list of extra bone names for tooling and runtime validation. This helps identify which bones belong to the +X layer."
            )
        ]
        public string[] AdditionalBoneNames { get; private set; } = new string[0];

        [field: SerializeField, HorizontalLine(color: EColor.Blue)]
        public CharacterBehavior BehaviorSettings { get; private set; }

#if TURNROOT_BLOODLINES_MODULE
        [Foldout("Heredity"), SerializeField]
        [HorizontalLine(color: EColor.Indigo)]
        private HereditaryTraits _passedDownTraits = new();

        [Foldout("Heredity"), SerializeField]
        private bool _hasDesignatedChildUnit = false;

        [Foldout("Heredity"), SerializeField, ShowIf(nameof(_hasDesignatedChildUnit))]
        private CharacterData _childUnitId;
#endif

        [field: SerializeField]
        public List<InventorySlot> StartingInventory { get; private set; } = new();

        [field: SerializeField]
        public List<SupportRelationship> SupportRelationships { get; private set; } = new();

        [field:
            BoxGroup("Stats & Progression"),
            SerializeField,
            HorizontalLine(color: EColor.Orange)
        ]
        public int Level { get; private set; } = 1;

        [field: SerializeField, BoxGroup("Stats & Progression")]
        public int Exp { get; private set; } = 0;

        [field: BoxGroup("Stats & Progression"), SerializeField]
        public List<BoundedCharacterStat> BoundedStats { get; private set; } = new();

        [field: BoxGroup("Stats & Progression"), SerializeField]
        public List<CharacterStat> UnboundedStats { get; private set; } = new();

#if UNITY_EDITOR
        private void ValidateStats()
        {
            var errorList = new List<string>();
            var warningList = new List<string>();

            // Check bounded stats
            var requiredBounded = System.Enum.GetValues(typeof(BoundedStatType));
            var existingBounded = new HashSet<BoundedStatType>();

            foreach (var stat in BoundedStats)
            {
                if (stat == null)
                {
                    warningList.Add($"{name}: BoundedStats contains null entry");
                    continue;
                }

                if (!existingBounded.Add(stat.StatType))
                {
                    errorList.Add($"{name}: Duplicate bounded stat {stat.StatType}");
                }
            }

            foreach (BoundedStatType type in requiredBounded)
            {
                if (!existingBounded.Contains(type))
                {
                    warningList.Add($"{name}: Missing bounded stat {type}");
                }
            }

            // Check unbounded stats
            var requiredUnbounded = System.Enum.GetValues(typeof(UnboundedStatType));
            var existingUnbounded = new HashSet<UnboundedStatType>();

            foreach (var stat in UnboundedStats)
            {
                if (stat == null)
                {
                    warningList.Add($"{name}: UnboundedStats contains null entry");
                    continue;
                }

                if (!existingUnbounded.Add(stat.StatType))
                {
                    errorList.Add($"{name}: Duplicate unbounded stat {stat.StatType}");
                }
            }

            foreach (UnboundedStatType type in requiredUnbounded)
            {
                if (!existingUnbounded.Contains(type))
                {
                    warningList.Add($"{name}: Missing unbounded stat {type}");
                }
            }

            // Report consolidated results using TurnrootLogger to reduce spam
            if (errorList.Count > 0)
            {
                TurnrootLogger.Log(
                    $"{name}: Stat validation errors:\n{string.Join("\n", errorList)}",
                    TurnrootLogger.LogLevel.Error
                );
            }

            if (warningList.Count > 0)
            {
                TurnrootLogger.Log(
                    $"{name}: Stat validation warnings:\n{string.Join("\n", warningList)}\nConsider using 'Tools > Turnroot > Refresh Character Stats' or checking the DefaultCharacterStats asset.",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }
#endif

        [field: BoxGroup("Stats & Progression"), SerializeField]
        [Tooltip(
            "Personal growth rates (percentage 0-100) for stat increases on level up. If empty, uses class growth rates only."
        )]
        public List<UnboundedStatModifier> PersonalGrowthRates { get; private set; } = new();

        [field: BoxGroup("Skills & Abilities"), SerializeField, HorizontalLine(color: EColor.Green)]
        public List<Skill> Skills { get; private set; } = new();

        [field: BoxGroup("Skills & Abilities"), SerializeField]
        public List<Skill> SpecialSkills { get; private set; } = new();

        [field:
            BoxGroup("Experience & Aptitudes"),
            SerializeField,
            HorizontalLine(color: EColor.Indigo)
        ]
        [Tooltip(
            "Experience/aptitude ranks for weapon types and other trainable skills (e.g., Riding, Flying)"
        )]
        public List<ExperienceRank> ExperienceRanks { get; private set; } = new();

        // NOTE: properties are declared inline with field-targeted attributes.
#if TURNROOT_BLOODLINES_MODULE
        public Color HairColor => _hairColor;
        public Color EyeColor => _eyeColor;
#endif
        public bool IsNotAvatar => Which != CharacterWhich.AVATAR;
        public bool IsEnemyOrNPC => Which == CharacterWhich.ENEMY || Which == CharacterWhich.NPC;

        // NaughtyAttributes ShowIf helper methods
        private bool CanShowSSupportAvatar() => Which != CharacterWhich.AVATAR;

        private bool CanShowRecruitable() =>
            Which == CharacterWhich.ENEMY || Which == CharacterWhich.NPC;

        private bool CanShowUnique() => Which != CharacterWhich.AVATAR;

        private bool IsAllyOrRecruitable() => Which == CharacterWhich.ALLY || IsRecruitable;

        private bool IsRecruitableRequiresMinSupportLevel() =>
            IsRecruitable && RequiresMinSupportLevel;

        private bool IsRecruitableUseRecruitmentChance() => IsRecruitable && UseRecruitmentChance;

        public Portrait[] PortraitArray
        {
            get
            {
                _portraitArrayCache ??= Portraits?.Values.ToArray();
                return _portraitArrayCache;
            }
        }

#if TURNROOT_BLOODLINES_MODULE
        public HereditaryTraits PassedDownTraits => _passedDownTraits;

        public bool HasDesignatedChildUnit => _hasDesignatedChildUnit;
        public CharacterData ChildUnitId => _childUnitId;
#endif

        [Serializable]
        public class InventorySlot
        {
            [SerializeField]
            private ObjectItem _item;

            [SerializeField]
            private int _slotIndex = 1;

            public ObjectItem Item => _item;
            public int SlotIndex => _slotIndex;
        }

        [Serializable, HideInInspector]
        public class TaggedLayerDefault
        {
            public string Tag;
            public Sprite Sprite;
            public Vector2 Offset;
            public float Scale;
            public Color Tint;
        }

        [Serializable]
        public class ExperienceRank
        {
            [Tooltip("ID of the experience type (e.g., 'sword', 'riding', 'flying')")]
            [SerializeField]
            private string _experienceTypeId;

            [Tooltip("Current rank/level (E=0, D=1, C=2, B=3, A=4, S=5)")]
            [SerializeField]
            private LeveledLetteredField _rank = new(LeveledLetteredField.E);

            public string ExperienceTypeId
            {
                get => _experienceTypeId;
                set => _experienceTypeId = value;
            }

            public LeveledLetteredField Rank
            {
                get => _rank;
                set => _rank = value;
            }

            public ExperienceRank() { }

            public ExperienceRank(string experienceTypeId, string rankValue)
            {
                _experienceTypeId = experienceTypeId;
                _rank = new LeveledLetteredField(rankValue);
            }
        }
    }
}
