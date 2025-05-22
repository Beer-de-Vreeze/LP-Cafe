using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogueManager : Singleton<DialogueManager>
{
    [Header("Settings")]
    [SerializeField, Tooltip("The default speaker name.")]
    private string defaultSpeakerName = "Unknown Speaker";

    [SerializeField, Tooltip("The default speaker image.")]
    private Sprite defaultSpeakerImage;

    [Header("Debugging")]
    [SerializeField, Tooltip("Enable debug logs for the dialogue manager.")]
    private bool enableDebugLogs = false;

    [Header("UI Elements")]
    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [SerializeField]
    private TextMeshProUGUI speakerNameText;

    [SerializeField]
    private Image speakerImage;

    [SerializeField]
    private GameObject dialogueOptionsContainer;

    [SerializeField]
    private GameObject optionButtonPrefab;

    [Header("Animation Settings")]
    [SerializeField, Range(0.01f, 1f), Tooltip("The lower the value, the faster the typing speed.")]
    private float typingSpeed = 0.1f;

    [SerializeField, Range(0.5f, 5f), Tooltip("The time between messages.")]
    private float timeBetweenMessages = 1.5f;

    [Header("Debug")]
    [SerializeField, Tooltip("Dialogue to test the dialogue manager. Not USED IN THE GAME.")]
    private Dialogue testDialogue;

    private Dialogue currentDialogue;
    private DialogueNode currentNode;
    private bool isTyping = false;
    private bool isLastMessage = false;
    private Coroutine autoCloseCoroutine;

    // Events
    public UnityEvent OnDialogueStart = new UnityEvent();
    public UnityEvent OnDialogueEnd = new UnityEvent();
    public UnityEvent<DialogueNode> OnNodeStart = new UnityEvent<DialogueNode>();
    public UnityEvent<DialogueOption> OnOptionSelected = new UnityEvent<DialogueOption>();

    private List<GameObject> hardCodedButtons = new List<GameObject>();

    private void Start()
    {
        // Hide options container at start
        if (dialogueOptionsContainer)
            dialogueOptionsContainer.SetActive(false);

        // Subscribe to input manager events
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnDialogueAdvance += HandleDialogueAdvance;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from input manager events
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnDialogueAdvance -= HandleDialogueAdvance;
        }
    }

    private void HandleDialogueAdvance()
    {
        // If typing is in progress, complete the typing first instead of advancing
        if (isTyping)
        {
            StopAllCoroutines();
            if (currentNode != null)
            {
                dialogueText.text = currentNode.text;
                isTyping = false;
                OnTypingComplete();
            }
            return;
        }

        // Only advance if we're in dialogue mode and there's a next node to go to
        if (
            currentNode != null
            && currentNode.options.Count == 0
            && !string.IsNullOrEmpty(currentNode.nextNodeID)
        )
        {
            AdvanceToNextNode();
        }
    }

    /// <summary>
    /// Starts the dialogue.
    /// Needs a dialogue object to start the conversation.
    /// </summary>
    public void StartDialogue(Dialogue dialogue)
    {
        if (enableDebugLogs)
            Debug.Log(
                $"Starting dialogue. Has startNode: {dialogue.startNode != null}, Nodes count: {dialogue.nodes?.Count ?? 0}"
            );

        // Enter visual novel mode
        OnDialogueStart.Invoke();

        // Switch to UI input mode
        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIControls();

        currentDialogue = dialogue;

        // Handle legacy dialogues
        if (dialogue.startNode == null && dialogue.messages?.Length > 0)
        {
            if (enableDebugLogs)
                Debug.Log("Using legacy dialogue format");

            // Legacy mode - convert old format to new format
            HandleLegacyDialogue(dialogue);
            return;
        }

        // Start with the dialogue's first node
        if (dialogue.startNode != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Starting with dialogue.startNode: {dialogue.startNode.nodeID}");

            DisplayNode(dialogue.startNode);
        }
        else if (dialogue.nodes != null && dialogue.nodes.Count > 0)
        {
            if (enableDebugLogs)
                Debug.Log($"Starting with first node: {dialogue.nodes[0].nodeID}");

            DisplayNode(dialogue.nodes[0]);
        }
        else
        {
            Debug.LogWarning("Dialogue has no nodes.");
            EndDialogue();
        }
    }

    private void HandleLegacyDialogue(Dialogue dialogue)
    {
        // Legacy support code
        Queue<string> sentences = new Queue<string>();
        foreach (string sentence in dialogue.messages)
        {
            sentences.Enqueue(sentence);
        }

        if (sentences.Count > 0)
        {
            string sentence = sentences.Dequeue();
            StopAllCoroutines();
            StartCoroutine(
                TypeSentence(
                    sentence,
                    () =>
                    {
                        if (sentences.Count > 0)
                            HandleLegacyDialogue(dialogue);
                        else
                            EndDialogue();
                    }
                )
            );
        }
        else
        {
            EndDialogue();
        }
    }

    public void DisplayNode(DialogueNode node)
    {
        currentNode = node;
        OnNodeStart.Invoke(node);

        // Update speaker info
        if (speakerNameText != null)
        {
            speakerNameText.text = node.speakerName;
        }

        if (speakerImage != null && node.speakerImage != null)
        {
            speakerImage.sprite = node.speakerImage;
            speakerImage.gameObject.SetActive(true);
        }
        else if (speakerImage != null)
        {
            speakerImage.gameObject.SetActive(false);
        }

        // Play voice clip if available
        if (node.voiceClip != null)
        {
            // You can implement audio source and playback here
        }

        // Clear any existing options
        ClearDialogueOptions();

        // Hide options during typing
        if (dialogueOptionsContainer)
            dialogueOptionsContainer.SetActive(false);

        // Type out the dialogue text
        StopAllCoroutines();
        StartCoroutine(TypeSentence(node.text, OnTypingComplete));
    }

    private void OnTypingComplete()
    {
        // Show options if we have any
        if (currentNode.options != null && currentNode.options.Count > 0)
        {
            DisplayDialogueOptions(currentNode.options);
        }
        // Check if we have choices instead (for backwards compatibility)
        else if (currentNode.choices != null && currentNode.choices.Count > 0)
        {
            DisplayDialogueChoices(currentNode.choices);
        }
        // Check if this is the last node
        else if (string.IsNullOrEmpty(currentNode.nextNodeID))
        {
            isLastMessage = true;
            // Start auto-close timer
            if (autoCloseCoroutine != null)
                StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = StartCoroutine(AutoCloseDialogue(3f));
        }
    }

    public void AdvanceToNextNode()
    {
        // If typing, complete current text and allow advancing
        if (isTyping)
        {
            StopAllCoroutines();
            if (currentNode != null)
            {
                dialogueText.text = currentNode.text;
                isTyping = false;
                OnTypingComplete();
            }
            return;
        }

        // If this is the last message and the player clicks again, close the dialogue
        if (isLastMessage)
        {
            if (autoCloseCoroutine != null)
                StopCoroutine(autoCloseCoroutine);
            EndDialogue();
            return;
        }

        // Safety check - make sure we have a node to advance from
        if (currentNode == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("Cannot advance dialogue: current node is null");
            return;
        }

        // If no options, advance to next node
        if (currentNode.options.Count == 0 && !string.IsNullOrEmpty(currentNode.nextNodeID))
        {
            DialogueNode nextNode = FindNodeByID(currentNode.nextNodeID);
            if (nextNode != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"Advancing from {currentNode.nodeID} to {nextNode.nodeID}");

                DisplayNode(nextNode);
            }
            else
            {
                Debug.LogWarning($"Could not find node with ID: {currentNode.nextNodeID}");

                EndDialogue();
            }
        }
        // Check if this is the last node (no more nodes to advance to)
        else if (currentNode.options.Count == 0 && string.IsNullOrEmpty(currentNode.nextNodeID))
        {
            isLastMessage = true;
            // Start auto-close timer
            if (autoCloseCoroutine != null)
                StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = StartCoroutine(AutoCloseDialogue(3f));
        }
    }

    private IEnumerator AutoCloseDialogue(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndDialogue();
    }

    private void EndDialogue()
    {
        OnDialogueEnd.Invoke();

        // Clear any hard-coded buttons
        ClearHardCodedButtons();

        // Exit visual novel mode
        if (InputManager.Instance != null)
            InputManager.Instance.EnablePlayerControls();
        UIMananager.Instance.OpenCloseDialogueBox();

        // Reset dialogue state
        isLastMessage = false;
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        Debug.Log("End of conversation.");
    }

    private DialogueNode FindNodeByID(string id)
    {
        return currentDialogue.nodes.Find(node => node.nodeID == id);
    }

    private void DisplayDialogueOptions(List<DialogueOption> options)
    {
        // Clear any existing hard-coded buttons first
        ClearHardCodedButtons();

        // Check if we have the proper UI setup for dialogue options
        if (dialogueOptionsContainer == null || optionButtonPrefab == null)
        {
            Debug.LogWarning(
                "Dialogue options container or button prefab not assigned. Creating hard-coded buttons instead."
            );
            CreateHardCodedButtons(options);
            return;
        }

        dialogueOptionsContainer.SetActive(true);

        foreach (var option in options)
        {
            // Skip options with unsatisfied conditions
            if (option.hasCondition && !CheckCondition(option.conditionName))
                continue;

            GameObject buttonObj = Instantiate(
                optionButtonPrefab,
                dialogueOptionsContainer.transform
            );
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = option.text;

            if (button != null)
            {
                DialogueOption capturedOption = option; // Capture for lambda
                button.onClick.AddListener(() => SelectOption(capturedOption));
            }
        }
    }

    private void CreateHardCodedButtons(List<DialogueOption> options)
    {
        if (options == null || options.Count == 0)
            return;

        // Create a canvas if needed
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DialogueCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Calculate positioning variables
        int buttonCount = options.Count;
        float buttonHeight = 60f;
        float buttonWidth = 300f;
        float spacing = 20f;
        float startY = (buttonCount * (buttonHeight + spacing)) / 2f;

        // Create buttons for each option
        for (int i = 0; i < options.Count; i++)
        {
            DialogueOption option = options[i];

            // Skip options with unsatisfied conditions
            if (option.hasCondition && !CheckCondition(option.conditionName))
                continue;

            GameObject buttonObj = new GameObject($"HardCodedButton_{i}");
            buttonObj.transform.SetParent(canvas.transform, false);
            hardCodedButtons.Add(buttonObj);

            // Set up RectTransform
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rectTransform.anchoredPosition = new Vector2(
                0,
                startY - (i * (buttonHeight + spacing))
            );
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            // Add Image component for background
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            button.colors = colors;

            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRectTransform = textObj.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = new Vector2(10, 5);
            textRectTransform.offsetMax = new Vector2(-10, -5);

            // Add TextMeshProUGUI component
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = option.text;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 24;

            // Add click listener
            DialogueOption capturedOption = option;
            button.onClick.AddListener(() =>
            {
                SelectOption(capturedOption);
                ClearHardCodedButtons();
            });
        }
    }

    private void ClearHardCodedButtons()
    {
        foreach (GameObject button in hardCodedButtons)
        {
            if (button != null)
                Destroy(button);
        }
        hardCodedButtons.Clear();
    }

    private void DisplayDialogueChoices(List<DialogueChoice> choices)
    {
        // Clear any existing hard-coded buttons first
        ClearHardCodedButtons();

        if (dialogueOptionsContainer == null || optionButtonPrefab == null)
        {
            Debug.LogWarning(
                "Dialogue options container or button prefab not assigned. Creating hard-coded buttons for choices."
            );
            CreateHardCodedButtonsForChoices(choices);
            return;
        }

        dialogueOptionsContainer.SetActive(true);

        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(
                optionButtonPrefab,
                dialogueOptionsContainer.transform
            );
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = choice.text;

            if (button != null)
            {
                DialogueChoice capturedChoice = choice; // Capture for lambda
                button.onClick.AddListener(() => SelectChoice(capturedChoice));
            }
        }
    }

    private void CreateHardCodedButtonsForChoices(List<DialogueChoice> choices)
    {
        if (choices == null || choices.Count == 0)
            return;

        // Create a canvas if needed
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DialogueCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Calculate positioning variables
        int buttonCount = choices.Count;
        float buttonHeight = 60f;
        float buttonWidth = 300f;
        float spacing = 20f;
        float startY = (buttonCount * (buttonHeight + spacing)) / 2f;

        // Create buttons for each choice
        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoice choice = choices[i];
            GameObject buttonObj = new GameObject($"HardCodedButton_{i}");
            buttonObj.transform.SetParent(canvas.transform, false);
            hardCodedButtons.Add(buttonObj);

            // Set up RectTransform
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rectTransform.anchoredPosition = new Vector2(
                0,
                startY - (i * (buttonHeight + spacing))
            );
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            // Add Image component for background
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            button.colors = colors;

            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRectTransform = textObj.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = new Vector2(10, 5);
            textRectTransform.offsetMax = new Vector2(-10, -5);

            // Add TextMeshProUGUI component
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = choice.text;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 24;

            // Add click listener
            DialogueChoice capturedChoice = choice;
            button.onClick.AddListener(() =>
            {
                SelectChoice(capturedChoice);
                ClearHardCodedButtons();
            });
        }
    }

    private bool CheckCondition(string conditionName)
    {
        // Implement your condition checking system here
        // For now, we'll just return true
        return true;
    }

    private void ClearDialogueOptions()
    {
        if (dialogueOptionsContainer == null)
            return;

        foreach (Transform child in dialogueOptionsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SelectOption(DialogueOption option)
    {
        OnOptionSelected.Invoke(option);

        // Check if this option reveals a bachelor preference
        if (option.isPreference && option.bachelor != null)
        {
            // Learn bachelor's preference
            option.bachelor.ArrayCheck(option.preferenceText);

            if (enableDebugLogs)
                Debug.Log(
                    $"Learned preference for {option.bachelor.m_name}: {option.preferenceText}"
                );
        }

        // Find the target node
        DialogueNode targetNode = FindNodeByID(option.targetNodeID);
        if (targetNode != null)
        {
            DisplayNode(targetNode);
        }
        else
        {
            Debug.LogWarning($"Could not find target node: {option.targetNodeID}");
            EndDialogue();
        }
    }

    public void SelectChoice(DialogueChoice choice)
    {
        // Handle preference learning if we've added that capability to DialogueChoice
        // This would require updating the DialogueChoice class similar to DialogueOption

        // Find the target node
        DialogueNode targetNode = FindNodeByID(choice.nextNodeID);
        if (targetNode != null)
        {
            DisplayNode(targetNode);
        }
        else
        {
            Debug.LogWarning($"Could not find target node: {choice.nextNodeID}");
            EndDialogue();
        }
    }

    private IEnumerator TypeSentence(string sentence, Action onComplete = null)
    {
        dialogueText.text = "";
        isTyping = true;
        string[] lines = sentence.Split(new[] { '\n' }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            foreach (char letter in line.ToCharArray())
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
            dialogueText.text += "\n";
        }

        isTyping = false;
        yield return new WaitForSeconds(timeBetweenMessages);

        if (onComplete != null)
            onComplete();
    }

    [ContextMenu("Start Test Dialogue")]
    private void StartDialogueFromInspector()
    {
        if (testDialogue != null)
        {
            StartDialogue(testDialogue);
        }
        else
        {
            Debug.LogWarning("No dialogue available to start.");
        }
    }
}
