using System;
using System.Collections.Generic;
using Assets.Prototypes.Characters.Configuration;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Characters.Subclasses;
using NaughtyAttributes;
using UnityEngine;

namespace Assets.Prototypes.Characters
{
    [CreateAssetMenu(
        fileName = "NewCharacterConfiguration",
        menuName = "Turnroot/Character/CharacterData"
    )]
    public class CharacterData : ScriptableObject
    {
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

        [Foldout("CharacterData Flags"), SerializeField]
        [HorizontalLine(color: EColor.Green)]
        private bool _canSSupport = false;

        [Foldout("CharacterData Flags"), SerializeField]
        private bool _canHaveChildren = false;

        [Foldout("CharacterData Flags"), SerializeField]
        private bool _isRecruitable = false;

        [Foldout("CharacterData Flags"), SerializeField]
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
        private List<Skill> _skills = new List<Skill>();

        [Foldout("Skills & Abilities"), SerializeField]
        private List<Skill> _specialSkills = new List<Skill>();

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
        private CharacterData _childUnitId;

        [Foldout("Attachments"), SerializeField]
        [HorizontalLine(color: EColor.Black)]
        private CharacterInventoryInstance _characterInventory;
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
        public Color AccentColor1 => _accentColor1;
        public Color AccentColor2 => _accentColor2;
        public Color AccentColor3 => _accentColor3;
        public Portrait[] Portraits => _portraits;
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

        public List<Skill> Skills => _skills;
        public List<Skill> SpecialSkills => _specialSkills;

        public UnityEngine.Object AI => _ai;
        public List<string> Goals => _goals;

        public List<SupportRelationship> SupportRelationships => _supportRelationships;

        public HereditaryTraits PassedDownTraits => _passedDownTraits;
        public CharacterData ChildUnitId => _childUnitId;

        public CharacterInventoryInstance CharacterInventory => _characterInventory;

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
        public SupportRelationship GetSupportRelationship(CharacterData character)
        {
            return _supportRelationships.Find(s => s.Character == character);
        }

        public void AddSupportRelationship(CharacterData character)
        {
            if (_supportRelationships.Find(s => s.Character == character) == null)
            {
                _supportRelationships.Add(new SupportRelationship { Character = character });
            }
        }

        public void RemoveSupportRelationship(CharacterData character)
        {
            _ = _supportRelationships.RemoveAll(s => s.Character == character);
        }
    }
}
