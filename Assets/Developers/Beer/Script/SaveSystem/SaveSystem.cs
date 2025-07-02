using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string fileName = "save.json";

    public static void SerializeData(SaveData data)
    {
        // Clean up any corrupt data before saving
        CleanupSaveData(data);

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

            // Clean up any corrupt data (like empty strings)
            CleanupSaveData(data);

            Debug.Log("loaded game from " + path);
            return data;
        }
    }

    /// <summary>
    /// Cleans up empty strings from save data
    /// </summary>
    private static void CleanupSaveData(SaveData data)
    {
        if (data == null)
            return;

        // Remove empty strings from bachelor lists
        if (data.DatedBachelors != null)
        {
            var originalCount = data.DatedBachelors.Count;
            data.DatedBachelors.RemoveAll(name => string.IsNullOrEmpty(name));
            if (originalCount != data.DatedBachelors.Count)
            {
                Debug.Log(
                    $"[SaveSystem] Cleaned {originalCount - data.DatedBachelors.Count} empty entries from DatedBachelors"
                );
            }
        }

        if (data.RealDatedBachelors != null)
        {
            var originalCount = data.RealDatedBachelors.Count;
            data.RealDatedBachelors.RemoveAll(name => string.IsNullOrEmpty(name));
            if (originalCount != data.RealDatedBachelors.Count)
            {
                Debug.Log(
                    $"[SaveSystem] Cleaned {originalCount - data.RealDatedBachelors.Count} empty entries from RealDatedBachelors"
                );
            }
        }
    }
}
