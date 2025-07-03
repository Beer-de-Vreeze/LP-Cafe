using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoteBook : MonoBehaviour
{
    [Header("Bachelor Reference")]
    [SerializeField]
    private NewBachelorSO currentBachelor;

    [Header("UI Elements")]
    [SerializeField]
    private Transform likesContainer;

    [SerializeField]
    private Transform dislikesContainer;

    [SerializeField]
    private GameObject likeEntryPrefab;

    [SerializeField]
    private GameObject dislikeEntryPrefab;

    [SerializeField]
    private GameObject lockedInfoText;

    [Header("Animation Settings")]
    [SerializeField]
    private float revealAnimationDuration = 0.5f;

    [SerializeField]
    private Color highlightColor = Color.yellow;

    [SerializeField]
    private float highlightDuration = 1.5f;

    // Track entries for manipulation
    private Dictionary<string, GameObject> likeEntryObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> dislikeEntryObjects =
        new Dictionary<string, GameObject>();
    private bool isInitialized = false;

    void Awake()
    {
        // Don't register events in Awake - do it in SetBachelor to ensure proper cleanup
    }

    void OnEnable()
    {
        if (currentBachelor != null)
        {
            // Validate bachelor name
            if (string.IsNullOrEmpty(currentBachelor._name))
            {
                Debug.LogError(
                    "NoteBook: Bachelor has empty name! Cannot synchronize with save data."
                );
            }
            else
            {
                Debug.Log($"NoteBook: OnEnable for bachelor {currentBachelor._name}");

                // Force synchronization with save data when notebook is opened
                currentBachelor.SynchronizeWithSaveData();
            }

            // Ensure we're properly registered to the current bachelor's events
            RegisterToBachelorEvents(currentBachelor);

            if (!isInitialized)
            {
                InitializeNotebook();
            }
            else
            {
                // Check for any newly discovered preferences and create entries
                RefreshDiscoveredEntries();
                UpdateVisibility();
            }
        }
    }

    void OnDisable()
    {
        // Extra cleanup if needed
    }

    void OnDestroy()
    {
        // Unregister from events using helper method
        UnregisterFromBachelorEvents(currentBachelor);
    }

    private void HandlePreferenceDiscovered(NewBachelorSO.BachelorPreference preference)
    {
        // Ensure we have a current bachelor and the preference belongs to it
        if (currentBachelor == null || preference == null)
        {
            Debug.LogWarning(
                "NoteBook: HandlePreferenceDiscovered called but currentBachelor or preference is null"
            );
            return;
        }

        // Double-check that this preference actually belongs to the current bachelor
        bool belongsToCurrentBachelor = false;
        if (currentBachelor._likes != null)
        {
            foreach (var like in currentBachelor._likes)
            {
                if (like == preference)
                {
                    belongsToCurrentBachelor = true;
                    break;
                }
            }
        }

        if (!belongsToCurrentBachelor && currentBachelor._dislikes != null)
        {
            foreach (var dislike in currentBachelor._dislikes)
            {
                if (dislike == preference)
                {
                    belongsToCurrentBachelor = true;
                    break;
                }
            }
        }

        if (!belongsToCurrentBachelor)
        {
            Debug.LogWarning(
                $"NoteBook: Received preference '{preference.description}' that doesn't belong to current bachelor '{currentBachelor._name}'"
            );
            return;
        }

        // Create the entry when it's discovered
        CreateEntryForPreference(preference);

        // Ensure the preference is saved with the bachelor name
        currentBachelor.SaveDiscoveredPreferences();

        // Visual feedback for new discoveries
        UpdateVisibility();

        // Find and highlight the newly discovered entry
        bool isLike = false;
        foreach (var like in currentBachelor._likes)
        {
            if (like == preference)
            {
                isLike = true;
                break;
            }
        }

        if (isLike && likeEntryObjects.TryGetValue(preference.description, out GameObject likeObj))
        {
            StartCoroutine(AnimateHighlightEntry(likeObj));
        }
        else if (
            !isLike
            && dislikeEntryObjects.TryGetValue(preference.description, out GameObject dislikeObj)
        )
        {
            StartCoroutine(AnimateHighlightEntry(dislikeObj));
        }
    }

    private IEnumerator AnimateHighlightEntry(GameObject entry)
    {
        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            Color originalColor = text.color;

            // Animate to highlight color
            float timer = 0f;
            while (timer < revealAnimationDuration)
            {
                timer += Time.deltaTime;
                text.color = Color.Lerp(
                    originalColor,
                    highlightColor,
                    timer / revealAnimationDuration
                );
                yield return null;
            }

            // Hold highlight for a moment
            yield return new WaitForSeconds(highlightDuration);

            // Animate back to original color
            timer = 0f;
            while (timer < revealAnimationDuration)
            {
                timer += Time.deltaTime;
                text.color = Color.Lerp(
                    highlightColor,
                    originalColor,
                    timer / revealAnimationDuration
                );
                yield return null;
            }

            text.color = originalColor;
        }
    }

    /// <summary>
    /// Sets up the notebook with the current bachelor's information
    /// </summary>
    private void InitializeNotebook()
    {
        if (currentBachelor == null)
        {
            Debug.LogWarning("Cannot initialize notebook: No bachelor assigned!");
            return;
        }

        // Validate bachelor name
        if (string.IsNullOrEmpty(currentBachelor._name))
        {
            Debug.LogError("NoteBook: Cannot initialize notebook: Bachelor has empty name!");
            return;
        }

        Debug.Log($"NoteBook: Initializing notebook for {currentBachelor._name}");

        // Clear any existing entries
        ClearEntries();

        // Only create entries for already discovered preferences
        if (currentBachelor._likes != null && likesContainer != null)
        {
            foreach (var like in currentBachelor._likes)
            {
                if (like.discovered)
                {
                    CreateLikeEntry(like);
                }
            }
        }

        // Only create entries for already discovered dislikes
        if (currentBachelor._dislikes != null && dislikesContainer != null)
        {
            foreach (var dislike in currentBachelor._dislikes)
            {
                if (dislike.discovered)
                {
                    CreateDislikeEntry(dislike);
                }
            }
        }

        UpdateVisibility();
        isInitialized = true;
    }

    /// <summary>
    /// Updates the visibility of the locked info text based on discovery status
    /// </summary>
    private void UpdateVisibility()
    {
        if (currentBachelor == null)
            return;

        // Show locked message if nothing is discovered yet
        bool anyPreferenceDiscovered = HasAnyPreferenceDiscovered();
        if (lockedInfoText != null)
        {
            lockedInfoText.SetActive(!anyPreferenceDiscovered);
        }
    }

    /// <summary>
    /// Checks if any preference has been discovered
    /// </summary>
    private bool HasAnyPreferenceDiscovered()
    {
        if (currentBachelor == null)
            return false;

        if (currentBachelor._likes != null)
        {
            foreach (var like in currentBachelor._likes)
            {
                if (like.discovered)
                    return true;
            }
        }

        if (currentBachelor._dislikes != null)
        {
            foreach (var dislike in currentBachelor._dislikes)
            {
                if (dislike.discovered)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clears all existing entries
    /// </summary>
    private void ClearEntries()
    {
        foreach (var entry in likeEntryObjects.Values)
        {
            Destroy(entry);
        }
        likeEntryObjects.Clear();

        foreach (var entry in dislikeEntryObjects.Values)
        {
            Destroy(entry);
        }
        dislikeEntryObjects.Clear();
        isInitialized = false;
    }

    /// <summary>
    /// Public method to switch to a different bachelor
    /// </summary>
    public void SetBachelor(NewBachelorSO bachelor)
    {
        // Unregister from old bachelor events using helper method
        UnregisterFromBachelorEvents(currentBachelor);

        currentBachelor = bachelor;

        // Register for new bachelor events using helper method
        RegisterToBachelorEvents(currentBachelor);

        // Always ensure bachelor preferences are loaded from save data
        if (currentBachelor != null)
        {
            // Validate bachelor name
            if (string.IsNullOrEmpty(currentBachelor._name))
            {
                Debug.LogError(
                    "NoteBook: SetBachelor called with bachelor that has empty name! Cannot synchronize with save data."
                );
            }
            else
            {
                Debug.Log($"NoteBook: Setting bachelor to {currentBachelor._name}");
                currentBachelor.SynchronizeWithSaveData();
            }
        }

        // Re-initialize with the new bachelor
        isInitialized = false;
        InitializeNotebook();
    }

    /// <summary>
    /// Clears the current bachelor and all notebook entries, allowing a new bachelor to be set.
    /// </summary>
    public void ClearBachelor()
    {
        // Unregister from current bachelor events using helper method
        UnregisterFromBachelorEvents(currentBachelor);

        // Clear all UI entries
        ClearEntries();
        // Hide locked info text
        if (lockedInfoText != null)
        {
            lockedInfoText.SetActive(false);
        }
        currentBachelor = null;
        isInitialized = false;
    }

    /// <summary>
    /// Resets notebook entries while keeping the current bachelor reference.
    /// Used when restarting dialogue with the same bachelor.
    /// </summary>
    public void ResetNotebookEntries()
    {
        // Clear all UI entries but keep the bachelor reference
        ClearEntries();

        // Show locked info text since no preferences are discovered
        if (lockedInfoText != null)
        {
            lockedInfoText.SetActive(true);
        }

        // Mark as uninitialized so it will rebuild when needed
        isInitialized = false;
    }

    /// <summary>
    /// Creates an entry for a discovered preference
    /// </summary>
    private void CreateEntryForPreference(NewBachelorSO.BachelorPreference preference)
    {
        if (currentBachelor == null)
            return;

        // Check if it's a like preference
        bool isLike = false;
        foreach (var like in currentBachelor._likes)
        {
            if (like == preference)
            {
                isLike = true;
                break;
            }
        }

        if (isLike)
        {
            CreateLikeEntry(preference);
        }
        else
        {
            CreateDislikeEntry(preference);
        }
    }

    /// <summary>
    /// Creates a like entry
    /// </summary>
    private void CreateLikeEntry(NewBachelorSO.BachelorPreference like)
    {
        if (likesContainer == null || likeEntryPrefab == null)
            return;

        // Don't create if already exists
        if (likeEntryObjects.ContainsKey(like.description))
            return;

        GameObject entry = Instantiate(likeEntryPrefab, likesContainer);
        TextMeshProUGUI textComponent = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = like.description;
        }

        // Add icon if available
        if (like.icon != null)
        {
            Image iconImage = entry.GetComponentInChildren<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = like.icon;
                iconImage.enabled = true;
            }
        }

        likeEntryObjects[like.description] = entry;
        entry.SetActive(true); // Always active since it's only created when discovered
    }

    /// <summary>
    /// Creates a dislike entry
    /// </summary>
    private void CreateDislikeEntry(NewBachelorSO.BachelorPreference dislike)
    {
        if (dislikesContainer == null || dislikeEntryPrefab == null)
            return;

        // Don't create if already exists
        if (dislikeEntryObjects.ContainsKey(dislike.description))
            return;

        GameObject entry = Instantiate(dislikeEntryPrefab, dislikesContainer);
        TextMeshProUGUI textComponent = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = dislike.description;
        }

        // Add icon if available
        if (dislike.icon != null)
        {
            Image iconImage = entry.GetComponentInChildren<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = dislike.icon;
                iconImage.enabled = true;
            }
        }

        dislikeEntryObjects[dislike.description] = entry;
        entry.SetActive(true); // Always active since it's only created when discovered
    }

    /// <summary>
    /// Refreshes the notebook to create entries for any newly discovered preferences
    /// </summary>
    private void RefreshDiscoveredEntries()
    {
        if (currentBachelor == null)
            return;

        // Check for newly discovered likes
        if (currentBachelor._likes != null)
        {
            foreach (var like in currentBachelor._likes)
            {
                if (like.discovered && !likeEntryObjects.ContainsKey(like.description))
                {
                    CreateLikeEntry(like);
                }
            }
        }

        // Check for newly discovered dislikes
        if (currentBachelor._dislikes != null)
        {
            foreach (var dislike in currentBachelor._dislikes)
            {
                if (dislike.discovered && !dislikeEntryObjects.ContainsKey(dislike.description))
                {
                    CreateDislikeEntry(dislike);
                }
            }
        }
    }

    /// <summary>
    /// Helper method to safely register to bachelor events
    /// </summary>
    private void RegisterToBachelorEvents(NewBachelorSO bachelor)
    {
        if (bachelor != null)
        {
            // Ensure we don't double-register
            bachelor.OnPreferenceDiscovered -= HandlePreferenceDiscovered;
            bachelor.OnPreferenceDiscovered += HandlePreferenceDiscovered;
        }
    }

    /// <summary>
    /// Helper method to safely unregister from bachelor events
    /// </summary>
    private void UnregisterFromBachelorEvents(NewBachelorSO bachelor)
    {
        if (bachelor != null)
        {
            bachelor.OnPreferenceDiscovered -= HandlePreferenceDiscovered;
        }
    }

    /// <summary>
    /// Ensures the notebook is properly connected to the specified bachelor.
    /// Call this from DialogueDisplay or other systems when bachelor changes occur.
    /// </summary>
    public void EnsureBachelorConnection(NewBachelorSO bachelor)
    {
        if (bachelor == null)
        {
            Debug.LogWarning("NoteBook: EnsureBachelorConnection called with null bachelor!");
            return;
        }

        // Validate bachelor name
        if (string.IsNullOrEmpty(bachelor._name))
        {
            Debug.LogError(
                "NoteBook: EnsureBachelorConnection called with bachelor that has empty name!"
            );
            return;
        }

        if (currentBachelor != bachelor)
        {
            Debug.Log($"NoteBook: Connecting to different bachelor: {bachelor._name}");
            SetBachelor(bachelor);
        }
        else if (currentBachelor != null)
        {
            Debug.Log(
                $"NoteBook: Refreshing connection to current bachelor: {currentBachelor._name}"
            );

            // Even if it's the same bachelor, ensure we're properly registered
            RegisterToBachelorEvents(currentBachelor);

            // Reload preferences from save data
            ReloadFromSaveData();

            // Refresh the notebook to catch any state changes
            if (isInitialized)
            {
                RefreshDiscoveredEntries();
                UpdateVisibility();
            }
        }
    }

    /// <summary>
    /// Manually saves the current bachelor's discovered preferences
    /// Called automatically when preferences are discovered, but can be called manually if needed
    /// </summary>
    public void SaveCurrentBachelorPreferences()
    {
        if (currentBachelor == null)
        {
            Debug.LogWarning(
                "NoteBook: SaveCurrentBachelorPreferences called but no bachelor is assigned!"
            );
            return;
        }

        // Validate bachelor name
        if (string.IsNullOrEmpty(currentBachelor._name))
        {
            Debug.LogError(
                "NoteBook: SaveCurrentBachelorPreferences called with bachelor that has empty name! Cannot save preferences."
            );
            return;
        }

        Debug.Log($"NoteBook: Manually saving preferences for {currentBachelor._name}");
        currentBachelor.SaveDiscoveredPreferences();
    }

    /// <summary>
    /// Forces the notebook to reload from saved data
    /// Useful for debugging or when save data changes externally
    /// </summary>
    public void ReloadFromSaveData()
    {
        if (currentBachelor != null)
        {
            // Validate bachelor name
            if (string.IsNullOrEmpty(currentBachelor._name))
            {
                Debug.LogError(
                    "NoteBook: ReloadFromSaveData called with bachelor that has empty name! Cannot synchronize with save data."
                );
                return;
            }

            Debug.Log($"NoteBook: Reloading save data for {currentBachelor._name}");

            // Clear current state
            ClearEntries();

            // Force bachelor to reload from save data
            currentBachelor.SynchronizeWithSaveData();

            // Reinitialize notebook with updated data
            isInitialized = false;
            InitializeNotebook();
        }
        else
        {
            Debug.LogWarning("NoteBook: ReloadFromSaveData called but no bachelor is assigned!");
        }
    }

    /// <summary>
    /// NoteBook system for tracking and displaying bachelor preferences
    ///
    /// SAVE/LOAD FUNCTIONALITY:
    /// - Automatically saves discovered preferences when they are found
    /// - Loads previously discovered preferences when talking to a bachelor again
    /// - Preferences are saved to the game's save file (save.json) in the persistent data folder
    /// - Each bachelor's preferences and dating status are stored separately by bachelor name
    ///
    /// REFACTORED SAVE SYSTEM:
    /// - Bachelor preferences are now stored in BachelorPreferencesData objects in the SaveData.BachelorPreferences list
    /// - Each BachelorPreferencesData object contains:
    ///   - bachelorName: The unique identifier for the bachelor
    ///   - hasBeenSpeedDated: Boolean flag for speed dating status
    ///   - hasCompletedRealDate: Boolean flag for real dating status
    ///   - lastRealDateLocation: String storing the location of the last real date
    ///   - discoveredLikes: List of strings representing discovered likes
    ///   - discoveredDislikes: List of strings representing discovered dislikes
    /// - The bachelor name is CRITICAL for data integrity - never use a bachelor with an empty name
    /// - The system automatically migrates legacy data to the new format
    ///
    /// HOW IT WORKS:
    /// 1. When a preference is discovered via DiscoverLike() or DiscoverDislike(), it's automatically saved
    /// 2. When SetBachelor() is called, the bachelor's SynchronizeWithSaveData() loads previously discovered preferences
    /// 3. The notebook then displays all discovered preferences from the save data
    /// 4. No manual save/load calls are needed - it's all automatic
    /// 5. All data is stored by bachelor name to ensure the correct preferences are loaded
    ///
    /// IMPORTANT NOTES:
    /// - Always ensure the bachelor has a valid, non-empty name before using with the notebook
    /// - Use EnsureBachelorConnection() when switching bachelors to ensure proper synchronization
    /// - The save system now avoids empty strings and properly handles bachelor-specific data
    ///
    /// DEBUG METHODS:
    /// - "Debug: Discover All Preferences" - Discovers all preferences for testing
    /// - "Debug: Save Current Preferences" - Manually saves current state
    /// - "Debug: Reload From Save Data" - Reloads notebook from save data
    /// - "Debug: Clear Save Data" - Resets and clears saved preferences
    /// - "Debug: Log Current State" - Shows current state in console
    /// - "Debug: Test Save System Integration" - Tests the refactored save system integration
    /// </summary>
    // Debug methods for testing
    [ContextMenu("Debug: Discover All Preferences")]
    public void DebugDiscoverAllPreferences()
    {
        if (currentBachelor == null)
        {
            Debug.LogWarning("No bachelor assigned to discover preferences for!");
            return;
        }

        // Validate bachelor name
        if (string.IsNullOrEmpty(currentBachelor._name))
        {
            Debug.LogError(
                "NoteBook: DebugDiscoverAllPreferences called with bachelor that has empty name!"
            );
            return;
        }

        currentBachelor.DiscoverAllPreferencesForTesting();
        Debug.Log($"Discovered all preferences for {currentBachelor._name}");

        // Force save to ensure preferences are stored properly per-bachelor
        currentBachelor.SaveDiscoveredPreferences();
    }

    [ContextMenu("Debug: Log Current State")]
    public void DebugLogCurrentState()
    {
        if (currentBachelor == null)
        {
            Debug.Log("No bachelor assigned");
            return;
        }

        Debug.Log($"Current Bachelor: {currentBachelor._name}");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Like entries count: {likeEntryObjects.Count}");
        Debug.Log($"Dislike entries count: {dislikeEntryObjects.Count}");

        if (currentBachelor._likes != null)
        {
            for (int i = 0; i < currentBachelor._likes.Length; i++)
            {
                var like = currentBachelor._likes[i];
                Debug.Log($"Like {i}: '{like.description}' - Discovered: {like.discovered}");
            }
        }

        if (currentBachelor._dislikes != null)
        {
            for (int i = 0; i < currentBachelor._dislikes.Length; i++)
            {
                var dislike = currentBachelor._dislikes[i];
                Debug.Log(
                    $"Dislike {i}: '{dislike.description}' - Discovered: {dislike.discovered}"
                );
            }
        }
    }

    [ContextMenu("Debug: Save Current Preferences")]
    public void DebugSaveCurrentPreferences()
    {
        if (currentBachelor != null)
        {
            SaveCurrentBachelorPreferences();
            Debug.Log($"Manually saved preferences for {currentBachelor._name}");
        }
        else
        {
            Debug.LogWarning("No bachelor assigned to save preferences for!");
        }
    }

    [ContextMenu("Debug: Reload From Save Data")]
    public void DebugReloadFromSaveData()
    {
        if (currentBachelor != null)
        {
            Debug.Log($"Reloading notebook from save data for {currentBachelor._name}");
            ReloadFromSaveData();
            Debug.Log(
                $"Reload complete. Current entries - Likes: {likeEntryObjects.Count}, Dislikes: {dislikeEntryObjects.Count}"
            );
        }
        else
        {
            Debug.LogWarning("No bachelor assigned to reload for!");
        }
    }

    [ContextMenu("Debug: Clear Save Data")]
    public void DebugClearSaveData()
    {
        if (currentBachelor != null)
        {
            // Validate bachelor name
            if (string.IsNullOrEmpty(currentBachelor._name))
            {
                Debug.LogError(
                    "NoteBook: DebugClearSaveData called with bachelor that has empty name! Cannot clear preferences."
                );
                return;
            }

            Debug.Log($"NoteBook: Clearing save data for {currentBachelor._name}");

            // Reset discoveries in memory
            currentBachelor.ResetDiscoveries();

            // Save the reset state with proper bachelor name
            currentBachelor.SaveDiscoveredPreferences();

            // Refresh the notebook
            ReloadFromSaveData();

            Debug.Log($"Cleared saved preferences for {currentBachelor._name}");
        }
        else
        {
            Debug.LogWarning("No bachelor assigned to clear preferences for!");
        }
    }

    [ContextMenu("Debug: Test Save System Integration")]
    public void DebugTestSaveSystemIntegration()
    {
        if (currentBachelor == null)
        {
            Debug.LogError("NoteBook: Cannot test save system integration - no bachelor assigned!");
            return;
        }

        if (string.IsNullOrEmpty(currentBachelor._name))
        {
            Debug.LogError(
                "NoteBook: Cannot test save system integration - bachelor has empty name!"
            );
            return;
        }

        Debug.Log($"NoteBook: Testing save system integration for {currentBachelor._name}");

        // 1. Load current save data
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
        {
            Debug.LogWarning("No save data found. Creating new save data.");
            saveData = new SaveData();
        }

        // 2. Look for this bachelor's data
        BachelorPreferencesData prefData = saveData.BachelorPreferences.Find(bp =>
            bp.bachelorName == currentBachelor._name
        );

        if (prefData != null)
        {
            Debug.Log($"Found existing data for {currentBachelor._name}:");
            Debug.Log($"  - Speed dated: {prefData.hasBeenSpeedDated}");
            Debug.Log($"  - Real dated: {prefData.hasCompletedRealDate}");
            Debug.Log($"  - Real date location: {prefData.lastRealDateLocation}");
            Debug.Log($"  - Discovered likes: {prefData.discoveredLikes.Count}");
            Debug.Log($"  - Discovered dislikes: {prefData.discoveredDislikes.Count}");

            // List all preferences
            if (prefData.discoveredLikes.Count > 0)
            {
                Debug.Log("  - Likes:");
                foreach (var like in prefData.discoveredLikes)
                {
                    Debug.Log($"    • {like}");
                }
            }

            if (prefData.discoveredDislikes.Count > 0)
            {
                Debug.Log("  - Dislikes:");
                foreach (var dislike in prefData.discoveredDislikes)
                {
                    Debug.Log($"    • {dislike}");
                }
            }
        }
        else
        {
            Debug.Log($"No saved preferences found for {currentBachelor._name}");
        }

        // 3. Test saving a preference if we have any undiscovered ones
        bool discoveredNewPreference = false;

        if (currentBachelor._likes != null)
        {
            foreach (var like in currentBachelor._likes)
            {
                if (!like.discovered)
                {
                    Debug.Log($"Discovering new like preference for testing: {like.description}");
                    like.discovered = true;
                    discoveredNewPreference = true;
                    break;
                }
            }
        }

        if (!discoveredNewPreference && currentBachelor._dislikes != null)
        {
            foreach (var dislike in currentBachelor._dislikes)
            {
                if (!dislike.discovered)
                {
                    Debug.Log(
                        $"Discovering new dislike preference for testing: {dislike.description}"
                    );
                    dislike.discovered = true;
                    discoveredNewPreference = true;
                    break;
                }
            }
        }

        if (discoveredNewPreference)
        {
            // Save the discovered preference
            currentBachelor.SaveDiscoveredPreferences();

            // Refresh the notebook UI
            RefreshDiscoveredEntries();
            UpdateVisibility();

            Debug.Log("Saved new preference and refreshed notebook UI");

            // Verify it was saved correctly
            SaveData updatedSaveData = SaveSystem.Deserialize();
            BachelorPreferencesData updatedPrefData = updatedSaveData.BachelorPreferences.Find(bp =>
                bp.bachelorName == currentBachelor._name
            );

            if (updatedPrefData != null)
            {
                Debug.Log($"Updated data for {currentBachelor._name}:");
                Debug.Log($"  - Discovered likes: {updatedPrefData.discoveredLikes.Count}");
                Debug.Log($"  - Discovered dislikes: {updatedPrefData.discoveredDislikes.Count}");
            }
        }
        else
        {
            Debug.Log("No undiscovered preferences found to test with.");
        }
    }
}
