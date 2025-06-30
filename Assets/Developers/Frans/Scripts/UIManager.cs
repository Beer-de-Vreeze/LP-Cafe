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

    private void Start()
    {
        // Check save file and update UI on start
        UpdateUIBasedOnSaveFile();

        // In builds, check if we need to reset bachelor data
        CheckForRuntimeReset();
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
        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
        bool allResetCorrectly = true;

#if !UNITY_EDITOR
        // In builds, also check if reset flag is set properly
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null && !saveData.ShouldResetBachelors)
        {
            Debug.Log("Build mode: Reset flag is properly cleared in save data");
        }
        else if (saveData != null && saveData.ShouldResetBachelors)
        {
            Debug.LogWarning(
                "Build mode: Reset flag is still set - runtime reset may not have been applied yet"
            );
        }
#endif
        foreach (NewBachelorSO bachelor in allBachelors)
        {
            if (bachelor != null)
            {
                // Check if bachelor is properly reset
                if (bachelor._HasBeenSpeedDated)
                {
                    Debug.LogError($"✗ Bachelor {bachelor._name} still marked as speed dated!");
                    allResetCorrectly = false;
                }

                // Check if preferences are reset
                bool hasDiscoveredLikes = false;
                bool hasDiscoveredDislikes = false;

                if (bachelor._likes != null)
                {
                    foreach (var like in bachelor._likes)
                    {
                        if (like.discovered)
                        {
                            hasDiscoveredLikes = true;
                            break;
                        }
                    }
                }

                if (bachelor._dislikes != null)
                {
                    foreach (var dislike in bachelor._dislikes)
                    {
                        if (dislike.discovered)
                        {
                            hasDiscoveredDislikes = true;
                            break;
                        }
                    }
                }

                if (hasDiscoveredLikes || hasDiscoveredDislikes)
                {
                    Debug.LogError(
                        $"✗ Bachelor {bachelor._name} still has discovered preferences!"
                    );
                    allResetCorrectly = false;
                }

                // Check love meter
                if (bachelor._loveMeter != null && bachelor._loveMeter.GetCurrentLove() != 3)
                {
                    Debug.LogError(
                        $"✗ Bachelor {bachelor._name} love meter not reset to 3! Current: {bachelor._loveMeter.GetCurrentLove()}"
                    );
                    allResetCorrectly = false;
                }
            }
        }

        if (allResetCorrectly)
        {
            Debug.Log("✓ All bachelors verified as properly reset!");
        }
        else
        {
            Debug.LogError("✗ Some bachelors were not properly reset!");
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
        Application.Quit();
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

        foreach (string guid in bachelorGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            NewBachelorSO bachelor = UnityEditor.AssetDatabase.LoadAssetAtPath<NewBachelorSO>(path);

            if (bachelor != null)
            {
                Debug.Log($"Bachelor: {bachelor._name}");
                Debug.Log($"  - Speed Dated: {bachelor._HasBeenSpeedDated}");
                Debug.Log($"  - Like Discovered: {bachelor._isLikeDiscovered}");
                Debug.Log($"  - Dislike Discovered: {bachelor._isDislikeDiscovered}");

                if (bachelor._loveMeter != null)
                {
                    Debug.Log($"  - Love Value: {bachelor._loveMeter.GetCurrentLove()}");
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
            }
        }
#else
        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
        Debug.Log($"Found {allBachelors.Length} bachelor instances to check");

        foreach (NewBachelorSO bachelor in allBachelors)
        {
            if (bachelor != null)
            {
                Debug.Log($"Bachelor: {bachelor._name}");
                if (bachelor._loveMeter != null)
                {
                    Debug.Log($"  - Love Value: {bachelor._loveMeter.GetCurrentLove()}");
                }
            }
        }
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
}
