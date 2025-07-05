using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    /// <summary>
    /// Flag indicating that bachelor data should be reset to initial state
    /// Used in builds where we can't modify ScriptableObject assets directly
    /// </summary>
    [SerializeField]
    public bool ShouldResetBachelors = false;

    /// <summary>
    /// List to store data for each bachelor including preferences and dating status
    /// </summary>
    [SerializeField]
    public List<BachelorPreferencesData> BachelorPreferences = new List<BachelorPreferencesData>();

    /// <summary>
    /// Legacy lists for backward compatibility - these are kept but no longer used
    /// </summary>
    [SerializeField, HideInInspector]
    public List<string> DatedBachelors = new List<string>();

    [SerializeField, HideInInspector]
    public List<string> RealDatedBachelors = new List<string>();

    /// <summary>
    /// Gets all bachelors that have been speed dated
    /// </summary>
    public List<string> GetAllSpeedDatedBachelors()
    {
        List<string> result = new List<string>();
        foreach (var bachelor in BachelorPreferences)
        {
            if (bachelor.hasBeenSpeedDated)
            {
                result.Add(bachelor.bachelorName);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all bachelors that have been real dated
    /// </summary>
    public List<string> GetAllRealDatedBachelors()
    {
        List<string> result = new List<string>();
        foreach (var bachelor in BachelorPreferences)
        {
            if (bachelor.hasCompletedRealDate)
            {
                result.Add(bachelor.bachelorName);
            }
        }
        return result;
    }

    /// <summary>
    /// Migrate old format data to new format
    /// </summary>
    public void MigrateOldData()
    {
        // Migrate data from the legacy lists to the per-bachelor records
        if (DatedBachelors != null && DatedBachelors.Count > 0)
        {
            foreach (string bachelorName in DatedBachelors)
            {
                if (!string.IsNullOrEmpty(bachelorName))
                {
                    SetBachelorSpeedDated(bachelorName, true);
                }
            }
        }

        if (RealDatedBachelors != null && RealDatedBachelors.Count > 0)
        {
            foreach (string bachelorName in RealDatedBachelors)
            {
                if (!string.IsNullOrEmpty(bachelorName))
                {
                    SetBachelorRealDated(bachelorName, true, "Unknown");
                }
            }
        }

        // Clear the old lists after migration
        DatedBachelors.Clear();
        RealDatedBachelors.Clear();
    }

    /// <summary>
    /// Sets a bachelor's speed dated status
    /// </summary>
    public void SetBachelorSpeedDated(string bachelorName, bool dated)
    {
        if (string.IsNullOrEmpty(bachelorName))
            return;

        BachelorPreferencesData prefData = GetOrCreateBachelorData(bachelorName);
        prefData.hasBeenSpeedDated = dated;
    }

    /// <summary>
    /// Sets a bachelor's real dated status and location
    /// </summary>
    public void SetBachelorRealDated(string bachelorName, bool dated, string location)
    {
        if (string.IsNullOrEmpty(bachelorName))
            return;

        BachelorPreferencesData prefData = GetOrCreateBachelorData(bachelorName);
        prefData.hasCompletedRealDate = dated;

        if (dated && !string.IsNullOrEmpty(location))
        {
            prefData.lastRealDateLocation = location;
        }

        // If real dated, also mark as speed dated for consistency
        if (dated)
        {
            prefData.hasBeenSpeedDated = true;
        }
    }

    /// <summary>
    /// Gets or creates a bachelor's preference data
    /// </summary>
    public BachelorPreferencesData GetOrCreateBachelorData(string bachelorName)
    {
        if (string.IsNullOrEmpty(bachelorName))
            return null;

        BachelorPreferencesData prefData = BachelorPreferences.Find(bp =>
            bp.bachelorName == bachelorName
        );

        if (prefData == null)
        {
            prefData = new BachelorPreferencesData(bachelorName);
            BachelorPreferences.Add(prefData);
        }

        return prefData;
    }
}

[System.Serializable]
public class BachelorPreferencesData
{
    [SerializeField]
    public string bachelorName;

    [Header("Dating Status")]
    [SerializeField]
    public bool hasBeenSpeedDated = false;

    [SerializeField]
    public bool hasCompletedRealDate = false;

    [SerializeField]
    public string lastRealDateLocation = "";

    [Header("Preferences")]
    [SerializeField]
    public List<string> discoveredLikes = new List<string>();

    [SerializeField]
    public List<string> discoveredDislikes = new List<string>();

    public BachelorPreferencesData()
    {
        // Default constructor for deserialization
    }

    public BachelorPreferencesData(string name)
    {
        bachelorName = name;
    }
}
