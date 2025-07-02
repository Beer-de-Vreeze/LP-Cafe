using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    [Header("Save Data")]
    private SaveData _currentSaveData;

    [Header("UI Buttons")]
    [SerializeField]
    private Button goToGameButton;

    [SerializeField]
    private Button continueButton;

    [SerializeField]
    private Button newGameButton;

    [SerializeField]
    private Button quitButton;

    private void Start()
    {
        // Check save file and update UI on start
        UpdateUIBasedOnSaveFile();

        // In builds, check if we need to reset bachelor data
        CheckForRuntimeReset();

        // Hide quit button on WebGL builds
        HideQuitButtonOnWebGL();
    }

    private void UpdateUIBasedOnSaveFile()
    {
        // Load save data to check if it exists
        _currentSaveData = SaveSystem.Deserialize();

        if (_currentSaveData != null && HasPlayedBefore())
        {
            // Player has save data - show continue/new game options
            if (goToGameButton != null)
                goToGameButton.gameObject.SetActive(false);
            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
            if (newGameButton != null)
                newGameButton.gameObject.SetActive(true);

            Debug.Log("Save file found. Showing continue/new game options.");
        }
        else
        {
            // No save data or new player - show go to game button
            if (goToGameButton != null)
                goToGameButton.gameObject.SetActive(true);
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
            if (newGameButton != null)
                newGameButton.gameObject.SetActive(false);

            Debug.Log("No save file found. Showing go to game button.");
        }
    }

    private bool HasPlayedBefore()
    {
        // Check if the player has any progress (dated any bachelors)
        return _currentSaveData.DatedBachelors.Count > 0
            || _currentSaveData.RealDatedBachelors.Count > 0;
    }

    public void GoToGame()
    {
        // Load the save file
        LoadSaveFile();

        // Load the game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("FirstDate");
    }

    public void ContinueGame()
    {
        // Load existing save data
        LoadSaveFile();

        // Go to TestCafe scene for continuing players
        UnityEngine.SceneManagement.SceneManager.LoadScene("TestCafe");
    }

    public void NewGame()
    {
        // Reset all bachelor ScriptableObjects and their love meters
        ResetAllBachelorData();

        // Verify the reset worked
        VerifyBachelorReset();

        // Create fresh save data
        _currentSaveData = new SaveData();

        // Save the new empty save file
        SaveSystem.SerializeData(_currentSaveData);

        // Verify the save file was created successfully
        VerifySaveFileCreation();

        Debug.Log("Starting new game. Created fresh save data and reset all bachelor progress.");

        // Load the first date scene for new game
        UnityEngine.SceneManagement.SceneManager.LoadScene("FirstDate");
    }

    /// <summary>
    /// Resets all bachelor ScriptableObjects and their love meters to initial state
    /// Works in both editor and builds
    /// </summary>
    private void ResetAllBachelorData()
    {
        Debug.Log("[UIManager] Starting ResetAllBachelorData");

#if UNITY_EDITOR
        // In Editor: Find all bachelor assets and reset them directly
        Debug.Log("[UIManager] Running in Editor - resetting asset files directly");

        // Find all NewBachelorSO assets in the project
        string[] bachelorGuids = UnityEditor.AssetDatabase.FindAssets("t:NewBachelorSO");
        Debug.Log($"[UIManager] Found {bachelorGuids.Length} bachelor assets");

        foreach (string guid in bachelorGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            NewBachelorSO bachelor = UnityEditor.AssetDatabase.LoadAssetAtPath<NewBachelorSO>(path);

            if (bachelor != null)
            {
                Debug.Log(
                    $"[UIManager] Resetting bachelor asset: {bachelor._name} at path: {path}"
                );
                bachelor.ResetToInitialState();
                UnityEditor.EditorUtility.SetDirty(bachelor);

                // Also mark love meter dirty if it exists
                if (bachelor._loveMeter != null)
                {
                    UnityEditor.EditorUtility.SetDirty(bachelor._loveMeter);
                    Debug.Log($"[UIManager] Marked love meter dirty for {bachelor._name}");
                }
            }
        }

        // Find and reset standalone LoveMeterSO assets
        string[] loveMeterGuids = UnityEditor.AssetDatabase.FindAssets("t:LoveMeterSO");
        Debug.Log($"[UIManager] Found {loveMeterGuids.Length} love meter assets");

        foreach (string guid in loveMeterGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            LoveMeterSO loveMeter = UnityEditor.AssetDatabase.LoadAssetAtPath<LoveMeterSO>(path);

            if (loveMeter != null)
            {
                // Check if this love meter is linked to any bachelor
                bool isLinked = false;
                foreach (string bachelorGuid in bachelorGuids)
                {
                    string bachelorPath = UnityEditor.AssetDatabase.GUIDToAssetPath(bachelorGuid);
                    NewBachelorSO bachelor =
                        UnityEditor.AssetDatabase.LoadAssetAtPath<NewBachelorSO>(bachelorPath);
                    if (bachelor != null && bachelor._loveMeter == loveMeter)
                    {
                        isLinked = true;
                        break;
                    }
                }

                // Only reset standalone love meters
                if (!isLinked)
                {
                    Debug.Log($"[UIManager] Resetting standalone love meter at path: {path}");
                    loveMeter.Reset();
                    UnityEditor.EditorUtility.SetDirty(loveMeter);
                }
            }
        }

        // Save all dirty assets
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("[UIManager] Saved all dirty assets to disk");

#else
        // In Build: Set flag for runtime reset (bachelors will check this on their next OnEnable)
        Debug.Log("[UIManager] Running in build - setting runtime reset flag");
        SaveData currentSave = SaveSystem.Deserialize();
        if (currentSave == null)
        {
            currentSave = new SaveData();
        }
        currentSave.ShouldResetBachelors = true;
        SaveSystem.SerializeData(currentSave);

        // Also immediately reset any currently loaded bachelors
        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
        foreach (NewBachelorSO bachelor in allBachelors)
        {
            if (bachelor != null)
            {
                bachelor.ResetRuntimeState();
                Debug.Log($"[UIManager] Reset runtime state for bachelor: {bachelor._name}");
            }
        }
#endif

        Debug.Log("[UIManager] ResetAllBachelorData completed");
    }

    /// <summary>
    /// Verifies that the save file was successfully created
    /// </summary>
    private void VerifySaveFileCreation()
    {
        // Try to load the save file we just created
        SaveData verificationData = SaveSystem.Deserialize();

        if (verificationData != null)
        {
            Debug.Log("✓ Save file verification successful - file was created and can be loaded.");
        }
        else
        {
            Debug.LogError(
                "✗ Save file verification failed - file was not created or cannot be loaded!"
            );
        }
    }

    /// <summary>
    /// Verifies that bachelor data has been properly reset
    /// </summary>
    private void VerifyBachelorReset()
    {
        Debug.Log("[UIManager] Starting bachelor reset verification");

        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
        bool allResetCorrectly = true;
        int bachelorCount = 0;

#if !UNITY_EDITOR
        // In builds, also check if reset flag is set properly
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null && !saveData.ShouldResetBachelors)
        {
            Debug.Log("[UIManager] ✓ Build mode: Reset flag is properly cleared in save data");
        }
        else if (saveData != null && saveData.ShouldResetBachelors)
        {
            Debug.LogWarning(
                "[UIManager] ⚠ Build mode: Reset flag is still set - runtime reset may not have been applied yet"
            );
        }
#endif

        foreach (NewBachelorSO bachelor in allBachelors)
        {
            if (bachelor != null)
            {
                bachelorCount++;
                Debug.Log($"[UIManager] Verifying reset for bachelor: {bachelor._name}");

                // Use the bachelor's own verification method for comprehensive checking
                bool bachelorResetCorrectly = bachelor.VerifyCompleteReset();

                if (!bachelorResetCorrectly)
                {
                    allResetCorrectly = false;
                    Debug.LogError(
                        $"[UIManager] ❌ Bachelor {bachelor._name} failed reset verification!"
                    );
                }
                else
                {
                    Debug.Log($"[UIManager] ✅ Bachelor {bachelor._name} passed reset verification");
                }
            }
        }

        Debug.Log($"[UIManager] Verified reset for {bachelorCount} bachelors");

        if (allResetCorrectly)
        {
            Debug.Log("[UIManager] ✅ ALL BACHELORS VERIFIED AS PROPERLY RESET!");
        }
        else
        {
            Debug.LogError("[UIManager] ❌ SOME BACHELORS WERE NOT PROPERLY RESET!");
        }
    }

    private void LoadSaveFile()
    {
        // Attempt to load existing save data
        _currentSaveData = SaveSystem.Deserialize();

        if (_currentSaveData == null)
        {
            // Create new save data if no save file exists
            _currentSaveData = new SaveData();

            // Save the new save data to disk
            SaveSystem.SerializeData(_currentSaveData);

            Debug.Log("No save file found. Created and saved new save data.");
        }
        else
        {
#if !UNITY_EDITOR
            // In builds, clear the reset flag if it's still set from a previous session
            if (_currentSaveData.ShouldResetBachelors)
            {
                _currentSaveData.ShouldResetBachelors = false;
                SaveSystem.SerializeData(_currentSaveData);
                Debug.Log("Build mode: Cleared stale reset flag from save data");
            }
#endif
            Debug.Log("Save file loaded successfully.");
            Debug.Log(
                $"Dated Bachelors ({_currentSaveData.DatedBachelors.Count}): {string.Join(", ", _currentSaveData.DatedBachelors)}"
            );
            Debug.Log(
                $"Real Dated Bachelors ({_currentSaveData.RealDatedBachelors.Count}): {string.Join(", ", _currentSaveData.RealDatedBachelors)}"
            );
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // In Unity Editor, stop play mode
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("Stopping play mode in Unity Editor");
#elif UNITY_WEBGL
        // This should never be called on WebGL since the button is hidden
        Debug.LogWarning(
            "QuitGame called on WebGL build - this should not happen as the button should be hidden"
        );
#else
        // For standalone builds (Windows, Mac, Linux)
        Application.Quit();
        Debug.Log("Quitting application");
#endif
    }

    /// <summary>
    /// Hides the quit button on WebGL builds since quitting is not applicable
    /// </summary>
    private void HideQuitButtonOnWebGL()
    {
#if UNITY_WEBGL
        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(false);
            Debug.Log("Quit button hidden on WebGL build");
        }
#endif
    }

    /// <summary>
    /// Test method to manually reset all bachelor data from the inspector
    /// </summary>
    [ContextMenu("Test Reset All Bachelors")]
    private void TestResetAllBachelors()
    {
        ResetAllBachelorData();
        VerifyBachelorReset();
    }

    /// <summary>
    /// Public method to test reset functionality - can be called from Inspector or other scripts
    /// </summary>
    [ContextMenu("Force Reset All Bachelors Now")]
    public void ForceResetAllBachelorsNow()
    {
        Debug.Log("=== FORCE RESET TRIGGERED ===");
        ResetAllBachelorData();
        VerifyBachelorReset();
        Debug.Log("=== FORCE RESET COMPLETED ===");
    }

    /// <summary>
    /// Temporarily modify bachelor data to test reset functionality
    /// </summary>
    [ContextMenu("Modify Bachelors for Testing")]
    public void ModifyBachelorsForTesting()
    {
        Debug.Log("=== MODIFYING BACHELORS FOR TESTING ===");

#if UNITY_EDITOR
        string[] bachelorGuids = UnityEditor.AssetDatabase.FindAssets("t:NewBachelorSO");

        foreach (string guid in bachelorGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            NewBachelorSO bachelor = UnityEditor.AssetDatabase.LoadAssetAtPath<NewBachelorSO>(path);

            if (bachelor != null)
            {
                Debug.Log($"Modifying {bachelor._name} for testing");

                // Set some test data
                bachelor._HasBeenSpeedDated = true;
                bachelor._isLikeDiscovered = true;
                bachelor._isDislikeDiscovered = true;

                // Discover first preference if available
                if (bachelor._likes != null && bachelor._likes.Length > 0)
                {
                    bachelor._likes[0].discovered = true;
                }

                if (bachelor._dislikes != null && bachelor._dislikes.Length > 0)
                {
                    bachelor._dislikes[0].discovered = true;
                }

                // Increase love meter
                if (bachelor._loveMeter != null)
                {
                    bachelor._loveMeter.IncreaseLove(2); // Should go from 3 to 5
                }

                UnityEditor.EditorUtility.SetDirty(bachelor);
                if (bachelor._loveMeter != null)
                {
                    UnityEditor.EditorUtility.SetDirty(bachelor._loveMeter);
                }

                // Make sure changes are saved to the save file too
                SaveBachelorChanges(bachelor);
            }
        }

        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("=== BACHELOR MODIFICATION COMPLETED ===");
#endif
    }

    /// <summary>
    /// Check if any bachelor has been modified from default state
    /// </summary>
    [ContextMenu("Check Bachelor States")]
    public void CheckBachelorStates()
    {
        Debug.Log("=== CHECKING BACHELOR STATES ===");

#if UNITY_EDITOR
        string[] bachelorGuids = UnityEditor.AssetDatabase.FindAssets("t:NewBachelorSO");
        Debug.Log($"Found {bachelorGuids.Length} bachelor assets to check");

        int resetCount = 0;
        int modifiedCount = 0;

        foreach (string guid in bachelorGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            NewBachelorSO bachelor = UnityEditor.AssetDatabase.LoadAssetAtPath<NewBachelorSO>(path);

            if (bachelor != null)
            {
                Debug.Log($"Bachelor: {bachelor._name}");
                Debug.Log($"  - Speed Dated: {bachelor._HasBeenSpeedDated}");
                Debug.Log($"  - Real Dated: {bachelor._HasCompletedRealDate}");
                Debug.Log($"  - Real Date Location: '{bachelor._LastRealDateLocation}'");
                Debug.Log($"  - Like Discovered: {bachelor._isLikeDiscovered}");
                Debug.Log($"  - Dislike Discovered: {bachelor._isDislikeDiscovered}");

                if (bachelor._loveMeter != null)
                {
                    Debug.Log($"  - Love Value: {bachelor._loveMeter.GetCurrentLove()}");
                }
                else
                {
                    Debug.Log($"  - Love Meter: Not assigned");
                }

                // Check individual preferences
                if (bachelor._likes != null)
                {
                    foreach (var like in bachelor._likes)
                    {
                        if (like.discovered)
                        {
                            Debug.Log($"  - DISCOVERED LIKE: {like.description}");
                        }
                    }
                }

                if (bachelor._dislikes != null)
                {
                    foreach (var dislike in bachelor._dislikes)
                    {
                        if (dislike.discovered)
                        {
                            Debug.Log($"  - DISCOVERED DISLIKE: {dislike.description}");
                        }
                    }
                }

                // Use verification to determine if bachelor is in reset state
                bool isInResetState = bachelor.VerifyCompleteReset();
                if (isInResetState)
                {
                    resetCount++;
                    Debug.Log($"  - STATE: ✅ Reset (Initial State)");
                }
                else
                {
                    modifiedCount++;
                    Debug.Log($"  - STATE: ⚠️ Modified (Has Progress)");
                }

                Debug.Log(""); // Empty line for readability
            }
        }

        Debug.Log(
            $"SUMMARY: {resetCount} bachelors in reset state, {modifiedCount} bachelors with progress"
        );

#else
        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
        Debug.Log($"Found {allBachelors.Length} bachelor instances to check");

        int resetCount = 0;
        int modifiedCount = 0;

        foreach (NewBachelorSO bachelor in allBachelors)
        {
            if (bachelor != null)
            {
                Debug.Log($"Bachelor: {bachelor._name}");
                if (bachelor._loveMeter != null)
                {
                    Debug.Log($"  - Love Value: {bachelor._loveMeter.GetCurrentLove()}");
                }

                bool isInResetState = bachelor.VerifyCompleteReset();
                if (isInResetState)
                {
                    resetCount++;
                    Debug.Log($"  - STATE: ✅ Reset");
                }
                else
                {
                    modifiedCount++;
                    Debug.Log($"  - STATE: ⚠️ Modified");
                }
            }
        }

        Debug.Log(
            $"SUMMARY: {resetCount} bachelors in reset state, {modifiedCount} bachelors with progress"
        );
#endif

        Debug.Log("=== BACHELOR STATE CHECK COMPLETED ===");
    }

    /// <summary>
    /// Checks if bachelor data should be reset based on save data flag
    /// Used in builds where we can't modify ScriptableObject assets
    /// </summary>
    private void CheckForRuntimeReset()
    {
#if !UNITY_EDITOR
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null && saveData.ShouldResetBachelors)
        {
            Debug.Log("Build mode: Applying runtime bachelor reset based on save data flag");

            // Find all bachelors and reset their runtime state
            NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
            foreach (NewBachelorSO bachelor in allBachelors)
            {
                if (bachelor != null)
                {
                    bachelor.ResetRuntimeState();
                }
            }

            // Clear the reset flag since we've applied it
            saveData.ShouldResetBachelors = false;
            SaveSystem.SerializeData(saveData);

            Debug.Log($"Applied runtime reset to {allBachelors.Length} bachelors");
        }
#endif
    }

    /// <summary>
    /// Ensures any changes to the bachelor's flags are properly saved to the save file
    /// </summary>
    private void SaveBachelorChanges(NewBachelorSO bachelor)
    {
        if (bachelor == null || string.IsNullOrEmpty(bachelor._name))
        {
            Debug.LogError("Cannot save changes for null or unnamed bachelor");
            return;
        }

        Debug.Log($"Saving changes for {bachelor._name} to save file");

        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        // Update the bachelor-specific data
        BachelorPreferencesData prefData = saveData.GetOrCreateBachelorData(bachelor._name);

        // Update flags from local memory to save data
        prefData.hasBeenSpeedDated = bachelor._HasBeenSpeedDated;
        prefData.hasCompletedRealDate = bachelor._HasCompletedRealDate;
        prefData.lastRealDateLocation = bachelor._LastRealDateLocation;

        // Also update legacy lists for backward compatibility
        if (bachelor._HasBeenSpeedDated && !saveData.DatedBachelors.Contains(bachelor._name))
        {
            saveData.DatedBachelors.Add(bachelor._name);
        }

        if (bachelor._HasCompletedRealDate && !saveData.RealDatedBachelors.Contains(bachelor._name))
        {
            saveData.RealDatedBachelors.Add(bachelor._name);
        }

        // Synchronize preferences too
        bachelor.SaveDiscoveredPreferences();

        // Save the updated data
        SaveSystem.SerializeData(saveData);
        Debug.Log($"Saved changes for {bachelor._name} to save file");
    }

    /// <summary>
    /// Comprehensive test of the complete reset system - can be called from Inspector
    /// </summary>
    [ContextMenu("Test Complete Reset System")]
    public void TestCompleteResetSystem()
    {
        Debug.Log("=== COMPREHENSIVE RESET SYSTEM TEST ===");

        // Step 1: Check initial state
        Debug.Log("Step 1: Checking initial bachelor states...");
        CheckBachelorStates();

        // Step 2: Modify bachelors to have some data
        Debug.Log("\nStep 2: Modifying bachelors for testing...");
        ModifyBachelorsForTesting();

        // Step 3: Verify modifications worked
        Debug.Log("\nStep 3: Verifying modifications...");
        CheckBachelorStates();

        // Step 4: Perform reset
        Debug.Log("\nStep 4: Performing complete reset...");
        ResetAllBachelorData();

        // Step 5: Verify reset worked
        Debug.Log("\nStep 5: Verifying reset was successful...");
        VerifyBachelorReset();

        // Step 6: Final state check
        Debug.Log("\nStep 6: Final state verification...");
        CheckBachelorStates();

        // Step 7: Verify individual bachelors using their own verification
        Debug.Log("\nStep 7: Individual bachelor verification...");
        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
        int passCount = 0;
        int totalCount = 0;

        foreach (NewBachelorSO bachelor in allBachelors)
        {
            if (bachelor != null)
            {
                totalCount++;
                if (bachelor.VerifyCompleteReset())
                {
                    passCount++;
                }
            }
        }

        Debug.Log($"\nIndividual Verification Results: {passCount}/{totalCount} bachelors passed");

        if (passCount == totalCount)
        {
            Debug.Log("🎉 COMPLETE RESET SYSTEM TEST PASSED! All systems working correctly.");
        }
        else
        {
            Debug.LogError("❌ COMPLETE RESET SYSTEM TEST FAILED! Some issues detected.");
        }

        Debug.Log("=== RESET SYSTEM TEST COMPLETE ===");
    }
}
