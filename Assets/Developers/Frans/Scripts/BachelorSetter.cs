using UnityEngine;

public class BachelorSetter : MonoBehaviour
{
    [SerializeField]
    private NewBachelorSO m_bachelor;

    private DialogueDisplay m_dialogueDisplay;

    void Start()
    {
        m_dialogueDisplay = FindFirstObjectByType<DialogueDisplay>();
    }

    public void SetBatchelor()
    {
        m_dialogueDisplay.StartDialogue(m_bachelor);
    }
}
