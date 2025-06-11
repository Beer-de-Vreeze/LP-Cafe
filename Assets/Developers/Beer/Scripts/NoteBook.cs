using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    private List<GameObject> likeEntries = new List<GameObject>();
    private List<GameObject> dislikeEntries = new List<GameObject>();

    void Start()
    {
        if (currentBachelor != null)
        {
            InitializeNotebook();
        }
        else
        {
            Debug.LogWarning("No bachelor assigned to notebook!");
        }
    }

    void Update()
    {
        // Check for any changes to the bachelor's discovery status
        if (currentBachelor != null)
        {
            UpdateVisibility();
        }
    }

    /// <summary>
    /// Sets up the notebook with the current bachelor's information
    /// </summary>
    private void InitializeNotebook()
    {
        // Clear any existing entries
        ClearEntries();

        // Create like entries
        if (currentBachelor._knownLikes != null && likesContainer != null)
        {
            foreach (string like in currentBachelor._knownLikes)
            {
                GameObject entry = Instantiate(likeEntryPrefab, likesContainer);
                TextMeshProUGUI textComponent = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = like;
                }
                likeEntries.Add(entry);
            }
        }

        // Create dislike entries
        if (currentBachelor._knownDislikes != null && dislikesContainer != null)
        {
            foreach (string dislike in currentBachelor._knownDislikes)
            {
                GameObject entry = Instantiate(dislikeEntryPrefab, dislikesContainer);
                TextMeshProUGUI textComponent = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = dislike;
                }
                dislikeEntries.Add(entry);
            }
        }

        // Initialize visibility
        UpdateVisibility();
    }

    /// <summary>
    /// Updates the visibility of like/dislike entries based on discovery status
    /// </summary>
    private void UpdateVisibility()
    {
        // Show/hide likes
        bool likesDiscovered = currentBachelor._isLikeDiscovered;
        foreach (GameObject entry in likeEntries)
        {
            entry.SetActive(likesDiscovered);
        }

        // Show/hide dislikes
        bool dislikesDiscovered = currentBachelor._isDislikeDiscovered;
        foreach (GameObject entry in dislikeEntries)
        {
            entry.SetActive(dislikesDiscovered);
        }

        // Show locked message if nothing is discovered yet
        if (lockedInfoText != null)
        {
            lockedInfoText.SetActive(!likesDiscovered && !dislikesDiscovered);
        }
    }

    /// <summary>
    /// Clears all existing entries
    /// </summary>
    private void ClearEntries()
    {
        foreach (GameObject entry in likeEntries)
        {
            Destroy(entry);
        }
        likeEntries.Clear();

        foreach (GameObject entry in dislikeEntries)
        {
            Destroy(entry);
        }
        dislikeEntries.Clear();
    }

    /// <summary>
    /// Public method to switch to a different bachelor
    /// </summary>
    public void SetBachelor(NewBachelorSO bachelor)
    {
        currentBachelor = bachelor;
        InitializeNotebook();
    }
}
