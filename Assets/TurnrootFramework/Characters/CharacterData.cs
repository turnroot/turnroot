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
using UnityEngine;

[Serializable]
public struct CharacterModelBlendshapeSet
{
    [Range(0f, 100f)]
    public float chestSize;

    [Range(0f, 100f)]
    public float waistSize;

    [Range(0f, 100f)]
    public float hipSize;

    [Range(0f, 100f)]
    public float thighThickness;

    [Range(0f, 100f)]
    public float armThickness;

    [Range(0f, 100f)]
    public float neckThickness;
}

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "NewCharacterConfiguration",
        menuName = "Turnroot/Characters/CharacterData"
    )]
    public class CharacterData : ScriptableObject, IHasStats
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

        [field: Foldout("Visual"), HideInInspector]
        public SerializableDictionary<string, TaggedLayerDefault> TaggedLayerDefaults
        {
            get;
            private set;
        } = new();
        private Portrait[] _portraitArrayCache;

        [field: Foldout("Visual"), SerializeField]
        public Sprite[] Sprites { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        [Tooltip("Complete base model of the character without class-specific parts")]
        public SkinnedMeshRenderer CharacterDefaultModel { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        [Tooltip("Parts of the character that are combined with a class model")]
        public SkinnedMeshRenderer CharacterHeadHandsAndHair { get; private set; }

        [field:
            Foldout("Rigging"),
            SerializeField,
            Tooltip("Enable if this character has an additional bone layer (+X)")
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

        [field:
            Foldout("Rigging"),
            SerializeField,
            Tooltip(
                "Optional per-character animator controller (or override) that contains animations specifically for the +X layer. Can be applied on a separate Animator layer."
            )
        ]
        public RuntimeAnimatorController ExtraLayerController { get; private set; }

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

        private bool CanShowUnique() =>
            Which == CharacterWhich.ENEMY || Which == CharacterWhich.NPC;

        private bool IsAllyOrRecruitable() => Which == CharacterWhich.ALLY || IsRecruitable;

        private bool IsRecruitableRequiresMinSupportLevel() =>
            IsRecruitable && RequiresMinSupportLevel;

        private bool IsRecruitableUseRecruitmentChance() => IsRecruitable && UseRecruitmentChance;

        // Helper: returns the dictionary values as an array (cached). Use when you need indexed access.
        public Portrait[] PortraitArray
        {
            get
            {
                _portraitArrayCache ??= Portraits?.Values.ToArray();
                return _portraitArrayCache;
            }
        }
        // auto-properties declared earlier provide the public surface for these fields

#if TURNROOT_BLOODLINES_MODULE
        public HereditaryTraits PassedDownTraits => _passedDownTraits;

        public bool HasDesignatedChildUnit => _hasDesignatedChildUnit;
        public CharacterData ChildUnitId => _childUnitId;
#endif

        // Editor helper: invalidate cached PortraitArray so editors can refresh after changes.
        public void InvalidatePortraitArrayCache() => _portraitArrayCache = null;

        // Editor/API convenience: allow saving/loading character defaults (called from StackedImageEditorWindow)
        // These perform minimal delegation to contained Portraits so editor UI can invoke them.
        public void SaveDefaults()
        {
            TaggedLayerDefaults.Clear();
            if (Portraits != null)
            {
                CharacterHelpers.ForEachPortraitLayer(
                    Portraits,
                    layer =>
                    {
                        if (!string.IsNullOrEmpty(layer.Tag))
                        {
                            TaggedLayerDefaults[layer.Tag] = new TaggedLayerDefault
                            {
                                Tag = layer.Tag,
                                Sprite = layer.Sprite,
                                Offset = layer.Offset,
                                Scale = layer.Scale,
                                Tint = layer.Tint,
                            };
                        }
                    }
                );
            }
        }

        public void LoadDefaults()
        {
            if (Portraits != null)
            {
                CharacterHelpers.ForEachPortraitLayer(
                    Portraits,
                    layer =>
                    {
                        if (
                            !string.IsNullOrEmpty(layer.Tag)
                            && TaggedLayerDefaults.TryGetValue(layer.Tag, out var def)
                        )
                        {
                            layer.Sprite = def.Sprite;
                            layer.Offset = def.Offset;
                            layer.Scale = def.Scale;
                            layer.Tint = def.Tint;
                        }
                    }
                );
            }
            InvalidatePortraitArrayCache();
        }

        // Helper methods to get stats by type
        public BoundedCharacterStat GetBoundedStat(BoundedStatType type) =>
            StatHelpers.GetBoundedStat(BoundedStats, type);

        public CharacterStat GetUnboundedStat(UnboundedStatType type) =>
            StatHelpers.GetUnboundedStat(UnboundedStats, type);

        public ExperienceRank GetExperienceRank(string experienceTypeId) =>
            ExperienceRanks?.Find(e => e.ExperienceTypeId == experienceTypeId);

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
            private CommonAncestors.LeveledLetteredField _rank = new(LeveledLetteredField.E);

            public string ExperienceTypeId
            {
                get => _experienceTypeId;
                set => _experienceTypeId = value;
            }

            public CommonAncestors.LeveledLetteredField Rank
            {
                get => _rank;
                set => _rank = value;
            }

            public ExperienceRank() { }

            public ExperienceRank(string experienceTypeId, string rankValue)
            {
                _experienceTypeId = experienceTypeId;
                _rank = new CommonAncestors.LeveledLetteredField(rankValue);
            }
        }

        /* ----------------------------- Core Functions ----------------------------- */

        private void OnEnable()
        {
            // Load settings from centralized cache
            var settings = CharacterSettings.PrototypeSettings;
            if (settings == null)
            {
                return;
            }

            // Initialize stats from defaults if stats are empty
            if (BoundedStats.Count == 0 && UnboundedStats.Count == 0)
            {
                var defaultStats = CharacterSettings.DefaultStats;
                if (defaultStats != null)
                {
                    BoundedStats = defaultStats.CreateBoundedStats();
                    UnboundedStats = defaultStats.CreateUnboundedStats();
                }
            }

            // Initialize personal growth rates if empty (set all unbounded stats to 0% growth)
            if (PersonalGrowthRates.Count == 0 && UnboundedStats.Count > 0)
            {
                foreach (var stat in UnboundedStats)
                {
                    PersonalGrowthRates.Add(new UnboundedStatModifier(stat.StatType, 0f));
                }
            }
        }

        private void OnValidate()
        {
            // Reset cached portrait array so changes in the inspector are reflected
            _portraitArrayCache = null;
            // Ensure that the character's name is not empty
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = "New Unit";
            }

            // Ensure that the full name is not empty
            if (string.IsNullOrWhiteSpace(FullName))
            {
                FullName = DisplayName;
            }

            // Validate support relationships - remove any that reference this character
            if (SupportRelationships != null)
            {
                var removed = SupportRelationship.SanitizeForCharacter(this, SupportRelationships);
                foreach (var r in removed)
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"Removed invalid support relationship: {name} cannot have a support relationship with themselves ({r.Character?.name})"
                    );
#endif
                }
            }

            // Editor-time validation for rigging properties
            if (HasExtraBoneLayer)
            {
                if (AdditionalBonesMask == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"{name}: 'HasExtraBoneLayer' is true but 'AdditionalBonesMask' is not set. This may cause Animator layering misconfiguration."
                    );
#endif
                }

                if (
                    (AdditionalBoneNames == null || AdditionalBoneNames.Length == 0)
                    && AdditionalBonesMask == null
                )
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"{name}: No additional bone names or AvatarMask were provided for the extra bone layer. Add names or an AvatarMask for tooling/runtime mapping."
                    );
#endif
                }
            }
        }
    }
}
