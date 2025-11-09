using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Objects.Components;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    [CreateAssetMenu(fileName = "ObjectItem", menuName = "Turnroot/Objects/Gameplay Item")]
    public class ObjectItem : ScriptableObject
    {
        [Foldout("Identity"), SerializeField, HorizontalLine(color: EColor.Black)]
        private string _name = "New Item";

        [Foldout("Identity")]
        private readonly string _id = System.Guid.NewGuid().ToString();

        [TextArea, Foldout("Identity"), SerializeField]
        private string _flavorText = "A new item";

        [Foldout("Identity"), SerializeField]
        private Sprite _icon;

        [Foldout("Identity"), SerializeField, ShowIf(nameof(IsEquipableSubtype))]
        private EquipableObjectType _equipableType;

        [Foldout("Type"), SerializeField, HorizontalLine(color: EColor.Blue)]
        private ObjectSubtype _subtype = new ObjectSubtype(ObjectSubtype.Weapon);

        [ShowIf(nameof(IsWeaponSubtype)), Foldout("Type")]
        private WeaponType _weaponType;

        [Foldout("Pricing"), SerializeField, HorizontalLine(color: EColor.Gray)]
        private int _basePrice = 100;

        [Foldout("Pricing"), SerializeField]
        private bool _sellable = true;

        [Foldout("Pricing"), SerializeField]
        private bool _buyable = true;

        [Foldout("Pricing"), SerializeField]
        private int _sellPriceDeductedPerUse = 2;

        [
            Foldout("Repair"),
            SerializeField,
            HorizontalLine(color: EColor.Green),
            ShowIf(nameof(IsWeaponOrMagicSubtype))
        ]
        private bool _repairable = true;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairable))]
        private int _repairPricePerUse = 10;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairable))]
        private bool _repairNeedsItems = true;

        [
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems))
        ]
        private ObjectItem _repairItem;

        [
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems))
        ]
        private int _repairItemAmountPerUse = 1;

        [
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems))
        ]
        private bool _forgeable = false;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsForgeable))]
        private ObjectItem[] _forgeInto;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsForgeable))]
        private int[] _forgePrices;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsForgeable))]
        private bool _forgeNeedsItems = false;

        [
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsForgeableAndNeedsItems))
        ]
        private ObjectItem[] _forgeItems;

        [
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsForgeableAndNeedsItems))
        ]
        private int _forgeItemAmountPerUse = 1;

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
            Foldout("Weapon Range"),
            SerializeField,
            HorizontalLine(color: EColor.Orange),
            ShowIf(nameof(IsWeaponSubtype))
        ]
        private int _lowerRange = 0;

        [Foldout("Attack Range"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private int _upperRange = 0;

        [Foldout("Attack Range"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private bool _rangeAdjustedByStat = false;

        [
            Foldout("Attack Range"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRangeAdjusted))
        ]
        private UnboundedStatType _rangeAdjustedByStatName = UnboundedStatType.Strength;

        [
            Foldout("Attack Range"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRangeAdjusted))
        ]
        private int _rangeAdjustedByStatAmount = 0;

        [
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtype)),
            HorizontalLine(color: EColor.Pink)
        ]
        private bool _durability = true;

        [
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsDurability))
        ]
        private int _uses = 100;

        [
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsDurability))
        ]
        private int _maxUses = 100;

        [
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsDurability))
        ]
        private bool _replenishUsesAfterBattle = false;

        [
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsDurability))
        ]
        private ReplenishUseType _replenishUsesAfterBattleAmount = ReplenishUseType.None;

        [Foldout("Stats"), SerializeField, HorizontalLine(color: EColor.Red)]
        private float _weight = 1.0f;

        [
            Foldout("Aptitude"),
            SerializeField,
            HorizontalLine(color: EColor.Violet),
            ShowIf(nameof(IsWeaponOrMagicSubtype))
        ]
        private Aptitude _minAptitude = new(Aptitude.E);

        public CharacterData BelongsTo => _belongsTo;

        public float Weight => _weight;

        public Sprite Icon => _icon;

        public ObjectSubtype Subtype => _subtype;

        public bool IsEquippable =>
            _subtype == ObjectSubtype.Weapon || _subtype == ObjectSubtype.Equipable;

        public EquipableObjectType EquipableType => _equipableType;

        // Helper methods for NaughtyAttributes ShowIf
        private bool IsEquipableSubtype() => _subtype == ObjectSubtype.Equipable;

        private bool IsWeaponSubtype() => _subtype == ObjectSubtype.Weapon;

        private bool IsWeaponOrMagicSubtype() =>
            _subtype == ObjectSubtype.Weapon || _subtype == ObjectSubtype.Magic;

        private bool IsLostItemSubtype() => _subtype == ObjectSubtype.LostItem;

        private bool IsGiftSubtype() => _subtype == ObjectSubtype.Gift;

        private bool IsWeaponOrMagicSubtypeAndIsDurability() =>
            IsWeaponOrMagicSubtype() && _durability;

        private bool IsWeaponOrMagicSubtypeAndIsRangeAdjusted() =>
            IsWeaponOrMagicSubtype() && _rangeAdjustedByStat;

        private bool IsWeaponOrMagicSubtypeAndIsRepairable() =>
            IsWeaponOrMagicSubtype() && _repairable;

        private bool IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems() =>
            IsWeaponOrMagicSubtypeAndIsRepairable() && _repairNeedsItems;

        private bool IsWeaponOrMagicSubtypeAndIsForgeable() =>
            IsWeaponOrMagicSubtype() && _forgeable;

        private bool IsWeaponOrMagicSubtypeAndIsForgeableAndNeedsItems() =>
            IsWeaponOrMagicSubtypeAndIsForgeable() && _forgeNeedsItems;
    }
}
