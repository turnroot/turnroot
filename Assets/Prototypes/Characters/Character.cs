using System;
using System.Collections.Generic;
using Assets.Prototypes.Characters.Configuration;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Characters.Subclasses;
using NaughtyAttributes;
using UnityEngine;

namespace Assets.Prototypes.Characters
{
    [CreateAssetMenu(fileName = "NewCharacterConfiguration", menuName = "Character/Character")]
    public class Character : ScriptableObject
    {
        [Foldout("Identity"), SerializeField]
        [HorizontalLine(color: EColor.Blue)]
        private CharacterWhich _which = CharacterWhich.Enemy;

        [Foldout("Identity"), SerializeField]
        private string _name = "New Unit";

        [Foldout("Identity"), SerializeField]
        private string _fullName = "Newly Created Unit";

        [Foldout("Identity"), SerializeField]
        private string _title = "";

        [Foldout("Identity"), SerializeField]
        private string _team;

        [Foldout("Demographics"), SerializeField]
        [HorizontalLine(color: EColor.Blue)]
        private int _age = 18;

        [Foldout("Demographics"), SerializeField]
        private Pronouns _pronouns = new Pronouns();

        [Foldout("Demographics"), SerializeField]
        private float _height = 166f;

        [Foldout("Demographics"), SerializeField]
        private int _birthdayDay = 1;

        [Foldout("Demographics"), SerializeField]
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
        private bool _canHaveChildren = false;

        [Foldout("Character Flags"), SerializeField]
        private bool _isRecruitable = false;

        [Foldout("Character Flags"), SerializeField]
        private bool _isUnique = false;

        [Foldout("Visual"), SerializeField]
        [HorizontalLine(color: EColor.Orange)]
        private bool _useAccentColors = false;

        [Foldout("Visual"), SerializeField, ShowIf("_useAccentColors")]
        private Color _accentColor1 = Color.black;

        [Foldout("Visual"), SerializeField, ShowIf("_useAccentColors")]
        private Color _accentColor2 = Color.black;

        [Foldout("Visual"), SerializeField]
        private Sprite[] _portraits;

        [Foldout("Visual"), SerializeField]
        private Sprite[] _sprites;

        [Foldout("Stats & Progression"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private int _level = 1;

        [Foldout("Stats & Progression"), SerializeField]
        private int _exp = 0;

        [Foldout("Stats & Progression"), SerializeField]
        private List<BoundedCharacterStat> _boundedStats = new List<BoundedCharacterStat>();

        [Foldout("Stats & Progression"), SerializeField]
        private List<CharacterStat> _unboundedStats = new List<CharacterStat>();

        [Foldout("Experiences"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private UnityEngine.Object _experiences;

        [Foldout("Experiences"), SerializeField]
        private UnityEngine.Object _experienceGrowths;

        [Foldout("Experiences"), SerializeField]
        private UnityEngine.Object _experienceAptitudes;

        [Foldout("Experiences"), SerializeField]
        private SerializableDictionary<string, int> _classExps =
            new SerializableDictionary<string, int>();

        [Foldout("Class & Battalion"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private UnityEngine.Object _unitClass;

        [Foldout("Class & Battalion"), SerializeField]
        private UnityEngine.Object _battalion;

        [Foldout("Class & Battalion"), SerializeField]
        private List<string> _specialUnitClasses = new List<string>();

        [Foldout("Skills & Abilities"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private List<string> _skills = new List<string>();

        [Foldout("Skills & Abilities"), SerializeField]
        private List<string> _specialSkills = new List<string>();

        [Foldout("AI & Behavior"), SerializeField]
        [HorizontalLine(color: EColor.Yellow)]
        private UnityEngine.Object _ai;

        [Foldout("AI & Behavior"), SerializeField]
        private List<string> _goals = new List<string>();

        [Foldout("Relationships"), SerializeField, ReorderableList]
        [HorizontalLine(color: EColor.Pink)]
        private List<SupportRelationship> _supportRelationships = new List<SupportRelationship>();

        [Foldout("Heredity"), SerializeField]
        [HorizontalLine(color: EColor.Pink)]
        private HereditaryTraits _passedDownTraits = new HereditaryTraits();

        [Foldout("Heredity"), SerializeField]
        private Character _childUnitId;
        public CharacterWhich Which => _which;
        public string Name => _name;
        public string FullName => _fullName;
        public string Title => _title;
        public string Team => _team;

        public int Age => _age;
        public Pronouns CharacterPronouns => _pronouns;
        public float Height => _height;
        public int BirthdayDay => _birthdayDay;
        public int BirthdayMonth => _birthdayMonth;

        public string ShortDescription => _shortDescription;
        public string Notes => _notes;

        public bool CanSSupport => _canSSupport;
        public bool CanHaveChildren => _canHaveChildren;
        public bool IsRecruitable => _isRecruitable;
        public bool IsUnique => _isUnique;

        public bool UseAccentColors => _useAccentColors;
        public Color AccentColor1 => _accentColor1;
        public Color AccentColor2 => _accentColor2;
        public Sprite[] Portraits => _portraits;
        public Sprite[] Sprites => _sprites;

        public int Level => _level;
        public int Exp => _exp;
        public List<BoundedCharacterStat> BoundedStats => _boundedStats;
        public List<CharacterStat> UnboundedStats => _unboundedStats;

        public UnityEngine.Object Experiences => _experiences;
        public UnityEngine.Object ExperienceGrowths => _experienceGrowths;
        public UnityEngine.Object ExperienceAptitudes => _experienceAptitudes;

        public UnityEngine.Object UnitClass => _unitClass;
        public UnityEngine.Object Battalion => _battalion;
        public List<string> SpecialUnitClasses => _specialUnitClasses;

        public List<string> Skills => _skills;
        public List<string> SpecialSkills => _specialSkills;

        public UnityEngine.Object AI => _ai;
        public List<string> Goals => _goals;

        public List<SupportRelationship> SupportRelationships => _supportRelationships;

        public HereditaryTraits PassedDownTraits => _passedDownTraits;
        public Character ChildUnitId => _childUnitId;

        // Helper methods for class experience
        public int GetClassExp(string classId)
        {
            return _classExps.TryGetValue(classId, out int value) ? value : 0;
        }

        public void SetClassExp(string classId, int value)
        {
            _classExps[classId] = value;
        }

        public SerializableDictionary<string, int> ClassExps => _classExps;

        // Helper methods to get stats by type
        public BoundedCharacterStat GetBoundedStat(BoundedStatType type)
        {
            return _boundedStats.Find(s => s.StatType == type);
        }

        public CharacterStat GetUnboundedStat(UnboundedStatType type)
        {
            return _unboundedStats.Find(s => s.StatType == type);
        }

        // Helper methods for support relationships
        public SupportRelationship GetSupportRelationship(Character character)
        {
            return _supportRelationships.Find(s => s.Character == character);
        }

        public void AddSupportRelationship(Character character)
        {
            if (_supportRelationships.Find(s => s.Character == character) == null)
            {
                _supportRelationships.Add(new SupportRelationship { Character = character });
            }
        }

        public void RemoveSupportRelationship(Character character)
        {
            _supportRelationships.RemoveAll(s => s.Character == character);
        }
    }
}
