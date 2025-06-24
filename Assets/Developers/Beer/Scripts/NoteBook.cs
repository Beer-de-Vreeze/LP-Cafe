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

        // Only reset discoveries if this is a completely new bachelor setup
        // Comment out the line below if you want to preserve discovered preferences
        // currentBachelor.EnsureUndiscoveredState();

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
        if (currentBachelor != bachelor)
        {
            SetBachelor(bachelor);
        }
        else if (currentBachelor != null)
        {
            // Even if it's the same bachelor, ensure we're properly registered
            RegisterToBachelorEvents(currentBachelor);

            // Refresh the notebook to catch any state changes
            if (isInitialized)
            {
                RefreshDiscoveredEntries();
                UpdateVisibility();
            }
        }
    }

    // Debug methods for testing
    [ContextMenu("Debug: Discover All Preferences")]
    public void DebugDiscoverAllPreferences()
    {
        if (currentBachelor != null)
        {
            currentBachelor.DiscoverAllPreferencesForTesting();
            Debug.Log($"Discovered all preferences for {currentBachelor._name}");
        }
        else
        {
            Debug.LogWarning("No bachelor assigned to discover preferences for!");
        }
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
}
