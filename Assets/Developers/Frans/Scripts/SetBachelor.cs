using DS;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetBachelor : MonoBehaviour
{
    [SerializeField]
    private NewBachelorSO m_bachelor;

    [SerializeField]
    private DSDialogue m_dialogue;

    [Header("Date Dialogues")]
    [SerializeField]
    private DSDialogue m_rooftopDateDialogue;

    [SerializeField]
    private DSDialogue m_aquariumDateDialogue;

    [SerializeField]
    private DSDialogue m_forestDateDialogue;

    [SerializeField]
    private Canvas m_dialogueCanvas;

    [SerializeField]
    private DialogueDisplay m_dialogueDisplay;

    [SerializeField]
    private bool currentlyDating = false;

    [SerializeField]
    private GameObject m_dialogueObject;

    private GameObject[] otherBachelors;

    void Start()
    {
        m_dialogueDisplay = FindFirstObjectByType<DialogueDisplay>();
        m_dialogueCanvas.enabled = false;
    }

    private void OnMouseDown()
    {
        // Check if bachelors are currently clickable (to handle barista events)
        if (m_dialogueDisplay != null && !m_dialogueDisplay.AreBachelorsClickable())
        {
            Debug.Log(
                $"[SetBachelor] Bachelor {m_bachelor?._name ?? "Unknown"} not clickable right now"
            );
            return;
        }

        if (!currentlyDating)
        {
            m_dialogueCanvas.enabled = true;
            m_dialogueObject.SetActive(true);

            // Check if this bachelor has already been dated
            if (HasBeenDated())
            {
                ShowPostDateOptions();
            }
            else
            {
                SetBatchelor();
            }
        }
    }

    public void SetBatchelor()
    {
        currentlyDating = true;
        m_dialogueCanvas.enabled = true;
        if (SceneManager.GetActiveScene().name != "FirstDate")
            m_dialogueObject.SetActive(true);
        if (SceneManager.GetActiveScene().name != "FirstDate")
        {
            // Disable all other bachelors
            otherBachelors = GameObject.FindGameObjectsWithTag("Bachelor");
            foreach (GameObject bachelorObj in otherBachelors)
            {
                if (bachelorObj != this.gameObject)
                {
                    bachelorObj.SetActive(false);
                }
            }
        }

        // Ensure preferences are synchronized with save data before starting dialogue
        SynchronizePreferencesWithSaveData();

        m_dialogueDisplay.StartDialogue(
            m_bachelor,
            m_dialogue,
            m_rooftopDateDialogue,
            m_aquariumDateDialogue,
            m_forestDateDialogue
        );
    }

    /// <summary>
    /// Call this when the dialogue is finished to mark the bachelor as dated and save the progress.
    /// This is called by DialogueDisplay when a speed date completes successfully.
    /// </summary>
    public void CompleteSpeedDateAndSave()
    {
        if (m_bachelor != null)
        {
            // Mark the bachelor as speed dated (this will save the changes)
            m_bachelor.MarkAsDated();
            Debug.Log(
                $"[SetBachelor] Completed speed dating with {m_bachelor._name} and saved progress"
            );

            // Force one more synchronization to ensure UI and save data are in sync
            SynchronizePreferencesWithSaveData();
        }
    }

    /// <summary>
    /// Call this when a real date is completed to mark the bachelor as real dated and save the progress.
    /// This should be called by DialogueDisplay when a real date session completes successfully.
    /// </summary>
    public void CompleteRealDateAndSave(string location)
    {
        if (m_bachelor != null && !string.IsNullOrEmpty(location))
        {
            // Mark the bachelor as real dated (this will save the changes)
            m_bachelor.MarkAsRealDated(location);
            Debug.Log(
                $"[SetBachelor] Completed real date with {m_bachelor._name} at {location} and saved progress"
            );

            // Force one more synchronization to ensure UI and save data are in sync
            SynchronizePreferencesWithSaveData();
        }
        else
        {
            Debug.LogWarning(
                "[SetBachelor] Cannot complete real date: bachelor or location missing"
            );
        }
    }

    /// <summary>
    /// Synchronizes the bachelor's preferences between the NoteBook UI and the save data
    /// </summary>
    private void SynchronizePreferencesWithSaveData()
    {
        if (m_bachelor == null)
        {
            Debug.LogError(
                "[SetBachelor] Cannot synchronize preferences: No bachelor assigned",
                this
            );
            return;
        }

        if (string.IsNullOrEmpty(m_bachelor._name))
        {
            Debug.LogError(
                "[SetBachelor] Cannot synchronize preferences: Bachelor has empty name",
                this
            );
            return;
        }

        // Load bachelor data from save system
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
        {
            Debug.Log("[SetBachelor] No save data found, creating new save data");
            saveData = new SaveData();
        }

        // Get or create this bachelor's data in the save system
        BachelorPreferencesData prefData = saveData.GetOrCreateBachelorData(m_bachelor._name);
        if (prefData == null)
        {
            Debug.LogError(
                $"[SetBachelor] Failed to get/create bachelor data for {m_bachelor._name}"
            );
            return;
        }

        // Find the notebook to ensure the UI stays in sync
        NoteBook notebook = FindFirstObjectByType<NoteBook>();
        if (notebook == null)
        {
            Debug.LogWarning("[SetBachelor] No NoteBook found in scene");
            // Continue anyway - we can still sync the bachelor data
        }

        // Ensure the notebook is connected to this bachelor
        if (notebook != null)
        {
            notebook.EnsureBachelorConnection(m_bachelor);
        }

        Debug.Log($"[SetBachelor] Synchronizing preferences for {m_bachelor._name}");
        // Track the dating flags before synchronization
        bool wasSpeedDated = m_bachelor._HasBeenSpeedDated;
        bool wasRealDated = m_bachelor._HasCompletedRealDate;

        // Log the state before synchronization
        Debug.Log(
            $"[SetBachelor] Before sync - SpeedDated: {wasSpeedDated}, RealDated: {wasRealDated}"
        );

        // Force a load from save data to local memory
        m_bachelor.SynchronizeWithSaveData();

        // After synchronization, if there are any discovered preferences that aren't
        // saved yet, make sure they get saved
        SaveDiscoveredPreferences();

        // Check if the flags changed during synchronization
        bool flagsChanged =
            (wasSpeedDated != m_bachelor._HasBeenSpeedDated)
            || (wasRealDated != m_bachelor._HasCompletedRealDate);

        // If flags changed during synchronization or they are true but not in save data,
        // make sure they get saved back
        if (flagsChanged || m_bachelor._HasBeenSpeedDated || m_bachelor._HasCompletedRealDate)
        {
            m_bachelor.SaveCurrentDatingState();
            Debug.Log($"[SetBachelor] Dating flags resaved after synchronization");
        }

        // Log the final state after synchronization
        Debug.Log(
            $"[SetBachelor] After sync - SpeedDated: {m_bachelor._HasBeenSpeedDated}, RealDated: {m_bachelor._HasCompletedRealDate}"
        );

        Debug.Log($"[SetBachelor] Preferences synchronized for {m_bachelor._name}");
    }

    /// <summary>
    /// Saves any discovered preferences to the save system
    /// </summary>
    private void SaveDiscoveredPreferences()
    {
        if (m_bachelor == null || string.IsNullOrEmpty(m_bachelor._name))
        {
            return;
        }

        // Use the bachelor's own method to save preferences
        m_bachelor.SaveDiscoveredPreferences();
    }

    /// <summary>
    /// Resets the dating state, allowing the bachelor to be interacted with again.
    /// Called by DialogueDisplay when a date session ends.
    /// </summary>
    public void ResetDatingState()
    {
        currentlyDating = false;

        // DO NOT reset the bachelor's discovered preferences - they should persist

        // Reset notebook entries UI while keeping the bachelor reference
        NoteBook notebook = FindFirstObjectByType<NoteBook>();
        if (notebook != null)
        {
            // This only resets UI elements, not the actual preference data
            notebook.ResetNotebookEntries();
        }

        // Ensure the bachelor's preferences are loaded from save data
        if (m_bachelor != null)
        {
            m_bachelor.SynchronizeWithSaveData();
        }

        // Re-enable all other bachelors
        if (otherBachelors != null)
        {
            foreach (GameObject bachelorObj in otherBachelors)
            {
                if (bachelorObj != this.gameObject)
                {
                    bachelorObj.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Call this when the date is finished to save and turn off the bachelor.
    /// </summary>
    public void FinishDateAndSave()
    {
        // Mark the bachelor as dated using the ScriptableObject method (handles all saving internally)
        if (m_bachelor != null)
        {
            m_bachelor.MarkAsDated();
            Debug.Log($"Finished dating {m_bachelor._name} and saved progress");
        }

        // Turn off this bachelor
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Checks if this bachelor has already been dated by using the bachelor's own method
    /// </summary>
    /// <returns>True if the bachelor has been dated</returns>
    private bool HasBeenDated()
    {
        // Use the bachelor's own method which handles both local flags and save data consistency
        if (m_bachelor != null)
        {
            return m_bachelor.HasBeenDated();
        }
        return false;
    }

    /// <summary>
    /// Shows the post-date options (Come Back Later and Ask on a Date) without starting full dialogue
    /// </summary>
    private void ShowPostDateOptions()
    {
        currentlyDating = true;

        // Disable all other bachelors
        otherBachelors = GameObject.FindGameObjectsWithTag("Bachelor");
        foreach (GameObject bachelorObj in otherBachelors)
        {
            if (bachelorObj != this.gameObject)
            {
                bachelorObj.SetActive(false);
            }
        }

        // Check if this bachelor has completed a real date
        if (HasCompletedRealDate())
        {
            // Ensure preferences and love meter data are synchronized before showing the message
            SynchronizePreferencesWithSaveData();

            // Show personalized message and only Come Back Later option
            m_dialogueDisplay.ShowPostRealDateOptionsInCafe(m_bachelor);
        }
        else
        {
            // Ensure preferences are synchronized for post-speed-date options too
            SynchronizePreferencesWithSaveData();

            // Show standard post-speed-date options (Come Back Later and Ask on a Date)
            m_dialogueDisplay.ShowPostDateOptions(m_bachelor);
        }
    }

    /// <summary>
    /// Checks if this bachelor has completed a real date (not just a speed date)
    /// </summary>
    /// <returns>True if the bachelor has completed a real date</returns>
    private bool HasCompletedRealDate()
    {
        // Use the bachelor's own method which handles both local flags and save data consistency
        if (m_bachelor != null)
        {
            return m_bachelor.HasCompletedRealDate();
        }
        return false;
    }

    /// <summary>
    /// Gets the bachelor data for this SetBachelor component
    /// </summary>
    /// <returns>The NewBachelorSO data</returns>
    public NewBachelorSO GetBachelor()
    {
        return m_bachelor;
    }

    // ======= CONTEXT MENU METHODS FOR DEBUGGING AND TESTING =======

    /// <summary>
    /// Debug the current state of this bachelor - accessible from component context menu
    /// </summary>
    [ContextMenu("Debug Bachelor State")]
    public void DebugBachelorState()
    {
        if (m_bachelor == null)
        {
            Debug.LogError(
                "[SetBachelor] No bachelor assigned to this SetBachelor component!",
                this
            );
            return;
        }

        Debug.Log($"=== Debug State for SetBachelor component with {m_bachelor._name} ===", this);
        Debug.Log($"Local Flags:");
        Debug.Log($"  _HasBeenSpeedDated: {m_bachelor._HasBeenSpeedDated}");
        Debug.Log($"  _HasCompletedRealDate: {m_bachelor._HasCompletedRealDate}");
        Debug.Log($"  _LastRealDateLocation: '{m_bachelor._LastRealDateLocation}'");

        Debug.Log($"Method Results:");
        Debug.Log($"  HasBeenDated(): {m_bachelor.HasBeenDated()}");
        Debug.Log($"  HasCompletedRealDate(): {m_bachelor.HasCompletedRealDate()}");

        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null)
        {
            Debug.Log($"Save Data:");
            bool inDatedList =
                saveData.DatedBachelors != null
                && saveData.DatedBachelors.Contains(m_bachelor._name);
            bool inRealDatedList =
                saveData.RealDatedBachelors != null
                && saveData.RealDatedBachelors.Contains(m_bachelor._name);
            Debug.Log($"  In DatedBachelors: {inDatedList}");
            Debug.Log($"  In RealDatedBachelors: {inRealDatedList}");
        }
        else
        {
            Debug.Log($"Save Data: No save data found");
        }

        if (m_bachelor._loveMeter != null)
        {
            Debug.Log($"Love Meter: {m_bachelor._loveMeter.GetCurrentLove()}");
        }
        else
        {
            Debug.Log($"Love Meter: Not assigned");
        }

        Debug.Log("=== End Debug State ===");
    }

    /// <summary>
    /// Test the complete dating flow for this bachelor - accessible from component context menu
    /// </summary>
    [ContextMenu("Test Complete Dating Flow")]
    public void TestCompleteDatingFlow()
    {
        if (m_bachelor == null)
        {
            Debug.LogError(
                "[SetBachelor] No bachelor assigned to this SetBachelor component!",
                this
            );
            return;
        }

        Debug.Log(
            $"=== Testing Complete Dating Flow for {m_bachelor._name} via SetBachelor ===",
            this
        );

        // Step 1: Initial state check
        Debug.Log($"Step 1 - Initial State:");
        DebugBachelorState();

        // Step 2: Simulate speed dating
        Debug.Log($"\nStep 2 - Simulating Speed Date:");
        m_bachelor.MarkAsDated();
        Debug.Log($"After MarkAsDated() - HasBeenDated(): {m_bachelor.HasBeenDated()}");

        // Step 3: Simulate real dating
        Debug.Log($"\nStep 3 - Simulating Real Date:");
        string testLocation = "Rooftop";
        m_bachelor.MarkAsRealDated(testLocation);
        Debug.Log(
            $"After MarkAsRealDated('{testLocation}') - HasCompletedRealDate(): {m_bachelor.HasCompletedRealDate()}"
        );

        // Step 4: Final state check
        Debug.Log($"\nStep 4 - Final State Check:");
        DebugBachelorState();

        Debug.Log($"=== End Complete Dating Flow Test ===");
    }

    /// <summary>
    /// Test the normal dating flow without using MarkAsDated directly - accessible from component context menu
    /// </summary>
    [ContextMenu("Test Normal Dating Flow")]
    public void TestNormalDatingFlow()
    {
        if (m_bachelor == null)
        {
            Debug.LogError(
                "[SetBachelor] No bachelor assigned to this SetBachelor component!",
                this
            );
            return;
        }

        if (m_dialogueDisplay == null)
        {
            Debug.LogError("[SetBachelor] No DialogueDisplay found!", this);
            return;
        }

        Debug.Log($"=== Testing Normal Dating Flow for {m_bachelor._name} ===", this);

        // Step 1: Check initial state
        Debug.Log($"Step 1 - Initial State:");
        Debug.Log($"  Bachelor name: {m_bachelor._name}");
        Debug.Log($"  HasBeenDated(): {m_bachelor.HasBeenDated()}");
        Debug.Log($"  HasCompletedRealDate(): {m_bachelor.HasCompletedRealDate()}");

        // Step 2: Simulate normal dating dialogue trigger
        Debug.Log($"Step 2 - Starting dialogue (normal flow):");
        SetBatchelor(); // This calls the normal StartDialogue flow

        // Step 3: Check if DialogueDisplay has the bachelor set
        Debug.Log($"Step 3 - Checking DialogueDisplay state:");
        if (m_dialogueDisplay != null)
        {
            // We can't access private fields, but we can check if the dialogue started
            Debug.Log("Dialogue started through normal flow");
        }

        Debug.Log(
            $"=== Normal Dating Flow Test Started (check console for DialogueDisplay logs) ==="
        );
    }

    /// <summary>
    /// Validate that this bachelor's name is properly set - accessible from component context menu
    /// </summary>
    [ContextMenu("Validate Bachelor Name")]
    public void ValidateBachelorName()
    {
        if (m_bachelor == null)
        {
            Debug.LogError(
                "[SetBachelor] No bachelor assigned to this SetBachelor component!",
                this
            );
            return;
        }

        if (string.IsNullOrEmpty(m_bachelor._name))
        {
            Debug.LogError(
                $"[SetBachelor] ❌ Bachelor '{m_bachelor.name}' has an empty _name field! This will cause save issues.",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[SetBachelor] ✅ Bachelor '{m_bachelor._name}' has a proper name set.",
                this
            );
        }
    }

    /// <summary>
    /// Force synchronization with save data - accessible from component context menu
    /// </summary>
    [ContextMenu("Force Sync With Save Data")]
    public void ForceSyncWithSaveData()
    {
        if (m_bachelor == null)
        {
            Debug.LogError(
                "[SetBachelor] No bachelor assigned to this SetBachelor component!",
                this
            );
            return;
        }

        Debug.Log($"=== Force Sync Test for {m_bachelor._name} via SetBachelor ===", this);
        Debug.Log(
            $"Before sync - SpeedDated: {m_bachelor._HasBeenSpeedDated}, RealDated: {m_bachelor._HasCompletedRealDate}"
        );

        // Call the bachelor's private sync method through a public wrapper
        m_bachelor.ForceSyncWithSaveData();

        Debug.Log(
            $"After sync - SpeedDated: {m_bachelor._HasBeenSpeedDated}, RealDated: {m_bachelor._HasCompletedRealDate}"
        );
        Debug.Log($"HasBeenDated(): {m_bachelor.HasBeenDated()}");
        Debug.Log($"HasCompletedRealDate(): {m_bachelor.HasCompletedRealDate()}");
        Debug.Log("=== End Sync Test ===");
    }

    /// <summary>
    /// Clean up empty strings from save data - accessible from component context menu
    /// </summary>
    [ContextMenu("Clean Save Data")]
    public void CleanSaveData()
    {
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
        {
            Debug.Log("[SetBachelor] No save data found to clean", this);
            return;
        }

        int removedDatedCount = 0;
        int removedRealDatedCount = 0;

        // Remove empty strings from DatedBachelors
        if (saveData.DatedBachelors != null)
        {
            var originalCount = saveData.DatedBachelors.Count;
            saveData.DatedBachelors.RemoveAll(name => string.IsNullOrEmpty(name));
            removedDatedCount = originalCount - saveData.DatedBachelors.Count;
        }

        // Remove empty strings from RealDatedBachelors
        if (saveData.RealDatedBachelors != null)
        {
            var originalCount = saveData.RealDatedBachelors.Count;
            saveData.RealDatedBachelors.RemoveAll(name => string.IsNullOrEmpty(name));
            removedRealDatedCount = originalCount - saveData.RealDatedBachelors.Count;
        }

        if (removedDatedCount > 0 || removedRealDatedCount > 0)
        {
            SaveSystem.SerializeData(saveData);
            Debug.Log(
                $"[SetBachelor] Cleaned save data: removed {removedDatedCount} empty entries from DatedBachelors and {removedRealDatedCount} empty entries from RealDatedBachelors",
                this
            );
        }
        else
        {
            Debug.Log("[SetBachelor] Save data is clean - no empty entries found", this);
        }
    }

    /// <summary>
    /// Reset save data completely for testing - accessible from component context menu
    /// </summary>
    [ContextMenu("Reset Save Data")]
    public void ResetSaveData()
    {
        SaveData freshData = new SaveData();
        SaveSystem.SerializeData(freshData);
        Debug.Log("[SetBachelor] Save data has been completely reset", this);

        // Also reset this bachelor's local flags if assigned
        if (m_bachelor != null)
        {
            m_bachelor._HasBeenSpeedDated = false;
            m_bachelor._HasCompletedRealDate = false;
            m_bachelor._LastRealDateLocation = "";

            Debug.Log($"[SetBachelor] Local flags reset for {m_bachelor._name}", this);
        }
    }

    /// <summary>
    /// Test love-dependent post-real-date messages - accessible from component context menu
    /// </summary>
    [ContextMenu("Test Love-Dependent Messages")]
    public void TestLoveDependentMessages()
    {
        if (m_bachelor == null)
        {
            Debug.LogError(
                "[SetBachelor] No bachelor assigned to this SetBachelor component!",
                this
            );
            return;
        }

        if (m_bachelor._loveMeter == null)
        {
            Debug.LogError($"[SetBachelor] No love meter assigned to {m_bachelor._name}!", this);
            return;
        }

        Debug.Log($"=== Testing Love-Dependent Messages for {m_bachelor._name} ===", this);

        // Save original state
        bool originalRealDated = m_bachelor._HasCompletedRealDate;
        string originalLocation = m_bachelor._LastRealDateLocation;
        int originalLove = m_bachelor._loveMeter.GetCurrentLove();

        // Set up for testing - mark as real dated
        m_bachelor._HasCompletedRealDate = true;
        m_bachelor._LastRealDateLocation = "Rooftop";

        // Test different love levels
        int[] testLoveLevels = { 0, 1, 2, 3, 4, 5 };

        foreach (int loveLevel in testLoveLevels)
        {
            // Set the love level using the CurrentLove property
            m_bachelor._loveMeter.CurrentLove = loveLevel;

            // Get the message
            string message = m_bachelor.GetRealDateMessage();

            Debug.Log($"Love Level {loveLevel}: \"{message}\"");
        }

        // Restore original state
        m_bachelor._HasCompletedRealDate = originalRealDated;
        m_bachelor._LastRealDateLocation = originalLocation;
        m_bachelor._loveMeter.CurrentLove = originalLove;

        Debug.Log($"=== End Love-Dependent Message Test ===");
    }
}
