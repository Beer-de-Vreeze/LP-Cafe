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

        // Load save data and check if this bachelor has been dated
        SaveData data = SaveSystem.Deserialize();
        if (data != null && data.DatedBachelors.Contains(m_bachelor.name))
        {
            gameObject.SetActive(false);
        }
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
        // Mark the bachelor as dated in the ScriptableObject
        if (m_bachelor != null)
        {
            m_bachelor.MarkAsDated();
        }

        // Save this bachelor as dated
        SaveData data = SaveSystem.Deserialize();
        if (data == null)
        {
            data = new SaveData();
        }
        if (!data.DatedBachelors.Contains(m_bachelor.name))
        {
            data.DatedBachelors.Add(m_bachelor.name);
            SaveSystem.SerializeData(data);
        }

        // Turn off this bachelor
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Checks if this bachelor has already been dated by checking both the bachelor's internal state
    /// and the save system data.
    /// </summary>
    /// <returns>True if the bachelor has been dated</returns>
    private bool HasBeenDated()
    {
        // Check the bachelor's internal state
        if (m_bachelor != null && m_bachelor.HasBeenDated())
        {
            return true;
        }

        // Also check the save system
        SaveData data = SaveSystem.Deserialize();
        if (
            data != null
            && data.DatedBachelors != null
            && data.DatedBachelors.Contains(m_bachelor.name)
        )
        {
            return true;
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
        SaveData data = SaveSystem.Deserialize();
        if (data == null || data.RealDatedBachelors == null)
            return false;

        return data.RealDatedBachelors.Contains(m_bachelor.name);
    }
}
