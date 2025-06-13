using System.Collections.Generic;
using DS;
using DS.Enumerations;
using DS.ScriptableObjects;
using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.LayoutUtility;

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

    // Reference to the NoteBook script
    [SerializeField]
    private NoteBook _noteBook;

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

    // Colors for button text states
    [SerializeField]
    private Color _normalTextColor = Color.white;

    [SerializeField]
    private Color _hoverTextColor = new Color(1f, 0.8f, 0.2f);

    [SerializeField]
    private Color _disabledTextColor = Color.gray;

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
        EnsureVerticalLayoutSettings(); // Add this line

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
            string propertyName = conditionNode.m_propertyToCheckData;
            string comparisonType = conditionNode.m_comparisonTypeData;
            string comparisonValue = conditionNode.m_comparisonValueData;

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
        Debug.Log("Applying setter node...");
        try
        {
            DSDialogueSO setterNode = _dialogue.m_dialogue;

            // Get setter operation type from DSDialogueSO
            var operationType = setterNode.m_operationTypeData;

            // Apply the setter operation based on its type
            Debug.Log($"Applying setter operation: {operationType}");
            switch (operationType)
            {
                case SetterOperationType.SetValue:
                    Debug.Log("Setting variable value...");
                    // Set a variable value
                    string variableName = setterNode.m_variableNameData;
                    string value = setterNode.m_valueToSetData;
                    // Update the variable
                    _gameVariables[variableName] = value;
                    Debug.Log($"Set variable: {variableName} = {value}");
                    break;

                case SetterOperationType.UpdateLoveScore:
                    Debug.Log("Updating love score...");
                    // Update the love score
                    int amount = setterNode.m_loveScoreAmountData;

                    try
                    {
                        // Check if there's a specific love meter assigned in the setter node
                        LoveMeterSO targetLoveMeter = null;

                        // First try to get the love meter from the setter node
                        if (setterNode.m_loveMeterData != null)
                        {
                            targetLoveMeter = setterNode.m_loveMeterData as LoveMeterSO;
                            Debug.Log(
                                $"Using love meter from setter node: {targetLoveMeter?.name ?? "null"}"
                            );
                        }

                        // If that fails, use the default love meter
                        if (targetLoveMeter == null)
                        {
                            targetLoveMeter = _loveMeter;
                            Debug.Log(
                                $"Using default love meter: {targetLoveMeter?.name ?? "null"}"
                            );
                        }

                        if (targetLoveMeter != null)
                        {
                            // Verify that the love meter is properly initialized before using it
                            if (targetLoveMeter.IsInitialized())
                            {
                                if (amount > 0)
                                {
                                    Debug.Log($"Increasing love by {amount}");
                                    targetLoveMeter.IncreaseLove(amount);
                                }
                                else if (amount < 0)
                                {
                                    Debug.Log($"Decreasing love by {Mathf.Abs(amount)}");
                                    targetLoveMeter.DecreaseLove(Mathf.Abs(amount));
                                }

                                // If we're affecting the default love meter, update the local score variable
                                if (targetLoveMeter == _loveMeter)
                                {
                                    _loveScore = _loveMeter.GetCurrentLove();
                                    _gameVariables["Love"] = _loveScore.ToString();
                                    Debug.Log(
                                        $"Updated default love score: {_loveScore} (change: {amount})"
                                    );
                                }
                            }
                            else
                            {
                                Debug.LogWarning(
                                    "Love meter is not properly initialized, using local variables instead"
                                );
                                // Fall back to local variable update
                                _loveScore += amount;
                                _gameVariables["Love"] = _loveScore.ToString();
                            }
                        }
                        else
                        {
                            // No love meter available, just update the local variable
                            _loveScore += amount;
                            _gameVariables["Love"] = _loveScore.ToString();
                            Debug.Log(
                                $"Updated local love score: {_loveScore} (no love meter available)"
                            );
                        }
                    }
                    catch (System.Exception e)
                    {
                        // Fallback: just update the local variable if anything goes wrong
                        Debug.LogError($"Error updating love score: {e.Message}\n{e.StackTrace}");
                        _loveScore += amount;
                        _gameVariables["Love"] = _loveScore.ToString();
                        Debug.Log($"Fallback: Updated local love score: {_loveScore}");
                    }
                    break;

                case SetterOperationType.UpdateBoolean:
                    Debug.Log("Updating boolean value...");
                    // Update a boolean value
                    string boolName = setterNode.m_variableNameData;
                    bool boolValue = setterNode.m_boolValueData;

                    // Update the variable
                    _gameVariables[boolName] = boolValue.ToString().ToLower();
                    Debug.Log($"Set boolean: {boolName} = {boolValue}");
                    break;

                case SetterOperationType.DiscoverPreference:
                    Debug.Log("Discovering preference...");
                    // Get info from setter node
                    string prefName = setterNode.m_selectedPreferenceData;
                    bool isLike = setterNode.m_isLikePreferenceData;
                    // Update bachelor data and game variables
                    DiscoverBachelorPreference(prefName, isLike);
                    // Update notebook UI if available
                    if (_noteBook != null)
                    {
                        if (isLike)
                            _noteBook.DiscoverLike(prefName);
                        else
                            _noteBook.DiscoverDislike(prefName);
                    }
                    break;

                default:
                    Debug.LogWarning($"Unknown setter operation type: {operationType}");
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
            Debug.LogError($"Error applying setter node: {e.Message}\n{e.StackTrace}");
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

        if (_bachelor != null)
        {
            // Use the bachelor's love meter if available
            if (_bachelor._loveMeter != null)
            {
                _loveMeter = _bachelor._loveMeter;
                _loveScore = _loveMeter.GetCurrentLove();
                _gameVariables["Love"] = _loveScore.ToString();
            }

            // Initialize preference discovery status variables
            bool hasAnyLikeDiscovered = false;
            bool hasAnyDislikeDiscovered = false;

            if (_bachelor._likes != null)
            {
                foreach (var like in _bachelor._likes)
                {
                    if (like.discovered)
                    {
                        hasAnyLikeDiscovered = true;
                        break;
                    }
                }
            }

            if (_bachelor._dislikes != null)
            {
                foreach (var dislike in _bachelor._dislikes)
                {
                    if (dislike.discovered)
                    {
                        hasAnyDislikeDiscovered = true;
                        break;
                    }
                }
            }

            _gameVariables["LikeDiscovered"] = hasAnyLikeDiscovered.ToString().ToLower();
            _gameVariables["DislikeDiscovered"] = hasAnyDislikeDiscovered.ToString().ToLower();
        }
        else
        {
            _bachelor = NewBachelorSO.CreateInstance<NewBachelorSO>();
            _bachelor._name = "Chantal";
        }

        ShowDialogue();
    }

    // Public method to start a dialogue with a DSDialogueSO
    public void StartDialogue(DSDialogueSO dialogueSO)
    {
        if (dialogueSO == null)
            return;
        DSDialogue newDialogue = new DSDialogue { m_dialogue = dialogueSO };
        SetDialogue(newDialogue, _bachelor);
        ShowDialogue();
    }

    // Public method to start a dialogue with a NewBachelorSO
    public void StartDialogue(NewBachelorSO bachelor)
    {
        if (bachelor == null || bachelor._dialogue == null)
            return;
        _bachelor = bachelor;
        _loveMeter = bachelor._loveMeter; 
        SetDialogue(bachelor._dialogue, bachelor);
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

            // Add or ensure ContentSizeFitter exists on button
            EnsureContentSizeFitter(btnObj);

            // Rest of your existing button setup code...
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
                        btnText.color = _disabledTextColor;

                    Debug.Log(
                        $"Disabled choice button '{choice.m_dialogueChoiceText}' because condition would fail"
                    );
                }
                else
                {
                    // Set initial normal color for enabled buttons
                    if (btnText != null)
                        btnText.color = _normalTextColor;

                    // Add hover effects using event triggers
                    AddHoverEffects(button.gameObject, btnText);
                }

                // Always add the listener, but the button will be non-interactable if conditions fail
                button.onClick.AddListener(() =>
                {
                    OnChoiceSelected(choice);
                });
            }
            _activeChoiceButtons.Add(btnObj);
        }

        // Give Unity a frame to recalculate sizes
        StartCoroutine(RefreshLayoutAfterDelay(0.05f));
    }

    // Helper method to add ContentSizeFitter if needed
    private void EnsureContentSizeFitter(GameObject buttonObj)
    {
        // First, check if there's a ContentSizeFitter on the button itself
        ContentSizeFitter fitter = buttonObj.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = buttonObj.AddComponent<ContentSizeFitter>();
        }

        // Configure it to adjust horizontally based on text content
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        // Also set vertical fit mode to ensure proper height calculation
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Set the RectTransform to anchor at the left and expand to the right
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Change from stretching to left-anchored
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f); // Pivot at left-center
        }

        // Make sure the layout group is present to properly expand the button background
        HorizontalLayoutGroup layout = buttonObj.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = buttonObj.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft; // Align content to the left
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(10, 10, 5, 5); // Add some padding
        }
    }

    // Helper coroutine to refresh layout after sizes change
    private System.Collections.IEnumerator RefreshLayoutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Refresh each button first
        foreach (var btn in _activeChoiceButtons)
        {
            if (btn != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(btn.GetComponent<RectTransform>());
            }
        }

        // Then force layout rebuild on parent
        if (_choicesParent != null)
        {
            VerticalLayoutGroup verticalLayout = _choicesParent.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                // Ensure the VerticalLayoutGroup has appropriate settings
                Canvas.ForceUpdateCanvases();
                verticalLayout.CalculateLayoutInputHorizontal();
                verticalLayout.CalculateLayoutInputVertical();
                verticalLayout.SetLayoutHorizontal();
                verticalLayout.SetLayoutVertical();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                _choicesParent.GetComponent<RectTransform>()
            );
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
        string propertyName = conditionNode.m_propertyToCheckData;
        string comparisonType = conditionNode.m_comparisonTypeData;
        string comparisonValue = conditionNode.m_comparisonValueData;

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

    private void AddHoverEffects(GameObject buttonObj, TextMeshProUGUI text)
    {
        // Make sure we have valid objects
        if (buttonObj == null || text == null)
            return;

        // Add or get EventTrigger component
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = buttonObj.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();

        // Add pointer enter event (hover start)
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener(
            (eventData) =>
            {
                text.color = _hoverTextColor;
            }
        );
        trigger.triggers.Add(enterEntry);

        // Add pointer exit event (hover end)
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener(
            (eventData) =>
            {
                text.color = _normalTextColor;
            }
        );
        trigger.triggers.Add(exitEntry);
    }

    private void EnsureVerticalLayoutSettings()
    {
        if (_choicesParent != null)
        {
            VerticalLayoutGroup vertLayout = _choicesParent.GetComponent<VerticalLayoutGroup>();
            if (vertLayout != null)
            {
                // Recommended settings for dialogue choice buttons
                vertLayout.padding = new RectOffset(0, 0, 0, -100);
                vertLayout.childControlWidth = true;
                vertLayout.childForceExpandWidth = true;
                vertLayout.childControlHeight = true;
                vertLayout.childForceExpandHeight = false;
                vertLayout.spacing = 8f;
            }
        }
    }

    // New method to handle preference discoveries through dialogue
    public void DiscoverBachelorPreference(string preferenceName, bool isLike)
    {
        if (_bachelor == null)
            return;

        if (isLike)
        {
            // Find the like by description and discover it
            for (int i = 0; i < _bachelor._likes.Length; i++)
            {
                if (_bachelor._likes[i].description == preferenceName)
                {
                    _bachelor.DiscoverLike(i);
                    Debug.Log($"Discovered like: {preferenceName}");

                    // Also update our game variables
                    _gameVariables["LikeDiscovered"] = "true";
                    _gameVariables["NotebookLikeEntry"] = "true";
                    break;
                }
            }
        }
        else
        {
            // Find the dislike by description and discover it
            for (int i = 0; i < _bachelor._dislikes.Length; i++)
            {
                if (_bachelor._dislikes[i].description == preferenceName)
                {
                    _bachelor.DiscoverDislike(i);
                    Debug.Log($"Discovered dislike: {preferenceName}");

                    // Also update our game variables
                    _gameVariables["DislikeDiscovered"] = "true";
                    _gameVariables["NotebookDislikeEntry"] = "true";
                    break;
                }
            }
        }
    }
}
