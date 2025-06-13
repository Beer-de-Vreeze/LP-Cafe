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
        m_canvas.enabled = false;
    }

    public void OnClick(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        Debug.Log(data);
        SetBatchelor();
    }    
    
    
    public void OnMouseDown(/*BaseEventData data*/)
    {
        /*        PointerEventData pData = (PointerEventData)data;
                Debug.Log(data);*/
        Debug.Log("m_dialogueDisplay");
        SetBatchelor();
    }

    public void SetBatchelor()
    {
        m_canvas.enabled = true;
        m_dialogueDisplay.StartDialogue(m_bachelor);
    }
}
