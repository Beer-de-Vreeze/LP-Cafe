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
        m_dialogueDisplay.StartDialogue(
            m_bachelor,
            m_dialogue,
            m_rooftopDateDialogue,
            m_aquariumDateDialogue,
            m_forestDateDialogue
        );
    }

    /// <summary>
    /// Resets the dating state, allowing the bachelor to be interacted with again.
    /// Called by DialogueDisplay when a date session ends.
    /// </summary>
    public void ResetDatingState()
    {
        currentlyDating = false;

        // Reset the bachelor's discovered preferences
        if (m_bachelor != null)
        {
            m_bachelor.ResetDiscoveries();
        }
        // Reset notebook entries while keeping the bachelor reference
        NoteBook notebook = FindFirstObjectByType<NoteBook>();
        if (notebook != null)
        {
            notebook.ResetNotebookEntries();
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
            // Show personalized message and only Come Back Later option
            m_dialogueDisplay.ShowPostRealDateOptionsInCafe(m_bachelor);
        }
        else
        {
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
    /// Simulate completing a real date for testing - accessible from component context menu
    /// </summary>
    [ContextMenu("Simulate Real Date Completion")]
    public void SimulateRealDateCompletion()
    {
        if (m_bachelor == null)
        {
            Debug.LogError(
                "[SetBachelor] No bachelor assigned to this SetBachelor component!",
                this
            );
            return;
        }

        string[] testLocations = { "Rooftop", "Aquarium", "Forest" };
        string randomLocation = testLocations[UnityEngine.Random.Range(0, testLocations.Length)];

        m_bachelor.MarkAsRealDated(randomLocation);

        // Set a random love level for testing
        if (m_bachelor._loveMeter != null)
        {
            int randomLove = UnityEngine.Random.Range(0, 6);
            m_bachelor._loveMeter._currentLove = randomLove;
            Debug.Log($"Set love level to {randomLove} for testing");
        }

        Debug.Log(
            $"[SetBachelor] Simulated real date completion at {randomLocation} for {m_bachelor._name}",
            this
        );

        // Test the real date message
        string message = m_bachelor.GetRealDateMessage();
        Debug.Log($"Real Date Message: {message}");
    }
}
