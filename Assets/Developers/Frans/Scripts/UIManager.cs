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

    [SerializeField]
    private Button creditsButton;

    [SerializeField]
    private Button backButton;

    [Header("Credits Screen")]
    [SerializeField]
    private GameObject creditsScreen;

    [Header("Main Menu Background")]
    [SerializeField]
    private GameObject mainMenuBackgroundImage;

    private void Start()
    {
        // Initialize credits UI elements
        InitializeCreditsUI();

        // Check save file and update UI on start
        UpdateUIBasedOnSaveFile();

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

        // Always show credits button in main menu
        if (creditsButton != null)
            creditsButton.gameObject.SetActive(true);

        // Always show quit button in main menu (unless WebGL)
#if !UNITY_WEBGL
        if (quitButton != null)
            quitButton.gameObject.SetActive(true);
#endif

        // Always hide back button in main menu (only shown in credits)
        if (backButton != null)
            backButton.gameObject.SetActive(false);

        // Ensure credits screen is hidden in main menu
        if (creditsScreen != null)
            creditsScreen.SetActive(false);
    }

    private bool HasPlayedBefore()
    {
        // Check if save data exists and is not null
        if (_currentSaveData == null)
            return false;

        // Check for meaningful progress using the new save system format
        if (
            _currentSaveData.BachelorPreferences != null
            && _currentSaveData.BachelorPreferences.Count > 0
        )
        {
            // Check if player has any meaningful progress with bachelors
            foreach (var bachelorData in _currentSaveData.BachelorPreferences)
            {
                if (bachelorData == null || string.IsNullOrEmpty(bachelorData.bachelorName))
                    continue;

                // Consider it meaningful progress if:
                // 1. Player has dated (speed or real) any bachelor
                // 2. Player has discovered any preferences
                if (
                    bachelorData.hasBeenSpeedDated
                    || bachelorData.hasCompletedRealDate
                    || (
                        bachelorData.discoveredLikes != null
                        && bachelorData.discoveredLikes.Count > 0
                    )
                    || (
                        bachelorData.discoveredDislikes != null
                        && bachelorData.discoveredDislikes.Count > 0
                    )
                )
                {
                    return true;
                }
            }
        }

        // Fallback: Check legacy lists for backward compatibility
        // (These should be empty in new saves but might exist in old saves)
        if (_currentSaveData.DatedBachelors != null && _currentSaveData.DatedBachelors.Count > 0)
        {
            // Check if there's actual data (not just empty strings)
            bool hasRealData = _currentSaveData.DatedBachelors.Exists(name =>
                !string.IsNullOrEmpty(name)
            );
            if (hasRealData)
                return true;
        }

        if (
            _currentSaveData.RealDatedBachelors != null
            && _currentSaveData.RealDatedBachelors.Count > 0
        )
        {
            // Check if there's actual data (not just empty strings)
            bool hasRealData = _currentSaveData.RealDatedBachelors.Exists(name =>
                !string.IsNullOrEmpty(name)
            );
            if (hasRealData)
                return true;
        }

        // No meaningful progress found
        return false;
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
    /// Shows the credits screen and hides all main menu buttons
    /// </summary>
    public void ShowCredits()
    {
        Debug.Log("Showing credits screen");

        // Hide all main menu buttons
        HideAllMainMenuButtons();

        // Hide main menu background image
        if (mainMenuBackgroundImage != null)
        {
            mainMenuBackgroundImage.SetActive(false);
            Debug.Log("Main menu background image hidden");
        }
        else
        {
            Debug.LogWarning("Main menu background image is not assigned!");
        }

        // Show credits screen
        if (creditsScreen != null)
        {
            creditsScreen.SetActive(true);
        }
        else
        {
            Debug.LogError("Credits screen is not assigned!");
        }

        // Show back button
        if (backButton != null)
        {
            backButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Back button is not assigned!");
        }
    }

    /// <summary>
    /// Hides the credits screen and returns to the main menu
    /// </summary>
    public void BackToMainMenu()
    {
        Debug.Log("Returning to main menu from credits");

        // Hide credits screen
        if (creditsScreen != null)
        {
            creditsScreen.SetActive(false);
        }

        // Hide back button
        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
        }

        // Show main menu background image
        if (mainMenuBackgroundImage != null)
        {
            mainMenuBackgroundImage.SetActive(true);
            Debug.Log("Main menu background image shown");
        }
        else
        {
            Debug.LogWarning("Main menu background image is not assigned!");
        }

        // Show appropriate main menu buttons based on save data
        UpdateUIBasedOnSaveFile();
    }

    /// <summary>
    /// Hides all main menu buttons
    /// </summary>
    private void HideAllMainMenuButtons()
    {
        if (goToGameButton != null)
            goToGameButton.gameObject.SetActive(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (newGameButton != null)
            newGameButton.gameObject.SetActive(false);
        if (quitButton != null)
            quitButton.gameObject.SetActive(false);
        if (creditsButton != null)
            creditsButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Initializes credits UI elements on start
    /// </summary>
    private void InitializeCreditsUI()
    {
        // Ensure credits screen starts hidden
        if (creditsScreen != null)
            creditsScreen.SetActive(false);

        // Ensure back button starts hidden
        if (backButton != null)
            backButton.gameObject.SetActive(false);

        // Ensure main menu background starts visible
        if (mainMenuBackgroundImage != null)
            mainMenuBackgroundImage.SetActive(true);
    }

    /// <summary>
    /// Check if we're currently showing the credits screen
    /// </summary>
    public bool IsShowingCredits()
    {
        return creditsScreen != null && creditsScreen.activeInHierarchy;
    }

    /// <summary>
    /// Handle keyboard input for credits navigation
    /// </summary>
    private void Update()
    {
        // ESC key to go back from credits to main menu
        if (Input.GetKeyDown(KeyCode.Escape) && IsShowingCredits())
        {
            BackToMainMenu();
        }
    }
}
