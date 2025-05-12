using System.Collections.Generic;
using UnityEngine;

public class TestInteractible : MonoBehaviour, Interfaces.IInteractable
{
    [Header("Bachelor Settings")]
    [SerializeField]
    private string speakerName = "Jerry";

    [SerializeField]
    private Sprite speakerImage;

    [SerializeField]
    private BachelorSO jerryBachelor;

    [Header("Dialogue")]
    [SerializeField]
    private Dialogue testDialogue;

    private void Start()
    {
        // Create or ensure Jerry's bachelor data exists
        EnsureBachelorData();

        if (testDialogue == null)
        {
            CreateTestDialogue();
        }
    }

    private void CreateTestDialogue()
    {
        testDialogue = ScriptableObject.CreateInstance<Dialogue>();

        // Create dialogue nodes
        List<DialogueNode> nodes = new List<DialogueNode>();

        // Create first node (start)
        DialogueNode startNode = new DialogueNode();
        startNode.nodeID = "node1";
        startNode.text =
            "Hey there! Name's Jerry. I'm the new barista here. What can I get you today?";
        startNode.speakerName = speakerName;
        startNode.speakerImage = speakerImage;
        startNode.nextNodeID = "node2";
        nodes.Add(startNode);

        // Second node
        DialogueNode node2 = new DialogueNode();
        node2.nodeID = "node2";
        node2.text = "Actually, I'm on break right now. Perfect timing to chat!";
        node2.speakerName = speakerName;
        node2.speakerImage = speakerImage;
        node2.nextNodeID = "node3";
        nodes.Add(node2);

        // Main choice node
        DialogueNode node3 = new DialogueNode();
        node3.nodeID = "node3";
        node3.text = "So what would you like to talk about?";
        node3.speakerName = speakerName;
        node3.speakerImage = speakerImage;

        // Add dialogue options
        node3.options = new List<DialogueOption>
        {
            new DialogueOption
            {
                text = "What kind of music do you like?",
                targetNodeID = "music_node",
                isPreference = true,
                bachelor = jerryBachelor,
                preferenceText = "Jazz music"
            },
            new DialogueOption
            {
                text = "How do you feel about spicy food?",
                targetNodeID = "spicy_node",
                isPreference = true,
                bachelor = jerryBachelor,
                preferenceText = "Spicy food"
            },
            new DialogueOption
            {
                text = "Tell me about coffee brewing",
                targetNodeID = "coffee_node",
                isPreference = true,
                bachelor = jerryBachelor,
                preferenceText = "Coffee brewing"
            },
            new DialogueOption
            {
                text = "Do you like working in this busy café?",
                targetNodeID = "busy_node",
                isPreference = true,
                bachelor = jerryBachelor,
                preferenceText = "Crowded places"
            }
        };
        nodes.Add(node3);

        // Music response (like)
        DialogueNode musicNode = new DialogueNode();
        musicNode.nodeID = "music_node";
        musicNode.text =
            "Oh man, I LOVE jazz! The classics especially - Miles Davis, John Coltrane. I actually play saxophone in a little jazz combo on weekends. Nothing beats improvising with friends!";
        musicNode.speakerName = speakerName;
        musicNode.speakerImage = speakerImage;
        musicNode.nextNodeID = "return_node";
        nodes.Add(musicNode);

        // Spicy food response (dislike)
        DialogueNode spicyNode = new DialogueNode();
        spicyNode.nodeID = "spicy_node";
        spicyNode.text =
            "Ugh, can't stand spicy food! Last time I tried a jalapeño, I had to drink a gallon of milk. My friends always laugh, but my taste buds are just super sensitive.";
        spicyNode.speakerName = speakerName;
        spicyNode.speakerImage = speakerImage;
        spicyNode.nextNodeID = "return_node";
        nodes.Add(spicyNode);

        // Coffee brewing response (like)
        DialogueNode coffeeNode = new DialogueNode();
        coffeeNode.nodeID = "coffee_node";
        coffeeNode.text =
            "Coffee brewing is an art form! I've been experimenting with pour-over methods lately. The way the water temperature and grind size affect flavor profiles is fascinating. I could talk about this for hours!";
        coffeeNode.speakerName = speakerName;
        coffeeNode.speakerImage = speakerImage;
        coffeeNode.nextNodeID = "return_node";
        nodes.Add(coffeeNode);

        // Crowded places response (dislike)
        DialogueNode busyNode = new DialogueNode();
        busyNode.nodeID = "busy_node";
        busyNode.text =
            "Between you and me, the rush hour crowds are overwhelming sometimes. I love making coffee, but when it gets crowded? Feels like I can't breathe! I'm much better during the quiet morning shifts.";
        busyNode.speakerName = speakerName;
        busyNode.speakerImage = speakerImage;
        busyNode.nextNodeID = "return_node";
        nodes.Add(busyNode);

        // Return node
        DialogueNode returnNode = new DialogueNode();
        returnNode.nodeID = "return_node";
        returnNode.text =
            "Hey, thanks for chatting! My break's almost over. Anything else you wanted to know?";
        returnNode.speakerName = speakerName;
        returnNode.speakerImage = speakerImage;

        // Add options to continue talking or end conversation
        returnNode.options = new List<DialogueOption>
        {
            new DialogueOption
            {
                text = "Let's talk about something else",
                targetNodeID = "node3" // Loop back to main choices
            },
            new DialogueOption
            {
                text = "I should get going, thanks for chatting",
                targetNodeID = "goodbye_node" // Go to goodbye node
            }
        };
        nodes.Add(returnNode);

        // Create goodbye node
        DialogueNode goodbyeNode = new DialogueNode();
        goodbyeNode.nodeID = "goodbye_node";
        goodbyeNode.text =
            "No problem! It was great talking to you. Enjoy your coffee and hope to see you again soon!";
        goodbyeNode.speakerName = speakerName;
        goodbyeNode.speakerImage = speakerImage;
        goodbyeNode.isEndNode = true; // Mark as end node if you have such a flag, otherwise handle differently
        nodes.Add(goodbyeNode);

        testDialogue.nodes = nodes;
        testDialogue.startNode = startNode;
    }

    private void EnsureBachelorData()
    {
        // If no bachelor is assigned, create one at runtime
        if (jerryBachelor == null)
        {
            jerryBachelor = ScriptableObject.CreateInstance<BachelorSO>();

            // Set bachelor's basic info
            jerryBachelor.m_name = "Jerry";
            jerryBachelor.m_bachelorNumber = 1;

            // Initialize dialogue list
            jerryBachelor.m_dialogue = new List<string>();

            // Set up likes
            jerryBachelor.m_likes = new List<string> { "Jazz music", "Coffee brewing" };
            jerryBachelor.m_likesUnlocked = new List<bool> { false, false };

            // Set up dislikes
            jerryBachelor.m_dislikes = new List<string> { "Spicy food", "Crowded places" };
            jerryBachelor.m_dislikesUnlocked = new List<bool> { false, false };

            // Log creation of bachelor data
            Debug.Log("Created BachelorSO for Jerry programmatically");
        }
    }

    public void Interact()
    {
        // Ensure bachelor data is set up when interacting
        EnsureBachelorData();

        UIMananager.Instance.OpenCloseDialogueBox();
        DialogueManager.Instance.StartDialogue(testDialogue);
    }
}
