using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Pastebin : https://pastebin.com/zsy1tNRb
/// 
/// 
/// Base non-generic class for SerializableDictionary to enable inspector display
/// and custom property drawers in Unity.
/// </summary>
public class SerializableDictionary { }

/// <summary>
/// A serializable dictionary implementation for Unity that can be viewed and edited in the Inspector.
/// Uses a list of key-value pairs internally since Unity cannot directly serialize dictionaries.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
[Serializable]
public class SerializableDictionary<TKey, TValue>
    : SerializableDictionary,
        IDictionary<TKey, TValue>,
        ISerializationCallbackReceiver
{
    /// <summary>
    /// The internal list of key-value pairs that Unity will serialize.
    /// </summary>
    [SerializeField]
    private List<SerializableKeyValuePair> list = new List<SerializableKeyValuePair>();

    /// <summary>
    /// Serializable structure to store a key-value pair.
    /// </summary>
    [Serializable]
    public struct SerializableKeyValuePair
    {
        /// <summary>The key of the key-value pair.</summary>
        public TKey Key;

        /// <summary>The value of the key-value pair.</summary>
        public TValue Value;

        /// <summary>
        /// Creates a new key-value pair with the specified key and value.
        /// </summary>
        /// <param name="key">The key for the pair</param>
        /// <param name="value">The value for the pair</param>
        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Updates the value of this key-value pair.
        /// </summary>
        /// <param name="value">The new value</param>
        public void SetValue(TValue value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Lookup dictionary that maps keys to their position in the list for O(1) access.
    /// Lazily initialized to avoid serialization issues.
    /// </summary>
    private Dictionary<TKey, uint> KeyPositions => _keyPositions.Value;

    /// <summary>
    /// Lazy-loaded reference to the key positions dictionary.
    /// </summary>
    private Lazy<Dictionary<TKey, uint>> _keyPositions;

    /// <summary>
    /// Initializes a new empty serializable dictionary.
    /// </summary>
    public SerializableDictionary()
    {
        _keyPositions = new Lazy<Dictionary<TKey, uint>>(MakeKeyPositions);
    }

    /// <summary>
    /// Initializes a new serializable dictionary with contents copied from the specified dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary whose elements are copied to the new serializable dictionary</param>
    /// <exception cref="ArgumentException">Thrown when the dictionary parameter is null</exception>
    public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
    {
        _keyPositions = new Lazy<Dictionary<TKey, uint>>(MakeKeyPositions);

        if (dictionary == null)
        {
            throw new ArgumentException("The passed dictionary is null.");
        }

        foreach (KeyValuePair<TKey, TValue> pair in dictionary)
        {
            Add(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// Creates a lookup dictionary that maps keys to their position in the list for fast access.
    /// </summary>
    /// <returns>A dictionary mapping keys to their index positions</returns>
    private Dictionary<TKey, uint> MakeKeyPositions()
    {
        int numEntries = list.Count;

        Dictionary<TKey, uint> result = new Dictionary<TKey, uint>(numEntries);

        for (int i = 0; i < numEntries; ++i)
        {
            result[list[i].Key] = (uint)i;
        }

        return result;
    }

    /// <summary>
    /// Unity callback called before the dictionary is serialized.
    /// </summary>
    public void OnBeforeSerialize()
    {
        // No action needed before serialization
    }

    /// <summary>
    /// Unity callback called after the dictionary is deserialized.
    /// Rebuilds the key lookup dictionary.
    /// </summary>
    public void OnAfterDeserialize()
    {
        // After deserialization, the key positions might be changed
        _keyPositions = new Lazy<Dictionary<TKey, uint>>(MakeKeyPositions);
    }

    #region IDictionary
    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set</param>
    /// <returns>The value associated with the specified key</returns>
    public TValue this[TKey key]
    {
        get => list[(int)KeyPositions[key]].Value;
        set
        {
            if (KeyPositions.TryGetValue(key, out uint index))
            {
                // Update existing value
                list[(int)index].SetValue(value);
            }
            else
            {
                // Add new key-value pair
                KeyPositions[key] = (uint)list.Count;
                list.Add(new SerializableKeyValuePair(key, value));
            }
        }
    }

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    public ICollection<TKey> Keys => list.Select(tuple => tuple.Key).ToArray();

    /// <summary>
    /// Gets a collection containing the values in the dictionary.
    /// </summary>
    public ICollection<TValue> Values => list.Select(tuple => tuple.Value).ToArray();

    public void Add(TKey key, TValue value)
    {
        if (KeyPositions.ContainsKey(key))
        {
            throw new ArgumentException(
                "An element with the same key already exists in the dictionary."
            );
        }
        else
        {
            KeyPositions[key] = (uint)list.Count;

            list.Add(new SerializableKeyValuePair(key, value));
        }
    }

    public bool ContainsKey(TKey key) => KeyPositions.ContainsKey(key);

    public bool Remove(TKey key)
    {
        if (KeyPositions.TryGetValue(key, out uint index))
        {
            Dictionary<TKey, uint> kp = KeyPositions;

            kp.Remove(key);

            list.RemoveAt((int)index);

            int numEntries = list.Count;

            for (uint i = index; i < numEntries; i++)
            {
                kp[list[(int)i].Key] = i;
            }

            return true;
        }

        return false;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (KeyPositions.TryGetValue(key, out uint index))
        {
            value = list[(int)index].Value;

            return true;
        }

        value = default;

        return false;
    }
    #endregion

    #region ICollection
    public int Count => list.Count;
    public bool IsReadOnly => false;

    public void Add(KeyValuePair<TKey, TValue> kvp) => Add(kvp.Key, kvp.Value);

    public void Clear()
    {
        list.Clear();
        KeyPositions.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> kvp) => KeyPositions.ContainsKey(kvp.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        int numKeys = list.Count;

        if (array.Length - arrayIndex < numKeys)
        {
            throw new ArgumentException("arrayIndex");
        }

        for (int i = 0; i < numKeys; ++i, ++arrayIndex)
        {
            SerializableKeyValuePair entry = list[i];

            array[arrayIndex] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> kvp) => Remove(kvp.Key);
    #endregion

    #region IEnumerable
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return list.Select(ToKeyValuePair).GetEnumerator();

        KeyValuePair<TKey, TValue> ToKeyValuePair(SerializableKeyValuePair skvp)
        {
            return new KeyValuePair<TKey, TValue>(skvp.Key, skvp.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion
}
