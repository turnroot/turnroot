using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable dictionary for Unity Inspector
/// </summary>
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [SerializeField]
    private List<TKey> _keys = new();

    [SerializeField]
    private List<TValue> _values = new();

    private Dictionary<TKey, TValue> _dictionary;

    public Dictionary<TKey, TValue> Dictionary
    {
        get
        {
            if (_dictionary == null)
            {
                _dictionary = new Dictionary<TKey, TValue>();
                for (int i = 0; i < Mathf.Min(_keys.Count, _values.Count); i++)
                {
                    if (!_dictionary.ContainsKey(_keys[i]))
                    {
                        _dictionary[_keys[i]] = _values[i];
                    }
                }
            }
            return _dictionary;
        }
    }

    public TValue this[TKey key]
    {
        get => Dictionary.ContainsKey(key) ? Dictionary[key] : default;
        set
        {
            if (Dictionary.ContainsKey(key))
            {
                Dictionary[key] = value;
                int index = _keys.IndexOf(key);
                if (index >= 0)
                {
                    _values[index] = value;
                }
            }
            else
            {
                Dictionary[key] = value;
                _keys.Add(key);
                _values.Add(value);
            }
        }
    }

    public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);

    public bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);

    public void Add(TKey key, TValue value)
    {
        if (!Dictionary.ContainsKey(key))
        {
            Dictionary[key] = value;
            _keys.Add(key);
            _values.Add(value);
        }
    }

    public bool Remove(TKey key)
    {
        if (Dictionary.ContainsKey(key))
        {
            int index = _keys.IndexOf(key);
            if (index >= 0)
            {
                _keys.RemoveAt(index);
                _values.RemoveAt(index);
            }
            return Dictionary.Remove(key);
        }
        return false;
    }

    public void Clear()
    {
        _dictionary?.Clear();
        _keys.Clear();
        _values.Clear();
    }

    public int Count => _keys.Count;

    public IEnumerable<TKey> Keys => _keys;
    public IEnumerable<TValue> Values => _values;
}
