using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for properties that can be attached to map grid points and features.
/// Provides a flexible property system with strongly-typed property containers.
/// </summary>
[System.Serializable]
public abstract class MapGridPropertyBase : ScriptableObject
{
    public interface IProperty
    {
        string key { get; set; }
        object GetValue();
        void SetValue(object value);
    }

    [System.Serializable]
    public class EventProperty : IProperty
    {
        public string key = string.Empty;
        public UnityEvent value = new();

        string IProperty.key
        {
            get => key;
            set => key = value;
        }

        public object GetValue() => value;

        public void SetValue(object val) => value = val as UnityEvent ?? new UnityEvent();
    }

    [System.Serializable]
    public class UnitProperty : IProperty
    {
        public string key = string.Empty;
        public CharacterInstance value = null;

        string IProperty.key
        {
            get => key;
            set => key = value;
        }

        public object GetValue() => value;

        public void SetValue(object val) => value = val as CharacterInstance;
    }

    [System.Serializable]
    public class ObjectItemProperty : IProperty
    {
        public string key = string.Empty;
        public ObjectItemInstance value = null;

        string IProperty.key
        {
            get => key;
            set => key = value;
        }

        public object GetValue() => value;

        public void SetValue(object val) => value = val as ObjectItemInstance;
    }

    [System.Serializable]
    public class BoolProperty : IProperty
    {
        public string key = string.Empty;
        public bool value = false;

        string IProperty.key
        {
            get => key;
            set => key = value;
        }

        public object GetValue() => value;

        public void SetValue(object val) => value = val is bool b && b;
    }

    [System.Serializable]
    public class FloatProperty : IProperty
    {
        public string key = string.Empty;
        public float value = 0f;

        string IProperty.key
        {
            get => key;
            set => key = value;
        }

        public object GetValue() => value;

        public void SetValue(object val) => value = val is float f ? f : 0f;
    }

    // Property collections
    public List<EventProperty> eventProperties = new();
    public List<UnitProperty> unitProperties = new();
    public List<ObjectItemProperty> objectItemProperties = new();
    public List<BoolProperty> boolProperties = new();
    public List<FloatProperty> floatProperties = new();

    // Helper methods for property access
    public T GetProperty<T>(string key)
        where T : class, IProperty
    {
        var list = GetPropertyList<T>();
        return list?.Find(p => p.key == key);
    }

    public void SetProperty<T>(string key, object value)
        where T : class, IProperty, new()
    {
        var list = GetPropertyList<T>();
        if (list == null)
        {
            return;
        }

        var prop = list.Find(p => p.key == key);
        if (prop != null)
        {
            prop.SetValue(value);
        }
        else
        {
            var newProp = new T { key = key };
            newProp.SetValue(value);
            list.Add(newProp);
        }
    }

    private List<T> GetPropertyList<T>()
        where T : class, IProperty
    {
        return typeof(T).Name switch
        {
            nameof(EventProperty) => eventProperties as List<T>,
            nameof(UnitProperty) => unitProperties as List<T>,
            nameof(ObjectItemProperty) => objectItemProperties as List<T>,
            nameof(BoolProperty) => boolProperties as List<T>,
            nameof(FloatProperty) => floatProperties as List<T>,
            _ => null,
        };
    }
}
