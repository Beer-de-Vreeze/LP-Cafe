using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class TestInteractible : MonoBehaviour, Interfaces.IInteractable
{
    [Header("Test Dialogue")]
    [SerializeField]
    private Dialogue testDialogue;

    [SerializeField]
    private string testMessage = "This is a test message from the interactible object!";

    [SerializeField]
    private string speakerName = "Test Bachelor";

    [SerializeField]
    private Sprite speakerImage;

    private void Start()
    {
        if (testDialogue == null)
        {
            testDialogue = ScriptableObject.CreateInstance<Dialogue>();

            // Create 10 dialogue nodes
            List<DialogueNode> nodes = new List<DialogueNode>();

            // Create first node (start)
            DialogueNode startNode = new DialogueNode();
            startNode.nodeID = "node1";
            startNode.text = testMessage;
            startNode.speakerName = speakerName;
            startNode.speakerImage = speakerImage;
            startNode.nextNodeID = "node2";
            nodes.Add(startNode);
            // Create 8 middle nodes
            for (int i = 2; i <= 9; i++)
            {
                DialogueNode node = new DialogueNode();
                node.nodeID = "node" + i;
                node.text = $"This is test message #{i}. Continuing the dialogue...";
                node.speakerName = speakerName;
                node.speakerImage = speakerImage;
                node.nextNodeID = "node" + (i + 1);
                nodes.Add(node);
            }

            // Create final node (end)
            DialogueNode endNode = new DialogueNode();
            endNode.nodeID = "node10";
            endNode.text = "This is the end of the test dialogue.";
            endNode.speakerName = speakerName;
            endNode.speakerImage = speakerImage;
            endNode.nextNodeID = "node11"; // Point to the split node instead of ending
            nodes.Add(endNode);

            // Create a split node
            DialogueNode splitNode = new DialogueNode();
            splitNode.nodeID = "node11";
            splitNode.text = "What would you like to talk about next?";
            splitNode.speakerName = speakerName;
            splitNode.speakerImage = speakerImage;
            splitNode.choices = new List<DialogueChoice>
            {
                new DialogueChoice { text = "Tell me about your day", nextNodeID = "node12A" },
                new DialogueChoice
                {
                    text = "Let's talk about something else",
                    nextNodeID = "node12B"
                }
            };
            nodes.Add(splitNode);

            // Create branch A
            DialogueNode branchANode = new DialogueNode();
            branchANode.nodeID = "node12A";
            branchANode.text = "My day was quite interesting. I found a rare coffee bean!";
            branchANode.speakerName = speakerName;
            branchANode.speakerImage = speakerImage;
            nodes.Add(branchANode);

            // Create branch B
            DialogueNode branchBNode = new DialogueNode();
            branchBNode.nodeID = "node12B";
            branchBNode.text = "Sure, what else would you like to discuss?";
            branchBNode.speakerName = speakerName;
            branchBNode.speakerImage = speakerImage;
            nodes.Add(branchBNode);

            testDialogue.nodes = nodes;
            testDialogue.startNode = startNode;
        }
    }

    public void Interact()
    {
        UIMananager.Instance.OpenCloseDialogueBox();
        DialogueManager.Instance.StartDialogue(testDialogue);
    }
}
