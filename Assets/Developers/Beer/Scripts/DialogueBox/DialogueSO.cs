using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue")]
public class Dialogue : ScriptableObject
{
    public DialogueNode startNode;
    public List<DialogueNode> nodes = new List<DialogueNode>();

    // Legacy support - will be removed after migration
    [HideInInspector]
    public string[] messages;

    // Helper method to convert any legacy choices to options
    public void ConvertLegacyChoices()
    {
        foreach (var node in nodes)
        {
            node.ConvertChoicesToOptions();
        }
    }
}

[Serializable]
public class DialogueNode
{
    public string nodeID;
    public string text;
    public string speakerName;
    public Sprite speakerImage;
    public AudioClip voiceClip;
    public string nextNodeID;
    public bool isEndNode; // Added missing property

    // Main options list (replaces choices)
    public List<DialogueOption> options = new List<DialogueOption>();

    // Legacy support - kept for serialization compatibility
    [HideInInspector]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    // Helper method to convert any legacy choices to options
    public void ConvertChoicesToOptions()
    {
        if (choices != null && choices.Count > 0)
        {
            foreach (var choice in choices)
            {
                // Only add if a similar option doesn't already exist
                if (
                    !options.Exists(o =>
                        o.text == choice.text && o.targetNodeID == choice.nextNodeID
                    )
                )
                {
                    options.Add(
                        new DialogueOption
                        {
                            text = choice.text,
                            targetNodeID = choice.nextNodeID,
                            hasCondition = false
                        }
                    );
                }
            }
        }
    }
}

[Serializable]
public class DialogueOption
{
    public string text;
    public string targetNodeID;
    public bool hasCondition;
    public string conditionName;

    // Add bachelor preference tracking
    public bool isPreference;
    public BachelorSO bachelor;
    public string preferenceText;
}

// Kept for backward compatibility with existing saved data
[Serializable]
public class DialogueChoice
{
    public string text;
    public string nextNodeID;
}
