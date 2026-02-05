using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.GameSettings;
using Turnroot.Skills;
using Turnroot.Utilities.AbstractScripts;
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

        [field: Foldout("Pricing"), SerializeField, HorizontalLine(color: EColor.Gray)]
        public int BasePrice { get; set; } = 100;

        [field: Foldout("Pricing"), SerializeField]
        public bool Sellable { get; set; } = true;

        [field: Foldout("Pricing"), SerializeField]
        public bool Buyable { get; set; } = true;

        [field: Foldout("Pricing"), SerializeField]
        public int SellPriceDeductedPerUse { get; set; } = 2;

        [field:
            Foldout("Repair"),
            SerializeField,
            HorizontalLine(color: EColor.Green),
            ShowIf(nameof(IsWeaponOrMagicSubtype))
        ]
        public bool Repairable { get; private set; } = true;

        [Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairable))]
        private int _repairPricePerUse = 10;

        [field:
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairable))
        ]
        public bool RepairNeedsItems { get; set; } = true;

        [field:
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems))
        ]
        public ObjectItem RepairItem { get; set; }

        [field:
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems))
        ]
        public int RepairItemAmountPerUse { get; set; } = 1;

        [field: Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public bool Forgeable { get; set; } = false;

        [field:
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsForgeable))
        ]
        public ForgeOption[] ForgeOptions { get; set; }

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

        [field:
            Foldout("Range"),
            SerializeField,
            HorizontalLine(color: EColor.Orange),
            ShowIf(nameof(IsWeaponOrMagicOrStaffSubtype))
        ]
        public int LowerRange { get; set; } = 0;

        [field: Foldout("Range"), SerializeField, ShowIf(nameof(IsWeaponOrMagicOrStaffSubtype))]
        public int UpperRange { get; set; } = 0;

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

        [field:
            Foldout("Durability"),
            HideInInspector,
            ShowIf(nameof(IsWeaponOrMagicSubtype)),
            HorizontalLine(color: EColor.Pink)
        ]
        [HideInInspector]
        public bool Durability { get; private set; } = true;

        [field:
            Foldout("Durability"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsDurability))
        ]
        public int MaxUses { get; set; } = 100;

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

        // Public getters for effectiveness criteria
        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public SpeciesType[] SpeciesEffectiveAgainst { get; set; } = new SpeciesType[0];

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public WeaponType[] WeaponTypesEffectiveAgainst { get; set; } = new WeaponType[0];

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        private Skill _weaponEffect;

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public SerializableDictionary<UnboundedStatType, float> StatBonuses { get; set; } = new();

        // Expose combat values for use by damage calculator
        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public float Might { get; set; } = 0f;

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public float Hit { get; set; } = 0f;

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public float Critical { get; set; } = 0f;

        [
            Foldout("Aptitude"),
            SerializeField,
            HorizontalLine(color: EColor.Violet),
            ShowIf(nameof(IsWeaponOrMagicSubtype))
        ]
        private Aptitude _minWeaponTypeAptitude = new(CommonAncestors.LeveledLetteredField.E);

        private void ApplyGameplayDefaultsFromSettings()
        {
            var settings = GameplayGeneralSettings.Instance;
            if (settings == null)
            {
                return;
            }

            Durability = settings.WeaponsHaveDurability;
            Repairable = settings.WeaponsCanBeRepaired;
            Forgeable = settings.WeaponsCanBeForged;
        }

        private void OnEnable() => ApplyGameplayDefaultsFromSettings();

        private void OnValidate() => ApplyGameplayDefaultsFromSettings();

        [field:
            Foldout("Lost Items"),
            SerializeField,
            HorizontalLine(color: EColor.White),
            ShowIf(nameof(IsLostItemSubtype))
        ]
        public CharacterData BelongsTo { get; set; }

        [field:
            Foldout("Combat"),
            SerializeField,
            HorizontalLine(color: EColor.Red),
            ShowIf(nameof(IsWeaponOrMagicSubtype))
        ]
        public float Weight { get; set; } = 1.0f;

        [field: Foldout("Type"), SerializeField, HorizontalLine(color: EColor.Blue)]
        public ObjectSubtype Subtype { get; set; } = new(ObjectSubtype.Weapon);

        [field: Foldout("Type"), SerializeField, ShowIf(nameof(IsWeaponSubtype))]
        public WeaponType WeaponType { get; set; }

        [field: Foldout("Identity"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public bool IsUnequippable { get; set; } = true;

        public bool IsEquippable =>
            Subtype == ObjectSubtype.Weapon || Subtype == ObjectSubtype.Equipable;

        [field: Foldout("Type"), SerializeField, ShowIf(nameof(IsEquipableSubtype))]
        public EquipableObjectType EquipableType { get; set; }

        /* --------------- Helper methods for NaughtyAttributes ShowIf -------------- */
        private bool IsEquipableSubtype() => Subtype == ObjectSubtype.Equipable;

        private bool IsWeaponSubtype() => Subtype == ObjectSubtype.Weapon;

        private bool IsWeaponOrMagicSubtype() =>
            Subtype == new ObjectSubtype(ObjectSubtype.Weapon)
            || Subtype == new ObjectSubtype(ObjectSubtype.Magic);

        private bool IsWeaponOrMagicOrStaffSubtype() =>
            IsWeaponOrMagicSubtype() || EquipableType == EquipableObjectType.Staff;

        private bool IsLostItemSubtype() => Subtype == ObjectSubtype.LostItem;

        private bool IsGiftSubtype() => Subtype == ObjectSubtype.Gift;

        private bool IsWeaponOrMagicSubtypeAndIsDurability() =>
            IsWeaponOrMagicSubtype() && Durability;

        private bool IsWeaponOrMagicOrStaffSubtypeAndIsRangeAdjusted() =>
            IsWeaponOrMagicOrStaffSubtype() && _rangeAdjustedByStat;

        private bool IsWeaponOrMagicSubtypeAndIsRepairable() =>
            IsWeaponOrMagicSubtype() && Repairable;

        private bool IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems() =>
            IsWeaponOrMagicSubtypeAndIsRepairable() && RepairNeedsItems;

        private bool IsWeaponOrMagicSubtypeAndIsForgeable() =>
            IsWeaponOrMagicSubtype() && Forgeable;

        private bool IsDurabilityAndIsReplenishUsesAfterBattle() =>
            _replenishUsesAfterBattle && Durability;
    }
}
