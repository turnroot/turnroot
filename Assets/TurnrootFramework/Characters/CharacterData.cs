using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.CommonAncestors;
using UnityEngine;
using UnityEngine.Serialization;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "NewCharacterConfiguration",
        menuName = "Turnroot/Characters/CharacterData"
    )]
    public partial class CharacterData : ScriptableObject, IHasStats
    {
        [field: Foldout("Identity"), HorizontalLine(color: EColor.White), SerializeField]
        public CharacterWhich Which { get; private set; } = new("Enemy");

        [field: Foldout("Identity"), SerializeField]
        public string DisplayName { get; private set; } = "New Unit";

        [field: Foldout("Identity"), SerializeField]
        public string FullName { get; private set; } = "Newly Created Unit";

        [field: Foldout("Demographics"), HorizontalLine(color: EColor.Black), SerializeField]
        public Pronouns CharacterPronouns { get; private set; } = new();

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

        [field: Foldout("Character Flags"), SerializeField, ShowIf(nameof(IsAllyOrRecruitable))]
        public bool CanHaveChildren { get; private set; } = false;

        [field: Foldout("Character Flags"), SerializeField, ShowIf(nameof(CanShowRecruitable))]
        public bool IsRecruitable { get; private set; } = false;

        [field: SerializeField, HideInInspector]
        public bool RecruitRequiresMinSupportLevel { get; private set; } = true;

        [field: SerializeField, HideInInspector]
        public LeveledLetteredField RecruitSupportRelationshipMinRank { get; private set; } =
            new LeveledLetteredField(LeveledLetteredField.E);

        [HideInInspector]
        public bool AvatarMustHaveMinimumExperienceLevelsToRecruit;

        [HideInInspector]
        public List<ExperienceRank> AvatarMinimumExperienceRanksToRecruit = new();

        [HideInInspector]
        public bool SupportCanCompensateForMissingExperienceLevels;

        [field: SerializeField, HideInInspector]
        public LeveledLetteredField RecruitCompensationSupportLevel { get; private set; } =
            new LeveledLetteredField(LeveledLetteredField.E);

        [HideInInspector]
        public bool WillJoinIfAllyIsAlreadyRecruited;

        [HideInInspector]
        public CharacterData SpecificAllyRequiredForRecruitment;

        [SerializeField, Foldout("Character Flags"), ShowIf(nameof(CanShowUnique))]
        [FormerlySerializedAs("<IsUnique>k__BackingField")]
        private bool _isUnique = false;
        public bool IsUnique => Which == CharacterWhich.AVATAR || _isUnique;

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

        [Foldout("Visual"), SerializeField]
        private Color _hairColor;

        [Foldout("Visual"), SerializeField]
        private Color _eyeColor;

        [field: Foldout("Visual"), SerializeField]
        public Color AccentColor1 { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        public Color AccentColor2 { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        public Color AccentColor3 { get; private set; }

    }
}
