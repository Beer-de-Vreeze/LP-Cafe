using System.Collections.Generic;
using DS;
using DS.Enumerations;
using DS.ScriptableObjects;
using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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

    // Continue icon that appears when single dialogue finishes
    [SerializeField]
    private GameObject _continueIcon;

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

    [SerializeField]
    public int _succesfulDateCount = 0;

    // Save data reference for syncing successful date count
    private SaveData _saveData;

    private void Start()
    {
        // Load successful date count from save data
        LoadSuccessfulDateCountFromSave();

        // Initialize variables with default values
        InitializeGameVariables(); // Ensure typewriter events are set up correctly
        if (_typewriter != null)
        {
            _typewriter.onTextShowed.RemoveListener(OnTypewriterEnd);
            _typewriter.onTextShowed.AddListener(OnTypewriterEnd);
        }

        // Initialize continue icon as hidden
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

        /*        SetDialogue(_dialogue, _bachelor);*/
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
    } // Called once per frame

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

            // Hide continue icon when advancing
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(false);
            }

            NextDialogue();
        }
    } // Displays the current dialogue and sets up choices if available

    public void ShowDialogue()
    {
        ClearChoices();
        EnsureVerticalLayoutSettings(); // Add this line

        // Hide continue icon when starting new dialogue
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

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
                    bool isLike = setterNode.m_isLikePreferenceData; // Check if preference was actually newly discovered
                    bool wasNewlyDiscovered = DiscoverBachelorPreference(prefName, isLike);

                    // The NoteBook will automatically create entries via the OnPreferenceDiscovered event
                    // No need to manually call NoteBook methods anymore

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

    private void OnTypewriterEnd()
    {
        _canAdvance = true;

        // Show continue icon only if there are no multiple choices (single dialogue)
        if (_activeChoiceButtons.Count == 0 && _continueIcon != null)
        {
            _continueIcon.SetActive(true);
        }
    } // Sets the current dialogue and bachelor, then displays the dialogue

    public void SetDialogue(DSDialogue dialogue, NewBachelorSO bachelor)
    {
        _dialogue = dialogue;
        _bachelor = bachelor;
        if (_bachelor != null)
        {
            // Ensure all preferences start as undiscovered
            _bachelor.EnsureUndiscoveredState();

            // Use the bachelor's love meter if available
            if (_bachelor._loveMeter != null)
            {
                _loveMeter = _bachelor._loveMeter;
                _loveScore = _loveMeter.GetCurrentLove();
                _gameVariables["Love"] = _loveScore.ToString();
            }

            // Note: LikeDiscovered and DislikeDiscovered variables are only set
            // when preferences are actually discovered through setter nodes
        }
        else
        {
            _bachelor = NewBachelorSO.CreateInstance<NewBachelorSO>();
            _bachelor._name = "Chantal";
        }
        ShowDialogue();
    }

    public void StartDialogue(NewBachelorSO bachelor, DSDialogue dialogueSO)
    {
        bachelor._dialogue = dialogueSO;
        if (bachelor == null || bachelor._dialogue == null)
            return;
        _bachelor = bachelor;

        // Ensure all preferences start as undiscovered
        _bachelor.EnsureUndiscoveredState();

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
                Debug.Log("No next dialogue found. Showing end dialogue buttons.");
                ShowEndDialogueButtons();
            }
        }
    } // Instantiates choice buttons for each available choice

    private void ShowChoices(List<DS.Data.DSDialogueChoiceData> choices)
    {
        // Hide continue icon when showing multiple choices
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

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
    } // Destroys all active choice buttons

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
    } // New method to handle preference discoveries through dialogue

    public bool DiscoverBachelorPreference(string preferenceName, bool isLike)
    {
        if (_bachelor == null)
            return false;

        if (isLike)
        {
            // Find the like by description and discover it only if not already discovered
            for (int i = 0; i < _bachelor._likes.Length; i++)
            {
                if (_bachelor._likes[i].description == preferenceName)
                { // Only set the preference if it's not already discovered
                    if (!_bachelor._likes[i].discovered)
                    {
                        _bachelor.DiscoverLike(i);
                        Debug.Log($"Discovered like: {preferenceName}");

                        // Only update game variables when actually discovering a new preference
                        _gameVariables["LikeDiscovered"] = "true";
                        _gameVariables["NotebookLikeEntry"] = "true";
                        return true; // Preference was newly discovered
                    }
                    else
                    {
                        Debug.Log(
                            $"Like '{preferenceName}' was already discovered, skipping variable updates"
                        );
                        return false; // Preference was already discovered
                    }
                }
            }
        }
        else
        {
            // Find the dislike by description and discover it only if not already discovered
            for (int i = 0; i < _bachelor._dislikes.Length; i++)
            {
                if (_bachelor._dislikes[i].description == preferenceName)
                { // Only set the preference if it's not already discovered
                    if (!_bachelor._dislikes[i].discovered)
                    {
                        _bachelor.DiscoverDislike(i);
                        Debug.Log($"Discovered dislike: {preferenceName}");

                        // Only update game variables when actually discovering a new preference
                        _gameVariables["DislikeDiscovered"] = "true";
                        _gameVariables["NotebookDislikeEntry"] = "true";
                        return true; // Preference was newly discovered
                    }
                    else
                    {
                        Debug.Log(
                            $"Dislike '{preferenceName}' was already discovered, skipping variable updates"
                        );
                        return false; // Preference was already discovered
                    }
                }
            }
        }

        // Preference not found
        Debug.LogWarning($"Preference '{preferenceName}' not found in bachelor data");
        return false;
    } // Show end dialogue buttons using the same container and prefab as choice buttons

    private void ShowEndDialogueButtons()
    {
        // Only show end dialogue buttons in the Cafe Scene
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isCafeScene =
            currentSceneName.ToLower().Contains("cafe")
            || currentSceneName.ToLower().Contains("cafeScene")
            || currentSceneName.Equals("CafeScene", System.StringComparison.OrdinalIgnoreCase);

        if (!isCafeScene)
        {
            Debug.Log(
                $"Not in Cafe Scene (current scene: {currentSceneName}). End dialogue buttons will not be shown."
            );
            return;
        }

        Debug.Log($"In Cafe Scene ({currentSceneName}). Showing end dialogue buttons.");

        // Clear any existing choice buttons first
        ClearChoices();

        // Create "Come Back Later" button
        var comeBackLaterBtn = Instantiate(_choiceButtonPrefab, _choicesParent);
        var comeBackLaterText = comeBackLaterBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (comeBackLaterText != null)
            comeBackLaterText.text = "Come Back Later";

        // Add or ensure ContentSizeFitter exists on button
        EnsureContentSizeFitter(comeBackLaterBtn);
        var comeBackLaterButton = comeBackLaterBtn.GetComponent<UnityEngine.UI.Button>();
        if (comeBackLaterButton != null)
        {
            comeBackLaterButton.onClick.AddListener(OnComeBackLaterClicked);

            // Set normal color for the button
            if (comeBackLaterText != null)
                comeBackLaterText.color = _normalTextColor;

            // Add hover effects
            AddHoverEffects(comeBackLaterBtn, comeBackLaterText);
        }

        _activeChoiceButtons.Add(comeBackLaterBtn);

        // Create "Next Scene" button
        var nextSceneBtn = Instantiate(_choiceButtonPrefab, _choicesParent);
        var nextSceneText = nextSceneBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (nextSceneText != null)
        {
            if (!string.IsNullOrEmpty(_bachelor._nextSceneName))
            {
                nextSceneText.text = $"Go to {_bachelor._nextSceneName}";
            }
            else
            {
                nextSceneText.text = "Continue";
            }
        }

        // Add or ensure ContentSizeFitter exists on button
        EnsureContentSizeFitter(nextSceneBtn);
        var nextSceneButton = nextSceneBtn.GetComponent<UnityEngine.UI.Button>();
        if (nextSceneButton != null)
        {
            nextSceneButton.onClick.AddListener(OnNextSceneClicked);

            // Set normal color for the button
            if (nextSceneText != null)
                nextSceneText.color = _normalTextColor;

            // Add hover effects
            AddHoverEffects(nextSceneBtn, nextSceneText);
        }

        _activeChoiceButtons.Add(nextSceneBtn);
    }

    // Handle come back later button click
    private void OnComeBackLaterClicked()
    {
        Debug.Log("Come Back Later button clicked - closing dialogue");
        SceneManager.LoadScene("MainMenu");
        // Clear the buttons
        ClearChoices();

        // Hide the entire dialogue display
        gameObject.SetActive(false);
    } // Handle next scene button click

    private void OnNextSceneClicked()
    {
        Debug.Log("Next Scene button clicked");

        // Check if this was a successful date completion
        // A successful date is one where the dialogue ended normally and the user chose to continue
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isDateScene =
            !currentSceneName.ToLower().Contains("cafe")
            && !currentSceneName.ToLower().Contains("barista")
            && !currentSceneName.ToLower().Contains("menu")
            && !currentSceneName.ToLower().Contains("main");

        if (isDateScene)
        {
            // This is likely a date scene, so increment successful date count
            IncrementSuccessfulDateCount();
            Debug.Log("Date completed successfully!");
        }

        SceneManager.LoadScene("MainMenu");
        if (_bachelor != null && !string.IsNullOrEmpty(_bachelor._nextSceneName))
        {
            Debug.Log($"Loading scene: {_bachelor._nextSceneName}");
            SceneManager.LoadScene(_bachelor._nextSceneName);
        }
        else
        {
            // Fallback: load next scene in build settings
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"Loading next scene in build order: {nextSceneIndex}");
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.LogWarning(
                    "No scene name specified in bachelor SO and no more scenes in build settings"
                );
            }
        }
    }

    // Method to increment successful date count and save data
    public void IncrementSuccessfulDateCount()
    {
        _succesfulDateCount++;
        Debug.Log($"Successful date count incremented to: {_succesfulDateCount}");

        // Sync with save data
        SyncWithSaveData();
    }

    // Method to increment failed date count
    public void IncrementFailedDateCount()
    {
        // Load current save data to get failed date count
        _saveData = SaveSystem.Deserialize();
        if (_saveData == null)
        {
            _saveData = new SaveData();
        }

        _saveData.FailedDateCount++;
        SaveSystem.SerializeData(_saveData);

        Debug.Log($"Failed date count incremented to: {_saveData.FailedDateCount}");
    }

    // Method to handle a failed date (can be called from dialogue choices or events)
    public void OnDateFailed()
    {
        Debug.Log("Date failed!");

        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isDateScene =
            !currentSceneName.ToLower().Contains("cafe")
            && !currentSceneName.ToLower().Contains("barista")
            && !currentSceneName.ToLower().Contains("menu")
            && !currentSceneName.ToLower().Contains("main");

        if (isDateScene)
        {
            // This is likely a date scene, so increment failed date count
            IncrementFailedDateCount();
            Debug.Log("Date failure tracked!");
        } // You can add additional logic here, such as:
        // - Showing a specific "date failed" message
        // - Playing different audio
        // - Changing the scene transition

        SceneManager.LoadScene("MainMenu");
    }

    // Method to sync with save data
    public void SyncWithSaveData()
    {
        // Load current save data
        _saveData = SaveSystem.Deserialize();

        if (_saveData == null)
        {
            _saveData = new SaveData();
        }

        // Update save data with current count
        _saveData.SuccessfulDateCount = _succesfulDateCount;

        // Save the data
        SaveSystem.SerializeData(_saveData);
        Debug.Log($"Synced successful date count with save data: {_succesfulDateCount}");
    }

    // Method to load successful date count from save data
    public void LoadSuccessfulDateCountFromSave()
    {
        _saveData = SaveSystem.Deserialize();

        if (_saveData != null)
        {
            _succesfulDateCount = _saveData.SuccessfulDateCount;
            Debug.Log($"Loaded successful date count from save: {_succesfulDateCount}");
        }
        else
        {
            Debug.Log("No save data found, keeping current successful date count");
        }
    }
}
