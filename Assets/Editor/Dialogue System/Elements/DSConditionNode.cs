using System;
using System.Collections.Generic;
using System.Linq;
using DS.Data.Save;
using DS.Elements;
using DS.Enumerations;
using DS.Utilities;
using DS.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Elements
{
    /// <summary>
    /// Represents a conditional branching node in the dialogue system.
    /// This node evaluates a condition based on a property's value and provides different dialogue paths.
    /// </summary>
    public class DSConditionNode : NodeBase
    {
        // Properties that define the condition to be evaluated
        /// <summary>Property name that will be checked during condition evaluation</summary>
        public string propertyToCheck { get; set; } = "Love"; // Default to checking love value

        /// <summary>Comparison operator used to evaluate the condition</summary>
        public string comparisonType { get; set; } = ">="; // Default to greater than or equal

        /// <summary>The value to compare the property against</summary>
        public string comparisonValue { get; set; } = "0"; // Default value to compare against

        // Available comparison operators for the condition
        /// <summary>Array of available comparison operators for condition evaluation</summary>
        private readonly string[] comparisonTypes = { "==", "!=", ">", "<", ">=", "<=" };

        // Collection of properties that can be checked in conditions
        /// <summary>
        /// Static list of property names that can be used in conditions.
        /// This list can be modified at runtime using the Register/Unregister methods.
        /// </summary>
        private static List<string> availableProperties = new List<string>
        {
            "Love",
            "LikeDiscovered",
            "DislikeDiscovered",
            "NotebookEntry",
        };

        /// <summary>
        /// Adds a new property to the list of available properties for condition nodes.
        /// </summary>
        /// <param name="propertyName">The name of the property to register</param>
        public static void RegisterProperty(string propertyName)
        {
            if (!availableProperties.Contains(propertyName))
            {
                availableProperties.Add(propertyName);
            }
        }

        /// <summary>
        /// Removes a property from the list of available properties for condition nodes.
        /// </summary>
        /// <param name="propertyName">The name of the property to unregister</param>
        public static void UnregisterProperty(string propertyName)
        {
            availableProperties.Remove(propertyName);
        }

        /// <summary>
        /// Gets a copy of the current available properties list.
        /// </summary>
        /// <returns>A new list containing all available property names</returns>
        public static List<string> GetAvailableProperties()
        {
            return new List<string>(availableProperties);
        }

        /// <summary>
        /// Initializes the condition node with default values and creates an initial choice.
        /// </summary>
        /// <param name="nodeName">Name of the node</param>
        /// <param name="dsGraphView">Reference to the graph view</param>
        /// <param name="pos">Initial position of the node in the graph</param>
        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(nodeName, dsGraphView, pos);

            // Set node type to single choice for the condition node
            m_nodeDialogueType = DSDialogueType.MultipleChoice;

            // Create a default "Next Dialogue" choice path
            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                m_choiceTextData = "Next Dialogue",
            };

            m_nodeChoices.Add(choiceData);
        }

        /// <summary>
        /// Draws the UI components of the condition node in the graph editor.
        /// Includes property selection, comparison operator, and value input fields.
        /// </summary>
        public override void Draw()
        {
            // Call base.Draw() to set up the standard node structure
            base.Draw();

            // Set the node title
            m_nodeDialogueName = "Condition Node";

            // Add button to create additional condition paths
            Button addChoiceButton = DSElementUtility.CreateButton(
                "Add Condition Path",
                () =>
                {
                    // Create a new choice data object
                    DSChoiceSaveData choiceData = new DSChoiceSaveData()
                    {
                        m_choiceTextData = "Condition Path",
                    };

                    // Add the new choice to the node's choices
                    m_nodeChoices.Add(choiceData);

                    // Create a port for this new choice
                    Port choicePort = CreatePort(choiceData.m_choiceTextData);
                    choicePort.userData = choiceData;
                    outputContainer.Add(choicePort);
                }
            );

            // Create container for the condition UI elements
            var conditionContainer = new VisualElement();
            conditionContainer.AddToClassList("ds-node__custom-data-container");

            // Create dropdown for property selection
            var propertyField = new PopupField<string>(
                GetAvailableProperties(),
                propertyToCheck != null && GetAvailableProperties().Contains(propertyToCheck)
                    ? propertyToCheck
                    : GetAvailableProperties().FirstOrDefault() ?? "Love"
            );
            propertyField.label = "Property to Check";
            propertyField.RegisterValueChangedCallback(evt => propertyToCheck = evt.newValue);
            conditionContainer.Add(propertyField);

            // Create dropdown for comparison operator
            var comparisonField = new PopupField<string>(
                comparisonTypes.ToList(),
                comparisonType != null ? comparisonType : ">="
            );
            comparisonField.label = "Comparison";
            comparisonField.RegisterValueChangedCallback(evt => comparisonType = evt.newValue);
            conditionContainer.Add(comparisonField);

            // Create text field for the comparison value
            var valueCompareField = new TextField("Value") { value = comparisonValue };
            valueCompareField.RegisterValueChangedCallback(evt => comparisonValue = evt.newValue);
            conditionContainer.Add(valueCompareField);

            extensionContainer.Add(conditionContainer);

            // Create output ports for each choice path
            foreach (DSChoiceSaveData choice in m_nodeChoices)
            {
                // Create a port for this choice path
                Port choicePort = this.CreatePort(choice.m_choiceTextData);

                // Store the choice data in the port's userData for reference
                choicePort.userData = choice;

                outputContainer.Add(choicePort);
            }

            addChoiceButton.AddToClassList("ds-node__button");
            extensionContainer.Add(addChoiceButton);

            // Update the visual state of the node
            RefreshExpandedState();
        }

        /// <summary>
        /// Helper method to create a port with a specific name.
        /// Creates a new connection point for output paths in the condition node.
        /// </summary>
        /// <param name="portName">The name to display on the port</param>
        /// <returns>A configured Port object</returns>
        private Port CreatePort(string portName)
        {
            Port port = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(float)
            );
            port.portName = portName;
            return port;
        }
    }
}
