using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class SaveSystem
{
    private static string fileName = "save.json";


    public static void SerializeData(SaveData data)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        using (StreamWriter writer = File.CreateText(path))
        {
            string json = JsonUtility.ToJson(data);
            writer.Write(json);
        }
        Debug.Log("saved game to " + path);
    }

    public static SaveData Deserialize()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError("Save file not found in " + path);
            return null;
        }
        using (StreamReader reader = File.OpenText(path))
        {
            string json = reader.ReadToEnd();
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("loaded game from " + path);
            return data;
        }
    }
}