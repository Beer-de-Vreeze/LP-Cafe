using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class GayAssDictionary : MonoBehaviour
{
    [SerializeField]
    SerializableDictionary<string, bool> gayDictionary = new SerializableDictionary<string, bool>();
    [SerializeField]
    SerializableDictionary<string, int> gayDictionary2 = new SerializableDictionary<string, int>();

    void Start()
    {
        // Adding key-value pairs to the dictionary
        gayDictionary.Add("Beer", true);
        gayDictionary.Add("Frans", false);
        gayDictionary.Add("Niels", true);

        Debug.Log(gayDictionary["Beer"]); // Output: True
        Debug.Log(gayDictionary["Frans"]); // Output: False
        Debug.Log(gayDictionary["Niels"]); // Output: True

        gayDictionary2.Add("Beer", 1);

        // For loop to count up the Beer value
        for (int i = 0; i < 5; i++)
        {
            gayDictionary2["Beer"]++;
            Debug.Log("Beer value: " + gayDictionary2["Beer"]);
        }
    }
}
