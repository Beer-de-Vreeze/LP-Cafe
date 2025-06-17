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

// Start is called once before the first execution of Update after the MonoBehaviour is created
void Start()
{
    m_dialogueDisplay = FindFirstObjectByType<DialogueDisplay>();
    m_dialogueCanvas.enabled = false;
}

private void OnMouseDown()
{
    SetBatchelor();
}

public void SetBatchelor()
{
    m_dialogueCanvas.enabled = true;
    m_dialogueDisplay.StartDialogue(m_bachelor, m_dialogue);
}
}
