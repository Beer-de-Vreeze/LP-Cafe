using UnityEngine;

public class UIMananager : Singleton<UIMananager>
{
    [SerializeField]
    private GameObject _dialogueBox;

    void Start()
    {
        OpenCloseDialogueBox();
    }

    public void OpenCloseDialogueBox()
    {
        if (_dialogueBox.activeSelf)
        {
            _dialogueBox.SetActive(false);
        }
        else
        {
            _dialogueBox.SetActive(true);
        }
    }
}
