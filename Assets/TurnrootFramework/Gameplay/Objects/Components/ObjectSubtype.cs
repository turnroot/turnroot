using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents the subtype of an item. Provides dynamic validation based on GameplayGeneralSettings.
/// </summary>
[Serializable]
public class ObjectSubtype
{
    // Enum values
    public const string Weapon = "Weapon";
    public const string Magic = "Magic";
    public const string Consumable = "Consumable";
    public const string Equipable = "Equipable";
    public const string Gift = "Gift";
    public const string LostItem = "LostItem";

    [SerializeField]
    private string _value = Weapon;

    public ObjectSubtype() { }

    public ObjectSubtype(string value)
    {
        _value = (IsValid(value) && IsEnabled(value)) ? value : Weapon;
    }

    public string Value
    {
        get => _value;
        set
        {
            if (IsValid(value) && IsEnabled(value))
            {
                _value = value;
            }
            else
            {
                Debug.LogWarning(
                    $"Invalid or disabled ObjectSubtype value: {value}. Using Weapon as default."
                );
                _value = Weapon;
            }
        }
    }

    // Convenience properties for checking type
    public bool IsWeapon => _value == Weapon;
    public bool IsMagic => _value == Magic;
    public bool IsConsumable => _value == Consumable;
    public bool IsEquipable => _value == Equipable;
    public bool IsGift => _value == Gift;
    public bool IsLostItem => _value == LostItem;

    /// <summary>
    /// Gets all valid ObjectSubtype values based on current GameplayGeneralSettings.
    /// </summary>
    public static string[] GetValidValues()
    {
        var values = new List<string> { Weapon, Magic, Consumable, Equipable };

        var settings = GameplayGeneralSettings.Instance;
        if (settings != null)
        {
            if (settings.UseItemsCanBeGifts())
                values.Add(Gift);
            if (settings.UseItemsCanBeLostItems())
                values.Add(LostItem);
        }
        else
        {
            // If settings not available, include all types
            values.Add(Gift);
            values.Add(LostItem);
        }

        return values.ToArray();
    }

    /// <summary>
    /// Checks if a value is valid (exists in the defined constants).
    /// </summary>
    public static bool IsValid(string value)
    {
        return value == Weapon
            || value == Magic
            || value == Consumable
            || value == Equipable
            || value == Gift
            || value == LostItem;
    }

    /// <summary>
    /// Checks if a value is enabled based on GameplayGeneralSettings.
    /// </summary>
    public static bool IsEnabled(string value)
    {
        if (value == Weapon || value == Magic || value == Consumable || value == Equipable)
            return true;

        var settings = GameplayGeneralSettings.Instance;
        if (settings == null)
            return true; // If no settings, allow all

        if (value == Gift)
            return settings.UseItemsCanBeGifts();
        if (value == LostItem)
            return settings.UseItemsCanBeLostItems();

        return false;
    }

    // Implicit conversion to string
    public static implicit operator string(ObjectSubtype subtype) => subtype._value;

    // Explicit conversion from string
    public static explicit operator ObjectSubtype(string value) => new(value);

    public override string ToString() => _value;

    public override bool Equals(object obj)
    {
        if (obj is ObjectSubtype other)
            return _value == other._value;
        if (obj is string str)
            return _value == str;
        return false;
    }

    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(ObjectSubtype a, ObjectSubtype b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;
        return a._value == b._value;
    }

    public static bool operator !=(ObjectSubtype a, ObjectSubtype b) => !(a == b);

    public static bool operator ==(ObjectSubtype a, string b) => a?._value == b;

    public static bool operator !=(ObjectSubtype a, string b) => a?._value != b;
}
