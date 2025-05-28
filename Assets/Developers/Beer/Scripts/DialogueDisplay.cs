using System.Collections.Generic;
using DS;
using Febucci.UI;
using TMPro;
using UnityEngine;

public class DialogueDisplay : MonoBehaviour
{
    // Reference to the UI element displaying the character's name
    [SerializeField]
    private TextMeshProUGUI _nameText;

    // Reference to the typewriter effect component
    [SerializeField]
    private TypewriterByCharacter _typewriter;

    // Reference to the UI element displaying the dialogue text
    [SerializeField]
    private TextMeshProUGUI _displayText;

    // The current dialogue data
    [SerializeField]
    private DSDialogue _dialogue;

    // The current bachelor (character) data
    [SerializeField]
    private NewBachelorSO _bachelor;

    // Parent transform for choice buttons
    [SerializeField]
    private Transform _choicesParent;

    // Prefab for choice buttons
    [SerializeField]
    private GameObject _choiceButtonPrefab;

    // Audio source for playing feedback (now used for dialogue audio)
    [SerializeField]
    private AudioSource _audioSource;

    // Flag to determine if the player can advance the dialogue
    private bool _canAdvance = false;

    // List to keep track of active choice button GameObjects
    private List<GameObject> _activeChoiceButtons = new List<GameObject>();

    // Called when the script instance is being loaded
    private void Start()
    {
        // Ensure typewriter events are set up correctly
        if (_typewriter != null)
        {
            _typewriter.onTextShowed.RemoveListener(OnTypewriterEnd);
            _typewriter.onTextShowed.AddListener(OnTypewriterEnd);
        }
        SetDialogue(_dialogue, _bachelor);
    }

    // Called once per frame
    private void Update()
    {
        // Allow advancing dialogue if possible and no choices are being shown
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

    // Displays the current dialogue and sets up choices if available
    public void ShowDialogue()
    {
        ClearChoices();

        // Set the character's name
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
            if (_nameText.text == null)
            {
                _nameText.text = "Unknown";
            }
        }

        // Set the dialogue text and show choices if present
        if (_dialogue != null && _displayText != null && _dialogue.m_dialogue != null)
        {
            _displayText.text = _dialogue.m_dialogue.m_dialogueTextData;
            Debug.Log("Displaying dialogue: " + _displayText.text);

            // Play dialogue audio if available
            if (_audioSource != null)
            {
                var audioClip = _dialogue.m_dialogue.m_dialogueAudioData;
                if (audioClip != null)
                {
                    _audioSource.Stop();
                    _audioSource.clip = audioClip;
                    _audioSource.Play();
                    Debug.Log("Playing audio: " + audioClip.name);
                }
                else
                {
                    _audioSource.Stop();
                    _audioSource.clip = null;
                }
            }

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

    // Called when the typewriter effect finishes displaying text
    private void OnTypewriterEnd()
    {
        _canAdvance = true;
    }

    // Sets the current dialogue and bachelor, then displays the dialogue
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

    // Advances to the next dialogue if available
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

    // Instantiates choice buttons for each available choice
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

    // Destroys all active choice buttons
    private void ClearChoices()
    {
        foreach (var btn in _activeChoiceButtons)
        {
            Destroy(btn);
        }
        _activeChoiceButtons.Clear();
    }

    // Handles logic when a choice is selected
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
