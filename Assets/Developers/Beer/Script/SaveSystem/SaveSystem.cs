using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
            Debug.Log("No save file found in " + path + " (this is normal for new players)");
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
