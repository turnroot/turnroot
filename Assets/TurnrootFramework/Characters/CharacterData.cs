using System;
using System.Collections.Generic;
using System.Linq;
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
            // Reset cached portrait array so changes in the inspector are reflected
            _portraitArrayCache = null;
            // Ensure that the character's name is not empty
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                _displayName = "New Unit";
            }

            // Ensure that the full name is not empty
            if (string.IsNullOrWhiteSpace(_fullName))
            {
                _fullName = _displayName;
            }

            // Validate support relationships - remove any that reference this character
            if (_supportRelationships != null)
            {
                _supportRelationships.RemoveAll(rel =>
                {
                    if (rel.Character == this)
                    {
                        Debug.LogWarning(
                            $"Removed invalid support relationship: {name} cannot have a support relationship with themselves"
                        );
                        return true;
                    }
                    return false;
                });

                // Ensure all support relationships have proper default values
                foreach (var rel in _supportRelationships)
                {
                    rel.InitializeDefaults();
                }
            }
        }

#if UNITY_EDITOR
        [
            InfoBox(
                "This is pre-runtime data. Use this editor to define the character's base stats, skills, inventory, and relationships- anything that should be in place before the game starts."
            ),
            SerializeField
        ]
        private string __;
#endif

        [Foldout("Identity"), SerializeField]
        [HorizontalLine(color: EColor.Blue)]
        private readonly CharacterWhich _which = new("Enemy");

        [Foldout("Identity"), SerializeField]
        private string _displayName = "New Unit";

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
        private SerializableDictionary<string, Portrait> _portraits;

        [Foldout("Visual"), HideInInspector]
        private SerializableDictionary<string, TaggedLayerDefault> _taggedLayerDefaults = new();

        // Cached array view of the portraits dictionary values. Use PortraitArray to access.
        private Portrait[] _portraitArrayCache;

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

        [Foldout("Heredity"), SerializeField]
        [HorizontalLine(color: EColor.Pink)]
        private HereditaryTraits _passedDownTraits = new();

        [Foldout("Heredity"), SerializeField]
        private bool _hasDesignatedChildUnit = false;

        [Foldout("Heredity"), SerializeField, ShowIf(nameof(_hasDesignatedChildUnit))]
        private CharacterData _childUnitId;

        public CharacterWhich Which => _which;
        public string DisplayName => _displayName;
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
        public SerializableDictionary<string, Portrait> Portraits => _portraits;

        public SerializableDictionary<string, TaggedLayerDefault> TaggedLayerDefaults =>
            _taggedLayerDefaults;

        // Helper: returns the dictionary values as an array (cached). Use when you need indexed access.
        public Portrait[] PortraitArray
        {
            get
            {
                if (_portraitArrayCache == null)
                {
                    _portraitArrayCache = _portraits?.Values.ToArray();
                }
                return _portraitArrayCache;
            }
        }
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

        public List<InventorySlot> StartingInventory => _startingInventory;
        public List<SupportRelationship> SupportRelationships => _supportRelationships;

        public HereditaryTraits PassedDownTraits => _passedDownTraits;

        public bool HasDesignatedChildUnit => _hasDesignatedChildUnit;
        public CharacterData ChildUnitId => _childUnitId;

        // Editor helper: invalidate cached PortraitArray so editors can refresh after changes.
        public void InvalidatePortraitArrayCache()
        {
            _portraitArrayCache = null;
        }

        // Editor/API convenience: allow saving/loading character defaults (called from StackedImageEditorWindow)
        // These perform minimal delegation to contained Portraits so editor UI can invoke them.
        public void SaveDefaults()
        {
            _taggedLayerDefaults.Clear();
            if (_portraits != null)
            {
                foreach (var portrait in _portraits.Values)
                {
                    if (portrait.ImageStack?.Layers != null)
                    {
                        foreach (var layer in portrait.ImageStack.Layers)
                        {
                            if (!string.IsNullOrEmpty(layer.Tag))
                            {
                                _taggedLayerDefaults[layer.Tag] = new TaggedLayerDefault
                                {
                                    Tag = layer.Tag,
                                    Sprite = layer.Sprite,
                                    Offset = layer.Offset,
                                    Scale = layer.Scale,
                                    Tint = layer.Tint,
                                };
                            }
                        }
                    }
                }
            }
        }

        public void LoadDefaults()
        {
            if (_portraits != null)
            {
                foreach (var portrait in _portraits.Values)
                {
                    if (portrait.ImageStack?.Layers != null)
                    {
                        foreach (var layer in portrait.ImageStack.Layers)
                        {
                            if (
                                !string.IsNullOrEmpty(layer.Tag)
                                && _taggedLayerDefaults.ContainsKey(layer.Tag)
                            )
                            {
                                var def = _taggedLayerDefaults[layer.Tag];
                                layer.Sprite = def.Sprite;
                                layer.Offset = def.Offset;
                                layer.Scale = def.Scale;
                                layer.Tint = def.Tint;
                            }
                        }
                    }
                }
            }
            InvalidatePortraitArrayCache();
        }

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
