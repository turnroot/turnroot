using System;
using System.Collections.Generic;
using Assets.Prototypes.Characters;
using Assets.Prototypes.Gameplay.Objects.Components;
using NaughtyAttributes;
using UnityEngine;

public enum EquipableObjectType
{
    Accessory,
    Shield,
    Staff,
    Ring,
}

public enum EquipableOutfitType
{
    Helmet,
    Hat,
    Shirt,
    Pants,
    Dress,
    Skirt,
    Robe,
    Gloves,
    Coat,
    Armor,
    Boots,
    Cloak,
}

[CreateAssetMenu(fileName = "ObjectItem", menuName = "Turnroot/Objects/Gameplay Item")]
public class ObjectItem : ScriptableObject
{
    [Foldout("Identity"), SerializeField, HorizontalLine(color: EColor.Green)]
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

    [Foldout("Pricing"), SerializeField, HorizontalLine(color: EColor.Orange)]
    private int _basePrice = 100;

    [Foldout("Pricing"), SerializeField]
    private bool _sellable = true;

    [Foldout("Pricing"), SerializeField]
    private bool _buyable = true;

    [Foldout("Pricing"), SerializeField]
    private int _sellPriceDeductedPerUse = 2;

    [Foldout("Repair"), SerializeField, HorizontalLine(color: EColor.Green)]
    private bool _repairable = true;

    [Foldout("Repair"), SerializeField, ShowIf("_repairable")]
    private int _repairPricePerUse = 10;

    [Foldout("Repair"), SerializeField, ShowIf("_repairable")]
    private bool _repairNeedsItems = true;

    [Foldout("Repair"), SerializeField, ShowIf("_repairNeedsItems")]
    private ObjectItem _repairItem;

    [Foldout("Repair"), SerializeField, ShowIf("_repairNeedsItems")]
    private int _repairItemAmountPerUse = 1;

    [Foldout("Repair"), SerializeField, ShowIf("_repairNeedsItems")]
    private bool _forgeable = false;

    [Foldout("Repair"), SerializeField, ShowIf("_forgeable")]
    private ObjectItem[] _forgeInto;

    [Foldout("Repair"), SerializeField, ShowIf("_forgeable")]
    private int[] _forgePrices;

    [Foldout("Repair"), SerializeField, ShowIf("_forgeable")]
    private bool _forgeNeedsItems = false;

    [Foldout("Repair"), SerializeField, ShowIf("_forgeNeedsItems")]
    private ObjectItem[] _forgeItems;

    [Foldout("Repair"), SerializeField, ShowIf("_forgeNeedsItems")]
    private int _forgeItemAmountPerUse = 1;

    [
        Foldout("Lost Items"),
        SerializeField,
        HorizontalLine(color: EColor.Green),
        ShowIf(nameof(IsLostItemSubtype))
    ]
    private bool _lostItem;

    [SerializeField, Foldout("Lost Items"), ShowIf("_lostItem")]
    private CharacterData _belongsTo;

    [
        SerializeField,
        Foldout("Gift"),
        ShowIf(nameof(IsGiftSubtype)),
        HorizontalLine(color: EColor.Gray)
    ]
    private int _giftRank = 1;

    [Foldout("Gift"), SerializeField, ShowIf(nameof(IsGiftSubtype))]
    private string[] _unitsLove;

    [Foldout("Gift"), SerializeField, ShowIf(nameof(IsGiftSubtype))]
    private string[] _unitsHate;

    [Foldout("Stats"), SerializeField, HorizontalLine(color: EColor.Yellow)]
    private float _weight = 1.0f;

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

    private bool IsLostItemSubtype() => _subtype == ObjectSubtype.LostItem;

    private bool IsGiftSubtype() => _subtype == ObjectSubtype.Gift;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // ObjectSubtype class now handles validation automatically
    }
#endif
}
