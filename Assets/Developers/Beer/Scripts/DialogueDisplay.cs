using System.Collections.Generic;
using DS;
using DS.ScriptableObjects;
using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    // Reference to the UI Image displaying the character's portrait
    [SerializeField]
    private Image _bachelorImage;

    // New fields for state tracking
    private bool _canAdvance = false;
    private List<GameObject> _activeChoiceButtons = new List<GameObject>();

    // Dictionary to store gameplay variables
    private Dictionary<string, string> _gameVariables = new Dictionary<string, string>();

    // Love score value
    [SerializeField]
    private int _loveScore = 0;

    // Reference to love meter scriptable object (if needed)
    [SerializeField]
    private LoveMeterSO _loveMeter;

    // Called when the script instance is being loaded
    private void Start()
    {
        // Initialize variables with default values
        InitializeGameVariables();

        // Ensure typewriter events are set up correctly
        if (_typewriter != null)
        {
            _typewriter.onTextShowed.RemoveListener(OnTypewriterEnd);
            _typewriter.onTextShowed.AddListener(OnTypewriterEnd);
        }
        SetDialogue(_dialogue, _bachelor);
    }

    // Initialize game variables with default values
    private void InitializeGameVariables()
    {
        // Default values for variables used in conditions
        _gameVariables["Love"] = _loveScore.ToString();
        _gameVariables["LikeDiscovered"] = "false";
        _gameVariables["DislikeDiscovered"] = "false";
        _gameVariables["NotebookLikeEntry"] = "false";
        _gameVariables["NotebookDislikeEntry"] = "false";
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

        // If this is a condition node, evaluate it and follow the appropriate path
        if (IsConditionNode(_dialogue))
        {
            EvaluateConditionNode();
            return; // After following condition path, the new dialogue will call ShowDialogue again
        }

        // If this is a setter node, apply the changes and move to the next dialogue
        if (IsSetterNode(_dialogue))
        {
            ApplySetterNode();
            return; // After applying setter, the next dialogue will call ShowDialogue again
        }

        // Set the character's name
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
            if (_nameText.text == null)
            {
                _nameText.text = "ERROR";
            }
        }

        // Set the character's image
        if (_dialogue != null && _dialogue.m_dialogue != null && _bachelorImage != null)
        {
            _bachelorImage.sprite = _dialogue.m_dialogue.m_bachelorImageData;
            _bachelorImage.color = new Color(
                _bachelorImage.color.r,
                _bachelorImage.color.g,
                _bachelorImage.color.b,
                1f
            );
            _bachelorImage.enabled = _dialogue.m_dialogue.m_bachelorImageData != null;
        }
        else if (_bachelorImage != null)
        {
            _bachelorImage.sprite = null;
            _bachelorImage.color = new Color(
                _bachelorImage.color.r,
                _bachelorImage.color.g,
                _bachelorImage.color.b,
                0f
            );
            _bachelorImage.enabled = false;
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

    // Checks if the current dialogue node is a condition node
    private bool IsConditionNode(DSDialogue dialogue)
    {
        // Instead of checking type directly, check the dialogue type from DSDialogueSO
        return dialogue != null
            && dialogue.m_dialogue != null
            && dialogue.m_dialogue.m_dialogueTypeData == DS.Enumerations.DSDialogueType.Condition;
    }

    // Checks if the current dialogue node is a setter node
    private bool IsSetterNode(DSDialogue dialogue)
    {
        // Instead of checking type directly, check the dialogue type from DSDialogueSO
        return dialogue != null
            && dialogue.m_dialogue != null
            && dialogue.m_dialogue.m_dialogueTypeData == DS.Enumerations.DSDialogueType.Setter;
    }

    // Evaluates a condition node and follows the appropriate path
    private void EvaluateConditionNode()
    {
        try
        {
            DSDialogueSO conditionNode = _dialogue.m_dialogue;

            // Get the property to check, comparison type, and value from properties in DSDialogueSO
            // Assuming these are accessible through custom properties in DSDialogueSO
            string propertyName = conditionNode.propertyToCheck;
            string comparisonType = conditionNode.comparisonType;
            string comparisonValue = conditionNode.comparisonValue;

            // Get the current value of the property
            bool conditionMet = false;

            // Make sure we have the property
            if (!_gameVariables.TryGetValue(propertyName, out string currentValue))
            {
                Debug.LogWarning(
                    $"Property {propertyName} not found, defaulting to false condition"
                );
                currentValue = "0";
            }

            // Handle number comparisons
            if (
                float.TryParse(currentValue, out float currentFloat)
                && float.TryParse(comparisonValue, out float compareFloat)
            )
            {
                // Evaluate the numeric condition
                switch (comparisonType)
                {
                    case "==":
                        conditionMet = Mathf.Approximately(currentFloat, compareFloat);
                        break;
                    case "!=":
                        conditionMet = !Mathf.Approximately(currentFloat, compareFloat);
                        break;
                    case ">":
                        conditionMet = currentFloat > compareFloat;
                        break;
                    case "<":
                        conditionMet = currentFloat < compareFloat;
                        break;
                    case ">=":
                        conditionMet = currentFloat >= compareFloat;
                        break;
                    case "<=":
                        conditionMet = currentFloat <= compareFloat;
                        break;
                    default:
                        Debug.LogError($"Unknown comparison type: {comparisonType}");
                        break;
                }
            }
            // Handle boolean comparisons
            else if (
                bool.TryParse(currentValue, out bool currentBool)
                && bool.TryParse(comparisonValue, out bool compareBool)
            )
            {
                // For boolean values, we typically just check equality
                switch (comparisonType)
                {
                    case "==":
                        conditionMet = currentBool == compareBool;
                        break;
                    case "!=":
                        conditionMet = currentBool != compareBool;
                        break;
                    default:
                        Debug.LogWarning($"Comparison {comparisonType} not ideal for booleans");
                        break;
                }
            }
            // Handle string comparisons as a fallback
            else
            {
                switch (comparisonType)
                {
                    case "==":
                        conditionMet = currentValue == comparisonValue;
                        break;
                    case "!=":
                        conditionMet = currentValue != comparisonValue;
                        break;
                    default:
                        Debug.LogWarning($"Comparison {comparisonType} not ideal for strings");
                        break;
                }
            }

            Debug.Log(
                $"Condition: {propertyName} {comparisonType} {comparisonValue}, Current value: {currentValue}, Result: {conditionMet}"
            );

            // Get the appropriate path based on the condition
            var choices = conditionNode.m_dialogueChoiceData;
            if (choices != null && choices.Count > 0)
            {
                // For now, we'll go with a simple implementation:
                // If condition is true, take the first path (index 0)
                // If condition is false, take the second path (index 1) if it exists
                int pathIndex = conditionMet ? 0 : 1;

                // Make sure the path exists
                if (pathIndex < choices.Count)
                {
                    // Follow the selected path
                    if (choices[pathIndex].m_nextDialogue != null)
                    {
                        _dialogue.m_dialogue = choices[pathIndex].m_nextDialogue;
                        ShowDialogue(); // Recursively show the next dialogue
                    }
                    else
                    {
                        Debug.LogWarning($"No next dialogue found for condition path {pathIndex}");
                    }
                }
                else
                {
                    Debug.LogError($"Condition node doesn't have path for result: {conditionMet}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error evaluating condition node: {e.Message}");
        }
    }

    // Applies changes from a setter node
    private void ApplySetterNode()
    {
        try
        {
            DSDialogueSO setterNode = _dialogue.m_dialogue;

            // Get setter operation type from DSDialogueSO
            string operationType = setterNode.operationType;

            // Apply the setter operation based on its type
            switch (operationType)
            {
                case "SetValue":
                    // Set a variable value
                    string variableName = setterNode.variableName;
                    string value = setterNode.valueToSet;

                    // Update the variable
                    _gameVariables[variableName] = value;
                    Debug.Log($"Set variable: {variableName} = {value}");
                    break;

                case "UpdateLoveScore":
                    // Update the love score
                    int amount = int.Parse(setterNode.loveScoreAmount);
                    _loveScore += amount;

                    // Update the Love variable as well
                    _gameVariables["Love"] = _loveScore.ToString();

                    // Update love meter if available
                    if (_loveMeter != null)
                    {
                        // Use the appropriate methods based on whether we're adding or subtracting points
                        if (amount > 0)
                        {
                            _loveMeter.IncreaseLove(amount);
                        }
                        else if (amount < 0)
                        {
                            _loveMeter.DecreaseLove(Mathf.Abs(amount));
                        }

                        // Update our local love score to match the love meter's current value
                        _loveScore = _loveMeter.GetCurrentLove();
                        _gameVariables["Love"] = _loveScore.ToString();
                    }

                    Debug.Log($"Updated love score: {_loveScore} (change: {amount})");
                    break;

                case "UpdateBoolean":
                    // Update a boolean value
                    string boolName = setterNode.variableName;
                    bool boolValue = bool.Parse(setterNode.boolValue);

                    // Update the variable
                    _gameVariables[boolName] = boolValue.ToString().ToLower();
                    Debug.Log($"Set boolean: {boolName} = {boolValue}");
                    break;
            }

            // After applying the setter, move to the next dialogue
            var choices = setterNode.m_dialogueChoiceData;
            if (choices != null && choices.Count > 0 && choices[0].m_nextDialogue != null)
            {
                _dialogue.m_dialogue = choices[0].m_nextDialogue;
                ShowDialogue(); // Recursively show the next dialogue
            }
            else
            {
                Debug.LogWarning("No next dialogue found after setter node");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error applying setter node: {e.Message}");
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
            _bachelor._name = "Chantal";
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
                // Check if this choice leads to a condition that would fail
                bool conditionPassesIfSelected = WouldChoiceConditionPass(choice);

                // If condition would not pass, disable the button
                if (!conditionPassesIfSelected)
                {
                    button.interactable = false;

                    // Make the text gray to indicate it's locked
                    if (btnText != null)
                        btnText.color = Color.gray;

                    Debug.Log(
                        $"Disabled choice button '{choice.m_dialogueChoiceText}' because condition would fail"
                    );
                }

                // Always add the listener, but the button will be non-interactable if conditions fail
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
        if (choice != null && choice.m_nextDialogue != null)
        {
            _dialogue.m_dialogue = choice.m_nextDialogue;
            ShowDialogue();
        }
        else
        {
            Debug.LogWarning("Selected choice has no valid next dialogue");
        }
    }

    // First, add a new helper method to check if a choice leads to a condition that would pass
    private bool WouldChoiceConditionPass(DS.Data.DSDialogueChoiceData choice)
    {
        // If there's no next dialogue or it's not a condition node, it passes by default
        if (
            choice.m_nextDialogue == null
            || choice.m_nextDialogue.m_dialogueTypeData != DS.Enumerations.DSDialogueType.Condition
        )
        {
            return true;
        }

        // Get the condition node
        DSDialogueSO conditionNode = choice.m_nextDialogue;

        // Get condition parameters
        string propertyName = conditionNode.propertyToCheck;
        string comparisonType = conditionNode.comparisonType;
        string comparisonValue = conditionNode.comparisonValue;

        // Check if we have the property
        if (!_gameVariables.TryGetValue(propertyName, out string currentValue))
        {
            Debug.LogWarning(
                $"Property {propertyName} not found for condition check, defaulting to false"
            );
            return false;
        }

        // Evaluate the condition similar to EvaluateConditionNode method
        // Handle number comparisons
        if (
            float.TryParse(currentValue, out float currentFloat)
            && float.TryParse(comparisonValue, out float compareFloat)
        )
        {
            switch (comparisonType)
            {
                case "==":
                    return Mathf.Approximately(currentFloat, compareFloat);
                case "!=":
                    return !Mathf.Approximately(currentFloat, compareFloat);
                case ">":
                    return currentFloat > compareFloat;
                case "<":
                    return currentFloat < compareFloat;
                case ">=":
                    return currentFloat >= compareFloat;
                case "<=":
                    return currentFloat <= compareFloat;
                default:
                    return false;
            }
        }
        // Handle boolean comparisons
        else if (
            bool.TryParse(currentValue, out bool currentBool)
            && bool.TryParse(comparisonValue, out bool compareBool)
        )
        {
            switch (comparisonType)
            {
                case "==":
                    return currentBool == compareBool;
                case "!=":
                    return currentBool != compareBool;
                default:
                    return false;
            }
        }
        // Handle string comparisons
        else
        {
            switch (comparisonType)
            {
                case "==":
                    return currentValue == comparisonValue;
                case "!=":
                    return currentValue != comparisonValue;
                default:
                    return false;
            }
        }
    }
}
