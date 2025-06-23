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
            SetBatchelor();
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
        m_dialogueDisplay.StartDialogue(m_bachelor, m_dialogue);
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
}
