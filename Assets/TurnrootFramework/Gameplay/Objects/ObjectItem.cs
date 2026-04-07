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
    /// <summary>
    /// Defines a forging recipe to transform one item into another with required price and materials.
    /// </summary>
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

    /// <summary>
    /// Represents a gameplay item template with properties for weapons, consumables, gifts, and equipment.
    /// </summary>
    [CreateAssetMenu(fileName = "ObjectItem", menuName = "Turnroot/Objects/Gameplay Item")]
    public class ObjectItem : ScriptableObject
    {
        [Foldout("Identity"), SerializeField, HorizontalLine(color: EColor.Black)]
        private string _name = "New Item";

        public string Name => _name;

        [Foldout("Identity")]
        [SerializeField]
        private readonly string _id = System.Guid.NewGuid().ToString();

        public string Id => _id;

        [TextArea, Foldout("Identity"), SerializeField]
        private string _flavorText = "A new item";

        public string FlavorText => _flavorText;

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

        [field:
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairable))
        ]
        public int RepairPricePerUse { get; set; } = 10;

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
            ShowIf(
                nameof(
                    IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItemsAndOneRepairItemDoesNotCoverFullRepair
                )
            ),
        ]
        public int RepairItemAmountPerUse { get; set; } = 1;

        [field:
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems)),
            InfoBox(
                "If true, one repair item fully repairs the object regardless of remaining durability. If false, the repair item is consumed per use as normal."
            )
        ]
        public bool OneRepairItemCoversFullRepair = false;

        [field: Foldout("Repair"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public bool Forgeable { get; set; } = false;

        [field:
            Foldout("Repair"),
            SerializeField,
            ShowIf(nameof(IsWeaponOrMagicSubtypeAndIsForgeable))
        ]
        public ForgeOption[] ForgeOptions { get; set; }

        [Foldout("Gift"), ShowIf(nameof(IsGiftSubtype)), HorizontalLine(color: EColor.Indigo)]
        public int GiftRank = 1;

        [Foldout("Gift"), ShowIf(nameof(IsGiftSubtype))]
        public CharacterData[] UnitsLove;

        [Foldout("Gift"), ShowIf(nameof(IsGiftSubtype))]
        public CharacterData[] UnitsHate;

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

        public bool ReplenishUsesAfterBattle => _replenishUsesAfterBattle;
        public ReplenishUseType ReplenishUsesAfterBattleAmount => _replenishUsesAfterBattleAmount;

        // Public getters for effectiveness criteria
        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public SpeciesType[] SpeciesEffectiveAgainst { get; set; } = new SpeciesType[0];

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public WeaponType[] WeaponTypesEffectiveAgainst { get; set; } = new WeaponType[0];

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public bool EffectiveAgainstFlying { get; set; } = false;

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public bool EffectiveAgainstArmored { get; set; } = false;

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public bool EffectiveAgainstRiding { get; set; } = false;

        [Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        private Skill _weaponEffect;

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public SerializableDictionary<UnboundedStatType, float> StatBonuses { get; set; } = new();

        // Expose combat values for use by damage calculator
        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public float Might { get; set; } = 0f;

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public float Hit { get; set; } = 0f;

        [field: Foldout("Combat"), SerializeField, ShowIf(nameof(IsCombatSectionVisible))]
        public float Critical { get; set; } = 0f;

        [
            Foldout("Aptitude"),
            SerializeField,
            HorizontalLine(color: EColor.Violet),
            ShowIf(nameof(IsCombatSectionVisible))
        ]
        public Aptitude MinWeaponTypeAptitude = new(CommonAncestors.LeveledLetteredField.E);

        [Foldout("Visuals"), HorizontalLine(color: EColor.Yellow)]
        public GameObject Prefab;

        [Foldout("Visuals")]
        public Sprite InventoryIcon;

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

        // Apply defaults when the asset is first enabled / loaded by Unity (e.g. when gameplay settings change
        // and the settings asset forces a reimport). Do NOT re-apply defaults on every inspector change — that
        // overwrites per-asset developer edits (e.g. unchecking `Repairable`).
        private void OnEnable() => ApplyGameplayDefaultsFromSettings();

        // Keep OnValidate present for future validation but do not re-apply gameplay defaults here —
        // users expect inspector edits to persist.
        private void OnValidate() { }

        // Convenience: allow re-applying gameplay defaults from the inspector/context menu if desired.
        [ContextMenu("Apply Gameplay Defaults")]
        private void Editor_ApplyGameplayDefaultsFromSettings() =>
            ApplyGameplayDefaultsFromSettings();

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
            ShowIf(nameof(IsCombatSectionVisible))
        ]
        public float Weight { get; set; } = 1.0f;

        [field: Foldout("Type"), SerializeField, HorizontalLine(color: EColor.Blue)]
        public ObjectSubtype Subtype { get; set; } = new(ObjectSubtype.Weapon);

        [field: Foldout("Type"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public WeaponType WeaponType { get; set; }

        [field: Foldout("Type"), SerializeField, ShowIf(nameof(IsMagicSubtype))]
        public bool TeamSupportMagic { get; set; } = false;

        [field: Foldout("Identity"), SerializeField, ShowIf(nameof(IsWeaponOrMagicSubtype))]
        public bool IsUnequippable { get; set; } = true;

        [field: Foldout("Support"), SerializeField, ShowIf(nameof(IsTeamSupportMagic))]
        public bool SupportHealing { get; set; } = false;

        [field: Foldout("Support"), SerializeField, ShowIf(nameof(SupportHealing)), Range(0, 100)]
        public int HealingAmountPercent { get; set; } = 50;

        [field: Foldout("Support"), SerializeField, ShowIf(nameof(IsTeamSupportMagic))]
        public Skill SupportSkill { get; set; }

        public bool IsEquippable =>
            (Subtype != null && Subtype.IsWeapon) || Subtype == ObjectSubtype.Equipable;

        [field: Foldout("Type"), SerializeField, ShowIf(nameof(IsEquipableSubtype))]
        public EquipableObjectType EquipableType { get; set; }

        /* --------------- Helper methods for NaughtyAttributes ShowIf -------------- */
        private bool IsEquipableSubtype() => Subtype == ObjectSubtype.Equipable;

        public bool IsWeaponOrMagicSubtype() =>
            Subtype != null && (Subtype.IsWeapon || Subtype.IsMagic);

        public bool IsWeaponSubtype() => Subtype != null && Subtype.IsWeapon;

        public bool IsMagicSubtype() => Subtype != null && Subtype.IsMagic;

        public bool IsTeamSupportMagic() => IsMagicSubtype() && TeamSupportMagic;

        public bool IsCombatSectionVisible() => IsWeaponOrMagicSubtype() && !TeamSupportMagic;

        private bool IsWeaponOrMagicOrStaffSubtype() =>
            IsWeaponOrMagicSubtype() || EquipableType == EquipableObjectType.Staff;

        private bool IsLostItemSubtype() => Subtype == ObjectSubtype.LostItem;

        private bool IsGiftSubtype() => Subtype == ObjectSubtype.Gift;

        public bool IsWeaponOrMagicSubtypeAndIsDurability() =>
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

        private bool IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItemsAndOneRepairItemDoesNotCoverFullRepair() =>
            IsWeaponOrMagicSubtypeAndIsRepairableAndNeedsItems() && !OneRepairItemCoversFullRepair;
    }
}
