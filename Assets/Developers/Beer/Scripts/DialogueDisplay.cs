/*
 * =====================================================================================
 * DIALOGUE DISPLAY SYSTEM
 * =====================================================================================
 *
 * Author: Beer (LP-Cafe Development Team)
 * Description: Core dialogue system for the dating simulation game.
 *
 * This script manages:
 * - Dialogue text display with typewriter effects
 * - Choice button generation and interaction
 * - Love meter integration and visual feedback
 * - Bachelor preference discovery system
 * - Dialogue flow control (conditions, setters, branching)
 * - Save system integration for progress tracking
 * - Audio and visual elements synchronization
 *
 * Dependencies:
 * - Dialogue System (DS) framework
 * - TextAnimator (Febucci) for typewriter effects
 * - DOTween for UI animations (via LoveMeter)
 * - Custom ScriptableObjects (NewBachelorSO, LoveMeterSO)
 *
 * Usage:
 * 1. Assign UI references in the inspector
 * 2. Set bachelor and dialogue data via SetDialogue() or StartDialogue()
 * 3. The system handles all dialogue flow automatically
 *
 * =====================================================================================
 */

using System.Collections;
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

/// <summary>
/// Handles the display and interaction of dialogue in the dating sim game.
/// Manages dialogue flow, character information, choice buttons, and integration with the notebook system.
/// Supports advanced dialogue features like conditions, setters, and preference discovery.
/// </summary>
public class DialogueDisplay : MonoBehaviour
{ // =====================================================================================
    // COMPONENT REFERENCES AND CONFIGURATION
    // =====================================================================================

    #region UI References
    [Header("📋 TEXT DISPLAY COMPONENTS")]
    /// <summary>Reference to the UI element displaying the character's name</summary>
    [SerializeField]
    private TextMeshProUGUI _nameText;

    /// <summary>Reference to the typewriter effect component for animated text display</summary>
    [SerializeField]
    private TypewriterByCharacter _typewriter;

    /// <summary>Reference to the UI element displaying the dialogue text</summary>
    [SerializeField]
    private TextMeshProUGUI _displayText;

    [Header("🖼️ VISUAL COMPONENTS")]
    /// <summary>Reference to the UI Image displaying the character's portrait</summary>
    [SerializeField]
    private Image _bachelorImage;

    /// <summary>Continue icon that appears when single dialogue finishes</summary>
    [SerializeField]
    private GameObject _continueIcon;

    [Header("🔘 CHOICE SYSTEM")]
    /// <summary>Parent transform for dynamically generated choice buttons</summary>
    [SerializeField]
    private Transform _choicesParent;

    /// <summary>Prefab template for creating choice buttons</summary>
    [SerializeField]
    private GameObject _choiceButtonPrefab;

    [Header("💖 LOVE METER UI")]
    /// <summary>Reference to the love meter UI component for displaying love progress</summary>
    [SerializeField]
    [Tooltip(
        "Love meter UI component that displays the visual love meter for the current bachelor"
    )]
    private LoveMeter _loveMeterUI;
    #endregion

    #region Dialogue Data
    [Header("📝 DIALOGUE CONTENT")]
    /// <summary>The current dialogue data being displayed</summary>
    [SerializeField]
    private DSDialogue _dialogue;

    /// <summary>The current bachelor (character) data containing preferences and information</summary>
    [SerializeField]
    private NewBachelorSO _bachelor;
    #endregion

    #region Audio System
    [Header("🔊 AUDIO SYSTEM")]
    /// <summary>Audio source for playing dialogue audio clips</summary>
    [SerializeField]
    private AudioSource _audioSource;
    #endregion

    #region Notebook Integration
    [Header("📖 NOTEBOOK SYSTEM")]
    /// <summary>Reference to the NoteBook script for tracking discovered preferences</summary>
    [SerializeField]
    private NoteBook _noteBook;
    #endregion

    #region State Management
    /// <summary>Tracks whether the player can advance to the next dialogue</summary>
    private bool _canAdvance = false;

    /// <summary>Tracks whether the dialogue advancement delay is active</summary>
    private bool _isDelayActive = false;

    /// <summary>List of currently active choice buttons for cleanup purposes</summary>
    private List<GameObject> _activeChoiceButtons = new List<GameObject>();

    /// <summary>Dictionary to store gameplay variables used in dialogue conditions</summary>
    private Dictionary<string, string> _gameVariables = new Dictionary<string, string>();
    #endregion

    #region Love System
    [Header("💕 LOVE SCORING SYSTEM")]
    /// <summary>Current love score value with the active bachelor</summary>
    [SerializeField]
    private int _loveScore = 0;

    /// <summary>Reference to the love meter scriptable object for score management</summary>
    [SerializeField]
    private LoveMeterSO _loveMeter;
    #endregion

    #region UI Styling
    [Header("🎨 UI STYLING & COLORS")]
    /// <summary>Normal color for choice button text</summary>
    [SerializeField]
    private Color _normalTextColor = Color.white;

    /// <summary>Color for choice button text when hovered</summary>
    [SerializeField]
    private Color _hoverTextColor = new Color(1f, 0.8f, 0.2f);

    /// <summary>Color for disabled choice button text</summary>
    [SerializeField]
    private Color _disabledTextColor = Color.gray;
    #endregion

    #region Save System
    [Header("💾 SAVE & PROGRESS TRACKING")]
    /// <summary>Counter for tracking successful dates completed</summary>
    [SerializeField]
    public int _succesfulDateCount = 0;

    /// <summary>Save data reference for persisting game progress</summary>
    private SaveData _saveData;
    #endregion

    #region Unity Lifecycle    /// <summary>
    /// Initializes the dialogue system, loads save data, and sets up event listeners.
    /// Called once when the component is first created.
    /// </summary>
    private void Start()
    {
        // Load successful date count from save data
        LoadSuccessfulDateCountFromSave(); // Initialize variables with default values
        InitializeGameVariables();

        // Initialize love meter UI if bachelor data is already available
        EnsureLoveMeterSetup();

        // Ensure typewriter events are set up correctly
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
    }

    /// <summary>
    /// Handles input for advancing dialogue. Called once per frame.
    /// Checks for space key or mouse click to progress dialogue when possible.
    /// </summary>
    private void Update()
    {
        // Allow advancing dialogue if possible, no choices are being shown, and delay is not active
        if (
            _canAdvance
            && !_isDelayActive
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
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initialize game variables with default values used in dialogue conditions.
    /// Sets up the baseline state for dialogue system variables.
    /// </summary>
    private void InitializeGameVariables()
    {
        // Update love score from love meter if available
        if (_loveMeter != null)
        {
            _loveScore = _loveMeter.GetCurrentLove();
        } // Default values for variables used in conditions
        _gameVariables["Love"] = _loveScore.ToString();
        _gameVariables["LikeDiscovered"] = "false";
        _gameVariables["DislikeDiscovered"] = "false";
        _gameVariables["NotebookLikeEntry"] = "false";
        _gameVariables["NotebookDislikeEntry"] = "false";
    }

    /// <summary>
    /// Ensures the love meter UI component has the correct LoveMeterSO data.
    /// Called whenever bachelor data changes or dialogue system initializes.
    /// </summary>
    private void EnsureLoveMeterSetup()
    {
        if (_bachelor != null && _bachelor._loveMeter != null && _loveMeterUI != null)
        {
            _loveMeter = _bachelor._loveMeter;
            _loveMeterUI.SetLoveMeterData(_loveMeter);
            _loveScore = _loveMeter.GetCurrentLove();

            // Update game variables to reflect current love score
            _gameVariables["Love"] = _loveScore.ToString();

            Debug.Log(
                $"Love meter setup complete for {_bachelor._name}. Current love: {_loveScore}"
            );
        }
        else if (_loveMeterUI != null && _bachelor == null)
        {
            Debug.LogWarning("DialogueDisplay: Bachelor is null, cannot setup love meter.");
        }
        else if (_loveMeterUI != null && _bachelor._loveMeter == null)
        {
            Debug.LogWarning(
                $"DialogueDisplay: Bachelor {_bachelor._name} has no LoveMeterSO assigned."
            );
        }
    }
    #endregion

    #region Core Dialogue Display    /// <summary>
    /// Displays the current dialogue and sets up choices if available.
    /// Handles condition nodes, setter nodes, character images, audio, and choice generation.
    /// This is the main method that orchestrates dialogue presentation.
    /// </summary>
    public void ShowDialogue()
    {
        ClearChoices();
        EnsureVerticalLayoutSettings();

        // Ensure love meter UI has the correct data before showing dialogue
        EnsureLoveMeterSetup();

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

    /// <summary>
    /// Advances to the next dialogue if available, otherwise shows end dialogue options.
    /// Called when player clicks/presses space during single dialogue or through choice selection.
    /// </summary>
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
    }

    /// <summary>
    /// Sets the current dialogue and bachelor, then displays the dialogue.
    /// Used to initialize a new dialogue conversation with character data.
    /// </summary>
    /// <param name="dialogue">The dialogue data to display</param>
    /// <param name="bachelor">The character data associated with this dialogue</param>
    public void SetDialogue(DSDialogue dialogue, NewBachelorSO bachelor)
    {
        _dialogue = dialogue;
        _bachelor = bachelor;
        if (_bachelor != null)
        {
            // Ensure all preferences start as undiscovered
            _bachelor.EnsureUndiscoveredState(); // Use the bachelor's love meter if available
            if (_bachelor._loveMeter != null)
            {
                _loveMeter = _bachelor._loveMeter;
                _loveScore = _loveMeter.GetCurrentLove();

                // Initialize the love meter UI component with the bachelor's love meter data
                EnsureLoveMeterSetup();
            }
        }
        else
        {
            Debug.LogWarning("Bachelor reference is null when setting dialogue!");
        }
        ShowDialogue();
    }

    /// <summary>
    /// Starts a new dialogue conversation with a bachelor character.
    /// Initializes the dialogue system with character-specific data and preferences.
    /// </summary>
    /// <param name="bachelor">The bachelor character to start dialogue with</param>
    /// <param name="dialogueSO">The initial dialogue to display</param>
    public void StartDialogue(NewBachelorSO bachelor, DSDialogue dialogueSO)
    {
        bachelor._dialogue = dialogueSO;
        if (bachelor == null || bachelor._dialogue == null)
            return;
        _bachelor = bachelor;
        _dialogue = dialogueSO; // Initialize love meter with bachelor's data
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveMeter = _bachelor._loveMeter;
            _loveScore = _loveMeter.GetCurrentLove();

            // Set up the love meter UI component
            EnsureLoveMeterSetup();
        }

        ShowDialogue();
    }
    #endregion

    #region Dialogue Node Processing
    /// <summary>
    /// Checks if the current dialogue node is a condition node.
    /// Condition nodes evaluate variables and branch dialogue paths based on the result.
    /// </summary>
    /// <param name="dialogue">The dialogue to check</param>
    /// <returns>True if this is a condition node</returns>
    private bool IsConditionNode(DSDialogue dialogue)
    {
        return dialogue != null
            && dialogue.m_dialogue != null
            && dialogue.m_dialogue.m_dialogueTypeData == DS.Enumerations.DSDialogueType.Condition;
    }

    /// <summary>
    /// Checks if the current dialogue node is a setter node.
    /// Setter nodes modify game variables, love scores, or trigger preference discoveries.
    /// </summary>
    /// <param name="dialogue">The dialogue to check</param>
    /// <returns>True if this is a setter node</returns>
    private bool IsSetterNode(DSDialogue dialogue)
    {
        return dialogue != null
            && dialogue.m_dialogue != null
            && dialogue.m_dialogue.m_dialogueTypeData == DS.Enumerations.DSDialogueType.Setter;
    }

    /// <summary>
    /// Evaluates a condition node and follows the appropriate dialogue path.
    /// Supports numeric, boolean, and string comparisons with various operators.
    /// </summary>
    private void EvaluateConditionNode()
    {
        try
        {
            DSDialogueSO conditionNode = _dialogue.m_dialogue;

            // Get condition parameters from the dialogue node
            string propertyName = conditionNode.m_propertyToCheckData;
            string comparisonType = conditionNode.m_comparisonTypeData;
            string comparisonValue = conditionNode.m_comparisonValueData;

            bool conditionMet = false;

            // Ensure we have the property, default to "0" if not found
            if (!_gameVariables.TryGetValue(propertyName, out string currentValue))
            {
                Debug.LogWarning(
                    $"Property {propertyName} not found, defaulting to false condition"
                );
                currentValue = "0";
            }

            // Handle numeric comparisons
            if (
                float.TryParse(currentValue, out float currentFloat)
                && float.TryParse(comparisonValue, out float compareFloat)
            )
            {
                conditionMet = comparisonType switch
                {
                    "==" => Mathf.Approximately(currentFloat, compareFloat),
                    "!=" => !Mathf.Approximately(currentFloat, compareFloat),
                    ">" => currentFloat > compareFloat,
                    "<" => currentFloat < compareFloat,
                    ">=" => currentFloat >= compareFloat,
                    "<=" => currentFloat <= compareFloat,
                    _ => false,
                };
            }
            // Handle boolean comparisons
            else if (
                bool.TryParse(currentValue, out bool currentBool)
                && bool.TryParse(comparisonValue, out bool compareBool)
            )
            {
                conditionMet = comparisonType switch
                {
                    "==" => currentBool == compareBool,
                    "!=" => currentBool != compareBool,
                    _ => false,
                };
            }
            // Handle string comparisons
            else
            {
                conditionMet = comparisonType switch
                {
                    "==" => currentValue == comparisonValue,
                    "!=" => currentValue != comparisonValue,
                    _ => false,
                };
            }

            Debug.Log(
                $"Condition: {propertyName} {comparisonType} {comparisonValue}, Current: {currentValue}, Result: {conditionMet}"
            );

            // Follow the appropriate path based on condition result
            var choices = conditionNode.m_dialogueChoiceData;
            if (choices != null && choices.Count > 0)
            {
                int pathIndex = conditionMet ? 0 : 1;

                if (pathIndex < choices.Count && choices[pathIndex].m_nextDialogue != null)
                {
                    _dialogue.m_dialogue = choices[pathIndex].m_nextDialogue;
                    ShowDialogue();
                }
                else
                {
                    Debug.LogError($"No valid path found for condition result: {conditionMet}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error evaluating condition node: {e.Message}");
        }
    }

    /// <summary>
    /// Applies changes from a setter node and moves to the next dialogue.
    /// Handles variable setting, love score updates, boolean updates, and preference discoveries.
    /// </summary>
    private void ApplySetterNode()
    {
        Debug.Log("Applying setter node...");
        try
        {
            DSDialogueSO setterNode = _dialogue.m_dialogue;
            var operationType = setterNode.m_operationTypeData;

            Debug.Log($"Applying setter operation: {operationType}");

            switch (operationType)
            {
                case SetterOperationType.SetValue:
                    HandleSetValue(setterNode);
                    break;
                case SetterOperationType.UpdateLoveScore:
                    HandleUpdateLoveScore(setterNode);
                    break;
                case SetterOperationType.UpdateBoolean:
                    HandleUpdateBoolean(setterNode);
                    break;
                case SetterOperationType.DiscoverPreference:
                    HandleDiscoverPreference(setterNode);
                    break;
                default:
                    Debug.LogWarning($"Unknown setter operation type: {operationType}");
                    break;
            }

            // Move to the next dialogue after applying the setter
            var choices = setterNode.m_dialogueChoiceData;
            if (choices != null && choices.Count > 0 && choices[0].m_nextDialogue != null)
            {
                _dialogue.m_dialogue = choices[0].m_nextDialogue;
                ShowDialogue();
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

    /// <summary>
    /// Handles setting a variable value from a setter node.
    /// </summary>
    private void HandleSetValue(DSDialogueSO setterNode)
    {
        string variableName = setterNode.m_variableNameData;
        string value = setterNode.m_valueToSetData;
        _gameVariables[variableName] = value;
        Debug.Log($"Set variable: {variableName} = {value}");
    }

    /// <summary>
    /// Handles updating the love score from a setter node.
    /// </summary>
    private void HandleUpdateLoveScore(DSDialogueSO setterNode)
    {
        if (setterNode.m_loveScoreAmountData == 0)
            return;

        if (_loveMeter != null)
        {
            if (setterNode.m_loveScoreAmountData > 0)
            {
                _loveMeter.IncreaseLove(setterNode.m_loveScoreAmountData);
            }
            else
            {
                _loveMeter.DecreaseLove(Mathf.Abs(setterNode.m_loveScoreAmountData));
            }
            _loveScore = _loveMeter.GetCurrentLove();

            // Update game variables with new love score
            _gameVariables["Love"] = _loveScore.ToString();

            // Update the UI love meter to reflect the change
            if (_loveMeterUI != null)
            {
                _loveMeterUI.RefreshMeter();
            }
        }
        else
        {
            Debug.LogWarning("Love meter is not initialized, cannot update love score!");
        }
    }

    /// <summary>
    /// Handles updating a boolean value from a setter node.
    /// </summary>
    private void HandleUpdateBoolean(DSDialogueSO setterNode)
    {
        string boolName = setterNode.m_variableNameData;
        bool boolValue = setterNode.m_boolValueData;
        _gameVariables[boolName] = boolValue.ToString().ToLower();
        Debug.Log($"Set boolean: {boolName} = {boolValue}");
    }

    /// <summary>
    /// Handles discovering a preference from a setter node.
    /// </summary>
    private void HandleDiscoverPreference(DSDialogueSO setterNode)
    {
        string prefName = setterNode.m_selectedPreferenceData;
        bool isLike = setterNode.m_isLikePreferenceData;
        bool wasNewlyDiscovered = DiscoverBachelorPreference(prefName, isLike);

        if (wasNewlyDiscovered)
        {
            Debug.Log($"Successfully discovered new preference: {prefName} (Like: {isLike})");
        }
    }

    /// <summary>
    /// Called when the typewriter effect finishes displaying text.
    /// Starts a delay before allowing dialogue advancement and shows the continue icon if appropriate.
    /// </summary>
    private void OnTypewriterEnd()
    {
        // Only start coroutine if the GameObject is active
        if (gameObject.activeInHierarchy)
        {
            // Start the delay before allowing advancement
            StartCoroutine(DelayBeforeAdvancement());
        }
        else
        {
            // If GameObject is inactive, immediately allow advancement without delay
            _isDelayActive = false;
            _canAdvance = true;
        }

        // Show continue icon only if there are no multiple choices (single dialogue)
        if (_activeChoiceButtons.Count == 0 && _continueIcon != null)
        {
            _continueIcon.SetActive(true);
        }
    }

    /// <summary>
    /// Coroutine that waits one second before allowing the player to advance to the next dialogue.
    /// </summary>
    /// <returns>Coroutine enumerator</returns>
    private IEnumerator DelayBeforeAdvancement()
    {
        _isDelayActive = true;
        _canAdvance = false;

        // Wait for one second
        yield return new WaitForSeconds(1f);

        _isDelayActive = false;
        _canAdvance = true;
    }
    #endregion

    #region Choice System
    /// <summary>
    /// Instantiates choice buttons for each available choice.
    /// Handles button styling, condition checking, hover effects, and layout management.
    /// </summary>
    /// <param name="choices">List of dialogue choices to display as buttons</param>
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

            // Ensure proper button sizing and layout
            EnsureContentSizeFitter(btnObj);

            var button = btnObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                // Check if this choice's condition would pass
                bool conditionPassesIfSelected = WouldChoiceConditionPass(choice);

                if (!conditionPassesIfSelected)
                {
                    // Disable button if condition would fail
                    button.interactable = false;
                    if (btnText != null)
                        btnText.color = _disabledTextColor;

                    Debug.Log(
                        $"Disabled choice '{choice.m_dialogueChoiceText}' due to failed condition"
                    );
                }
                else
                {
                    // Style enabled buttons with hover effects
                    if (btnText != null)
                        btnText.color = _normalTextColor;
                    AddHoverEffects(button.gameObject, btnText);
                }

                // Add click listener
                button.onClick.AddListener(() => OnChoiceSelected(choice));
            }

            _activeChoiceButtons.Add(btnObj);
        }

        // Refresh layout after all buttons are created
        StartCoroutine(RefreshLayoutAfterDelay(0.05f));
    }

    /// <summary>
    /// Destroys all active choice buttons and clears the list.
    /// Called before showing new choices or when dialogue ends.
    /// </summary>
    private void ClearChoices()
    {
        foreach (var btn in _activeChoiceButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        _activeChoiceButtons.Clear();
    }

    /// <summary>
    /// Handles logic when a choice is selected by the player.
    /// Clears current choices and advances to the next dialogue.
    /// </summary>
    /// <param name="choice">The selected choice data</param>
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

    /// <summary>
    /// Checks if a choice would pass its condition requirements.
    /// Used to determine if a choice button should be enabled or disabled.
    /// </summary>
    /// <param name="choice">The choice to evaluate</param>
    /// <returns>True if the choice's condition would pass</returns>
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

        DSDialogueSO conditionNode = choice.m_nextDialogue;
        string propertyName = conditionNode.m_propertyToCheckData;
        string comparisonType = conditionNode.m_comparisonTypeData;
        string comparisonValue = conditionNode.m_comparisonValueData;

        if (!_gameVariables.TryGetValue(propertyName, out string currentValue))
        {
            Debug.LogWarning($"Property {propertyName} not found for condition check");
            return false;
        }

        // Evaluate condition similar to EvaluateConditionNode
        if (
            float.TryParse(currentValue, out float currentFloat)
            && float.TryParse(comparisonValue, out float compareFloat)
        )
        {
            return comparisonType switch
            {
                "==" => Mathf.Approximately(currentFloat, compareFloat),
                "!=" => !Mathf.Approximately(currentFloat, compareFloat),
                ">" => currentFloat > compareFloat,
                "<" => currentFloat < compareFloat,
                ">=" => currentFloat >= compareFloat,
                "<=" => currentFloat <= compareFloat,
                _ => false,
            };
        }
        else if (
            bool.TryParse(currentValue, out bool currentBool)
            && bool.TryParse(comparisonValue, out bool compareBool)
        )
        {
            return comparisonType switch
            {
                "==" => currentBool == compareBool,
                "!=" => currentBool != compareBool,
                _ => false,
            };
        }
        else
        {
            return comparisonType switch
            {
                "==" => currentValue == comparisonValue,
                "!=" => currentValue != comparisonValue,
                _ => false,
            };
        }
    }
    #endregion

    #region UI Layout and Styling
    /// <summary>
    /// Adds hover effects to choice buttons using event triggers.
    /// Changes text color on mouse enter/exit events.
    /// </summary>
    /// <param name="buttonObj">The button game object</param>
    /// <param name="text">The text component to style</param>
    private void AddHoverEffects(GameObject buttonObj, TextMeshProUGUI text)
    {
        if (buttonObj == null || text == null)
            return;

        EventTrigger trigger =
            buttonObj.GetComponent<EventTrigger>() ?? buttonObj.AddComponent<EventTrigger>();
        trigger.triggers ??= new List<EventTrigger.Entry>();

        // Hover enter effect
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((eventData) => text.color = _hoverTextColor);
        trigger.triggers.Add(enterEntry);

        // Hover exit effect
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((eventData) => text.color = _normalTextColor);
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// Helper method to add ContentSizeFitter component to choice buttons.
    /// Ensures buttons resize properly based on their text content.
    /// </summary>
    /// <param name="buttonObj">The button game object to configure</param>
    private void EnsureContentSizeFitter(GameObject buttonObj)
    {
        // Add or configure ContentSizeFitter
        var fitter =
            buttonObj.GetComponent<ContentSizeFitter>()
            ?? buttonObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Configure RectTransform anchoring
        var rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);
        }

        // Add horizontal layout group for proper content expansion
        var layout =
            buttonObj.GetComponent<HorizontalLayoutGroup>()
            ?? buttonObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(10, 10, 5, 5);
    }

    /// <summary>
    /// Helper coroutine to refresh UI layout after button sizes change.
    /// Ensures proper button spacing and alignment after dynamic content changes.
    /// </summary>
    /// <param name="delay">Delay in seconds before refreshing layout</param>
    /// <returns>Coroutine enumerator</returns>
    private System.Collections.IEnumerator RefreshLayoutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Refresh each button's layout
        foreach (var btn in _activeChoiceButtons)
        {
            if (btn != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(btn.GetComponent<RectTransform>());
            }
        }

        // Refresh parent layout
        if (_choicesParent != null)
        {
            var verticalLayout = _choicesParent.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                Canvas.ForceUpdateCanvases();
                verticalLayout.CalculateLayoutInputHorizontal();
                verticalLayout.CalculateLayoutInputVertical();
                Debug.Log(verticalLayout);
                verticalLayout.SetLayoutHorizontal();
                verticalLayout.SetLayoutVertical();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                _choicesParent.GetComponent<RectTransform>()
            );
        }
    }

    /// <summary>
    /// Ensures the vertical layout group has proper settings for dialogue choices.
    /// Configures spacing, sizing, and padding for optimal choice button display.
    /// </summary>
    private void EnsureVerticalLayoutSettings()
    {
        if (_choicesParent == null)
            return;

        var vertLayout = _choicesParent.GetComponent<VerticalLayoutGroup>();
        if (vertLayout != null)
        {
            vertLayout.padding = new RectOffset(0, 0, 0, -105);
            vertLayout.childControlWidth = true;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childControlHeight = true;
            vertLayout.childForceExpandHeight = false;
            vertLayout.spacing = 15f;
        }
    }
    #endregion

    #region Preference Discovery System
    /// <summary>
    /// Handles preference discoveries through dialogue interactions.
    /// Updates bachelor preferences and game variables when new likes/dislikes are found.
    /// </summary>
    /// <param name="preferenceName">Name of the preference to discover</param>
    /// <param name="isLike">True if this is a like, false if it's a dislike</param>
    /// <returns>True if the preference was newly discovered</returns>
    public bool DiscoverBachelorPreference(string preferenceName, bool isLike)
    {
        if (_bachelor == null)
            return false;

        if (isLike)
        {
            return DiscoverLikePreference(preferenceName);
        }
        else
        {
            return DiscoverDislikePreference(preferenceName);
        }
    }

    /// <summary>
    /// Discovers a like preference for the current bachelor.
    /// </summary>
    /// <param name="preferenceName">Name of the like to discover</param>
    /// <returns>True if newly discovered</returns>
    private bool DiscoverLikePreference(string preferenceName)
    {
        for (int i = 0; i < _bachelor._likes.Length; i++)
        {
            if (_bachelor._likes[i].description == preferenceName)
            {
                if (!_bachelor._likes[i].discovered)
                {
                    _bachelor.DiscoverLike(i);
                    Debug.Log($"Discovered like: {preferenceName}");

                    // Update game variables for newly discovered preference
                    _gameVariables["LikeDiscovered"] = "true";
                    _gameVariables["NotebookLikeEntry"] = "true";
                    return true;
                }
                else
                {
                    Debug.Log($"Like '{preferenceName}' was already discovered");
                    return false;
                }
            }
        }

        Debug.LogWarning($"Like preference '{preferenceName}' not found in bachelor data");
        return false;
    }

    /// <summary>
    /// Discovers a dislike preference for the current bachelor.
    /// </summary>
    /// <param name="preferenceName">Name of the dislike to discover</param>
    /// <returns>True if newly discovered</returns>
    private bool DiscoverDislikePreference(string preferenceName)
    {
        for (int i = 0; i < _bachelor._dislikes.Length; i++)
        {
            if (_bachelor._dislikes[i].description == preferenceName)
            {
                if (!_bachelor._dislikes[i].discovered)
                {
                    _bachelor.DiscoverDislike(i);
                    Debug.Log($"Discovered dislike: {preferenceName}");

                    // Update game variables for newly discovered preference
                    _gameVariables["DislikeDiscovered"] = "true";
                    _gameVariables["NotebookDislikeEntry"] = "true";
                    return true;
                }
                else
                {
                    Debug.Log($"Dislike '{preferenceName}' was already discovered");
                    return false;
                }
            }
        }

        Debug.LogWarning($"Dislike preference '{preferenceName}' not found in bachelor data");
        return false;
    }
    #endregion

    #region End Dialogue Management
    /// <summary>
    /// Shows end dialogue buttons when the conversation is complete.
    /// Only displays in the Cafe Scene, provides options to continue or return later.
    /// </summary>
    private void ShowEndDialogueButtons()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isCafeScene =
            currentSceneName.ToLower().Contains("cafe")
            || currentSceneName.ToLower().Contains("cafescene")
            || currentSceneName.Equals("CafeScene", System.StringComparison.OrdinalIgnoreCase);

        if (!isCafeScene)
        {
            Debug.Log(
                $"Not in Cafe Scene (current: {currentSceneName}). End dialogue buttons will not be shown."
            );
            return;
        }

        Debug.Log($"In Cafe Scene ({currentSceneName}). Showing end dialogue buttons.");

        ClearChoices();

        // Create "Come Back Later" button
        CreateEndDialogueButton("Come Back Later", OnComeBackLaterClicked);

        // Create "Next Scene" button
        string nextSceneText = !string.IsNullOrEmpty(_bachelor?._nextSceneName)
            ? $"Go to {_bachelor._nextSceneName}"
            : "Continue";
        CreateEndDialogueButton(nextSceneText, OnNextSceneClicked);
    }

    /// <summary>
    /// Helper method to create end dialogue buttons with consistent styling.
    /// </summary>
    /// <param name="buttonText">Text to display on the button</param>
    /// <param name="onClickAction">Action to perform when button is clicked</param>
    private void CreateEndDialogueButton(string buttonText, System.Action onClickAction)
    {
        var btnObj = Instantiate(_choiceButtonPrefab, _choicesParent);
        var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = buttonText;

        EnsureContentSizeFitter(btnObj);

        var button = btnObj.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => onClickAction());

            if (btnText != null)
                btnText.color = _normalTextColor;

            AddHoverEffects(btnObj, btnText);
        }

        _activeChoiceButtons.Add(btnObj);
    }

    /// <summary>
    /// Handles the "Come Back Later" button click.
    /// Returns to the main menu and hides the dialogue display.
    /// </summary>
    private void OnComeBackLaterClicked()
    {
        Debug.Log("Come Back Later button clicked - returning to main menu");
        ClearChoices();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Handles the "Next Scene" button click.
    /// Tracks successful date completion and loads the next scene.
    /// </summary>
    private void OnNextSceneClicked()
    {
        Debug.Log("Next Scene button clicked");

        // Check if this was a successful date completion
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isDateScene =
            !currentSceneName.ToLower().Contains("cafe")
            && !currentSceneName.ToLower().Contains("barista")
            && !currentSceneName.ToLower().Contains("menu")
            && !currentSceneName.ToLower().Contains("main");

        if (isDateScene)
        {
            IncrementSuccessfulDateCount();
            Debug.Log("Date completed successfully!");
        }

        // Load the next scene
        if (_bachelor != null && !string.IsNullOrEmpty(_bachelor._nextSceneName))
        {
            Debug.Log($"Loading scene: {_bachelor._nextSceneName}");
            SceneManager.LoadScene(_bachelor._nextSceneName);
        }
        else
        {
            // Fallback: load next scene in build settings or main menu
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"Loading next scene in build order: {nextSceneIndex}");
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.Log("No more scenes available");
            }
        }
    }
    #endregion

    #region Love Meter Control
    /// <summary>
    /// Sets the visibility of the love meter UI
    /// </summary>
    /// <param name="visible">Whether the love meter should be visible</param>
    public void SetLoveMeterVisibility(bool visible)
    {
        if (_loveMeterUI != null)
        {
            _loveMeterUI.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Updates the love meter UI with the current love value
    /// </summary>
    public void UpdateLoveMeterDisplay()
    {
        if (_loveMeterUI != null && _loveMeter != null)
        {
            _loveMeterUI.RefreshMeter();
        }
    }

    #endregion

    #region Date Tracking System
    /// <summary>
    /// Increments the successful date count and syncs with save data.
    /// Called when a date is completed successfully.
    /// </summary>
    public void IncrementSuccessfulDateCount()
    {
        _succesfulDateCount++;
        Debug.Log($"Successful date count incremented to: {_succesfulDateCount}");
        SyncWithSaveData();
    }

    /// <summary>
    /// Increments the failed date count in save data.
    /// Called when a date fails or is abandoned.
    /// </summary>
    public void IncrementFailedDateCount()
    {
        _saveData = SaveSystem.Deserialize() ?? new SaveData();
        _saveData.FailedDateCount++;
        SaveSystem.SerializeData(_saveData);
        Debug.Log($"Failed date count incremented to: {_saveData.FailedDateCount}");
    }

    /// <summary>
    /// Handles a failed date scenario.
    /// Tracks the failure and returns to the main menu.
    /// </summary>
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
            IncrementFailedDateCount();
            Debug.Log("Date failure tracked!");
        }

        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Syncs the current successful date count with the save system.
    /// Ensures progress is properly persisted.
    /// </summary>
    public void SyncWithSaveData()
    {
        _saveData = SaveSystem.Deserialize() ?? new SaveData();
        _saveData.SuccessfulDateCount = _succesfulDateCount;
        SaveSystem.SerializeData(_saveData);
        Debug.Log($"Synced successful date count with save data: {_succesfulDateCount}");
    }

    /// <summary>
    /// Loads the successful date count from save data.
    /// Called during initialization to restore progress.
    /// </summary>
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
    #endregion
}
