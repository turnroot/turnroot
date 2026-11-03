using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Characters.Configuration;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Skills;
using UnityEngine;

namespace Turnroot.Characters
{
    public partial class CharacterData
    {
        #region Class and Loadout Progression
        // ---------------------------------------------------------------------
        // Class progression ladder (per-character, generic units only)
        // ---------------------------------------------------------------------
        [Serializable]
        public struct ClassProgressionLadder
        {
            [Serializable]
            public struct TierData
            {
                [Tooltip("Class assigned to this progression tier")]
                public CharacterClassData Class;

                [Tooltip(
                    "Possible items awarded when this tier is selected. Percentages are independent and roll each time."
                )]
                public List<LoadoutEntry> Loadout;
            }

            public TierData Starter;
            public TierData Base;
            public TierData Advanced;
            public TierData Master;
            public TierData Expert;

            public CharacterClassData GetClassForTier(ProgressionLevel tier)
            {
                return tier switch
                {
                    ProgressionLevel.Starter => Starter.Class,
                    ProgressionLevel.Base => Base.Class,
                    ProgressionLevel.Advanced => Advanced.Class,
                    ProgressionLevel.Master => Master.Class,
                    ProgressionLevel.Expert => Expert.Class,
                    _ => null,
                };
            }

            public List<LoadoutEntry> GetLoadoutForTier(ProgressionLevel tier)
            {
                return tier switch
                {
                    ProgressionLevel.Starter => Starter.Loadout,
                    ProgressionLevel.Base => Base.Loadout,
                    ProgressionLevel.Advanced => Advanced.Loadout,
                    ProgressionLevel.Master => Master.Loadout,
                    ProgressionLevel.Expert => Expert.Loadout,
                    _ => null,
                };
            }
        }

        [Serializable]
        public struct LoadoutEntry
        {
            public ObjectItem Item;

            [Range(0f, 100f)]
            public float Chance;
        }
        #endregion

        [field:
            SerializeField,
            Tooltip(
                "If true, use the per-tier class ladder defined below instead of the usual StartingClass. Hidden for unique characters."
            ),
            ShowIf(nameof(CanShowClassProgressionOption))
        ]
        public bool UseClassProgressionLadder { get; private set; } = false;

        [field: SerializeField]
        [ShowIf(nameof(ShowClassProgressionFields))]
        public ClassProgressionLadder ProgressionLadder = new();

        [field: SerializeField]
        public List<BoundedCharacterStat> BoundedStats { get; private set; } = new();

        [field: SerializeField, HideInInspector]
        public List<CharacterStat> UnboundedStats { get; private set; } = new();

        [field: SerializeField, HideInInspector]
        [Tooltip(
            "Personal growth rates (percentage 0-100) for stat increases on level up. If empty, uses class growth rates only."
        )]
        public List<UnboundedStatModifier> PersonalGrowthRates { get; private set; } = new();

        [field: Foldout("Class and Skills"), SerializeField, HorizontalLine(color: EColor.Green)]
        [field: HideInInspector]
        internal List<Skill> Skills { get; private set; } = new();

        [field: Foldout("Class and Skills"), SerializeField]
        [field: Tooltip(
            "Personal skill assigned to this unit. This is a single, always-equipped ability."
        )]
        // TODO: PersonalSkill cannot be unequipped - runtime systems should treat this as permanently assigned.
        public Skill PersonalSkill { get; private set; }

        [field:
            BoxGroup("Experience & Aptitudes"),
            SerializeField,
            HorizontalLine(color: EColor.Indigo)
        ]
        [Tooltip(
            "Experience/aptitude ranks for weapon types and other trainable skills (e.g., Riding, Flying)"
        )]
        [field: HideInInspector]
        public List<ExperienceRank> ExperienceRanks { get; private set; } = new();


        public Color HairColor => _hairColor;
        public Color EyeColor => _eyeColor;

        public bool IsNotAvatar => Which != CharacterWhich.AVATAR;
        public bool IsEnemyOrNPC => Which == CharacterWhich.ENEMY || Which == CharacterWhich.NPC;

        // NaughtyAttributes ShowIf helper methods

        #region Recruitment and Visibility Rules

        private bool CanShowRecruitable() =>
            Which == CharacterWhich.ENEMY
            || Which == CharacterWhich.NPC
            || Which == CharacterWhich.ALLY;

        private bool CanShowUnique() => Which != CharacterWhich.AVATAR;

        // progress ladder toggle is only visible on generic characters (not unique) and
        // follows the same avatar restriction used by CanShowUnique
        private bool CanShowClassProgressionOption() => !IsUnique && CanShowUnique();

        // the ladder struct itself only shows when the option is enabled and the
        // character is not unique (guarding against case where option was enabled
        // then later made unique)
        private bool ShowClassProgressionFields() => UseClassProgressionLadder && !IsUnique;

        public bool IsAllyOrRecruitable() => Which == CharacterWhich.ALLY || IsRecruitable;

        private bool IsRecruitableRequiresMinSupportLevel() =>
            IsRecruitable && RecruitRequiresMinSupportLevel;

        #endregion

        public Portrait[] PortraitArray
        {
            get
            {
                _portraitArrayCache ??= Portraits?.Select(p => p.Portrait).ToArray();
                return _portraitArrayCache;
            }
        }


        public HereditaryTraits PassedDownTraits => _passedDownTraits;

        public bool HasDesignatedChildUnit => _hasDesignatedChildUnit;
        public CharacterData ChildUnitId => _childUnitId;


        /// <summary>
        /// Represents an item slot in a character's inventory with an item and slot index.
        /// </summary>
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

        /// <summary>
        /// Defines default visual layer settings for a tagged portrait layer (sprite, offset, scale, tint).
        /// </summary>
        [Serializable, HideInInspector]
        public class TaggedLayerDefault
        {
            public string Tag;
            public Sprite Sprite;
            public Vector2 Offset;
            public float Scale;
            public Color Tint;
        }
    }
}
