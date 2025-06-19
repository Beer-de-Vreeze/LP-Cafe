using UnityEngine;
using UnityEngine.EventSystems;

public class BachelorSetter : MonoBehaviour
{
    [SerializeField]
    private NewBachelorSO m_bachelor;

    [SerializeField]
    private DialogueDisplay m_dialogueDisplay;

    [SerializeField]
    private Canvas m_canvas;

    void Start()
    {
        m_dialogueDisplay = FindFirstObjectByType<DialogueDisplay>();
        // Check if this bachelor has already been dated using the save system
        var saveData = SaveSystem.Deserialize();
        if (
            saveData != null
            && saveData.DatedBachelors != null
            && saveData.DatedBachelors.Contains(m_bachelor.name)
        )
        {
            DisableSetter();
        }
        else
        {
            m_canvas.enabled = false;
        }
    }

    public void OnClick(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        Debug.Log(data);
        SetBatchelor();
    }

    public void OnMouseDown( /*BaseEventData data*/
    )
    {
        /*        PointerEventData pData = (PointerEventData)data;
                Debug.Log(data);*/
        Debug.Log("m_dialogueDisplay");
        SetBatchelor();
    }

    public void SetBatchelor()
    {
        m_canvas.enabled = true;
        m_dialogueDisplay.StartDialogue(m_bachelor, m_bachelor._dialogue);
    }

    public NewBachelorSO GetBachelor()
    {
        return m_bachelor;
    }

    public void DisableSetter()
    {
        if (m_canvas != null)
            m_canvas.enabled = false;
        // Optionally disable the whole GameObject or add more logic
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.interactable = false;
        this.enabled = false;
    }
}
