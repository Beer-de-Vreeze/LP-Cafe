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

    /// <summary>Reference to the SetBachelor component that manages bachelor initialization</summary>
    private SetBachelor _setBachelor;
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

    /// <summary>Tracks whether the dialogue system is waiting for user input to clear text before showing end buttons</summary>
    private bool _waitingForClearBeforeEndButtons = false;

    /// <summary>Tracks whether end dialogue buttons have already been shown to prevent chaining</summary>
    private bool _endDialogueButtonsShown = false;
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

    /// <summary>List of barista dialogues to display after real dates, indexed by number of completed real dates. Does not include special ending dialogues.</summary>
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

    /// <summary>Barista dialogue to display when all dates have succeeded, triggers game end</summary>
    [SerializeField]
    private DSDialogue _allDatesSucceededBaristaDialogue;
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

        // Turn off good/bad date screens initially
        if (_goodDateScreen != null)
        {
            _goodDateScreen.SetActive(false);
        }
        if (_badDateScreen != null)
        {
            _badDateScreen.SetActive(false);
        }
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

        // Handle clearing text before showing end buttons
        if (
            _waitingForClearBeforeEndButtons
            && !_endDialogueButtonsShown
            && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        )
        {
            _waitingForClearBeforeEndButtons = false;
            _canAdvance = false;

            // Clear the text
            if (_displayText != null)
            {
                _displayText.text = "";
            }

            // Hide continue icon
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(false);
            }

            // Now show the end dialogue buttons
            ShowEndDialogueButtons();
            return;
        }

        // Allow advancing dialogue if possible, no choices are being shown, and delay is not active
        if (
            _canAdvance
            && !_isDelayActive
            && _activeChoiceButtons.Count == 0
            && !_waitingForClearBeforeEndButtons
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

        // Reset the state for waiting to clear before end buttons
        _waitingForClearBeforeEndButtons = false;
        _endDialogueButtonsShown = false;

        // Reset advancement state - must wait for typewriter
        _canAdvance = false;
        _isDelayActive = false;

        // Disable movement while dialogue is playing
        if (_moveCanvas != null)
        {
            _moveCanvas.enabled = false;
        }

        // Ensure dialogue canvas is enabled
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = true;
        }

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
    /// Advances to the next dialogue if available, otherwise sets up state for clearing text before end buttons.
    /// Called when player clicks/presses space during single dialogue or through choice selection.
    /// </summary>
    public void NextDialogue()
    {
        if (_dialogue != null && _dialogue.m_dialogue != null)
        {
            var choices = _dialogue.m_dialogue.m_dialogueChoiceData;
            if (choices != null && choices.Count > 0 && choices[0].m_nextDialogue != null)
            {
                // Reset the flag since we're continuing with more dialogue
                _endDialogueButtonsShown = false;
                _dialogue.m_dialogue = choices[0].m_nextDialogue;
                ShowDialogue();
            }
            else
            {
                Debug.Log(
                    "No next dialogue found. Waiting for user input to clear text before showing end buttons."
                );
                // Instead of directly showing end buttons, set state to wait for user input to clear text first
                _waitingForClearBeforeEndButtons = true;
                _canAdvance = true; // Allow input to clear text

                // Keep the continue icon visible to indicate user can interact
                if (_continueIcon != null)
                {
                    _continueIcon.SetActive(true);
                }
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

        // Show the final dialogue or set up for end dialogue buttons
        if (currentNode != null)
        {
            ShowDialogue();
        }
        else
        {
            Debug.Log(
                "[TEST] Reached end of dialogue chain, setting up to clear text before showing end buttons"
            );
            // Set up the state to wait for user input to clear text before showing end buttons
            _waitingForClearBeforeEndButtons = true;
            _canAdvance = true;

            if (_continueIcon != null)
            {
                _continueIcon.SetActive(true);
            }
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
        // Clear all previous dialogue state to ensure clean setup
        ClearDialogueState();

        _dialogue = dialogue;
        _bachelor = bachelor;

        // Reset the state for waiting to clear before end buttons
        _waitingForClearBeforeEndButtons = false;
        _endDialogueButtonsShown = false;

        if (_bachelor != null)
        {
            // Ensure bachelor synchronizes with save data to load discovered preferences
            _bachelor.SynchronizeWithSaveData();

            // Ensure notebook is properly connected to the new bachelor
            if (_noteBook != null)
            {
                _noteBook.EnsureBachelorConnection(_bachelor);
            }

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
        // Clear all previous dialogue state to ensure clean setup
        ClearDialogueState();

        bachelor._dialogue = dialogueSO;
        if (bachelor == null || bachelor._dialogue == null)
        {
            Debug.LogError("[StartDialogue] Bachelor or dialogue is null!");
            return;
        }

        _bachelor = bachelor;
        _dialogue = dialogueSO;

        Debug.Log($"[StartDialogue] Started dialogue with bachelor: {_bachelor._name}");

        // Find and cache the SetBachelor component for later use
        _setBachelor = FindFirstObjectByType<SetBachelor>();
        if (_setBachelor == null)
        {
            Debug.LogWarning(
                "[DialogueDisplay] No SetBachelor found in scene, dating status may not be properly saved"
            );

            // Try a more aggressive search as a fallback
            SetBachelor[] allSetBachelors = FindObjectsByType<SetBachelor>(
                FindObjectsSortMode.None
            );
            if (allSetBachelors != null && allSetBachelors.Length > 0)
            {
                _setBachelor = allSetBachelors[0];
                Debug.Log($"[DialogueDisplay] Found SetBachelor through alternative search method");
            }
        }
        else
        {
            Debug.Log($"[DialogueDisplay] Found SetBachelor component in the scene");
        }

        // Ensure bachelor synchronizes with save data to load discovered preferences
        _bachelor.SynchronizeWithSaveData();

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

        // Reference to SetBachelor already found above, no need to search again
        if (_setBachelor != null)
        {
            Debug.Log(
                $"[DialogueDisplay] Using SetBachelor for bachelor: {_setBachelor.GetBachelor()?._name}"
            );
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
        // Clear all previous dialogue state to ensure clean setup
        ClearDialogueState();

        bachelor._dialogue = dialogueSO;
        if (bachelor == null || bachelor._dialogue == null)
            return;

        _bachelor = bachelor;
        _dialogue = dialogueSO;

        // Set the date dialogues from the parameters instead of the SO
        _rooftopDateDialogue = rooftopDateDialogue;
        _aquariumDateDialogue = aquariumDateDialogue;
        _forestDateDialogue = forestDateDialogue;

        // Ensure bachelor synchronizes with save data to load discovered preferences
        _bachelor.SynchronizeWithSaveData();

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
        Debug.Log(
            $"[ShowPostDateOptions] Starting. Bachelor: {(bachelor != null ? bachelor._name : "null")}"
        );

        // Clear all previous dialogue state to ensure clean setup
        ClearDialogueState();

        _bachelor = bachelor;

        // Ensure bachelor synchronizes with save data to load discovered preferences
        _bachelor.SynchronizeWithSaveData();

        // Reset the state for waiting to clear before end buttons
        _waitingForClearBeforeEndButtons = false;
        _endDialogueButtonsShown = false;

        // Reset advancement state - must wait for typewriter
        _canAdvance = false;
        _isDelayActive = false;

        // Ensure bachelor image is displayed when showing post-date options
        if (_bachelor != null && _bachelor._portrait != null && _bachelorImage != null)
        {
            _bachelorImage.sprite = _bachelor._portrait;
            _bachelorImage.color = new Color(
                _bachelorImage.color.r,
                _bachelorImage.color.g,
                _bachelorImage.color.b,
                1f
            );
            _bachelorImage.enabled = true;
            Debug.Log(
                $"✓ Set bachelor image for post-date options: {_bachelor._name} (Portrait: {_bachelor._portrait.name})"
            );
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
            Debug.LogWarning(
                $"✗ No bachelor image available for post-date options. Bachelor: {(_bachelor != null ? "exists" : "null")}, Portrait: {(_bachelor?._portrait != null ? "exists" : "null")}, BachelorImage: {(_bachelorImage != null ? "exists" : "null")}"
            );
        }

        // Set up the UI - bachelor name
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
            Debug.Log($"Set bachelor name for post-date options: {_bachelor._name}");
        }
        else if (_nameText != null)
        {
            _nameText.text = "";
            Debug.Log("No bachelor name available for post-date options");
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

        // Show the canvas and ensure proper UI state
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = true;
            Debug.Log("Enabled dialogue canvas for post-date options");
        }

        if (_moveCanvas != null)
        {
            _moveCanvas.enabled = false;
            Debug.Log("Disabled move canvas for post-date options");
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

        // Don't animate the choices immediately - wait for typewriter to finish
        // The OnTypewriterEnd method will handle the animation
    }

    /// <summary>
    /// Shows limited dialogue options for a bachelor who has completed a real date.
    /// Displays personalized message and only "Come Back Later" option.
    /// </summary>
    /// <param name="bachelor">The bachelor who has completed a real date</param>
    public void ShowPostRealDateOptionsInCafe(NewBachelorSO bachelor)
    {
        // Clear all previous dialogue state to ensure clean setup
        ClearDialogueState();

        _bachelor = bachelor;

        // Ensure bachelor synchronizes with save data to load discovered preferences
        _bachelor.SynchronizeWithSaveData();

        // Reset the state for waiting to clear before end buttons
        _waitingForClearBeforeEndButtons = false;
        _endDialogueButtonsShown = false;

        // Reset advancement state - must wait for typewriter
        _canAdvance = false;
        _isDelayActive = false;

        // Initialize love meter if available and update love score BEFORE getting the message
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveMeter = _bachelor._loveMeter;
            _loveScore = _loveMeter.GetCurrentLove();
            EnsureLoveMeterSetup();
            Debug.Log($"Updated love score for post-real-date message: {_loveScore}");
        }

        // Set up the UI
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
        }

        if (_bachelor != null && _bachelor._portrait != null && _bachelorImage != null)
        {
            _bachelorImage.sprite = _bachelor._portrait;
            _bachelorImage.enabled = true;
            _bachelorImage.color = new Color(
                _bachelorImage.color.r,
                _bachelorImage.color.g,
                _bachelorImage.color.b,
                1f // Fully visible immediately
            );

            Debug.Log($"✓ Set bachelor image for post-real-date: {_bachelor._name}");
        }

        // Show personalized message for real date completion (now uses updated love score)
        if (_displayText != null)
        {
            string personalizedMessage = _bachelor.GetRealDateMessage();
            _displayText.text = personalizedMessage;

            if (_typewriter != null)
            {
                _typewriter.ShowText(personalizedMessage);
            }
        }

        // Initialize love meter if available (already done above, so just ensure consistency)
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            // Love meter already initialized above, just ensure consistency
            if (_loveMeter != _bachelor._loveMeter)
            {
                _loveMeter = _bachelor._loveMeter;
                _loveScore = _loveMeter.GetCurrentLove();
                EnsureLoveMeterSetup();
                Debug.Log($"Re-synchronized love meter for consistency: {_loveScore}");
            }
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

        // Don't animate the choices immediately - wait for typewriter to finish
        // The OnTypewriterEnd method will handle the animation
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
        // Show pending choices if there are any (multiple choice dialogue)
        if (_pendingChoices != null && _pendingChoices.Count > 1)
        {
            ShowChoices(_pendingChoices);
            _pendingChoices = null; // Clear pending choices after showing them

            // For multiple choice dialogues, don't allow manual advancement
            // The player must click a choice button to advance
            _isDelayActive = false;
            _canAdvance = false;
        }
        // If there are active choice buttons (like post-date options), animate them in
        else if (_activeChoiceButtons.Count > 0)
        {
            StartCoroutine(FadeInChoicesSequentially(_activeChoiceButtons));

            // For pre-created choice buttons, don't allow manual advancement
            _isDelayActive = false;
            _canAdvance = false;
        }
        // For single dialogue with no choices, allow manual advancement after delay
        else
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

            // Show continue icon only if there are no choices at all (single dialogue)
            if (_continueIcon != null)
            {
                _continueIcon.SetActive(true);
            }
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

        // Reset the flag since we're continuing with more dialogue
        _endDialogueButtonsShown = false;

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
    /// Coroutine that fades in choice buttons one by one with a smooth DOTween animation.
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

                    // Use DOTween to animate alpha from 0 to 1 with smooth easing
                    yield return canvasGroup
                        .DOFade(1f, _choiceFadeDuration)
                        .SetEase(Ease.OutQuad)
                        .WaitForCompletion();

                    // Re-enable interaction after fade-in completes
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
        // Prevent multiple calls to avoid chaining endings
        if (_endDialogueButtonsShown)
        {
            Debug.Log(
                "[ShowEndDialogueButtons] End dialogue buttons already shown, preventing chain"
            );
            return;
        }

        _endDialogueButtonsShown = true;

        Debug.Log(
            $"[ShowEndDialogueButtons] Starting. Bachelor: {(_bachelor != null ? _bachelor._name : "null")}"
        );

        // Update love score from love meter to ensure consistency
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveScore = _bachelor._loveMeter.GetCurrentLove();
            Debug.Log($"Updated love score for end dialogue buttons: {_loveScore}");
        }

        // Ensure bachelor image is displayed when showing end dialogue buttons
        if (_bachelor != null && _bachelor._portrait != null && _bachelorImage != null)
        {
            _bachelorImage.sprite = _bachelor._portrait;
            _bachelorImage.color = new Color(
                _bachelorImage.color.r,
                _bachelorImage.color.g,
                _bachelorImage.color.b,
                1f
            );
            _bachelorImage.enabled = true;
            Debug.Log(
                $"✓ Set bachelor image for end dialogue buttons: {_bachelor._name} (Portrait: {_bachelor._portrait.name})"
            );
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
            Debug.LogWarning(
                $"✗ No bachelor image available for end dialogue buttons. Bachelor: {(_bachelor != null ? "exists" : "null")}, Portrait: {(_bachelor?._portrait != null ? "exists" : "null")}, BachelorImage: {(_bachelorImage != null ? "exists" : "null")}"
            );
        }

        // Ensure bachelor name is displayed when showing end dialogue buttons
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
            Debug.Log($"Set bachelor name for end dialogue buttons: {_bachelor._name}");
        }
        else if (_nameText != null)
        {
            _nameText.text = "";
            Debug.Log("No bachelor name available for end dialogue buttons");
        }

        // Ensure dialogue canvas is enabled when showing end dialogue buttons
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = true;
            Debug.Log("Enabled dialogue canvas for end dialogue buttons");
        }

        // Ensure move canvas is disabled when showing end dialogue buttons
        if (_moveCanvas != null)
        {
            _moveCanvas.enabled = false;
            Debug.Log("Disabled move canvas for end dialogue buttons");
        }

        // Mark the bachelor as having completed a speed date
        if (_bachelor != null)
        {
            // Use the bachelor's own method which handles both local flags and save data consistency
            MarkBachelorAsDated(_bachelor);

            // Also notify SetBachelor to ensure full synchronization with save data
            if (_setBachelor != null)
            {
                _setBachelor.CompleteSpeedDateAndSave();
                Debug.Log(
                    "[DialogueDisplay] Called CompleteSpeedDateAndSave() to ensure bachelor dating status is saved"
                );
            }
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        string sceneLower = currentSceneName.ToLower();

        // Check if we're at the end of a real date - detect by having a real date location set
        bool isEndOfRealDate = !string.IsNullOrEmpty(_currentRealDateLocation);

        if (isEndOfRealDate)
        {
            Debug.Log(
                $"End of real date detected ({_currentRealDateLocation}). Setting flag and showing Good/Bad Date result screen, then proceeding to barista."
            );

            // NOW set the flag since we're truly at the end of the real date
            _justCompletedRealDate = true;

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
            // Apply fade-in animation to the Enter Cafe button
            StartCoroutine(FadeInChoicesSequentially(_activeChoiceButtons));
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

        // Apply fade-in animation to all created buttons
        StartCoroutine(FadeInChoicesSequentially(_activeChoiceButtons));
    }

    /// <summary>
    /// Shows the post-real-date options with no message and only Come Back Later button
    /// Used at the end of real date scenes (aquarium, forest, rooftop)
    /// </summary>
    private void ShowPostRealDateOptions()
    {
        Debug.Log(
            $"[ShowPostRealDateOptions] Starting. Bachelor: {(_bachelor != null ? _bachelor._name : "null")}"
        );

        // Ensure bachelor image is displayed
        if (_bachelor != null && _bachelor._portrait != null && _bachelorImage != null)
        {
            _bachelorImage.sprite = _bachelor._portrait;
            _bachelorImage.color = new Color(
                _bachelorImage.color.r,
                _bachelorImage.color.g,
                _bachelorImage.color.b,
                1f
            );
            _bachelorImage.enabled = true;
            Debug.Log(
                $"✓ Set bachelor image for post-real-date options: {_bachelor._name} (Portrait: {_bachelor._portrait.name})"
            );
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
            Debug.LogWarning(
                $"✗ No bachelor image available for post-real-date options. Bachelor: {(_bachelor != null ? "exists" : "null")}, Portrait: {(_bachelor?._portrait != null ? "exists" : "null")}, BachelorImage: {(_bachelorImage != null ? "exists" : "null")}"
            );
        }

        // Ensure bachelor name is displayed
        if (_bachelor != null && _nameText != null)
        {
            _nameText.text = _bachelor._name;
            Debug.Log($"Set bachelor name for post-real-date options: {_bachelor._name}");
        }
        else if (_nameText != null)
        {
            _nameText.text = "";
            Debug.Log("No bachelor name available for post-real-date options");
        }

        // Ensure dialogue canvas is enabled
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = true;
            Debug.Log("Enabled dialogue canvas for post-real-date options");
        }

        // Ensure move canvas is disabled
        if (_moveCanvas != null)
        {
            _moveCanvas.enabled = false;
            Debug.Log("Disabled move canvas for post-real-date options");
        }

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

        // Apply fade-in animation to the Back to Cafe button
        StartCoroutine(FadeInChoicesSequentially(_activeChoiceButtons));
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
        if (_bachelor != null && !string.IsNullOrEmpty(_currentRealDateLocation))
        {
            Debug.Log(
                $"[OnPostRealDateComeBackLaterClicked] About to mark {_bachelor._name} as real dated at {_currentRealDateLocation}"
            );

            // Use SetBachelor to ensure proper synchronization with save data
            if (_setBachelor != null)
            {
                _setBachelor.CompleteRealDateAndSave(_currentRealDateLocation);
                Debug.Log(
                    $"[OnPostRealDateComeBackLaterClicked] Called CompleteRealDateAndSave({_currentRealDateLocation}) via SetBachelor"
                );
            }
            else
            {
                // Fallback to direct method if SetBachelor isn't available
                MarkBachelorAsRealDated(_bachelor);
                Debug.LogWarning(
                    "[OnPostRealDateComeBackLaterClicked] SetBachelor not found, using direct method instead"
                );
            }

            IncrementRealDateCount();
        }
        else
        {
            Debug.LogError(
                "[OnPostRealDateComeBackLaterClicked] Bachelor is null or location is empty when trying to save real date progress!"
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
        if (_bachelor != null && !string.IsNullOrEmpty(_currentRealDateLocation))
        {
            // Use SetBachelor to ensure proper synchronization with save data
            if (_setBachelor != null)
            {
                _setBachelor.CompleteRealDateAndSave(_currentRealDateLocation);
                Debug.Log(
                    $"[OnReturnToCafeClicked] Called CompleteRealDateAndSave({_currentRealDateLocation}) via SetBachelor"
                );
            }
            else
            {
                // Fallback to direct method if SetBachelor isn't available
                MarkBachelorAsRealDated(_bachelor);
                Debug.LogWarning(
                    "[OnReturnToCafeClicked] SetBachelor not found, using direct method instead"
                );
            }

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
        if (_bachelor != null && !string.IsNullOrEmpty(_currentRealDateLocation))
        {
            // Use SetBachelor to ensure proper synchronization with save data
            if (_setBachelor != null)
            {
                _setBachelor.CompleteRealDateAndSave(_currentRealDateLocation);
                Debug.Log(
                    $"[OnBackToMenuClicked] Called CompleteRealDateAndSave({_currentRealDateLocation}) via SetBachelor"
                );
            }
            else
            {
                // Fallback to direct method if SetBachelor isn't available
                MarkBachelorAsRealDated(_bachelor);
                Debug.LogWarning(
                    "[OnBackToMenuClicked] SetBachelor not found, using direct method instead"
                );
            }

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
        if (_bachelor != null && !string.IsNullOrEmpty(_currentRealDateLocation))
        {
            // Use SetBachelor to ensure proper synchronization with save data
            if (_setBachelor != null)
            {
                _setBachelor.CompleteRealDateAndSave(_currentRealDateLocation);
                Debug.Log(
                    $"[OnQuitGameClicked] Called CompleteRealDateAndSave({_currentRealDateLocation}) via SetBachelor"
                );
            }
            else
            {
                // Fallback to direct method if SetBachelor isn't available
                MarkBachelorAsRealDated(_bachelor);
                Debug.LogWarning(
                    "[OnQuitGameClicked] SetBachelor not found, using direct method instead"
                );
            }

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

        // Add canvas group for fade effect if it doesn't exist
        var canvasGroup = btnObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = btnObj.AddComponent<CanvasGroup>();
        }
        // Set initial alpha to 0 (invisible)
        canvasGroup.alpha = 0f;

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

        // Add canvas group for fade effect if it doesn't exist
        var canvasGroup = btnObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = btnObj.AddComponent<CanvasGroup>();
        }
        // Set initial alpha to 0 (invisible)
        canvasGroup.alpha = 0f;

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

        // Reset end dialogue button state for next interaction
        _endDialogueButtonsShown = false;

        // Check if this is coming from a real date
        if (_justCompletedRealDate)
        {
            Debug.Log("Returning from real date - handling post-real-date cleanup");

            // Mark bachelor as real dated and save progress
            if (_bachelor != null && !string.IsNullOrEmpty(_currentRealDateLocation))
            {
                // Use SetBachelor to ensure proper synchronization with save data
                if (_setBachelor != null)
                {
                    _setBachelor.CompleteRealDateAndSave(_currentRealDateLocation);
                    Debug.Log(
                        $"[OnComeBackLaterClicked] Called CompleteRealDateAndSave({_currentRealDateLocation}) via SetBachelor"
                    );
                }
                else
                {
                    // Fallback to direct method if SetBachelor isn't available
                    MarkBachelorAsRealDated(_bachelor);
                    Debug.LogWarning(
                        "[OnComeBackLaterClicked] SetBachelor not found, using direct method instead"
                    );
                }

                IncrementRealDateCount();
            }
            else if (_bachelor == null)
            {
                Debug.LogError(
                    "[OnComeBackLaterClicked] Cannot mark real dated - bachelor is null"
                );
            }
            else if (string.IsNullOrEmpty(_currentRealDateLocation))
            {
                Debug.LogError(
                    "[OnComeBackLaterClicked] Cannot mark real dated - location is empty"
                );
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

        // Reset end dialogue button state for next interaction
        _endDialogueButtonsShown = false;

        // Show the asking on a date text
        ShowAskOnDateText();
    }

    /// <summary>
    /// Shows the text for asking the bachelor on a date and then displays location options.
    /// </summary>
    private void ShowAskOnDateText()
    {
        // Reset advancement state - must wait for typewriter
        _canAdvance = false;
        _isDelayActive = false;

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

        // Clear any existing choices and prepare date location buttons (they will be animated when typewriter finishes)
        ClearChoices();
        CreateDateLocationButton("Rooftop", OnRooftopDateSelected);
        CreateDateLocationButton("Aquarium", OnAquariumDateSelected);
        CreateDateLocationButton("Forest", OnForestDateSelected);
    }

    /// <summary>
    /// Shows the three date location options: Rooftop, Aquarium, and Forest.
    /// This is now called by OnTypewriterEnd when the ask date text finishes.
    /// </summary>
    private void ShowDateLocationOptions()
    {
        // Apply fade-in animation to all created buttons
        StartCoroutine(FadeInChoicesSequentially(_activeChoiceButtons));
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

        // Add canvas group for fade effect if it doesn't exist
        var canvasGroup = btnObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = btnObj.AddComponent<CanvasGroup>();
        }
        // Set initial alpha to 0 (invisible)
        canvasGroup.alpha = 0f;

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

        // DON'T set _justCompletedRealDate to true here - only set it when the date actually ends
        // Instead, just store the location so we know we're in a real date

        // Validate and store the date location
        if (string.IsNullOrEmpty(locationName))
        {
            Debug.LogWarning(
                "StartRealDateDialogue called with empty location name! Using 'Unknown' as fallback."
            );
            _currentRealDateLocation = "Unknown"; // Fallback to ensure we don't have an empty location
        }
        else
        {
            _currentRealDateLocation = locationName;
        }

        Debug.Log(
            $"Starting real date at {_currentRealDateLocation}. Will mark as completed when dialogue ends."
        );

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
            // Get counts from the new per-bachelor system
            _speedDateCount = saveData.GetAllSpeedDatedBachelors().Count;
            _realDateCount = saveData.GetAllRealDatedBachelors().Count;

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
        // Make bachelors non-clickable during barista event
        SetBachelorsClickable(false);
        Debug.Log("Bachelors set to non-clickable during barista event");

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

        // Update love score from love meter to get the most current value
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveScore = _bachelor._loveMeter.GetCurrentLove();
            Debug.Log(
                $"Updated love score from love meter for barista dialogue: {_loveScore}/{_loveNeededForSuccefulDate}"
            );
        }
        else
        {
            Debug.LogWarning(
                "Could not update love score for barista dialogue - bachelor or love meter is null"
            );
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
            else if (AllDatesSucceeded() && _allDatesSucceededBaristaDialogue != null)
            {
                baristaDialogue = _allDatesSucceededBaristaDialogue;
                isAllDateFailedDialogue = true; // This also triggers game end
                Debug.Log(
                    "Using 'all dates succeeded' barista dialogue - triggering game end sequence"
                );
            }
            // If no special ending conditions are met, fall through to regular dialogue
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
            Debug.Log(
                $"Using regular barista dialogue #{dialogueIndex + 1} after {_realDateCount} real dates"
            );
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

                // Play dialogue audio if available
                if (_audioSource != null)
                {
                    var audioClip = baristaDialogue.m_dialogue.m_dialogueAudioData;
                    if (audioClip != null)
                    {
                        _audioSource.Stop();
                        _audioSource.clip = audioClip;
                        _audioSource.Play();
                        Debug.Log("Playing barista audio: " + audioClip.name);
                    }
                    else
                    {
                        _audioSource.Stop();
                        _audioSource.clip = null;
                        Debug.Log("No audio clip found for barista dialogue");
                    }
                }

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

            // Re-enable bachelor clickability with a 0.5 second delay after barista event
            EnableBachelorsClickableWithDelay(0.5f);
            Debug.Log("Scheduled re-enabling of bachelor clickability after 0.5 seconds");
        }
    }
    #endregion

    /// <summary>
    /// Determines if all dates have failed (no successful dates)
    /// </summary>
    private bool AllDatesFailed()
    {
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
            return false;

        // Check if all 5 real dates have been completed
        int realDatedCount = saveData.GetAllRealDatedBachelors().Count;
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
        if (saveData == null)
            return false;

        // Check if all 5 real dates have been completed
        int realDatedCount = saveData.GetAllRealDatedBachelors().Count;
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
    /// Determines if all dates have succeeded (all 5 real dates completed successfully)
    /// </summary>
    private bool AllDatesSucceeded()
    {
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
            return false;

        // Check if all 5 real dates have been completed
        int realDatedCount = saveData.GetAllRealDatedBachelors().Count;
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

        // All dates succeeded if all 5 real dates were successful
        bool allSucceeded = successfulDates >= 5;
        Debug.Log(
            $"All dates succeeded check: Real dated: {realDatedCount}/5, Successful: {successfulDates}, All succeeded: {allSucceeded}"
        );

        return realDatedCount >= 5 && allSucceeded;
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

    /// <summary>
    /// Clears all dialogue state to ensure clean setup when switching between bachelors.
    /// This prevents issues where UI elements or dialogue data from the previous bachelor persist.
    /// </summary>
    private void ClearDialogueState()
    {
        Debug.Log("[ClearDialogueState] Clearing all dialogue state for clean bachelor switch");

        // Clear dialogue references
        string previousBachelorName = _bachelor != null ? _bachelor._name : "null";
        _dialogue = null;
        _bachelor = null;
        _loveMeter = null;
        _loveScore = 0;

        Debug.Log(
            $"[ClearDialogueState] Cleared references for previous bachelor: {previousBachelorName}"
        );

        // Clear UI elements
        if (_nameText != null)
        {
            _nameText.text = "";
        }

        if (_displayText != null)
        {
            _displayText.text = "";
        }

        if (_bachelorImage != null)
        {
            _bachelorImage.sprite = null;
            _bachelorImage.color = new Color(1f, 1f, 1f, 0f); // Make transparent
            _bachelorImage.enabled = false;
        }

        // Clear choices and pending state
        ClearChoices();
        _pendingChoices = null;
        _canAdvance = false;
        _isDelayActive = false;

        // Stop and clear audio
        if (_audioSource != null)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
        }

        // Stop typewriter if it's running
        if (_typewriter != null)
        {
            _typewriter.StopShowingText();
        }

        // Hide continue icon
        if (_continueIcon != null)
        {
            _continueIcon.SetActive(false);
        }

        // Hide love meter
        if (_loveMeterUI != null)
        {
            _loveMeterUI.HideLoveMeter();
        }

        // Clear date-related state
        _justCompletedRealDate = false;
        _currentRealDateLocation = "";
        _waitingForClearBeforeEndButtons = false;
        _endDialogueButtonsShown = false;

        // Turn off all date backgrounds
        TurnOffAllDateBackgrounds();

        // Reset game variables that might be bachelor-specific
        if (_gameVariables != null)
        {
            _gameVariables["Love"] = "0";
            _gameVariables["NotebookLikeEntry"] = "false";
            _gameVariables["NotebookDislikeEntry"] = "false";
        }

        // Clear notebook reference
        if (_noteBook != null)
        {
            _noteBook.ClearBachelor();
        }

        Debug.Log("[ClearDialogueState] Dialogue state cleared successfully");
    }

    /// <summary>
    /// TEST METHOD: Manually clear dialogue state - accessible from component context menu
    /// </summary>
    [ContextMenu("TEST: Clear Dialogue State")]
    public void TestClearDialogueState()
    {
        Debug.Log("[TEST] Manually clearing dialogue state");
        ClearDialogueState();
        Debug.Log("[TEST] Dialogue state cleared manually");
    }

    /// <summary>
    /// TEST METHOD: Log current dialogue state - accessible from component context menu
    /// </summary>
    [ContextMenu("TEST: Debug Current Dialogue State")]
    public void DebugCurrentDialogueState()
    {
        Debug.Log("=== CURRENT DIALOGUE STATE DEBUG ===");
        Debug.Log($"Bachelor: {(_bachelor != null ? _bachelor._name : "null")}");
        Debug.Log($"Dialogue: {(_dialogue != null ? _dialogue.name : "null")}");
        Debug.Log($"Love Meter: {(_loveMeter != null ? _loveMeter.name : "null")}");
        Debug.Log($"Love Score: {_loveScore}");
        Debug.Log($"Can Advance: {_canAdvance}");
        Debug.Log($"Is Delay Active: {_isDelayActive}");
        Debug.Log($"Waiting for Clear: {_waitingForClearBeforeEndButtons}");
        Debug.Log($"End Dialogue Buttons Shown: {_endDialogueButtonsShown}");
        Debug.Log($"Just Completed Real Date: {_justCompletedRealDate}");
        Debug.Log($"Current Real Date Location: '{_currentRealDateLocation}'");
        Debug.Log($"Active Choice Buttons: {_activeChoiceButtons.Count}");
        Debug.Log($"Pending Choices: {(_pendingChoices != null ? _pendingChoices.Count : 0)}");

        if (_nameText != null)
            Debug.Log($"Name Text: '{_nameText.text}'");
        if (_displayText != null)
            Debug.Log($"Display Text: '{_displayText.text}'");
        if (_bachelorImage != null)
            Debug.Log(
                $"Bachelor Image: {(_bachelorImage.sprite != null ? _bachelorImage.sprite.name : "null")} (enabled: {_bachelorImage.enabled})"
            );

        Debug.Log("=== END DIALOGUE STATE DEBUG ===");
    }

    /// <summary>
    /// Shows the date result screen (good/bad date) and then proceeds to show barista dialogue
    /// </summary>
    private IEnumerator ShowDateResultAndProceedToBarista()
    {
        Debug.Log("Starting ShowDateResultAndProceedToBarista coroutine");

        // Hide dialogue UI temporarily
        if (_dialogueCanvas != null)
        {
            _dialogueCanvas.enabled = false;
        }

        // Update love score from love meter to get the most current value
        if (_bachelor != null && _bachelor._loveMeter != null)
        {
            _loveScore = _bachelor._loveMeter.GetCurrentLove();
            Debug.Log(
                $"Updated love score from love meter: {_loveScore}/{_loveNeededForSuccefulDate}"
            );
        }
        else
        {
            Debug.LogWarning("Could not update love score - bachelor or love meter is null");
        }

        // Determine if the date was successful
        bool isGoodDate = _loveScore >= _loveNeededForSuccefulDate;

        // Show appropriate result screen with DOTween fade-in animation
        GameObject screenToShow = null;
        if (isGoodDate && _goodDateScreen != null)
        {
            Debug.Log("Showing good date result screen with DOTween fade-in");
            screenToShow = _goodDateScreen;
        }
        else if (!isGoodDate && _badDateScreen != null)
        {
            Debug.Log("Showing bad date result screen with DOTween fade-in");
            screenToShow = _badDateScreen;
        }

        if (screenToShow != null)
        {
            // Get or add CanvasGroup for fade effect BEFORE activating
            CanvasGroup canvasGroup = screenToShow.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = screenToShow.AddComponent<CanvasGroup>();
            }

            // Set alpha to 0 BEFORE activating to ensure it starts invisible
            canvasGroup.alpha = 0f;

            // Now activate the screen (it will be invisible due to alpha = 0)
            screenToShow.SetActive(true); // Fade in using DOTween
            yield return canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad).WaitForCompletion();
        }

        // Wait for 0 seconds to let player see the result (changed from 1.5f)
        yield return new WaitForSeconds(0f);

        // Fade out using DOTween and hide result screens
        if (screenToShow != null)
        {
            CanvasGroup canvasGroup = screenToShow.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                // Fade out using DOTween with a smooth ease
                yield return canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad).WaitForCompletion();
            }

            screenToShow.SetActive(false);
        }

        // Mark bachelor as real dated and save progress
        if (_bachelor != null && !string.IsNullOrEmpty(_currentRealDateLocation))
        {
            if (_setBachelor != null)
            {
                _setBachelor.CompleteRealDateAndSave(_currentRealDateLocation);
                Debug.Log(
                    $"Called CompleteRealDateAndSave({_currentRealDateLocation}) via SetBachelor"
                );
            }
            else
            {
                // Fallback to direct method if SetBachelor isn't available
                MarkBachelorAsRealDated(_bachelor);
                Debug.LogWarning("SetBachelor not found, using direct method instead");
            }

            IncrementRealDateCount();
        }

        // Clear notebook
        if (_noteBook != null)
        {
            _noteBook.ClearBachelor();
        }

        // End the date session
        EndDate();

        // Now proceed to barista dialogue
        Debug.Log("Proceeding to barista dialogue after date result");
        StartCoroutine(ShowBaristaAfterRealDate());
    }

    /// <summary>
    /// Deletes the save file completely - used when the game ends
    /// </summary>
    private void DeleteSaveFile()
    {
        try
        {
            // Use the SaveSystem to reset the save file
            SaveSystem.ResetSaveFile();
            Debug.Log("Save file deleted successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to delete save file: {ex.Message}");
        }
    }

    // =====================================================================================
    // CORE DIALOGUE DISPLAY
    // =====================================================================================

    // Controls whether bachelors are currently clickable
    private bool _bachelorsClickable = true;

    /// <summary>
    /// Call this to make bachelors not clickable (e.g., during barista event)
    /// </summary>
    public void SetBachelorsClickable(bool clickable)
    {
        _bachelorsClickable = clickable;
    }

    /// <summary>
    /// Call this after barista event to delay re-enabling bachelor clickability
    /// </summary>
    public void EnableBachelorsClickableWithDelay(float delaySeconds = 0.5f)
    {
        StartCoroutine(EnableBachelorsClickableCoroutine(delaySeconds));
    }

    private IEnumerator EnableBachelorsClickableCoroutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        _bachelorsClickable = true;
    }

    /// <summary>
    /// Returns whether bachelors are currently clickable
    /// </summary>
    public bool AreBachelorsClickable() => _bachelorsClickable;

    /// <summary>
    /// Returns whether the dialogue system is currently active (showing dialogue)
    /// </summary>
    public bool IsInDialogue()
    {
        // Check if dialogue canvas is active and enabled
        if (
            _dialogueCanvas == null
            || !_dialogueCanvas.gameObject.activeInHierarchy
            || !_dialogueCanvas.enabled
        )
            return false;

        // Check if we have actual dialogue content being displayed
        bool hasDialogueContent = false;

        // Check if there's text being displayed
        if (_displayText != null && !string.IsNullOrEmpty(_displayText.text))
            hasDialogueContent = true;

        // Check if typewriter is actively showing text
        if (_typewriter != null && _typewriter.isShowingText)
            hasDialogueContent = true;

        // Check if there are active choice buttons
        if (_activeChoiceButtons != null && _activeChoiceButtons.Count > 0)
            hasDialogueContent = true;

        // Check if we have a current bachelor set (indicating we're in a dialogue session)
        if (_bachelor != null)
            hasDialogueContent = true;

        return hasDialogueContent;
    }
}
    #endregion
