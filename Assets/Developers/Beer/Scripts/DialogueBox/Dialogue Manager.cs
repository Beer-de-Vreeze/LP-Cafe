using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events; 

public class DialogueManager : Singleton<DialogueManager>
{
    private Queue<string> sentences;

    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [SerializeField, Range(0.01f,1f), Tooltip("The lower the value, the faster the typing speed.")]
    private float typingSpeed = 0.1f;

    [SerializeField, Range(0.5f, 5f), Tooltip("The time between messages.")]
    private float timeBetweenMessages = 1.5f;

    [SerializeField, Tooltip("Dialogue to test the dialogue manager. Not USED IN THE GAME.")] 
    private Dialogue testDialogue;

    private Dialogue currentDialogue;

    public UnityEvent OnDialogueEnd = new UnityEvent();

    void Start()
    {
        sentences = new Queue<string>();
    }

    /// <summary>
    /// Starts the dialogue.
    /// needs a dialogue object to start the conversation.
    /// </summary>
    public void StartDialogue(Dialogue dialogue)
    {
        sentences.Clear();
        currentDialogue = dialogue;
        foreach (string sentence in dialogue.messages)
        {
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    [ContextMenu("Start Dialogue")]
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

    private void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            if (currentDialogue.messages.Length > 0)
            {
                Debug.Log("Displaying next message in the dialogue.");
                currentDialogue.messages = currentDialogue.messages.Skip(1).ToArray();
                if (currentDialogue.messages.Length > 0)
                {
                    StartDialogue(currentDialogue);
                }
                else
                {
                    EndDialogue();
                    return;
                }
            }
            else
            {
                EndDialogue();
                return;
            }
        }

        string sentence = sentences.Dequeue();
        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";
        string[] lines = sentence.ToUpper().Split(new[] { '\n' }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            foreach (char letter in line.ToCharArray())
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
            dialogueText.text += "\n";
        }
        yield return new WaitForSeconds(timeBetweenMessages);
        DisplayNextSentence();
    }

    private void EndDialogue()
    {
        OnDialogueEnd.Invoke();
        Debug.Log("End of conversation.");
    }
}
