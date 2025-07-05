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
        try
        {
            // Create a backup of the existing save file if it exists
            if (File.Exists(path))
            {
                string backupPath = path + ".backup";
                File.Copy(path, backupPath, true);
                Debug.Log($"Created backup of save file at {backupPath}");
            }

            // Write the new save data
            using (StreamWriter writer = File.CreateText(path))
            {
                string json = JsonUtility.ToJson(data);
                writer.Write(json);
            }
            Debug.Log($"Saved game to {path}");

            // Verify the save was successful by attempting to read it back
            using (StreamReader reader = File.OpenText(path))
            {
                string json = reader.ReadToEnd();
                JsonUtility.FromJson<SaveData>(json); // This will throw an exception if the JSON is malformed
                Debug.Log("Save file verified successfully");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving game data: {ex.Message}");

            // Try to restore from backup if save failed and backup exists
            string backupPath = path + ".backup";
            if (File.Exists(backupPath))
            {
                try
                {
                    File.Copy(backupPath, path, true);
                    Debug.Log("Restored save file from backup after save error");
                }
                catch (System.Exception backupEx)
                {
                    Debug.LogError($"Failed to restore from backup: {backupEx.Message}");
                }
            }
        }
    }

    public static SaveData Deserialize()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path))
        {
            Debug.Log("No save file found in " + path + " (this is normal for new players)");
            return null;
        }

        try
        {
            // Attempt to read the save file
            string json;
            using (StreamReader reader = File.OpenText(path))
            {
                json = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("Save file exists but is empty. Creating new save data.");
                return new SaveData();
            }

            // Try to parse the JSON
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null)
            {
                Debug.LogError("Failed to deserialize save data - JSON parsing returned null");
                return new SaveData();
            }

            // Clean up any corrupt data (like empty strings)
            CleanupSaveData(data);

            Debug.Log($"Loaded game from {path}");
            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading save file: {ex.Message}");

            // Try to restore from backup if it exists
            string backupPath = path + ".backup";
            if (File.Exists(backupPath))
            {
                try
                {
                    Debug.Log("Attempting to load backup save file...");
                    string backupJson;
                    using (StreamReader reader = File.OpenText(backupPath))
                    {
                        backupJson = reader.ReadToEnd();
                    }

                    SaveData backupData = JsonUtility.FromJson<SaveData>(backupJson);
                    if (backupData != null)
                    {
                        Debug.Log("Successfully loaded backup save file");
                        CleanupSaveData(backupData);

                        // Restore the backup as the main save file
                        File.Copy(backupPath, path, true);

                        return backupData;
                    }
                }
                catch (System.Exception backupEx)
                {
                    Debug.LogError($"Failed to load backup save file: {backupEx.Message}");
                }
            }

            // If we get here, we couldn't load the original or the backup
            Debug.LogWarning("Creating new save data after load failure");
            return new SaveData();
        }
    }

    /// <summary>
    /// Cleans up empty strings from save data and migrates old format data to new format
    /// </summary>
    private static void CleanupSaveData(SaveData data)
    {
        if (data == null)
            return;

        bool needsMigration = false;

        // Check for old format data that needs migration
        if (data.DatedBachelors != null && data.DatedBachelors.Count > 0)
        {
            // Check if there's actual data (not just empty strings)
            bool hasRealData = data.DatedBachelors.Exists(name => !string.IsNullOrEmpty(name));
            if (hasRealData)
            {
                needsMigration = true;
                Debug.Log("[SaveSystem] Found old format DatedBachelors data that needs migration");
            }
            else
            {
                // If it's just empty strings, we can clear the list
                data.DatedBachelors.Clear();
                Debug.Log("[SaveSystem] Cleared empty DatedBachelors list");
            }
        }

        if (data.RealDatedBachelors != null && data.RealDatedBachelors.Count > 0)
        {
            // Check if there's actual data (not just empty strings)
            bool hasRealData = data.RealDatedBachelors.Exists(name => !string.IsNullOrEmpty(name));
            if (hasRealData)
            {
                needsMigration = true;
                Debug.Log(
                    "[SaveSystem] Found old format RealDatedBachelors data that needs migration"
                );
            }
            else
            {
                // If it's just empty strings, we can clear the list
                data.RealDatedBachelors.Clear();
                Debug.Log("[SaveSystem] Cleared empty RealDatedBachelors list");
            }
        }

        // Migrate old format data if needed
        if (needsMigration)
        {
            Debug.Log("[SaveSystem] Migrating data from old format to new per-bachelor format");
            data.MigrateOldData();

            // After migration, verify the data was properly transferred before clearing
            List<string> speedDated = data.GetAllSpeedDatedBachelors();
            List<string> realDated = data.GetAllRealDatedBachelors();

            Debug.Log(
                $"[SaveSystem] After migration, {speedDated.Count} speed dated bachelors and {realDated.Count} real dated bachelors"
            );

            // Now that we've migrated, clear the old lists
            data.DatedBachelors.Clear();
            data.RealDatedBachelors.Clear();
        }

        // Clean up bachelor preferences data
        if (data.BachelorPreferences != null)
        {
            var originalCount = data.BachelorPreferences.Count;
            data.BachelorPreferences.RemoveAll(bp => string.IsNullOrEmpty(bp?.bachelorName));

            // Clean up empty strings within each bachelor's preferences
            foreach (var bachelorPref in data.BachelorPreferences)
            {
                if (bachelorPref.discoveredLikes != null)
                {
                    int beforeCount = bachelorPref.discoveredLikes.Count;
                    bachelorPref.discoveredLikes.RemoveAll(like => string.IsNullOrEmpty(like));

                    if (beforeCount != bachelorPref.discoveredLikes.Count)
                    {
                        Debug.Log(
                            $"[SaveSystem] Cleaned {beforeCount - bachelorPref.discoveredLikes.Count} empty entries from {bachelorPref.bachelorName}'s likes"
                        );
                    }
                }

                if (bachelorPref.discoveredDislikes != null)
                {
                    int beforeCount = bachelorPref.discoveredDislikes.Count;
                    bachelorPref.discoveredDislikes.RemoveAll(dislike =>
                        string.IsNullOrEmpty(dislike)
                    );

                    if (beforeCount != bachelorPref.discoveredDislikes.Count)
                    {
                        Debug.Log(
                            $"[SaveSystem] Cleaned {beforeCount - bachelorPref.discoveredDislikes.Count} empty entries from {bachelorPref.bachelorName}'s dislikes"
                        );
                    }
                }
            }

            if (originalCount != data.BachelorPreferences.Count)
            {
                Debug.Log(
                    $"[SaveSystem] Cleaned {originalCount - data.BachelorPreferences.Count} empty entries from BachelorPreferences"
                );
            }
        }
    }

    /// <summary>
    /// Completely resets the save file, creating a new empty save.
    /// Used for debugging or when save file becomes corrupted.
    /// </summary>
    public static void ResetSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // Create backup before deletion just in case
        if (File.Exists(path))
        {
            try
            {
                string backupPath = path + ".reset_backup";
                File.Copy(path, backupPath, true);
                Debug.Log($"Created backup of save file before reset at {backupPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create backup before reset: {ex.Message}");
            }
        }

        // Create a completely new save file
        SaveData newData = new SaveData();
        SerializeData(newData);
        Debug.Log("Save file has been completely reset");
    }
}
