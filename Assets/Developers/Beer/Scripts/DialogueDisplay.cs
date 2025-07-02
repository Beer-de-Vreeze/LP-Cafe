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
using DG.Tweening;
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

    /// <summary>Reference to the dialogue canvas for showing/hiding the dialogue UI</summary>
    [SerializeField]
    private Canvas _dialogueCanvas;

    /// <summary>Reference to the move canvas for enabling/disabling movement controls</summary>
    [SerializeField]
    private Canvas _moveCanvas;

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

    /// <summary>Stores choices that should be displayed after the typewriter finishes</summary>
    private List<DS.Data.DSDialogueChoiceData> _pendingChoices = null;
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

    #region Date Dialogues
    /// <summary>Dialogue for rooftop date location</summary>
    private DSDialogue _rooftopDateDialogue;

    /// <summary>Dialogue for aquarium date location</summary>
    private DSDialogue _aquariumDateDialogue;

    /// <summary>Dialogue for forest date location</summary>
    private DSDialogue _forestDateDialogue;
    #endregion
    [Header("☕ BARISTA DIALOGUES")]
    #region Barista Dialogues

    /// <summary>List of barista dialogues to display after real dates, indexed by number of completed real dates. The final dialogue is only shown if all dates were successful.</summary>
    [SerializeField]
    private List<DSDialogue> _baristaDialogues = new List<DSDialogue>();

    /// <summary>Barista dialogue to display when a real date fails (love score too low)</summary>
    [SerializeField]
    private DSDialogue _badDateBaristaDialogue;

    /// <summary>Barista dialogue to display when some dates succeeded and some failed, triggers game end</summary>
    [SerializeField]
    private DSDialogue _mixedResultsBaristaDialogue;

    /// <summary>Barista dialogue to display when all dates have failed, triggers game end</summary>
    [SerializeField]
    private DSDialogue _allDateFailedBaristaDialogue;
    #endregion

    #region Background System
    [Header("🌄 BACKGROUND SYSTEM")]
    /// <summary>Background sprite for rooftop date location</summary>
    [SerializeField]
    private Image _rooftopBackground;

    /// <summary>Background sprite for aquarium date location</summary>
    [SerializeField]
    private Image _aquariumBackground;

    /// <summary>Background sprite for forest date location</summary>
    [SerializeField]
    private Image _forestBackground;

    /// <summary>Tracks the currently loaded background to avoid unnecessary changes</summary>
    private Image _currentBackground;

    /// <summary>Tracks whether we just completed a real date and should show "Back to Cafe" button</summary>
    private bool _justCompletedRealDate = false;

    /// <summary>Stores the current real date location name for completion tracking</summary>
    private string _currentRealDateLocation = "";
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

    [Header("🎬 CHOICE ANIMATION SETTINGS")]
    /// <summary>Duration for each choice button fade-in animation</summary>
    [SerializeField]
    private float _choiceFadeDuration = 0.3f;

    /// <summary>Delay between each choice button appearing</summary>
    [SerializeField]
    private float _choiceAppearDelay = 0.2f;
    #endregion
    #region Good/Bad Date Screens
    [Header("🎉 DATE RESULT SCREENS")]
    /// <summary>Screen displayed for a good date result</summary>
    [SerializeField]
    private GameObject _goodDateScreen;

    /// <summary>Screen displayed for a bad date result</summary>
    [SerializeField]
    private GameObject _badDateScreen;
    #endregion


    #region Save System
    [Header("💾 SAVE & PROGRESS TRACKING")]
    /// <summary>Counter for tracking successful dates completed</summary>
    [SerializeField]
    public int _speedDateCount = 0;

    /// <summary>Counter for tracking real dates (aquarium, forest, rooftop) completed</summary>
    [SerializeField]
    public int _realDateCount = 0;

    [SerializeField]
    public int _loveNeededForSuccefulDate = 3;

    /// <summary>Save data reference for persisting game progress</summary>
    private SaveData _saveData;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the dialogue system, loads save data, and sets up event listeners.
    /// Called once when the component is first created.
    /// </summary>
    private void Start()
    {
        // Load successful date count and real date count from save data
        LoadSuccessfulDateCountFromSave(); // Initialize variables with default values
        InitializeGameVariables();

        // Initialize love meter UI if bachelor data is already available
        EnsureLoveMeterSetup();

        // Ensure typewriter events are set up correctly
        if (_typewriter != null)
        {
            _typewriter.onTextShowed.RemoveListener(OnTypewriterEnd);
            _typewriter.onTextShowed.AddListener(OnTypewriterEnd);
        } // Initialize continue icon as hidden
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

        // Initialize love meter as hidden/inactive
        if (_loveMeterUI != null)
        {
            _loveMeterUI.HideLoveMeter();
        }

        // Turn off all date backgrounds initially
        TurnOffAllDateBackgrounds();
    }

    /// <summary>
    /// Handles input for advancing dialogue. Called once per frame.
    /// Checks for space key or mouse click to progress dialogue when possible.
    /// </summary>
    private void Update()
    {
        // Playtest reset: Press '=' to clear save and return to Main Menu
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            Debug.Log("[TEST] Resetting save and returning to Main Menu");
            string path = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                Debug.Log("[TEST] Save file deleted: " + path);
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
            return;
        }

        // Test function: Press 'E' to skip to end of dialogue
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[TEST] Skipping to end of dialogue");
            GoToEndOfDialogue();
            return;
        }

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
    { // Update love score from love meter if available
        if (_loveMeter != null)
        {
            _loveScore = _loveMeter.GetCurrentLove();
        }

        // Default values for variables used in conditions
        _gameVariables["Love"] = _loveScore.ToString();
        _gameVariables["LikeDiscovered"] = "false";
        _gameVariables["DislikeDiscovered"] = "false";

        // Only set notebook variables if notebook exists
        if (_noteBook != null)
        {
            _gameVariables["NotebookLikeEntry"] = "false";
            _gameVariables["NotebookDislikeEntry"] = "false";
        }
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

    #region Core Dialogue Display
    /// <summary>
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
                // Store choices to be shown after typewriter finishes
                _pendingChoices = choices;
                _canAdvance = false;
            }
            else
            {
                // No multiple choices, clear any pending choices
                _pendingChoices = null;
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
    /// TEST FUNCTION: Skips to the end of the current dialogue chain.
    /// Traverses through all dialogue nodes following the first choice until reaching the end.
    /// Useful for testing end dialogue functionality without going through the entire conversation.
    /// </summary>
    [ContextMenu("TEST: Go To End Of Dialogue")]
    private void GoToEndOfDialogue()
    {
        if (_dialogue == null || _dialogue.m_dialogue == null)
        {
            Debug.LogWarning("[TEST] No dialogue to skip through");
            return;
        }

        DSDialogueSO currentNode = _dialogue.m_dialogue;
        int maxIterations = 100; // Safety limit to prevent infinite loops
        int iterations = 0;

        // Traverse through the dialogue chain until we reach the end
        while (currentNode != null && iterations < maxIterations)
        {
            iterations++;

            // Skip condition and setter nodes automatically
            if (
                currentNode.m_dialogueTypeData == DS.Enumerations.DSDialogueType.Condition
                || currentNode.m_dialogueTypeData == DS.Enumerations.DSDialogueType.Setter
            )
            {
                // For condition/setter nodes, just follow the first choice
                var choices = currentNode.m_dialogueChoiceData;
                if (choices != null && choices.Count > 0 && choices[0].m_nextDialogue != null)
                {
                    currentNode = choices[0].m_nextDialogue;
                    continue;
                }
                else
                {
                    break; // No next dialogue, we've reached the end
                }
            }

            // For regular dialogue nodes, check if there's a next dialogue
            var dialogueChoices = currentNode.m_dialogueChoiceData;
            if (
                dialogueChoices != null
                && dialogueChoices.Count > 0
                && dialogueChoices[0].m_nextDialogue != null
            )
            {
                currentNode = dialogueChoices[0].m_nextDialogue;
            }
            else
            {
                // No next dialogue, we've reached the end
                break;
            }
        }

        if (iterations >= maxIterations)
        {
            Debug.LogWarning(
                "[TEST] Reached maximum iterations while skipping dialogue. Possible infinite loop detected."
            );
        }

        // Set the dialogue to the final node we found
        _dialogue.m_dialogue = currentNode;

        // Clear any pending choices and active buttons
        ClearChoices();
        _pendingChoices = null;

        // Stop any audio and typewriter
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }

        if (_typewriter != null)
        {
            _typewriter.StopShowingText();
        }

        // Show the final dialogue or end dialogue buttons
        if (currentNode != null)
        {
            ShowDialogue();
        }
        else
        {
            Debug.Log("[TEST] Reached end of dialogue chain, showing end dialogue buttons");
            ShowEndDialogueButtons();
        }

        Debug.Log($"[TEST] Skipped through {iterations} dialogue nodes to reach the end");
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
            // Ensure notebook is properly connected to the new bachelor
            if (_noteBook != null)
            {
                _noteBook.EnsureBachelorConnection(_bachelor);
            }

            // Ensure all preferences start as undiscovered
            _bachelor.EnsureUndiscoveredState();

            // Use the bachelor's love meter if available
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
        {
            Debug.LogError("[StartDialogue] Bachelor or dialogue is null!");
            return;
        }

        _bachelor = bachelor;
        _dialogue = dialogueSO;

        Debug.Log($"[StartDialogue] Started dialogue with bachelor: {_bachelor._name}");

        // Ensure notebook is properly connected to the new bachelor
        if (_noteBook != null)
        {
            _noteBook.EnsureBachelorConnection(_bachelor);
        }

        // Initialize love meter with bachelor's data
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveMeter = _bachelor._loveMeter;
            _loveScore = _loveMeter.GetCurrentLove();

            // Set up the love meter UI component
            EnsureLoveMeterSetup();
        }
        ShowDialogue();
    }

    /// <summary>
    /// Starts a new dialogue conversation with a bachelor character and sets up date dialogues.
    /// This overloaded version allows date dialogues to be passed from SetBachelor instead of stored in the SO.
    /// </summary>
    /// <param name="bachelor">The bachelor character data</param>
    /// <param name="dialogueSO">The main dialogue to display</param>
    /// <param name="rooftopDateDialogue">Dialogue for rooftop date location</param>
    /// <param name="aquariumDateDialogue">Dialogue for aquarium date location</param>
    /// <param name="forestDateDialogue">Dialogue for forest date location</param>
    public void StartDialogue(
        NewBachelorSO bachelor,
        DSDialogue dialogueSO,
        DSDialogue rooftopDateDialogue,
        DSDialogue aquariumDateDialogue,
        DSDialogue forestDateDialogue
    )
    {
        bachelor._dialogue = dialogueSO;
        if (bachelor == null || bachelor._dialogue == null)
            return;

        _bachelor = bachelor;
        _dialogue = dialogueSO;

        // Set the date dialogues from the parameters instead of the SO
        _rooftopDateDialogue = rooftopDateDialogue;
        _aquariumDateDialogue = aquariumDateDialogue;
        _forestDateDialogue = forestDateDialogue;

        // Ensure notebook is properly connected to the new bachelor
        if (_noteBook != null)
        {
            _noteBook.EnsureBachelorConnection(_bachelor);
        }

        // Initialize love meter with bachelor's data
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveMeter = _bachelor._loveMeter;
            _loveScore = _loveMeter.GetCurrentLove();

            // Set up the love meter UI component
            EnsureLoveMeterSetup();
        }
        ShowDialogue();
    }

    /// <summary>
    /// Shows limited dialogue options for a bachelor who has already been dated.
    /// Only displays "Come Back Later" and "Ask on a Date" options.
    /// </summary>
    /// <param name="bachelor">The bachelor who has already been dated</param>
    public void ShowPostDateOptions(NewBachelorSO bachelor)
    {
        _bachelor = bachelor;

        // Set up the UI
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
        }

        if (_bachelor != null && _bachelor._portrait != null && _bachelorImage != null)
        {
            _bachelorImage.sprite = _bachelor._portrait;
            _bachelorImage.enabled = true;
        }

        if (_displayText != null)
        {
            string greetingText = $"Hey {_bachelor?._name ?? ""}! What would you like to do?";
            _displayText.text = greetingText;

            if (_typewriter != null)
            {
                _typewriter.ShowText(greetingText);
            }
        }

        // Initialize love meter if available
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveMeter = _bachelor._loveMeter;
            _loveScore = _loveMeter.GetCurrentLove();
            EnsureLoveMeterSetup();
        }

        // Ensure notebook connection
        if (_noteBook != null)
        {
            _noteBook.EnsureBachelorConnection(_bachelor);
        }

        // Show the canvas
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = true;
        }

        if (_moveCanvas != null)
        {
            _moveCanvas.enabled = false;
        }

        // Hide continue icon and show post-date options
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

        // Clear any existing choices and show the post-date options
        ClearChoices();
        CreateEndDialogueButton("Come Back Later", OnComeBackLaterClicked);
        CreateEndDialogueButton("Ask on a Date", OnAskOnDateClicked);
    }

    /// <summary>
    /// Shows limited dialogue options for a bachelor who has completed a real date.
    /// Displays personalized message and only "Come Back Later" option.
    /// </summary>
    /// <param name="bachelor">The bachelor who has completed a real date</param>
    public void ShowPostRealDateOptionsInCafe(NewBachelorSO bachelor)
    {
        _bachelor = bachelor;

        // Set up the UI
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
        }

        if (_bachelor != null && _bachelor._portrait != null && _bachelorImage != null)
        {
            _bachelorImage.sprite = _bachelor._portrait;
            _bachelorImage.enabled = true;
        }

        // Show personalized message for real date completion
        if (_displayText != null)
        {
            string personalizedMessage = _bachelor.GetRealDateMessage();
            _displayText.text = personalizedMessage;

            if (_typewriter != null)
            {
                _typewriter.ShowText(personalizedMessage);
            }
        }

        // Initialize love meter if available
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveMeter = _bachelor._loveMeter;
            _loveScore = _loveMeter.GetCurrentLove();
            EnsureLoveMeterSetup();
        }

        // Ensure notebook connection
        if (_noteBook != null)
        {
            _noteBook.EnsureBachelorConnection(_bachelor);
        }

        // Show the canvas
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = true;
        }

        if (_moveCanvas != null)
        {
            _moveCanvas.enabled = false;
        }

        // Hide continue icon and show only Come Back Later option
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

        // Clear any existing choices and show only the Come Back Later option
        ClearChoices();
        CreateEndDialogueButton("Come Back Later", OnComeBackLaterClicked);
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
    /// Supports value checks, love score checks, boolean checks, and preference discovery checks.
    /// </summary>
    private void EvaluateConditionNode()
    {
        try
        {
            DSDialogueSO conditionNode = _dialogue.m_dialogue;
            var operationType = conditionNode.m_operationTypeData;

            bool conditionMet = false;

            Debug.Log($"Evaluating condition operation: {operationType}");

            switch (operationType)
            {
                case SetterOperationType.SetValue:
                    conditionMet = EvaluateValueCondition(conditionNode);
                    break;
                case SetterOperationType.UpdateLoveScore:
                    conditionMet = EvaluateLoveScoreCondition(conditionNode);
                    break;
                case SetterOperationType.UpdateBoolean:
                    conditionMet = EvaluateBooleanCondition(conditionNode);
                    break;
                case SetterOperationType.DiscoverPreference:
                    conditionMet = EvaluatePreferenceCondition(conditionNode);
                    break;
                default:
                    Debug.LogWarning($"Unknown condition operation type: {operationType}");
                    break;
            }
            Debug.Log($"Condition result: {conditionMet}");

            // Follow the single "Next Dialogue" path after evaluating condition
            var choices = conditionNode.m_dialogueChoiceData;
            if (choices != null && choices.Count > 0 && choices[0].m_nextDialogue != null)
            {
                _dialogue.m_dialogue = choices[0].m_nextDialogue;
                ShowDialogue();
            }
            else
            {
                Debug.LogError("No valid next dialogue found after condition node");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error evaluating condition node: {e.Message}");
        }
    }

    /// <summary>
    /// Evaluates a value condition by checking if a variable matches the expected value.
    /// </summary>
    private bool EvaluateValueCondition(DSDialogueSO conditionNode)
    {
        string variableName = conditionNode.m_variableNameData;
        string expectedValue = conditionNode.m_valueToSetData;

        if (!_gameVariables.TryGetValue(variableName, out string currentValue))
        {
            Debug.LogWarning($"Variable {variableName} not found, defaulting to empty string");
            currentValue = "";
        }

        bool result = currentValue == expectedValue;
        Debug.Log(
            $"Value condition: {variableName} == {expectedValue}, Current: {currentValue}, Result: {result}"
        );
        return result;
    }

    /// <summary>
    /// Evaluates a love score condition by checking if the current love score meets the minimum requirement.
    /// </summary>
    private bool EvaluateLoveScoreCondition(DSDialogueSO conditionNode)
    {
        int minimumScore = conditionNode.m_loveScoreAmountData;

        if (_loveMeter != null)
        {
            int currentLove = _loveMeter.GetCurrentLove();
            bool result = currentLove >= minimumScore;
            Debug.Log($"Love score condition: {currentLove} >= {minimumScore}, Result: {result}");
            return result;
        }
        else
        {
            Debug.LogWarning("Love meter is not initialized, condition fails");
            return false;
        }
    }

    /// <summary>
    /// Evaluates a boolean condition by checking if a boolean variable matches the expected value.
    /// </summary>
    private bool EvaluateBooleanCondition(DSDialogueSO conditionNode)
    {
        string boolName = conditionNode.m_variableNameData;
        bool expectedValue = conditionNode.m_boolValueData;

        if (!_gameVariables.TryGetValue(boolName, out string currentValue))
        {
            Debug.LogWarning($"Boolean variable {boolName} not found, defaulting to false");
            currentValue = "false";
        }

        bool currentBool = bool.TryParse(currentValue, out bool parsedValue) ? parsedValue : false;
        bool result = currentBool == expectedValue;
        Debug.Log(
            $"Boolean condition: {boolName} == {expectedValue}, Current: {currentBool}, Result: {result}"
        );
        return result;
    }

    /// <summary>
    /// Evaluates a preference condition by checking if a specific preference has been discovered.
    /// </summary>
    private bool EvaluatePreferenceCondition(DSDialogueSO conditionNode)
    {
        string prefName = conditionNode.m_selectedPreferenceData;
        bool isLike = conditionNode.m_isLikePreferenceData;

        if (_bachelor == null)
        {
            Debug.LogWarning("No bachelor assigned, preference condition fails");
            return false;
        }

        bool isDiscovered = false;
        if (isLike && _bachelor._likes != null)
        {
            for (int i = 0; i < _bachelor._likes.Length; i++)
            {
                if (_bachelor._likes[i].description == prefName)
                {
                    isDiscovered = _bachelor._likes[i].discovered;
                    break;
                }
            }
        }
        else if (!isLike && _bachelor._dislikes != null)
        {
            for (int i = 0; i < _bachelor._dislikes.Length; i++)
            {
                if (_bachelor._dislikes[i].description == prefName)
                {
                    isDiscovered = _bachelor._dislikes[i].discovered;
                    break;
                }
            }
        }

        Debug.Log(
            $"Preference condition: {prefName} ({(isLike ? "Like" : "Dislike")}) is discovered: {isDiscovered}"
        );
        return isDiscovered;
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

            // Show the love meter with animation when score changes
            if (_loveMeterUI != null)
            {
                _loveMeterUI.ShowLoveMeterWithAnimation(
                    _loveScore,
                    () =>
                    {
                        Debug.Log($"Love meter animation completed. New score: {_loveScore}");
                    }
                );
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

        // Show pending choices if there are any (multiple choice dialogue)
        if (_pendingChoices != null && _pendingChoices.Count > 1)
        {
            ShowChoices(_pendingChoices);
            _pendingChoices = null; // Clear pending choices after showing them
        }
        // Show continue icon only if there are no multiple choices (single dialogue)
        else if (_activeChoiceButtons.Count == 0 && _continueIcon != null)
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

        // Create all buttons first, but make them invisible
        List<GameObject> buttonsToAnimate = new List<GameObject>();

        foreach (var choice in choices)
        {
            var btnObj = Instantiate(_choiceButtonPrefab, _choicesParent);
            var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = choice.m_dialogueChoiceText;

            // Ensure proper button sizing and layout
            EnsureContentSizeFitter(btnObj);

            // Set initial alpha to 0 (invisible)
            var canvasGroup = btnObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = btnObj.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;

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
            buttonsToAnimate.Add(btnObj);
        }

        // Start the fade-in animation for all buttons
        StartCoroutine(FadeInChoicesSequentially(buttonsToAnimate));

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

        // Also clear any pending choices
        _pendingChoices = null;
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
        var operationType = conditionNode.m_operationTypeData;

        switch (operationType)
        {
            case SetterOperationType.SetValue:
                return EvaluateValueCondition(conditionNode);
            case SetterOperationType.UpdateLoveScore:
                return EvaluateLoveScoreCondition(conditionNode);
            case SetterOperationType.UpdateBoolean:
                return EvaluateBooleanCondition(conditionNode);
            case SetterOperationType.DiscoverPreference:
                return EvaluatePreferenceCondition(conditionNode);
            default:
                Debug.LogWarning($"Unknown condition operation type: {operationType}");
                return false;
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

    /// <summary>
    /// Coroutine that fades in choice buttons one by one with a smooth animation.
    /// Creates a sequential reveal effect for dialogue choices.
    /// </summary>
    /// <param name="buttons">List of button GameObjects to animate</param>
    /// <returns>Coroutine enumerator</returns>
    private System.Collections.IEnumerator FadeInChoicesSequentially(List<GameObject> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                var canvasGroup = buttons[i].GetComponent<CanvasGroup>();
                var button = buttons[i].GetComponent<UnityEngine.UI.Button>();

                if (canvasGroup != null)
                {
                    // Disable button interaction during fade-in
                    if (button != null && button.interactable)
                    {
                        canvasGroup.interactable = false;
                    }

                    // Animate alpha from 0 to 1 over the fade duration
                    float elapsedTime = 0f;
                    while (elapsedTime < _choiceFadeDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float alpha = Mathf.Clamp01(elapsedTime / _choiceFadeDuration);
                        canvasGroup.alpha = alpha;
                        yield return null;
                    }

                    // Ensure final alpha is exactly 1 and re-enable interaction
                    canvasGroup.alpha = 1f;
                    if (button != null && button.interactable)
                    {
                        canvasGroup.interactable = true;
                    }
                }
            }

            // Wait before showing the next button (except for the last one)
            if (i < buttons.Count - 1)
            {
                yield return new WaitForSeconds(_choiceAppearDelay);
            }
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
                    Debug.Log($"Discovered like: {preferenceName}"); // Update game variables for newly discovered preference
                    _gameVariables["LikeDiscovered"] = "true";
                    if (_noteBook != null)
                    {
                        _gameVariables["NotebookLikeEntry"] = "true";
                    }
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
                    Debug.Log($"Discovered dislike: {preferenceName}"); // Update game variables for newly discovered preference
                    _gameVariables["DislikeDiscovered"] = "true";
                    if (_noteBook != null)
                    {
                        _gameVariables["NotebookDislikeEntry"] = "true";
                    }
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
    /// Uses boolean flag to determine if showing post-real-date options or regular dialogue options.
    /// </summary>
    private void ShowEndDialogueButtons()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        string sceneLower = currentSceneName.ToLower();

        // Check if we just completed a real date - use boolean flag instead of scene detection
        if (_justCompletedRealDate)
        {
            Debug.Log(
                "Just completed real date. Showing Good/Bad Date result screen, then proceeding to barista."
            );
            ClearChoices();

            // Trigger the Good/Bad Date result screen immediately
            StartCoroutine(ShowDateResultAndProceedToBarista());
            return;
        }

        // Check if we're in the FirstDate scene AND this is truly the last dialogue
        bool isFirstDateScene = sceneLower.Contains("firstdate");
        bool isLastDialogue =
            _dialogue?.m_dialogue?.m_dialogueChoiceData == null
            || _dialogue.m_dialogue.m_dialogueChoiceData.Count == 0
            || _dialogue.m_dialogue.m_dialogueChoiceData[0].m_nextDialogue == null;

        if (isFirstDateScene && isLastDialogue)
        {
            Debug.Log(
                $"At last dialogue in FirstDate Scene ({currentSceneName}). Showing Enter Cafe button."
            );
            ClearChoices();
            CreateEnterCafeButton();
            return;
        }
        else if (isFirstDateScene && !isLastDialogue)
        {
            Debug.Log(
                $"In FirstDate Scene ({currentSceneName}) but not at last dialogue. No buttons shown."
            );
            return;
        }

        bool isCafeScene =
            sceneLower.Contains("cafe")
            || sceneLower.Contains("cafescene")
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

        // Only create "Ask on a Date" button if bachelor has enough love for a real date
        if (
            _bachelor != null
            && _bachelor._loveMeter != null
            && _bachelor._loveMeter.CanGoOnRealDate()
        )
        {
            CreateEndDialogueButton("Ask on a Date", OnAskOnDateClicked);
            Debug.Log(
                $"Ask on a Date button shown - {_bachelor._name} has {_bachelor._loveMeter.GetCurrentLove()}/{_bachelor._loveMeter._loveNeededForRealDate} love"
            );
        }
        else
        {
            Debug.Log(
                $"Ask on a Date button hidden - {_bachelor?.name ?? "Unknown"} needs more love (Current: {_bachelor?._loveMeter?.GetCurrentLove() ?? 0}, Required: {_bachelor?._loveMeter?._loveNeededForRealDate ?? 0})"
            );
        }
    }

    /// <summary>
    /// Shows the post-real-date options with no message and only Come Back Later button
    /// Used at the end of real date scenes (aquarium, forest, rooftop)
    /// </summary>
    private void ShowPostRealDateOptions()
    {
        Debug.Log("Showing post-real-date options at end of real date scene");

        // Clear the display text - no message shown at the end of real date scenes
        if (_displayText != null)
        {
            _displayText.text = "";

            if (_typewriter != null)
            {
                _typewriter.ShowText("");
            }
        }
        // Hide continue icon and show only the Come Back Later option
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

        // Only show "Back to Cafe" button for real dates
        CreateEndDialogueButton("Back to Cafe", OnComeBackLaterClicked);
    }

    /// <summary>
    /// Handles the "Come Back Later" button click after a real date
    /// </summary>
    private void OnPostRealDateComeBackLaterClicked()
    {
        Debug.Log(
            $"[OnPostRealDateComeBackLaterClicked] Come Back Later button clicked after real date. Bachelor: {(_bachelor != null ? _bachelor._name : "null")}, Location: {_currentRealDateLocation}"
        );

        // Mark bachelor as real dated and save progress
        if (_bachelor != null)
        {
            Debug.Log(
                $"[OnPostRealDateComeBackLaterClicked] About to mark {_bachelor._name} as real dated at {_currentRealDateLocation}"
            );
            MarkBachelorAsRealDated(_bachelor);
            IncrementRealDateCount();
        }
        else
        {
            Debug.LogError(
                "[OnPostRealDateComeBackLaterClicked] Bachelor is null when trying to save real date progress!"
            );
        }

        // Clear notebook
        if (_noteBook != null)
        {
            _noteBook.ClearBachelor();
        }

        // End the date session
        EndDate();

        // Return to the cafe scene
        SceneManager.LoadScene("CafeScene"); // Adjust scene name as needed
    }

    /// <summary>
    /// Handles the "Return to Cafe" button click after a real date
    /// </summary>
    private void OnReturnToCafeClicked()
    {
        Debug.Log("Return to Cafe button clicked");

        // Mark bachelor as real dated and save progress
        if (_bachelor != null)
        {
            MarkBachelorAsRealDated(_bachelor);
            IncrementRealDateCount();
        }

        // Clear notebook
        if (_noteBook != null)
        {
            _noteBook.ClearBachelor();
        }

        // End the date session
        EndDate();

        // Load the cafe scene
        SceneManager.LoadScene("CafeScene"); // Adjust scene name as needed
    }

    /// <summary>
    /// Handles the "Back to Menu" button click after a real date
    /// </summary>
    private void OnBackToMenuClicked()
    {
        Debug.Log("Back to Menu button clicked");

        // Mark bachelor as real dated and save progress
        if (_bachelor != null)
        {
            MarkBachelorAsRealDated(_bachelor);
            IncrementRealDateCount();
        }

        // Clear notebook
        if (_noteBook != null)
        {
            _noteBook.ClearBachelor();
        }

        // Turn off all date backgrounds before leaving
        TurnOffAllDateBackgrounds();

        // End the date session
        EndDate();

        // Load the main menu scene
        SceneManager.LoadScene("Main Menu"); // Adjust scene name as needed
    }

    /// <summary>
    /// Handles the "Quit Game" button click after a real date
    /// </summary>
    private void OnQuitGameClicked()
    {
        Debug.Log("Quit Game button clicked");

        // Mark bachelor as real dated and save progress
        if (_bachelor != null)
        {
            MarkBachelorAsRealDated(_bachelor);
            IncrementRealDateCount();
        }

        // Clear notebook
        if (_noteBook != null)
        {
            _noteBook.ClearBachelor();
        }

        // Turn off all date backgrounds before quitting
        TurnOffAllDateBackgrounds();

        // End the date session
        EndDate();

        // Quit the application
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
    /// Creates the "Enter Cafe" button specifically for the FirstDate scene
    /// </summary>
    private void CreateEnterCafeButton()
    {
        var btnObj = Instantiate(_choiceButtonPrefab, _choicesParent);
        var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = "Enter Cafe";

        EnsureContentSizeFitter(btnObj);

        var button = btnObj.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnNextSceneClicked());

            if (btnText != null)
                btnText.color = _normalTextColor;

            AddHoverEffects(btnObj, btnText);
        }

        _activeChoiceButtons.Add(btnObj);
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

        Debug.Log(
            $"[FinishDialogue] Current scene: {currentSceneName}, IsDateScene: {isDateScene}, Bachelor: {(_bachelor != null ? _bachelor._name : "null")}"
        );

        if (isDateScene)
        {
            IncrementSuccessfulDateCount();
            Debug.Log("Date completed successfully!");
            // Mark bachelor as dated and disable their setter
            if (_bachelor != null)
            {
                Debug.Log(
                    $"[FinishDialogue] About to mark {_bachelor._name} as dated in scene {currentSceneName}"
                );
                MarkBachelorAsDated(_bachelor);
            }
            else
            {
                Debug.LogError(
                    "[FinishDialogue] Bachelor is null when trying to save in date scene!"
                );
            }
        }

        // Remove bachelor from notebook after date is over
        if (_noteBook != null)
        {
            _noteBook.ClearBachelor();
        }

        // End the date session to reset bachelor interaction states
        EndDate();

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
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                SceneManager.LoadScene("Main Menu");
            }
        }
    }

    /// <summary>
    /// Handles the "Come Back Later" button click.
    /// Returns to the main menu and hides the dialogue display.
    /// If coming from a real date, handles post-real-date cleanup.
    /// </summary>
    private void OnComeBackLaterClicked()
    {
        Debug.Log("Come Back Later button clicked");
        ClearChoices();

        // Check if this is coming from a real date
        if (_justCompletedRealDate)
        {
            Debug.Log("Returning from real date - handling post-real-date cleanup");

            // Mark bachelor as real dated and save progress
            if (_bachelor != null)
            {
                MarkBachelorAsRealDated(_bachelor);
                IncrementRealDateCount();
            }

            // Clear notebook
            if (_noteBook != null)
            {
                _noteBook.ClearBachelor();
            }

            // Reset the flags and clear location
            _justCompletedRealDate = false;
            _currentRealDateLocation = "";

            // Turn off all date backgrounds
            TurnOffAllDateBackgrounds();

            // End the date session
            EndDate();
            return;
        }

        // Regular "Come Back Later" logic for cafe scenes
        Debug.Log("Come Back Later button clicked - returning to main menu");

        // Turn off all date backgrounds to return to cafe scene
        TurnOffAllDateBackgrounds();

        // End the date session to reset bachelor interaction states
        EndDate();

        gameObject.SetActive(false);
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = false;
        }
        // Enable all BachelorSetter canvases in the scene (for cafe re-entry)
        var setters = FindObjectsByType<BachelorSetter>(FindObjectsSortMode.None);
        foreach (var setter in setters)
        {
            if (setter != null)
            {
                setter.EnableCanvas();
            }
        }
    }

    /// <summary>
    /// Handles the "Ask on a Date" button click.
    /// Shows asking on a date text and then presents three date location options.
    /// </summary>
    private void OnAskOnDateClicked()
    {
        Debug.Log("Ask on a Date button clicked");
        ClearChoices();

        // Show the asking on a date text
        ShowAskOnDateText();
    }

    /// <summary>
    /// Shows the text for asking the bachelor on a date and then displays location options.
    /// </summary>
    private void ShowAskOnDateText()
    {
        // Display asking on a date text
        if (_displayText != null)
        {
            string askDateText =
                $"Would you like to go on a date with me, {_bachelor?._name ?? ""}?";
            _displayText.text = askDateText;

            if (_typewriter != null)
            {
                _typewriter.ShowText(askDateText);
            }
        }

        // Hide continue icon since we'll show choices
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

        // Wait a moment then show date location options
        StartCoroutine(ShowDateLocationOptionsAfterDelay());
    }

    /// <summary>
    /// Coroutine that waits a moment then shows the three date location options.
    /// </summary>
    private System.Collections.IEnumerator ShowDateLocationOptionsAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        ShowDateLocationOptions();
    }

    /// <summary>
    /// Shows the three date location options: Rooftop, Aquarium, and Forest.
    /// </summary>
    private void ShowDateLocationOptions()
    {
        ClearChoices();

        CreateDateLocationButton("Rooftop", OnRooftopDateSelected);
        CreateDateLocationButton("Aquarium", OnAquariumDateSelected);
        CreateDateLocationButton("Forest", OnForestDateSelected);
    }

    /// <summary>
    /// Creates a date location selection button with consistent styling.
    /// </summary>
    private void CreateDateLocationButton(string locationName, System.Action onClickAction)
    {
        var btnObj = Instantiate(_choiceButtonPrefab, _choicesParent);
        var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = locationName;

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
    /// Handles selection of the Rooftop date location.
    /// </summary>
    private void OnRooftopDateSelected()
    {
        Debug.Log("Rooftop date selected");
        SetDateBackground("Rooftop");
        StartDateWithLocation("Rooftop", _rooftopDateDialogue);
    }

    /// <summary>
    /// Handles selection of the Aquarium date location.
    /// </summary>
    private void OnAquariumDateSelected()
    {
        Debug.Log("Aquarium date selected");
        SetDateBackground("Aquarium");
        StartDateWithLocation("Aquarium", _aquariumDateDialogue);
    }

    /// <summary>
    /// Handles selection of the Forest date location.
    /// </summary>
    private void OnForestDateSelected()
    {
        Debug.Log("Forest date selected");
        SetDateBackground("Forest");
        StartDateWithLocation("Forest", _forestDateDialogue);
    }

    /// <summary>
    /// Starts a date with the specified location and dialogue.
    /// </summary>
    private void StartDateWithLocation(string locationName, DSDialogue dateDialogue)
    {
        if (dateDialogue == null)
        {
            Debug.LogError($"No dialogue assigned for {locationName} date!");
            return;
        }

        // Set the date dialogue
        _dialogue = dateDialogue;

        // Set the flag to indicate we're starting a real date and store the location
        _justCompletedRealDate = true;

        // Store the date location in a variable we can access later
        _currentRealDateLocation = locationName;
        Debug.Log($"Starting real date at {locationName}. Flag set to true.");

        // Clear choices and start the date dialogue
        ClearChoices();
        ShowDialogue();
    }

    /// <summary>
    /// Marks a bachelor as having been dated and saves the data.
    /// </summary>
    private void MarkBachelorAsDated(NewBachelorSO bachelor)
    {
        if (bachelor == null)
        {
            Debug.LogError("[MarkBachelorAsDated] Bachelor is null! Cannot save dating progress.");
            return;
        }

        Debug.Log($"[MarkBachelorAsDated] About to mark {bachelor._name} as dated");

        // Mark in the ScriptableObject (this also handles saving to the save system)
        bachelor.MarkAsDated();

        Debug.Log(
            $"[MarkBachelorAsDated] Successfully marked {bachelor._name} as dated and saved progress"
        );
    }

    /// <summary>
    /// Marks a bachelor as having completed a real date (aquarium, forest, rooftop) and saves the data.
    /// </summary>
    private void MarkBachelorAsRealDated(NewBachelorSO bachelor)
    {
        if (bachelor == null)
        {
            Debug.LogError(
                "[MarkBachelorAsRealDated] Bachelor is null! Cannot save real dating progress."
            );
            return;
        }

        if (string.IsNullOrEmpty(_currentRealDateLocation))
        {
            Debug.LogError(
                $"[MarkBachelorAsRealDated] Current real date location is empty! Cannot save location for {bachelor._name}"
            );
            return;
        }

        Debug.Log(
            $"[MarkBachelorAsRealDated] About to mark {bachelor._name} as real dated at {_currentRealDateLocation}"
        );

        // Mark in the ScriptableObject with the date location (this also handles saving to the save system)
        bachelor.MarkAsRealDated(_currentRealDateLocation);

        Debug.Log(
            $"[MarkBachelorAsRealDated] Successfully marked {bachelor._name} as real dated at {_currentRealDateLocation} and saved progress"
        );
    }

    /// <summary>
    /// Checks if a bachelor has completed a real date (not just a speed date)
    /// </summary>
    private bool HasCompletedRealDate(NewBachelorSO bachelor)
    {
        if (bachelor == null)
            return false;

        // Use the bachelor's own method which handles both local flags and save data consistency
        return bachelor.HasCompletedRealDate();
    }

    /// <summary>
    /// Increments the successful date count and saves the data.
    /// </summary>
    private void IncrementSuccessfulDateCount()
    {
        _speedDateCount++;
        Debug.Log($"Successful date count incremented to: {_speedDateCount}");
    }

    /// <summary>
    /// Increments the real date count specifically for real dates (aquarium, forest, rooftop).
    /// /// </summary>
    private void IncrementRealDateCount()
    {
        _realDateCount++;
        Debug.Log($"Real date count incremented to: {_realDateCount}");
    }

    /// <summary>
    /// Resets all SetBachelor dating states when a date session ends.
    /// </summary>
    private void ResetAllBachelorStates()
    {
        // Find all SetBachelor components in the scene and reset their dating state
        SetBachelor[] setBachelors = FindObjectsByType<SetBachelor>(FindObjectsSortMode.None);
        foreach (var setBachelor in setBachelors)
        {
            if (setBachelor != null)
            {
                setBachelor.ResetDatingState();
            }
        }
        Debug.Log("Date session ended and bachelor states reset");
    }

    /// <summary>
    /// Loads the successful date count from save data.
    /// </summary>
    private void LoadSuccessfulDateCountFromSave()
    {
        // Load from save system
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null)
        {
            // Count successful dates (speed dates) from DatedBachelors list
            _speedDateCount = saveData.DatedBachelors?.Count ?? 0;

            // Count real dates from RealDatedBachelors list
            _realDateCount = saveData.RealDatedBachelors?.Count ?? 0;

            Debug.Log(
                $"Loaded from save data - Successful dates: {_speedDateCount}, Real dates: {_realDateCount}"
            );
        }
        else
        {
            // No save data exists, initialize to 0
            _speedDateCount = 0;
            _realDateCount = 0;
            Debug.Log("No save data found, initialized date counts to 0");
        }
    }

    /// <summary>
    /// Ends the current date and performs cleanup.
    /// </summary>
    private void EndDate()
    {
        ResetAllBachelorStates();

        // Turn off all date backgrounds when ending a date
        TurnOffAllDateBackgrounds();

        // Hide dialogue UI
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = false;
        }

        // Re-enable movement if move canvas exists
        if (_moveCanvas != null)
        {
            _moveCanvas.enabled = true;
        }
    }

    #region Background Management
    /// <summary>
    /// Turns off all date background images to ensure clean state
    /// </summary>
    private void TurnOffAllDateBackgrounds()
    {
        if (_rooftopBackground != null)
        {
            _rooftopBackground.gameObject.SetActive(false);
        }

        if (_aquariumBackground != null)
        {
            _aquariumBackground.gameObject.SetActive(false);
        }

        if (_forestBackground != null)
        {
            _forestBackground.gameObject.SetActive(false);
        }

        _currentBackground = null;
        Debug.Log("All date backgrounds turned off");
    }

    /// <summary>
    /// Sets the active background for the specified date location
    /// </summary>
    /// <param name="location">The date location (Rooftop, Aquarium, or Forest)</param>
    private void SetDateBackground(string location)
    {
        // First turn off all backgrounds
        TurnOffAllDateBackgrounds();

        // Then turn on the appropriate background
        switch (location.ToLower())
        {
            case "rooftop":
                if (_rooftopBackground != null)
                {
                    _rooftopBackground.gameObject.SetActive(true);
                    _currentBackground = _rooftopBackground;
                    Debug.Log("Rooftop background activated");
                }
                break;

            case "aquarium":
                if (_aquariumBackground != null)
                {
                    _aquariumBackground.gameObject.SetActive(true);
                    _currentBackground = _aquariumBackground;
                    Debug.Log("Aquarium background activated");
                }
                break;

            case "forest":
                if (_forestBackground != null)
                {
                    _forestBackground.gameObject.SetActive(true);
                    _currentBackground = _forestBackground;
                    Debug.Log("Forest background activated");
                }
                break;

            default:
                Debug.LogWarning($"Unknown date location: {location}");
                break;
        }
    }
    #endregion

    #region Barista Event
    /// <summary>
    /// Shows the barista event in the cafe after a real date, with no background, displaying dialogue based on number of completed real dates.
    /// No ending buttons are created for barista dialogues.
    /// </summary>
    private IEnumerator ShowBaristaAfterRealDate()
    {
        // Hide all date backgrounds to return to cafe environment
        TurnOffAllDateBackgrounds();

        // Clear any existing choices/buttons to ensure no buttons are shown
        ClearChoices();

        // Ensure we're back in cafe environment - restore basic UI elements
        RestoreCafeEnvironment();

        // Get the dialogue index based on real date count (0-indexed)
        int dialogueIndex = Mathf.Max(0, _realDateCount - 1);

        // Safety check: if no barista dialogues exist, don't delete save file
        if (_baristaDialogues == null || _baristaDialogues.Count == 0)
        {
            Debug.LogWarning("No barista dialogues configured. Skipping barista event.");

            // Close dialogue UI and return to cafe normally
            if (_dialogueCanvas != null)
            {
                _dialogueCanvas.enabled = false;
            }
            yield break;
        }

        // Determine if the date was successful or failed
        bool isGoodDate = _loveScore >= _loveNeededForSuccefulDate;
        DSDialogue baristaDialogue = null;
        bool isAllDateFailedDialogue = false;

        // Check if we're at the final dialogue and determine which special dialogue to use
        bool isAtFinalDialogue = dialogueIndex >= _baristaDialogues.Count - 1;

        // If we're at the final dialogue position, check for special ending scenarios
        if (isAtFinalDialogue)
        {
            if (AllDatesFailed() && _allDateFailedBaristaDialogue != null)
            {
                baristaDialogue = _allDateFailedBaristaDialogue;
                isAllDateFailedDialogue = true;
                Debug.Log(
                    "Using 'all dates failed' barista dialogue - triggering game end sequence"
                );
            }
            else if (HasMixedResults() && _mixedResultsBaristaDialogue != null)
            {
                baristaDialogue = _mixedResultsBaristaDialogue;
                isAllDateFailedDialogue = true; // This also triggers game end
                Debug.Log("Using 'mixed results' barista dialogue - triggering game end sequence");
            }
            // If all dates succeeded, fall through to use regular "good job" dialogue
        }
        // If date failed, use the bad date barista dialogue (for individual date failures)
        else if (!isGoodDate && _badDateBaristaDialogue != null)
        {
            baristaDialogue = _badDateBaristaDialogue;
            Debug.Log(
                $"Using bad date barista dialogue (Love: {_loveScore}/{_loveNeededForSuccefulDate})"
            );
        }
        // If date succeeded, use the regular barista dialogue based on real date count
        else if (
            dialogueIndex < _baristaDialogues.Count
            && _baristaDialogues[dialogueIndex] != null
        )
        {
            baristaDialogue = _baristaDialogues[dialogueIndex];
            if (isAtFinalDialogue)
            {
                Debug.Log(
                    $"Using final 'all dates succeeded' barista dialogue after {_realDateCount} successful real dates"
                );
            }
            else
            {
                Debug.Log(
                    $"Using regular barista dialogue #{dialogueIndex + 1} after {_realDateCount} real dates"
                );
            }
        }

        // Check if we have a dialogue to show
        if (baristaDialogue != null)
        {
            // Use the selected dialogue for this date result

            // Set the barista name
            if (_nameText != null)
            {
                _nameText.text = "Barista";
            }

            // Set the bachelor image from the dialogue data
            if (baristaDialogue.m_dialogue != null && _bachelorImage != null)
            {
                Debug.Log(
                    $"Setting up barista image. Current enabled state: {_bachelorImage.enabled}"
                );

                // If barista dialogue has a specific image, use it
                if (baristaDialogue.m_dialogue.m_bachelorImageData != null)
                {
                    _bachelorImage.sprite = baristaDialogue.m_dialogue.m_bachelorImageData;
                    Debug.Log("Set barista sprite from dialogue data");
                }
                else
                {
                    Debug.Log(
                        "No specific barista sprite in dialogue data, keeping current sprite"
                    );
                }

                // Always ensure the image is visible for barista dialogue
                _bachelorImage.color = new Color(
                    _bachelorImage.color.r,
                    _bachelorImage.color.g,
                    _bachelorImage.color.b,
                    1f
                );
                _bachelorImage.enabled = true;
                Debug.Log(
                    $"Barista image enabled and made visible. Final state - Enabled: {_bachelorImage.enabled}, Alpha: {_bachelorImage.color.a}"
                );
            }
            else if (_bachelorImage != null)
            {
                // Fallback: ensure image is enabled even without dialogue data
                _bachelorImage.enabled = true;
                _bachelorImage.color = new Color(
                    _bachelorImage.color.r,
                    _bachelorImage.color.g,
                    _bachelorImage.color.b,
                    1f
                );
                Debug.Log("Barista image enabled as fallback (no dialogue data)");
            }
            else
            {
                Debug.LogWarning("Bachelor image component is null - barista won't be visible!");
            }

            // Hide continue icon initially
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(false);
            }
            if (_loveMeterUI != null)
            {
                _loveMeterUI.HideLoveMeter();
            }

            // Show the dialogue canvas
            if (_dialogueCanvas != null)
            {
                _dialogueCanvas.enabled = true;
            }

            // Display the barista dialogue
            if (baristaDialogue.m_dialogue != null && _displayText != null)
            {
                _displayText.text = baristaDialogue.m_dialogue.m_dialogueTextData;

                if (_typewriter != null)
                {
                    _typewriter.ShowText(_displayText.text);

                    // Wait for typewriter to finish
                    yield return new WaitUntil(() => !_typewriter.isShowingText);
                }

                Debug.Log(
                    $"Showing barista dialogue #{dialogueIndex + 1} after {_realDateCount} real dates: {_displayText.text}"
                );
            }
        }
        else
        {
            // Fallback to generic message if no dialogue is available
            if (_displayText != null)
            {
                if (isGoodDate)
                {
                    _displayText.text =
                        $"The barista approaches you after your successful real date #{_realDateCount}.";
                }
                else
                {
                    _displayText.text =
                        "The barista notices you didn't have the best time on your date.";
                }
            }
            if (_nameText != null)
            {
                _nameText.text = "Barista";
            }
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(false);
            }
            if (_loveMeterUI != null)
            {
                _loveMeterUI.HideLoveMeter();
            }
            if (_dialogueCanvas != null)
            {
                _dialogueCanvas.enabled = true;
            }

            Debug.LogWarning(
                $"No barista dialogue found for date result (Success: {isGoodDate}). Using fallback message."
            );
        }

        // Fixed condition: delete save if we reached the last configured dialogue OR if it's the "all dates failed" dialogue
        bool isLastDialogue =
            (dialogueIndex >= _baristaDialogues.Count - 1) || isAllDateFailedDialogue;

        if (isLastDialogue || isAllDateFailedDialogue)
        {
            if (isAllDateFailedDialogue)
            {
                Debug.Log("All dates failed dialogue completed. Ending game and deleting save.");
            }
            else
            {
                Debug.Log(
                    "Last barista dialogue completed. Waiting for player input before ending game."
                );
            }

            // Add delay like regular dialogue
            yield return new WaitForSeconds(1f);

            // Show continue icon for final dialogue too
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(true);
            }

            // Wait for player input before ending
            yield return new WaitUntil(
                () => (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            );

            // Hide continue icon when advancing
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(false);
            }

            // Reset all bachelor states before ending the game
            ResetAllBachelorStates();

            // Delete the save file immediately
            DeleteSaveFile();

            // Load the main menu scene immediately without waiting
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
        }
        else
        {
            // Wait for spacebar input like regular dialogue for non-final dialogues
            _canAdvance = false;
            _isDelayActive = false;

            // Add delay like regular dialogue
            yield return new WaitForSeconds(1f);

            // Show continue icon to indicate player can advance
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(true);
            }

            // Wait for player input (spacebar or mouse click)
            yield return new WaitUntil(
                () => (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            );

            // Hide continue icon when advancing
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(false);
            }

            // Close the dialogue UI for non-final dialogues
            if (_dialogueCanvas != null)
            {
                _dialogueCanvas.enabled = false;
            }

            // Reset all bachelor states after barista dialogue completes (for non-final dialogues)
            ResetAllBachelorStates();
        }
    }

    /// <summary>
    /// Call this after a real date is completed to trigger the barista event.
    /// </summary>
    public void TriggerBaristaAfterRealDate()
    {
        StartCoroutine(ShowBaristaAfterRealDate());
    }

    /// <summary>
    /// Deletes the player's save file completely.
    /// </summary>
    private void DeleteSaveFile()
    {
        try
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                Debug.Log($"Save file deleted: {path}");
            }
            else
            {
                Debug.Log("No save file found to delete.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
        }
    }
    #endregion

    #region Date Result Handling
    /// <summary>
    /// Shows the Good Date or Bad Date screen after a real date, hides only Move Canvas and Bachelor Image, and proceeds to barista dialogue after 3 seconds.
    /// Uses DOTween for smooth fade-in and fade-out effects.
    /// </summary>
    private IEnumerator ShowDateResultAndProceedToBarista()
    {
        // Turn off all date backgrounds immediately when result screen starts
        TurnOffAllDateBackgrounds();

        // Hide only Move Canvas, Bachelor Image, and Continue Icon
        if (_moveCanvas != null)
            _moveCanvas.enabled = false;
        if (_bachelorImage != null)
            _bachelorImage.enabled = false;
        if (_continueIcon != null)
            _continueIcon.SetActive(false);

        // Determine which screen to show
        bool isGoodDate = _loveScore >= _loveNeededForSuccefulDate;
        GameObject screenToShow = isGoodDate ? _goodDateScreen : _badDateScreen;

        Debug.Log(
            $"Showing {(isGoodDate ? "Good" : "Bad")} Date screen with fade effects (Love: {_loveScore}/{_loveNeededForSuccefulDate})"
        );

        if (screenToShow != null)
        {
            // Get or add CanvasGroup for fade effects
            CanvasGroup canvasGroup = screenToShow.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = screenToShow.AddComponent<CanvasGroup>();
            }

            // Set initial alpha to 1 (fully visible) and activate the screen instantly
            canvasGroup.alpha = 1f;
            screenToShow.SetActive(true);

            // Wait for 2 seconds (showing the screen at full opacity)
            yield return new WaitForSeconds(2f);

            // Fade out over 1 second (using basic coroutine instead of DOTween)
            float fadeDuration = 1f;
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;

            // Deactivate the screen
            screenToShow.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"No {(isGoodDate ? "Good" : "Bad")} Date screen assigned!");
            // Fallback: wait for 3 seconds without showing anything
            yield return new WaitForSeconds(3f);
        }

        // Reset the real date flag and clear location
        _justCompletedRealDate = false;
        _currentRealDateLocation = "";

        // Turn off all date backgrounds when ending a date
        TurnOffAllDateBackgrounds();

        // Hide dialogue UI temporarily (barista will re-enable it)
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = false;
        }

        // Restore the Move Canvas but keep Bachelor Image hidden for barista dialogue
        if (_moveCanvas != null)
            _moveCanvas.enabled = true;

        // Immediately trigger the barista dialogue (backgrounds already turned off)
        // Note: Bachelor states are NOT reset here - they'll be reset after barista dialogue
        TriggerBaristaAfterRealDate();
    }
    #endregion

    /// <summary>
    /// Determines if all dates have failed (no successful dates)
    /// </summary>
    private bool AllDatesFailed()
    {
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null || saveData.RealDatedBachelors == null)
            return false;

        // Check if all 5 real dates have been completed
        int realDatedCount = saveData.RealDatedBachelors.Count;
        if (realDatedCount < 5)
            return false;

        // Get all bachelors in the scene to check their love scores
        SetBachelor[] allSetBachelors = FindObjectsByType<SetBachelor>(FindObjectsSortMode.None);
        if (allSetBachelors == null || allSetBachelors.Length == 0)
            return false;

        // Count successful dates
        int successfulDates = 0;

        foreach (var setBachelor in allSetBachelors)
        {
            if (setBachelor != null)
            {
                NewBachelorSO bachelor = setBachelor.GetBachelor();
                if (bachelor != null && bachelor._loveMeter != null)
                {
                    // Check if this bachelor was real dated
                    if (bachelor.HasCompletedRealDate())
                    {
                        int currentLove = bachelor._loveMeter.GetCurrentLove();
                        if (currentLove >= _loveNeededForSuccefulDate)
                        {
                            successfulDates++;
                        }
                    }
                }
            }
        }

        // All dates failed if no successful dates
        bool allFailed = successfulDates == 0;
        Debug.Log(
            $"All dates failed check: Real dated: {realDatedCount}/5, Successful: {successfulDates}, All failed: {allFailed}"
        );

        return realDatedCount >= 5 && allFailed;
    }

    /// <summary>
    /// Determines if there are mixed results (some successful, some failed)
    /// </summary>
    private bool HasMixedResults()
    {
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null || saveData.RealDatedBachelors == null)
            return false;

        // Check if all 5 real dates have been completed
        int realDatedCount = saveData.RealDatedBachelors.Count;
        if (realDatedCount < 5)
            return false;

        // Get all bachelors in the scene to check their love scores
        SetBachelor[] allSetBachelors = FindObjectsByType<SetBachelor>(FindObjectsSortMode.None);
        if (allSetBachelors == null || allSetBachelors.Length == 0)
            return false;

        // Count successful and failed dates
        int successfulDates = 0;
        int failedDates = 0;

        foreach (var setBachelor in allSetBachelors)
        {
            if (setBachelor != null)
            {
                NewBachelorSO bachelor = setBachelor.GetBachelor();
                if (bachelor != null && bachelor._loveMeter != null)
                {
                    // Check if this bachelor was real dated
                    if (bachelor.HasCompletedRealDate())
                    {
                        int currentLove = bachelor._loveMeter.GetCurrentLove();
                        if (currentLove >= _loveNeededForSuccefulDate)
                        {
                            successfulDates++;
                        }
                        else
                        {
                            failedDates++;
                        }
                    }
                }
            }
        }

        // Mixed results if both successful and failed dates exist
        bool mixedResults = successfulDates > 0 && failedDates > 0;
        Debug.Log(
            $"Mixed results check: Real dated: {realDatedCount}/5, Successful: {successfulDates}, Failed: {failedDates}, Mixed: {mixedResults}"
        );

        return realDatedCount >= 5 && mixedResults;
    }

    /// <summary>
    /// Restores the cafe environment after a date by ensuring UI elements are properly configured
    /// </summary>
    private void RestoreCafeEnvironment()
    {
        Debug.Log("Restoring cafe environment after date");

        // Ensure move canvas is enabled (player should be able to move in cafe)
        if (_moveCanvas != null)
        {
            _moveCanvas.enabled = true;
            Debug.Log("Move canvas enabled");
        }

        // Ensure dialogue canvas is ready for barista dialogue
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = true;
            Debug.Log("Dialogue canvas enabled");
        }

        // Ensure bachelor image is enabled and ready for barista sprite
        if (_bachelorImage != null)
        {
            _bachelorImage.enabled = true;
            _bachelorImage.color = new Color(
                _bachelorImage.color.r,
                _bachelorImage.color.g,
                _bachelorImage.color.b,
                1f
            );
            Debug.Log("Bachelor image enabled and made visible for barista");
        }

        // Hide any leftover UI elements from the date
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

        // Hide love meter as it's not relevant for barista dialogue
        if (_loveMeterUI != null)
        {
            _loveMeterUI.HideLoveMeter();
        }

        Debug.Log("Cafe environment restored - all backgrounds off, UI elements ready");
    }
}
    #endregion
