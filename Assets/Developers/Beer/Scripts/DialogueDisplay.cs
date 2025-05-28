using System.Collections.Generic;
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

    [SerializeField]
    private Transform _choicesParent;

    [SerializeField]
    private GameObject _choiceButtonPrefab;

    private AudioSource _audioSource;

    private bool _canAdvance = false;

    private List<GameObject> _activeChoiceButtons = new List<GameObject>();

    private void Start()
    {
        if (_typewriter != null)
        {
            _typewriter.onTextShowed.RemoveListener(OnTypewriterEnd);
            _typewriter.onTextShowed.AddListener(OnTypewriterEnd);
        }
        SetDialogue(_dialogue, _bachelor);
    }

    private void Update()
    {
        if (
            _canAdvance
            && _activeChoiceButtons.Count == 0
            && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        )
        {
            _canAdvance = false;
            NextDialogue();
        }
    }

    public void ShowDialogue()
    {
        ClearChoices();

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

            if (_typewriter != null)
            {
                _typewriter.ShowText(_displayText.text);
            }

            var choices = _dialogue.m_dialogue.m_dialogueChoiceData;
            if (choices != null && choices.Count > 1)
            {
                ShowChoices(choices);
                _canAdvance = false;
            }
        }
    }

    private void OnTypewriterEnd()
    {
        _canAdvance = true;
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

    private void ShowChoices(List<DS.Data.DSDialogueChoiceData> choices)
    {
        foreach (var choice in choices)
        {
            var btnObj = Instantiate(_choiceButtonPrefab, _choicesParent);
            var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = choice.m_dialogueChoiceText;

            var button = btnObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    OnChoiceSelected(choice);
                });
            }
            _activeChoiceButtons.Add(btnObj);
        }
    }

    private void ClearChoices()
    {
        foreach (var btn in _activeChoiceButtons)
        {
            Destroy(btn);
        }
        _activeChoiceButtons.Clear();
    }

    private void OnChoiceSelected(DS.Data.DSDialogueChoiceData choice)
    {
        ClearChoices();
        if (choice.m_nextDialogue != null)
        {
            _dialogue.m_dialogue = choice.m_nextDialogue;
            ShowDialogue();
        }
        else
        {
            Debug.Log("No next dialogue found.");
        }
    }
}
