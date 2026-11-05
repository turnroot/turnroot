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
        [field: HideInInspector, SerializeField]
        public CharacterWhich Which { get; private set; } = new("Enemy");

        [field: HideInInspector, SerializeField]
        public string DisplayName { get; private set; } = "New Unit";

        [field: Foldout("Identity"), SerializeField]
        public string FullName { get; private set; } = "Newly Created Unit";

        [field: HideInInspector, SerializeField]
        public Pronouns CharacterPronouns { get; private set; } = new();

        [field: HideInInspector, SerializeField, Range(1, 31)]
        public int BirthdayDay { get; private set; } = 1;
        [field: HideInInspector, SerializeField, Range(1, 12)]
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

        [field: HideInInspector, SerializeField]
        public string[] Likes { get; private set; } = new string[0];

        [field: HideInInspector, SerializeField]
        public string[] Dislikes { get; private set; } = new string[0];

        [field: Foldout("Description"), SerializeField, ResizableTextArea]
        public string Notes { get; private set; } =
            "Take private notes (only in the editor) about this unit";

        [field: HideInInspector, SerializeField]
        public bool CanHaveChildren { get; private set; } = false;

        [field: HideInInspector, SerializeField]
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

        [SerializeField, HideInInspector]
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

        [field: HideInInspector, SerializeField]
        public Color AccentColor1 { get; private set; }

        [field: HideInInspector, SerializeField]
        public Color AccentColor2 { get; private set; }

        [field: HideInInspector, SerializeField]
        public Color AccentColor3 { get; private set; }

    }
}
