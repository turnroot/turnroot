using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters.Components;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Configuration;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "NewCharacterConfiguration",
        menuName = "Turnroot/Character/CharacterData"
    )]
    public class CharacterData : ScriptableObject
    {
        [Serializable]
        public class InventorySlot
        {
            [SerializeField]
            private ObjectItem _item;

            [SerializeField]
            private int _quantity = 1;

            public ObjectItem Item => _item;
            public int Quantity => _quantity;
        }

        /* ----------------------------- Core Functions ----------------------------- */

        private void OnEnable()
        {
            // Load settings from Resources on initialization
            var settings = Resources.Load<CharacterPrototypeSettings>(
                "GameSettings/CharacterPrototypeSettings"
            );
            if (settings != null)
            {
                _useAccentColors = settings.UseAccentColors;
            }

            // Initialize stats from defaults if stats are empty
            if (_boundedStats.Count == 0 && _unboundedStats.Count == 0)
            {
                var defaultStats = Resources.Load<DefaultCharacterStats>(
                    "GameSettings/DefaultCharacterStats"
                );
                if (defaultStats != null)
                {
                    _boundedStats = defaultStats.CreateBoundedStats();
                    _unboundedStats = defaultStats.CreateUnboundedStats();
                }
            }
        }

        private void OnValidate()
        {
            // Ensure that the character's name is not empty
            if (string.IsNullOrWhiteSpace(_name))
            {
                _name = "New Unit";
            }

            // Ensure that the full name is not empty
            if (string.IsNullOrWhiteSpace(_fullName))
            {
                _fullName = _name;
            }
        }

        [Foldout("Identity"), SerializeField]
        [HorizontalLine(color: EColor.Blue)]
        private readonly CharacterWhich _which = new("Enemy");

        [Foldout("Identity"), SerializeField]
        private string _name = "New Unit";

        [Foldout("Identity"), SerializeField]
        private string _fullName = "Newly Created Unit";

        [Foldout("Identity")]
        private string _team;

        [Foldout("Demographics"), SerializeField]
        [HorizontalLine(color: EColor.Blue)]
        private Pronouns _pronouns = new();

        [Foldout("Demographics"), SerializeField, Range(100f, 250f)]
        private float _height = 166f;

        [Foldout("Demographics"), SerializeField, Range(1, 31)]
        private int _birthdayDay = 1;

        [Foldout("Demographics"), SerializeField, Range(1, 12)]
        private int _birthdayMonth = 1;

        [Foldout("Description"), SerializeField, ResizableTextArea]
        [HorizontalLine(color: EColor.Blue)]
        private string _shortDescription = "A new unit";

        [Foldout("Description"), SerializeField, ResizableTextArea]
        private string _notes = "Take private notes (only in the editor) about this unit";

        [Foldout("Character Flags"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private bool _canSSupport = false;

        [Foldout("Character Flags"), SerializeField]
        private bool _canSSupportAvatar = false;

        [Foldout("Character Flags"), SerializeField]
        private bool _canHaveChildren = false;

        [Foldout("Character Flags"), SerializeField]
        private bool _isRecruitable = false;

        [Foldout("Character Flags"), SerializeField]
        private bool _isUnique = false;

        [Foldout("Visual"), HideInInspector]
        [HorizontalLine(color: EColor.Orange)]
        private bool _useAccentColors = false;

        [Foldout("Visual"), SerializeField, ShowIf("_useAccentColors")]
        private Color _accentColor1 = Color.black;

        [Foldout("Visual"), SerializeField, ShowIf("_useAccentColors")]
        private Color _accentColor2 = Color.black;

        [Foldout("Visual"), SerializeField, ShowIf("_useAccentColors")]
        private Color _accentColor3 = Color.black;

        [Foldout("Visual"), SerializeField]
        private Portrait[] _portraits;

        [Foldout("Visual"), SerializeField]
        private Sprite[] _sprites;

        [Foldout("Stats & Progression"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private int _level = 1;

        [Foldout("Stats & Progression"), SerializeField]
        private int _exp = 0;

        [Foldout("Stats & Progression"), SerializeField]
        private List<BoundedCharacterStat> _boundedStats = new();

        [Foldout("Stats & Progression"), SerializeField]
        private List<CharacterStat> _unboundedStats = new();

        [Foldout("Class & Battalion"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private UnityEngine.Object _unitClass;

        [Foldout("Class & Battalion"), SerializeField]
        private UnityEngine.Object _battalion;

        [Foldout("Class & Battalion"), SerializeField]
        private List<string> _specialUnitClasses = new();

        [Foldout("Skills & Abilities"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private List<Skill> _skills = new();

        [Foldout("Skills & Abilities"), SerializeField]
        private List<Skill> _specialSkills = new();

        [Foldout("Inventory"), SerializeField]
        private List<InventorySlot> _startingInventory = new();

        [Foldout("Relationships"), SerializeField]
        private List<SupportRelationship> _supportRelationships = new();

        [Foldout("AI & Behavior"), SerializeField]
        [HorizontalLine(color: EColor.Yellow)]
        private UnityEngine.Object _ai;

        [Foldout("AI & Behavior"), SerializeField]
        private List<string> _goals = new();

        [Foldout("Heredity"), SerializeField]
        [HorizontalLine(color: EColor.Pink)]
        private HereditaryTraits _passedDownTraits = new();

        [Foldout("Heredity"), SerializeField]
        private bool _hasDesignatedChildUnit = false;

        [Foldout("Heredity"), SerializeField, ShowIf(nameof(_hasDesignatedChildUnit))]
        private CharacterData _childUnitId;

        public CharacterWhich Which => _which;
        public string Name => _name;
        public string FullName => _fullName;
        public string Team => _team;
        public Pronouns CharacterPronouns => _pronouns;
        public float Height => _height;
        public int BirthdayDay => _birthdayDay;
        public int BirthdayMonth => _birthdayMonth;

        public string ShortDescription => _shortDescription;
        public string Notes => _notes;

        public bool CanSSupport => _canSSupport;

        public bool CanSSupportAvatar => _canSSupportAvatar;
        public bool CanHaveChildren => _canHaveChildren;
        public bool IsRecruitable => _isRecruitable;
        public bool IsUnique => _isUnique;
        public Color AccentColor1 => _accentColor1;
        public Color AccentColor2 => _accentColor2;
        public Color AccentColor3 => _accentColor3;
        public Portrait[] Portraits => _portraits;
        public Sprite[] Sprites => _sprites;

        public int Level => _level;
        public int Exp => _exp;
        public List<BoundedCharacterStat> BoundedStats => _boundedStats;
        public List<CharacterStat> UnboundedStats => _unboundedStats;

        public UnityEngine.Object UnitClass => _unitClass;
        public UnityEngine.Object Battalion => _battalion;
        public List<string> SpecialUnitClasses => _specialUnitClasses;

        public List<Skill> Skills => _skills;
        public List<Skill> SpecialSkills => _specialSkills;

        public UnityEngine.Object AI => _ai;
        public List<string> Goals => _goals;

        public List<InventorySlot> StartingInventory => _startingInventory;
        public List<SupportRelationship> SupportRelationships => _supportRelationships;

        public HereditaryTraits PassedDownTraits => _passedDownTraits;

        public bool HasDesignatedChildUnit => _hasDesignatedChildUnit;
        public CharacterData ChildUnitId => _childUnitId;

        // Helper methods to get stats by type
        public BoundedCharacterStat GetBoundedStat(BoundedStatType type)
        {
            return StatHelpers.GetBoundedStat(_boundedStats, type);
        }

        public CharacterStat GetUnboundedStat(UnboundedStatType type)
        {
            return StatHelpers.GetUnboundedStat(_unboundedStats, type);
        }
    }
}
