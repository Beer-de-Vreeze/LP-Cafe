using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace LPCafe.Utilities
{
    public static class CollectionUtility
    {
        //
        public static void AddItem<K, V>(this SerializableDictionary<K, List<V>> serializableDictionary, K key, V value)
        {
            if (serializableDictionary.ContainsKey(key))
            {
                serializableDictionary[key].Add(value);

                return;
            }

            //Is called a collection Initializer
            //Allows you to initializa a list with certain values in it.
            serializableDictionary.Add(key, new List<V> {value});
        }
    }
}