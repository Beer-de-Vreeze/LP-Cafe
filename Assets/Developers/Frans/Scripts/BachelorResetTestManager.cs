using UnityEngine;

/// <summary>
/// Simple script to test the bachelor reset functionality
/// Attach to a GameObject and use the context menu or call methods in code
/// </summary>
public class BachelorResetTestManager : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField]
    private bool runTestOnStart = false;

    [SerializeField]
    private bool enableDetailedLogging = true;

    private void Start()
    {
        if (runTestOnStart)
        {
            Invoke("RunCompleteResetTest", 1f); // Delay to ensure everything is loaded
        }
    }

    [ContextMenu("Run Complete Reset Test")]
    public void RunCompleteResetTest()
    {
        Debug.Log("=== Starting Bachelor Reset Test ===");

        // Step 1: Show current state
        LogCurrentBachelorState();

        // Step 2: Try to modify some bachelor data (to have something to reset)
        ModifyBachelorDataForTesting();

        // Step 3: Show modified state
        Debug.Log("--- After Modifications ---");
        LogCurrentBachelorState();

        // Step 4: Reset all bachelors
        Debug.Log("--- Performing Reset ---");
        ResetAllBachelors();

        // Step 5: Verify reset worked
        Debug.Log("--- After Reset ---");
        LogCurrentBachelorState();

        Debug.Log("=== Bachelor Reset Test Complete ===");
    }

    [ContextMenu("Log Current Bachelor State")]
    public void LogCurrentBachelorState()
    {
        Debug.Log("[BachelorResetTestManager] Logging current bachelor state:");

#if UNITY_EDITOR
        // In Editor: Check assets directly
        string[] bachelorGuids = UnityEditor.AssetDatabase.FindAssets("t:NewBachelorSO");
        Debug.Log($"Found {bachelorGuids.Length} bachelor assets in project");

        foreach (string guid in bachelorGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            NewBachelorSO bachelor = UnityEditor.AssetDatabase.LoadAssetAtPath<NewBachelorSO>(path);

            if (bachelor != null)
            {
                LogBachelorDetails(bachelor, enableDetailedLogging);
            }
        }
#else
        // In Build: Check loaded instances
        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
        Debug.Log($"Found {allBachelors.Length} bachelor instances loaded");

        foreach (NewBachelorSO bachelor in allBachelors)
        {
            if (bachelor != null)
            {
                LogBachelorDetails(bachelor, enableDetailedLogging);
            }
        }
#endif
    }

    private void LogBachelorDetails(NewBachelorSO bachelor, bool detailed)
    {
        if (detailed)
        {
            Debug.Log($"Bachelor: {bachelor._name}");
            Debug.Log($"  - Speed Dated: {bachelor._HasBeenSpeedDated}");
            Debug.Log($"  - Like Discovered: {bachelor._isLikeDiscovered}");
            Debug.Log($"  - Dislike Discovered: {bachelor._isDislikeDiscovered}");

            if (bachelor._loveMeter != null)
            {
                Debug.Log(
                    $"  - Love Meter: {bachelor._loveMeter.GetCurrentLove()}/{bachelor._loveMeter._maxLove}"
                );
            }
            else
            {
                Debug.Log($"  - Love Meter: None assigned");
            }

            // Check individual preferences
            if (bachelor._likes != null)
            {
                int discoveredLikes = 0;
                foreach (var like in bachelor._likes)
                {
                    if (like.discovered)
                        discoveredLikes++;
                }
                Debug.Log($"  - Discovered Likes: {discoveredLikes}/{bachelor._likes.Length}");
            }

            if (bachelor._dislikes != null)
            {
                int discoveredDislikes = 0;
                foreach (var dislike in bachelor._dislikes)
                {
                    if (dislike.discovered)
                        discoveredDislikes++;
                }
                Debug.Log(
                    $"  - Discovered Dislikes: {discoveredDislikes}/{bachelor._dislikes.Length}"
                );
            }
        }
        else
        {
            string loveInfo =
                bachelor._loveMeter != null
                    ? bachelor._loveMeter.GetCurrentLove().ToString()
                    : "None";
            Debug.Log(
                $"{bachelor._name}: SpeedDated={bachelor._HasBeenSpeedDated}, Love={loveInfo}"
            );
        }
    }

    [ContextMenu("Modify Bachelor Data For Testing")]
    public void ModifyBachelorDataForTesting()
    {
        Debug.Log("[BachelorResetTestManager] Modifying bachelor data for testing purposes...");

#if UNITY_EDITOR
        string[] bachelorGuids = UnityEditor.AssetDatabase.FindAssets("t:NewBachelorSO");

        foreach (string guid in bachelorGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            NewBachelorSO bachelor = UnityEditor.AssetDatabase.LoadAssetAtPath<NewBachelorSO>(path);

            if (bachelor != null)
            {
                // Modify the bachelor for testing
                bachelor._HasBeenSpeedDated = true;
                bachelor._isLikeDiscovered = true;

                if (bachelor._loveMeter != null)
                {
                    bachelor._loveMeter.IncreaseLove(1);
                }

                // Mark as dirty so changes are saved
                UnityEditor.EditorUtility.SetDirty(bachelor);
                if (bachelor._loveMeter != null)
                {
                    UnityEditor.EditorUtility.SetDirty(bachelor._loveMeter);
                }

                Debug.Log($"Modified {bachelor._name} for testing");
            }
        }

        UnityEditor.AssetDatabase.SaveAssets();
#else
        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();

        foreach (NewBachelorSO bachelor in allBachelors)
        {
            if (bachelor != null)
            {
                // In builds, we can only modify runtime values
                if (bachelor._loveMeter != null)
                {
                    bachelor._loveMeter.IncreaseLove(1);
                }

                Debug.Log($"Modified runtime state for {bachelor._name}");
            }
        }
#endif
    }

    [ContextMenu("Reset All Bachelors")]
    public void ResetAllBachelors()
    {
        // Find UIManager and call its reset method
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            // Use reflection to call the private method
            var resetMethod = typeof(UIManager).GetMethod(
                "TestResetAllBachelors",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (resetMethod != null)
            {
                resetMethod.Invoke(uiManager, null);
                Debug.Log("Reset called via UIManager.TestResetAllBachelors()");
            }
            else
            {
                Debug.LogError("Could not find TestResetAllBachelors method in UIManager");
            }
        }
        else
        {
            Debug.LogError("UIManager not found in scene");
        }
    }
}
