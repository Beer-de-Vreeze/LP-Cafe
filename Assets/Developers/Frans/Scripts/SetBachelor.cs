using DS;
using Unity.VisualScripting;
using UnityEngine;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
            StartCoroutine(WaitForCanvasAndSetBatchelor());
        }
    }

    private System.Collections.IEnumerator WaitForCanvasAndSetBatchelor()
    {
        // Wait until the canvas is enabled and dialogue display is ready
        yield return new WaitUntil(() => m_dialogueCanvas.enabled && m_dialogueDisplay != null);

        // Wait an additional frame for UI components to properly initialize
        yield return new WaitForEndOfFrame();

        // Wait one more frame to ensure TextMeshPro components are fully initialized
        yield return new WaitForEndOfFrame();

        SetBatchelor();
    }

    public void SetBatchelor()
    {
        currentlyDating = true;
        m_dialogueCanvas.enabled = true;

        // Ensure notebook has the correct bachelor reference
        NoteBook notebook = FindFirstObjectByType<NoteBook>();
        if (notebook != null && m_bachelor != null)
        {
            notebook.SetBachelor(m_bachelor);
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
        } // Reset notebook entries while keeping the bachelor reference
        NoteBook notebook = FindFirstObjectByType<NoteBook>();
        if (notebook != null)
        {
            notebook.ResetNotebookEntries();
        }
    }
}
