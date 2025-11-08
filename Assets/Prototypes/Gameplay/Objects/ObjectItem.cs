using Assets.Prototypes.Gameplay.Objects.Components;
using NaughtyAttributes;
using UnityEngine;

public enum ObjectSubtype
{
    Weapon,
    Magic,
    Consumable,
    Equipable,
    Gift,
}

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
    private string _id = System.Guid.NewGuid().ToString();

    [TextArea, Foldout("Identity"), SerializeField]
    private string _flavorText = "A new item";

    [Foldout("Identity"), SerializeField]
    private Sprite _icon;

    [Foldout("Identity"), SerializeField, ShowIf("_subtype", ObjectSubtype.Equipable)]
    private EquipableObjectType _equipableType;

    [Foldout("Type"), SerializeField, HorizontalLine(color: EColor.Blue)]
    private ObjectSubtype _subtype = ObjectSubtype.Weapon;

    [ShowIf("_subtype", ObjectSubtype.Weapon), Foldout("Type")]
    private WeaponType _weaponType;

    [Foldout("Stats"), SerializeField, HorizontalLine(color: EColor.Yellow)]
    private float _weight = 1.0f;

    /// <summary>
    /// Gets the weight of this item.
    /// </summary>
    public float Weight => _weight;

    /// <summary>
    /// Gets the icon for this item.
    /// </summary>
    public Sprite Icon => _icon;

    /// <summary>
    /// Gets the subtype of this item (Weapon, Magic, Consumable, Equipable, Gift).
    /// </summary>
    public ObjectSubtype Subtype => _subtype;

    /// <summary>
    /// Gets the equipable type for this item. Only valid if Subtype is Equipable.
    /// </summary>
    public EquipableObjectType EquipableType => _equipableType;
}
