using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    [System.Serializable]
    public struct ForgeOption
    {
        [SerializeField]
        public ObjectItem ForgeInto;

        [SerializeField]
        public int Price;

        [SerializeField]
        public ObjectItem Item;

        [SerializeField]
        public int ItemAmount;
    }

    [CreateAssetMenu(fileName = "ObjectItem", menuName = "Turnroot/Objects/Gameplay Item")]
    public class ObjectItem : ScriptableObject
    {
        [Foldout("Identity"), SerializeField, HorizontalLine(color: EColor.Black)]
        private string _name = "New Item";

        [Foldout("Identity")]
        private readonly string _id = System.Guid.NewGuid().ToString();

        [TextArea, Foldout("Identity"), SerializeField]
        private string _flavorText = "A new item";

        [Foldout("Identity"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private bool _isUnequippable = true;

        [Foldout("Type"), SerializeField, HorizontalLine(color: EColor.Blue)]
        private ObjectSubtype _subtype = new(ObjectSubtype.Weapon);

        [Foldout("Type"), SerializeField, ShowIf(nameof(IsEquipableSubtype))]
        private EquipableObjectType _equipableType;

        [Foldout("Type"), SerializeField, ShowIf(nameof(IsWeaponSubtype))]
        private WeaponType _weaponType;

        [Foldout("Pricing"), SerializeField, HorizontalLine(color: EColor.Gray)]
        private int _basePrice = 100;

        public int BasePrice => _basePrice;

        [Foldout("Pricing"), SerializeField]
        private bool _sellable = true;

        public bool Sellable => _sellable;

        [Foldout("Pricing"), SerializeField]
        private bool _buyable = true;

        public bool Buyable => _buyable;

        [Foldout("Pricing"), SerializeField]
        private int _sellPriceDeductedPerUse = 2;

        public int SellPriceDeductedPerUse => _sellPriceDeductedPerUse;

        [
            Foldout("Repair"),
            SerializeField,
            HorizontalLine(color: EColor.Green),
            ShowIf(nameof(IsWeaponOrMagicSubtype))
        ]
        private bool _repairable = true;

        public bool Repairable => _repairable;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairable))]
        private int _repairPricePerUse = 10;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairable))]
        private bool _repairNeedsItems = true;

        public bool RepairNeedsItems => _repairNeedsItems;

        [
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems))
        ]
        private ObjectItem _repairItem;

        public ObjectItem RepairItem => _repairItem;

        [
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems))
        ]
        private int _repairItemAmountPerUse = 1;

        public int RepairItemAmountPerUse => _repairItemAmountPerUse;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private bool _forgeable = false;

        public bool Forgeable => _forgeable;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsForgeable))]
        private ForgeOption[] _forgeOptions;

        public ForgeOption[] ForgeOptions => _forgeOptions;

        [
            Foldout("Lost Items"),
            SerializeField,
            HorizontalLine(color: EColor.White),
            ShowIf(nameof(IsLostItemSubtype))
        ]
        private CharacterData _belongsTo;

        [
            SerializeField,
            Foldout("Gift"),
            ShowIf(nameof(IsGiftSubtype)),
            HorizontalLine(color: EColor.Indigo)
        ]
        private int _giftRank = 1;

        [Foldout("Gift"), SerializeField, ShowIf(nameof(IsGiftSubtype))]
        private CharacterData[] _unitsLove;

        [Foldout("Gift"), SerializeField, ShowIf(nameof(IsGiftSubtype))]
        private CharacterData[] _unitsHate;

        [
            Foldout("Range"),
            SerializeField,
            HorizontalLine(color: EColor.Orange),
            ShowIf(nameof(IsWeaponOrMagicOrStaffSubtype))
        ]
        private int _lowerRange = 0;

        [Foldout("Range"), SerializeField, ShowIf(nameof(IsWeaponOrMagicOrStaffSubtype))]
        private int _upperRange = 0;

        public int LowerRange => _lowerRange;
        public int UpperRange => _upperRange;

        [Foldout("Range"), SerializeField, ShowIf(nameof(IsWeaponOrMagicOrStaffSubtype))]
        private bool _rangeAdjustedByStat = false;

        [
            Foldout("Range"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicOrStaffSubtypeAndIsRangeAdjusted))
        ]
        private UnboundedStatType _rangeAdjustedByStatName = UnboundedStatType.Strength;

        [
            Foldout("Range"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicOrStaffSubtypeAndIsRangeAdjusted))
        ]
        private int _rangeAdjustedByStatAmount = 0;

        [
            Foldout("Durability"),
            HideInInspector,
            ShowIf(nameof(IsWeaponOrMagicSubtype)),
            HorizontalLine(color: EColor.Pink)
        ]
        private bool _durability = true;

        [HideInInspector]
        public bool Durability => _durability;

        [
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsDurability))
        ]
        private int _maxUses = 100;
        public int MaxUses => _maxUses;

        [
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsDurability))
        ]
        private bool _replenishUsesAfterBattle = false;

        [
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsDurabilityAndIsReplenishUsesAfterBattle))
        ]
        private ReplenishUseType _replenishUsesAfterBattleAmount = ReplenishUseType.None;

        [
            Foldout("Combat"),
            SerializeField,
            HorizontalLine(color: EColor.Red),
            ShowIf(nameof(IsWeaponOrMagicSubtype))
        ]
        private float _weight = 1.0f;

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private float _might = 0f;

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private float _hit = 0f;

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private float _critical = 0f;

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private SpeciesType[] _speciesEffectiveAgainst = new SpeciesType[0];

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private WeaponType[] _weaponTypesEffectiveAgainst = new WeaponType[0];

        // Public getters for effectiveness criteria
        public SpeciesType[] SpeciesEffectiveAgainst => _speciesEffectiveAgainst;
        public WeaponType[] WeaponTypesEffectiveAgainst => _weaponTypesEffectiveAgainst;

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private SerializableDictionary<UnboundedStatType, float> _StatBonuses = new();

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private Skill _weaponEffect;

        public SerializableDictionary<UnboundedStatType, float> StatBonuses => _StatBonuses;

        // Expose combat values for use by damage calculator
        public float Might => _might;
        public float Hit => _hit;
        public float Critical => _critical;

        [
            Foldout("Aptitude"),
            SerializeField,
            HorizontalLine(color: EColor.Violet),
            ShowIf(nameof(IsWeaponOrMagicSubtype))
        ]
        private Aptitude _minWeaponTypeAptitude = new(CommonAncestors.LeveledLetteredField.E);

        private void ApplyGameplayDefaultsFromSettings()
        {
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            if (settings == null)
            {
                return;
            }

            _durability = settings.GetWeaponsHaveDurability();
            _repairable = settings.GetWeaponsCanBeRepaired();
            _forgeable = settings.GetWeaponsCanBeForged();
        }

        private void OnEnable() => ApplyGameplayDefaultsFromSettings();

        private void OnValidate() => ApplyGameplayDefaultsFromSettings();

        public CharacterData BelongsTo => _belongsTo;

        public float Weight => _weight;

        public ObjectSubtype Subtype => _subtype;

        public WeaponType WeaponType => _weaponType;

        public bool IsUnequippable => _isUnequippable;

        public bool IsEquippable =>
            _subtype == ObjectSubtype.Weapon || _subtype == ObjectSubtype.Equipable;

        public EquipableObjectType EquipableType => _equipableType;

        /* --------------- Helper methods for NaughtyAttributes ShowIf -------------- */
        private bool IsEquipableSubtype() => _subtype == ObjectSubtype.Equipable;

        private bool IsWeaponSubtype() => _subtype == ObjectSubtype.Weapon;

        private bool IsWeaponOrMagicSubtype() =>
            _subtype == new ObjectSubtype(ObjectSubtype.Weapon)
            || _subtype == new ObjectSubtype(ObjectSubtype.Magic);

        private bool IsWeaponOrMagicOrStaffSubtype() =>
            IsWeaponOrMagicSubtype() || _equipableType == EquipableObjectType.Staff;

        private bool IsLostItemSubtype() => _subtype == ObjectSubtype.LostItem;

        private bool IsGiftSubtype() => _subtype == ObjectSubtype.Gift;

        private bool IsWeaponOrMagicSubtypeAndIsDurability() =>
            IsWeaponOrMagicSubtype() && _durability;

        private bool IsWeaponOrMagicOrStaffSubtypeAndIsRangeAdjusted() =>
            IsWeaponOrMagicOrStaffSubtype() && _rangeAdjustedByStat;

        private bool IsWeaponOrMagicSubtypeAndIsRepairable() =>
            IsWeaponOrMagicSubtype() && _repairable;

        private bool IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems() =>
            IsWeaponOrMagicSubtypeAndIsRepairable() && _repairNeedsItems;

        private bool IsWeaponOrMagicSubtypeAndIsForgeable() =>
            IsWeaponOrMagicSubtype() && _forgeable;

        private bool IsDurabilityAndIsReplenishUsesAfterBattle() =>
            _replenishUsesAfterBattle && _durability;
    }
}
