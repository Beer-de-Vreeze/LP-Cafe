using DS;
using Febucci.UI;
using MoreMountains.Feedbacks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class DialogueDisplay : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _nameText;

    [SerializeField]
    private TypewriterByCharacter _typewriter;

    [SerializeField]
    private TextMeshProUGUI _displayText;

    [SerializeField]
    private DSDialogue _dialogue;

    [SerializeField]
    private NewBachelorSO _bachelor;

    private AudioSource _audioSource;

    private void Start()
    {
        if (_typewriter != null)
        {
            _typewriter.onTextShowed.RemoveListener(OnTypewriterEnd);
            _typewriter.onTextShowed.AddListener(OnTypewriterEnd);
        }
        SetDialogue(_dialogue, _bachelor);
    }

    public void ShowDialogue()
    {
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
            if (_nameText.text == null)
            {
                _nameText.text = "Unknown";
            }
        }

        if (_dialogue != null && _displayText != null && _dialogue.m_dialogue != null)
        {
            _displayText.text = _dialogue.m_dialogue.m_dialogueTextData;
            Debug.Log("Displaying dialogue: " + _displayText.text);

            // Start typewriter effect
            if (_typewriter != null)
            {
                _typewriter.ShowText(_displayText.text);
            }
        }
    }

    // Called when typewriter finishes
    private void OnTypewriterEnd()
    {
        var choices = _dialogue?.m_dialogue?.m_dialogueChoiceData;
        if (choices != null && choices.Count > 0 && choices[0].m_nextDialogue != null)
        {
            StartCoroutine(AutoAdvanceDialogue());
        }
    }

    private System.Collections.IEnumerator AutoAdvanceDialogue()
    {
        yield return new WaitForSeconds(1.0f);
        NextDialogue();
    }

    public void SetDialogue(DSDialogue dialogue, NewBachelorSO bachelor)
    {
        _dialogue = dialogue;
        _bachelor = bachelor;
        if (bachelor == null)
        {
            _bachelor = NewBachelorSO.CreateInstance<NewBachelorSO>();
            _bachelor._name = "Unknown";
        }
        ShowDialogue();
    }

    public void NextDialogue()
    {
        if (_dialogue != null && _dialogue.m_dialogue != null)
        {
            var choices = _dialogue.m_dialogue.m_dialogueChoiceData;
            if (choices != null && choices.Count > 0 && choices[0].m_nextDialogue != null)
            {
                _dialogue.m_dialogue = choices[0].m_nextDialogue;
                ShowDialogue();
            }
            else
            {
                Debug.Log("No next dialogue found.");
            }
        }
    }

    
}
